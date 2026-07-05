using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps the ship near the world origin to avoid float-precision jitter at
// Neptune-scale distances (~4000+ units). When the focus drifts past the
// threshold, every scene root (except UI) is shifted back by the focus
// position. Orbit.cs must use a real `center` Transform (not null) so orbits
// follow the shift; orbit-line LineRenderers must be local-space.
public class FloatingOrigin : MonoBehaviour
{
    [Tooltip("Object to keep near the origin — the player ship.")]
    public Transform focus;

    [Tooltip("Distance from origin that triggers a recenter.")]
    public float threshold = 2000f;

    // Accumulated shift: trueWorldPos = transform.position + TotalOffset.
    public static Vector3 TotalOffset;

    // Fired after each shift with the delta applied to all roots (-shift).
    public static event Action<Vector3> OnOriginShifted;

    void LateUpdate()
    {
        if (focus == null) return;
        Vector3 p = focus.position;
        if (p.sqrMagnitude < threshold * threshold) return;
        Shift(p);
    }

    void Shift(Vector3 by)
    {
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                // Screen-space canvases and other UI roots are managed by Unity.
                if (roots[i].GetComponent<RectTransform>() != null) continue;
                roots[i].transform.position -= by;
            }
        }

        TotalOffset += by;
        Physics.SyncTransforms();
        if (OnOriginShifted != null) OnOriginShifted(-by);
    }
}
