using UnityEngine;

// Drives an engine AudioSource from pilot throttle: a low idle hum that swells
// louder and higher-pitched as the ship thrusts, strafes, climbs or boosts.
// Reads the same legacy Input axes the SpaceshipController uses, so it stays in
// sync with actual piloting without a hard reference.
[RequireComponent(typeof(AudioSource))]
public class EngineAudio : MonoBehaviour
{
    [Header("Volume")]
    public float idleVolume = 0.12f;
    public float maxVolume = 0.5f;

    [Header("Pitch")]
    public float idlePitch = 0.75f;
    public float maxPitch = 1.5f;
    public float boostPitch = 1.8f;

    [Tooltip("How fast the engine spools up/down toward the target throttle.")]
    public float responsiveness = 4f;

    AudioSource src;
    float throttle;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.loop = true;
        src.spatialBlend = 0f;
        src.playOnAwake = true;
        if (!src.isPlaying) src.Play();
    }

    void Update()
    {
        float f = Mathf.Abs(Input.GetAxisRaw("Vertical"));
        float s = Mathf.Abs(Input.GetAxisRaw("Horizontal"));
        float v = 0f;
        if (Input.GetKey(KeyCode.Space)) v = 1f;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) v = 1f;
        bool boosting = Input.GetKey(KeyCode.LeftShift) && f > 0.1f;

        float target = Mathf.Clamp01(Mathf.Max(f, s * 0.6f, v * 0.7f));
        throttle = Mathf.MoveTowards(throttle, target, responsiveness * Time.deltaTime);

        src.volume = Mathf.Lerp(idleVolume, maxVolume, throttle);
        float topPitch = boosting ? boostPitch : maxPitch;
        src.pitch = Mathf.Lerp(idlePitch, topPitch, throttle);
    }
}
