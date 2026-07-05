using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Flight-state prompts + altitude readout for the seamless-landing loop.
// Sits beside UIController on the GameManager; does not touch its logic.
public class FlightHUD : MonoBehaviour
{
    public FlightStateMachine fsm;
    public Text statusText;    // top-center prompt
    public Text altitudeText;  // bottom-left readout

    static readonly Dictionary<string, string> ThaiNames = new Dictionary<string, string>
    {
        { "Mercury", "ดาวพุธ" }, { "Venus", "ดาวศุกร์" }, { "Earth", "โลก" },
        { "Mars", "ดาวอังคาร" }, { "Jupiter", "ดาวพฤหัสบดี" }, { "Saturn", "ดาวเสาร์" },
        { "Uranus", "ดาวยูเรนัส" }, { "Neptune", "ดาวเนปจูน" }
    };

    Rigidbody shipBody;

    void Start()
    {
        if (fsm != null && fsm.ship != null) shipBody = fsm.ship.GetComponent<Rigidbody>();
        if (statusText != null) statusText.text = "";
        if (altitudeText != null) altitudeText.text = "";
    }

    void OnEnable() { FlightStateMachine.OnStateChanged += HandleState; }
    void OnDisable() { FlightStateMachine.OnStateChanged -= HandleState; }

    void HandleState(FlightStateMachine.FlightState s, PlanetLandingZone z)
    {
        if (statusText == null) return;
        string name = z != null && ThaiNames.ContainsKey(z.planetName) ? ThaiNames[z.planetName] : (z != null ? z.planetName : "");
        switch (s)
        {
            case FlightStateMachine.FlightState.Approach:
                statusText.text = z != null && z.Landable
                    ? "กำลังเข้าใกล้" + name + " — บินต่อเพื่อลงจอด"
                    : "กำลังเข้าใกล้" + name;
                break;
            case FlightStateMachine.FlightState.AtmosphericEntry:
                statusText.text = "⚠ กำลังเข้าสู่ชั้นบรรยากาศ" + name + "!";
                break;
            case FlightStateMachine.FlightState.SurfaceFlight:
                statusText.text = "บินเหนือพื้นผิว" + name + " — ลดความเร็วเพื่อลงจอด";
                break;
            case FlightStateMachine.FlightState.Landed:
                statusText.text = "🚀 ลงจอดบน" + name + "สำเร็จ! (Space ค้างเพื่อบินขึ้น)";
                break;
            case FlightStateMachine.FlightState.Ascending:
                statusText.text = "กำลังบินขึ้นสู่อวกาศ...";
                break;
            default:
                statusText.text = "";
                break;
        }
    }

    void Update()
    {
        if (altitudeText == null || fsm == null) return;
        bool onSurface = fsm.State == FlightStateMachine.FlightState.SurfaceFlight
                      || fsm.State == FlightStateMachine.FlightState.Landed
                      || fsm.State == FlightStateMachine.FlightState.Ascending;
        if (!onSurface) { if (altitudeText.text.Length > 0) altitudeText.text = ""; return; }

        float vSpeed = shipBody != null ? shipBody.linearVelocity.y : 0f;
        altitudeText.text = "ความสูง " + Mathf.RoundToInt(fsm.SurfaceAltitude) + " m   " +
                            (vSpeed >= 0 ? "▲ " : "▼ ") + Mathf.Abs(vSpeed).ToString("F0") + " m/s";
    }
}
