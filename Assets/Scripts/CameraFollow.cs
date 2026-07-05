using UnityEngine;

// Smooth chase camera for the ship. Runs in LateUpdate so it reads the ship's
// interpolated transform after physics. Position uses SmoothDamp and rotation an
// exponential slerp — both framerate-independent, so an uneven frame rate no longer
// makes the follow (and therefore the ship in view) shake.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 4f;
    public float height = 1.4f;
    public float lookAhead = 1.5f;

    [Tooltip("Seconds for the camera position to catch up. Smaller = tighter follow.")]
    public float positionSmoothTime = 0.08f;
    [Tooltip("Rotation responsiveness. Higher = snappier aim.")]
    public float rotationDamping = 8f;

    Vector3 followVelocity;   // SmoothDamp state — must persist between frames

    void Start()
    {
        if (target != null) SnapToTarget();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position - target.forward * distance + target.up * height;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref followVelocity, positionSmoothTime);

        Vector3 lookPoint = target.position + target.forward * lookAhead;
        Quaternion desiredRot = Quaternion.LookRotation(lookPoint - transform.position, target.up);
        float rotT = 1f - Mathf.Exp(-rotationDamping * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotT);
    }

    // Instantly place the camera behind the target with no smoothing (used after
    // the landing teleports so the view doesn't smear across the swap).
    public void SnapToTarget()
    {
        followVelocity = Vector3.zero;
        transform.position = target.position - target.forward * distance + target.up * height;
        transform.rotation = Quaternion.LookRotation(
            (target.position + target.forward * lookAhead) - transform.position, target.up);
    }
}
