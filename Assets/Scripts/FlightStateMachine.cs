using System;
using UnityEngine;

// Drives the seamless landing loop:
//   Space -> Approach -> AtmosphericEntry -> (LOD swap) -> SurfaceFlight -> Landed
//   Landed/SurfaceFlight -> Ascending -> (swap back) -> Space
// The "swap" teleports the ship (and snaps the camera) between the tiny space
// planet and a big-scale surface environment parked far below the system —
// masked by AtmosphereEntryFX so the player never sees the cut.
public class FlightStateMachine : MonoBehaviour
{
    public enum FlightState { Space, Approach, AtmosphericEntry, SurfaceFlight, Landed, Ascending }

    [Header("Refs")]
    public Transform ship;
    public CameraFollow cameraFollow;

    [Header("Surface tuning")]
    public float surfaceCeiling = 1000f;  // altitude that returns the ship to space
    public float landedSpeed = 5f;        // max speed to count as touched down
    public float landedAltitude = 8f;     // max hull altitude to count as touched down
    public float takeoffAltitude = 20f;   // altitude that lifts Landed back to flight
    public float spawnDescentSpeed = 60f; // downward speed given right after the swap

    public FlightState State { get; private set; }
    public PlanetLandingZone Zone { get; private set; }
    public float SurfaceAltitude { get; private set; }

    public static event Action<FlightState, PlanetLandingZone> OnStateChanged;

    Rigidbody rb;
    PlanetLandingZone[] zones;
    Orbit frozenOrbit;
    Vector3 returnDirLocal;   // exit direction stored relative to the planet (origin-shift safe)

    void Start()
    {
        rb = ship.GetComponent<Rigidbody>();
        zones = FindObjectsByType<PlanetLandingZone>(FindObjectsSortMode.None);
        SetState(FlightState.Space, null);
    }

    void FixedUpdate()
    {
        switch (State)
        {
            case FlightState.Space: TickSpace(); break;
            case FlightState.Approach: TickApproach(); break;
            case FlightState.AtmosphericEntry: TickEntry(); break;
            case FlightState.SurfaceFlight: TickSurface(); break;
            case FlightState.Landed: TickLanded(); break;
            case FlightState.Ascending: TickAscending(); break;
        }
    }

    // ---------- space side ----------

    void TickSpace()
    {
        PlanetLandingZone nearest = null;
        float best = float.MaxValue;
        for (int i = 0; i < zones.Length; i++)
        {
            float d = zones[i].DistanceTo(ship.position);
            if (d < zones[i].EntryRadius && d < best) { best = d; nearest = zones[i]; }
        }
        if (nearest != null) SetState(FlightState.Approach, nearest);
    }

    void TickApproach()
    {
        float d = Zone.DistanceTo(ship.position);
        if (d > Zone.EntryRadius * 1.15f) { SetState(FlightState.Space, null); return; }
        if (Zone.Landable && d < Zone.AtmosphereRadius) SetState(FlightState.AtmosphericEntry, Zone);
    }

    void TickEntry()
    {
        float d = Zone.DistanceTo(ship.position);
        if (d > Zone.AtmosphereRadius * 1.2f) { SetState(FlightState.Approach, Zone); return; }
        if (d < Zone.SwapRadius) SwapToSurface();
    }

    // ---------- surface side ----------

    void TickSurface()
    {
        ApplySurfaceGravity();
        UpdateSurfaceAltitude();
        if (SurfaceAltitude > surfaceCeiling) { SwapToSpace(); return; }
        if (SurfaceAltitude < landedAltitude && rb.linearVelocity.magnitude < landedSpeed)
            SetState(FlightState.Landed, Zone);
    }

    void TickLanded()
    {
        ApplySurfaceGravity();
        UpdateSurfaceAltitude();
        if (SurfaceAltitude > takeoffAltitude) SetState(FlightState.Ascending, Zone);
    }

    void TickAscending()
    {
        ApplySurfaceGravity();
        UpdateSurfaceAltitude();
        if (SurfaceAltitude > surfaceCeiling) SwapToSpace();
        else if (SurfaceAltitude < landedAltitude && rb.linearVelocity.magnitude < landedSpeed)
            SetState(FlightState.Landed, Zone);
    }

    void ApplySurfaceGravity()
    {
        rb.AddForce(Vector3.down * Zone.surfaceGravity, ForceMode.Acceleration);
    }

    void UpdateSurfaceAltitude()
    {
        RaycastHit hit;
        if (Physics.Raycast(ship.position, Vector3.down, out hit, 5000f))
            SurfaceAltitude = hit.distance;
        else
            SurfaceAltitude = ship.position.y - (Zone.surfaceSpawn != null ? Zone.surfaceSpawn.parent.position.y : 0f);
    }

    // ---------- swaps ----------

    void SwapToSurface()
    {
        // Remember which way to put the ship back, relative to the moving planet.
        returnDirLocal = (ship.position - Zone.Center).normalized;

        frozenOrbit = Zone.GetComponent<Orbit>();
        if (frozenOrbit != null) frozenOrbit.enabled = false;

        Zone.surfaceEnv.gameObject.SetActive(true);
        ship.position = Zone.surfaceSpawn.position;
        ship.rotation = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(ship.forward, Vector3.up).normalized + Vector3.down * 0.35f);
        rb.linearVelocity = ship.forward * spawnDescentSpeed;
        Physics.SyncTransforms();
        if (cameraFollow != null) cameraFollow.SnapToTarget();

        SurfaceAltitude = surfaceCeiling * 0.9f;
        SetState(FlightState.SurfaceFlight, Zone);
    }

    void SwapToSpace()
    {
        Vector3 exitPos = Zone.Center + returnDirLocal * (Zone.AtmosphereRadius * 1.25f);
        ship.position = exitPos;
        ship.rotation = Quaternion.LookRotation(returnDirLocal);
        rb.linearVelocity = returnDirLocal * Mathf.Min(rb.linearVelocity.magnitude, 80f);
        Physics.SyncTransforms();
        if (cameraFollow != null) cameraFollow.SnapToTarget();

        Zone.surfaceEnv.gameObject.SetActive(false);
        if (frozenOrbit != null) { frozenOrbit.enabled = true; frozenOrbit = null; }

        SetState(FlightState.Space, null);
    }

    void SetState(FlightState next, PlanetLandingZone zone)
    {
        State = next;
        Zone = zone;
        if (OnStateChanged != null) OnStateChanged(next, zone);
    }
}
