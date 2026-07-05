using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Masks the LOD swap: an additive plasma shell around the ship ramps up while
// punching through the atmosphere (both directions), camera shakes, and a
// planet-tinted distance-fog volume fades in on the surface side. Runs after
// CameraFollow (execution order) so the shake offset survives the frame.
[DefaultExecutionOrder(100)]
public class AtmosphereEntryFX : MonoBehaviour
{
    [Header("Refs")]
    public FlightStateMachine fsm;
    public Transform ship;
    public Transform cam;
    public Renderer plasmaShell;
    public Volume surfaceVolume;
    public GameObject orbitLines; // hidden while inside an atmosphere / on a surface

    [Header("Tuning")]
    public Color plasmaColor = new Color(1f, 0.42f, 0.1f);
    public float plasmaIntensity = 1200f; // nits at full glow (exposure is fixed 12.5 EV)
    public float glowRise = 2.4f;         // per-second toward target
    public float glowFall = 0.5f;
    public float shakeAmplitude = 0.45f;
    public float fogFade = 0.8f;

    float glow;
    float fogWeight;
    MaterialPropertyBlock mpb;

    void OnEnable() { FlightStateMachine.OnStateChanged += HandleState; }
    void OnDisable() { FlightStateMachine.OnStateChanged -= HandleState; }

    void HandleState(FlightStateMachine.FlightState s, PlanetLandingZone z)
    {
        // Force full glow on the frames where the actual teleport happens.
        if (s == FlightStateMachine.FlightState.SurfaceFlight) glow = 1f;
        if (s == FlightStateMachine.FlightState.Space && glow > 0.25f) glow = 1f;
        if (z != null) TintFog(z.surfaceTint);

        if (orbitLines != null)
            orbitLines.SetActive(s == FlightStateMachine.FlightState.Space
                              || s == FlightStateMachine.FlightState.Approach);
    }

    void TintFog(Color tint)
    {
        if (surfaceVolume == null || surfaceVolume.profile == null) return;
        // Defensive: this project has a history of volume-profile overrides
        // going missing — recreate the Fog on the runtime instance if needed.
        Fog fog;
        if (!surfaceVolume.profile.TryGet(out fog))
            fog = surfaceVolume.profile.Add<Fog>(false);
        fog.enabled.Override(true);
        fog.meanFreePath.Override(4200f);
        fog.colorMode.Override(FogColorMode.ConstantColor);
        fog.color.Override(tint * 0.45f);
    }

    void LateUpdate()
    {
        if (fsm == null || ship == null) return;
        var st = fsm.State;

        float target = 0f;
        if (st == FlightStateMachine.FlightState.AtmosphericEntry && fsm.Zone != null)
            target = fsm.Zone.EntryDepth(ship.position);
        else if (st == FlightStateMachine.FlightState.Ascending)
            target = Mathf.Pow(Mathf.Clamp01(fsm.SurfaceAltitude / fsm.surfaceCeiling), 6f);

        float rate = target > glow ? glowRise : glowFall;
        glow = Mathf.MoveTowards(glow, target, rate * Time.deltaTime);

        // plasma shell
        if (plasmaShell != null)
        {
            bool on = glow > 0.02f;
            if (plasmaShell.enabled != on) plasmaShell.enabled = on;
            if (on)
            {
                if (mpb == null) mpb = new MaterialPropertyBlock();
                plasmaShell.GetPropertyBlock(mpb);
                mpb.SetColor("_UnlitColor", plasmaColor * (plasmaIntensity * glow * glow));
                plasmaShell.SetPropertyBlock(mpb);
            }
        }

        // surface fog
        bool onSurface = st == FlightStateMachine.FlightState.SurfaceFlight
                      || st == FlightStateMachine.FlightState.Landed
                      || st == FlightStateMachine.FlightState.Ascending;
        fogWeight = Mathf.MoveTowards(fogWeight, onSurface ? 1f : 0f, fogFade * Time.deltaTime);
        if (surfaceVolume != null) surfaceVolume.weight = fogWeight;

        // camera shake
        if (cam != null && glow > 0.02f)
        {
            float t = Time.time * 11f;
            Vector3 jitter = new Vector3(
                Mathf.PerlinNoise(t, 0.3f) - 0.5f,
                Mathf.PerlinNoise(0.7f, t) - 0.5f,
                Mathf.PerlinNoise(t, t) - 0.5f) * (2f * shakeAmplitude * glow);
            cam.position += jitter;
        }
    }
}
