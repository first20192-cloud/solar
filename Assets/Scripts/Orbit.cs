using UnityEngine;

// Revolves this object around a center (the sun by default) and optionally spins it
// on its own axis. It keeps a fixed offset from the (possibly moving) center each
// frame, so a moon can orbit a planet that is itself orbiting the sun — no
// re-parenting required.
public class Orbit : MonoBehaviour
{
    [Tooltip("What to revolve around. Leave empty to orbit the world origin (the sun).")]
    public Transform center;

    [Tooltip("Revolution speed around the center, degrees per second.")]
    public float orbitSpeed = 5f;

    [Tooltip("Self-rotation (day) speed, degrees per second.")]
    public float spinSpeed = 15f;

    Vector3 offset;

    void Start()
    {
        Vector3 c = center != null ? center.position : Vector3.zero;
        offset = transform.position - c;
    }

    void Update()
    {
        Vector3 c = center != null ? center.position : Vector3.zero;
        offset = Quaternion.AngleAxis(orbitSpeed * Time.deltaTime, Vector3.up) * offset;
        transform.position = c + offset;
        if (spinSpeed != 0f) transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }
}
