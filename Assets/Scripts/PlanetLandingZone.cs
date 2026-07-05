using UnityEngine;

// Per-planet landing configuration. Lives on the planet object itself (next to
// its Orbit). All radii are world-space distances from the planet center.
// Planets here are tiny (Mars diameter ~4.3u) while the ship is ~7u long, so
// every radius is clamped to a minimum: the atmosphere-entry FX must cover the
// screen BEFORE the ship gets close enough to dwarf the planet visually.
public class PlanetLandingZone : MonoBehaviour
{
    [Tooltip("Display name; must match the SolarSystemManager facts key.")]
    public string planetName;

    [Tooltip("Visual radius of the planet mesh, world units.")]
    public float surfaceRadius = 2f;

    [Header("Trigger radii (auto-clamped for tiny planets)")]
    public float entryFactor = 3f;      // approach begins
    public float atmosphereFactor = 1.9f; // entry FX begins
    public float swapFactor = 1.3f;     // LOD swap to surface happens
    public float minEntryRadius = 55f;
    public float minAtmosphereRadius = 32f;
    public float minSwapRadius = 18f;

    [Header("Surface environment (optional — planet not landable if null)")]
    public Transform surfaceEnv;        // disabled root of the big-scale terrain
    public Transform surfaceSpawn;      // where the ship appears after the swap
    public float surfaceGravity = 14f;
    public Color surfaceTint = new Color(0.7f, 0.45f, 0.3f);

    public float EntryRadius { get { return Mathf.Max(surfaceRadius * entryFactor, minEntryRadius); } }
    public float AtmosphereRadius { get { return Mathf.Max(surfaceRadius * atmosphereFactor, minAtmosphereRadius); } }
    public float SwapRadius { get { return Mathf.Max(surfaceRadius * swapFactor, minSwapRadius); } }
    public bool Landable { get { return surfaceEnv != null && surfaceSpawn != null; } }

    public Vector3 Center { get { return transform.position; } }

    public float DistanceTo(Vector3 pos) { return (pos - Center).magnitude; }

    // 0 at atmosphere edge -> 1 at swap radius; drives FX intensity.
    public float EntryDepth(Vector3 pos)
    {
        float d = DistanceTo(pos);
        return Mathf.Clamp01(Mathf.InverseLerp(AtmosphereRadius, SwapRadius, d));
    }
}
