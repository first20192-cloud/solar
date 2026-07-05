using System.Collections.Generic;
using UnityEngine;

public class SolarSystemManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform planetsParent;
    public UIController ui;

    [Header("Settings")]
    [Tooltip("How close (world units) the ship must get to a planet's surface to detect it.")]
    public float detectRange = 60f;

    readonly HashSet<string> visited = new HashSet<string>();
    Dictionary<string, string> facts;
    int totalPlanets;
    string currentNearby = "";
    bool won;

    void Awake()
    {
        facts = new Dictionary<string, string>
        {
            { "Mercury", "ดาวพุธ\nเล็กที่สุดในระบบสุริยะ\nเส้นผ่านศูนย์กลาง 4,879 กม.\nระยะ 57.9 ล้าน กม." },
            { "Venus",   "ดาวศุกร์\nร้อนที่สุด 462°C บรรยากาศหนา\nเส้นผ่านศูนย์กลาง 12,104 กม.\nระยะ 108.2 ล้าน กม." },
            { "Earth",   "โลก\nมีสิ่งมีชีวิตและน้ำเหลว\nเส้นผ่านศูนย์กลาง 12,742 กม.\nระยะ 149.6 ล้าน กม." },
            { "Mars",    "ดาวอังคาร\nดาวแดง มีภูเขาสูงสุดในระบบ\nเส้นผ่านศูนย์กลาง 6,779 กม.\nระยะ 227.9 ล้าน กม." },
            { "Jupiter", "ดาวพฤหัส\nใหญ่ที่สุด มีพายุจุดแดงใหญ่\nเส้นผ่านศูนย์กลาง 139,820 กม.\nระยะ 778.5 ล้าน กม." },
            { "Saturn",  "ดาวเสาร์\nมีวงแหวนสวยงาม\nเส้นผ่านศูนย์กลาง 116,460 กม.\nระยะ 1,434 ล้าน กม." },
            { "Uranus",  "ดาวยูเรนัส\nหมุนตะแคงข้าง สีฟ้าเขียว\nเส้นผ่านศูนย์กลาง 50,724 กม.\nระยะ 2,871 ล้าน กม." },
            { "Neptune", "ดาวเนปจูน\nไกลสุด ลมแรงสุดในระบบ\nเส้นผ่านศูนย์กลาง 49,244 กม.\nระยะ 4,495 ล้าน กม." },
        };
        totalPlanets = facts.Count;
    }

    void Start()
    {
        if (ui) ui.SetProgress(0, totalPlanets);
    }

    void Update()
    {
        if (player == null || planetsParent == null) return;

        // Find the nearest KNOWN planet (by name) whose surface is within detectRange.
        // Uses real renderer bounds so it works for imported models of any scale
        // (not just unit-sphere primitives), and ignores the sun / moon / ship / VFX.
        string nearest = null;
        float nearestSq = detectRange * detectRange;
        foreach (Transform planet in planetsParent)
        {
            if (!facts.ContainsKey(planet.name)) continue;
            Bounds b;
            if (!TryGetBounds(planet, out b)) continue;
            float sq = b.SqrDistance(player.position);
            if (sq <= nearestSq)
            {
                nearestSq = sq;
                nearest = planet.name;
            }
        }

        if (nearest != null)
        {
            if (nearest != currentNearby)
            {
                currentNearby = nearest;
                if (ui != null) ui.ShowPlanetInfo(facts[nearest]);
                if (!visited.Contains(nearest))
                {
                    visited.Add(nearest);
                    if (visited.Count >= totalPlanets)
                    {
                        if (!won) { won = true; if (ui) ui.ShowWin(); }
                    }
                    else if (ui) ui.SetProgress(visited.Count, totalPlanets);
                }
            }
        }
        else
        {
            currentNearby = "";
        }
    }

    // Combined world-space bounds of all renderers under a planet (surface + rings + clouds).
    bool TryGetBounds(Transform t, out Bounds b)
    {
        var rends = t.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) { b = new Bounds(t.position, Vector3.zero); return false; }
        b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return true;
    }
}
