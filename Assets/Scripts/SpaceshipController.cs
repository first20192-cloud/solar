using UnityEngine;

// Newtonian spaceship flight. Thrust values are ACCELERATIONS (ForceMode.Acceleration),
// so the ship builds momentum and — in open space — coasts freely when you let go of
// the throttle; to slow down you fire reverse thrust (S) rather than relying on drag.
// Inside a planet's atmosphere the state machine flips us to a high drag so the ship
// handles like an aircraft and can settle gently enough to land.
[RequireComponent(typeof(Rigidbody))]
public class SpaceshipController : MonoBehaviour
{
    [Header("Thrust (acceleration, u/s^2)")]
    public float thrust = 70f;
    public float strafe = 42f;
    public float vertical = 42f;
    [Tooltip("Multiplies acceleration while boosting (Shift) — reaches top speed sooner and gives punchier reverse braking.")]
    public float boostMultiplier = 4.5f;

    [Header("Speed")]
    [Tooltip("Absolute top speed (u/s). Below this the ship coasts freely — this only clamps runaway, it never auto-brakes.")]
    public float maxSpeed = 500f;

    [Header("Drag (Newtonian)")]
    [Tooltip("Linear damping in open space — keep near zero so the ship carries real momentum.")]
    public float spaceDamping = 0.03f;
    [Tooltip("Linear damping inside an atmosphere / on a surface — air resistance for control + landing.")]
    public float atmosphereDamping = 1.3f;

    [Header("Look")]
    public float mouseSensitivity = 3f;
    public float rollSpeed = 70f;

    Rigidbody rb;
    FlightStateMachine flight;
    float yaw, pitch, roll;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = spaceDamping;
        rb.angularDamping = 5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        flight = FindFirstObjectByType<FlightStateMachine>();

        Vector3 e = transform.eulerAngles;
        yaw = e.y; pitch = e.x; roll = e.z;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked)
                ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -88f, 88f);

        float rollInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rollInput += 1f;
        if (Input.GetKey(KeyCode.E)) rollInput -= 1f;
        roll += rollInput * rollSpeed * Time.deltaTime;

        transform.rotation = Quaternion.Euler(pitch, yaw, roll);
    }

    void FixedUpdate()
    {
        // Aerodynamic drag inside an atmosphere, near-frictionless momentum in open space.
        rb.linearDamping = InAtmosphere() ? atmosphereDamping : spaceDamping;

        float f = Input.GetAxisRaw("Vertical");
        float s = Input.GetAxisRaw("Horizontal");
        float v = 0f;
        if (Input.GetKey(KeyCode.Space)) v += 1f;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) v -= 1f;
        float boost = Input.GetKey(KeyCode.LeftShift) ? boostMultiplier : 1f;

        Vector3 force = transform.forward * f * thrust * boost
                      + transform.right * s * strafe
                      + transform.up * v * vertical;
        rb.AddForce(force, ForceMode.Acceleration);

        // Single absolute ceiling — clamps only runaway speed, never brakes a coasting ship.
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }

    bool InAtmosphere()
    {
        if (flight == null) return false;
        switch (flight.State)
        {
            case FlightStateMachine.FlightState.AtmosphericEntry:
            case FlightStateMachine.FlightState.SurfaceFlight:
            case FlightStateMachine.FlightState.Landed:
            case FlightStateMachine.FlightState.Ascending:
                return true;
            default:
                return false;
        }
    }
}
