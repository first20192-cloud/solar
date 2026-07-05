using UnityEngine;
using UnityEditor;

// Editor-only utility that builds one planet-surface environment:
// procedural detail texture + Perlin terrain mesh + rocks + optional water
// plane + spawn point, then wires it into the planet's PlanetLandingZone.
// Mirrors how PlanetSurface_Mars was built by hand.
public static class SurfaceBuilder
{
    public static string Build(
        string planetName, Color sand, Color rock,
        float amp, int rockCount, bool bands, float bandFreq,
        float waterLevel, Color waterColor, Vector3 parkPos, float seed)
    {
        string envName = "PlanetSurface_" + planetName;
        var old = GameObject.Find(envName);
        if (old != null) Object.DestroyImmediate(old);

        // ---- detail texture ----
        int S = 1024;
        var tex = new Texture2D(S, S, TextureFormat.RGB24, true);
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                float m;
                if (bands)
                {
                    // gas-giant cloud bands: latitude stripes + swirl distortion
                    float swirl = (Mathf.PerlinNoise(u * 6f + seed, v * 6f + seed * 2f) - 0.5f) * 0.35f;
                    m = 0.5f + 0.5f * Mathf.Sin((v + swirl) * bandFreq * Mathf.PI * 2f);
                    m = Mathf.Lerp(m, Mathf.PerlinNoise(u * 30f + seed, v * 30f), 0.2f);
                }
                else
                {
                    float n1 = Mathf.PerlinNoise(u * 8f + seed, v * 8f + seed * 1.7f);
                    float n2 = Mathf.PerlinNoise(u * 34f + seed * 2f, v * 34f + seed);
                    float n3 = Mathf.PerlinNoise(u * 90f + seed * 3f, v * 90f + seed * 4f);
                    m = Mathf.Clamp01(0.55f + (n1 - 0.5f) * 0.5f + (n2 - 0.5f) * 0.35f + (n3 - 0.5f) * 0.22f);
                }
                px[y * S + x] = Color.Lerp(rock, sand, m);
            }
        }
        tex.SetPixels(px); tex.Apply(true);
        string texPath = "Assets/Textures/surface_" + planetName.ToLower() + ".png";
        System.IO.File.WriteAllBytes(
            Application.dataPath + "/../" + texPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);
        var detail = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        // ---- terrain mesh ----
        int N = 161; float SIZE = 5000f;
        var verts = new Vector3[N * N];
        var uvs = new Vector2[N * N];
        for (int z = 0; z < N; z++)
        {
            for (int x = 0; x < N; x++)
            {
                float u = (float)x / (N - 1), v = (float)z / (N - 1);
                float h = Mathf.PerlinNoise(u * 4f + seed, v * 4f + seed * 0.7f) * 0.55f
                        + Mathf.PerlinNoise(u * 11f + seed * 2f, v * 11f + seed) * 0.30f
                        + Mathf.PerlinNoise(u * 29f + seed * 3f, v * 29f + seed * 5f) * 0.15f;
                verts[z * N + x] = new Vector3((u - 0.5f) * SIZE, h * amp, (v - 0.5f) * SIZE);
                uvs[z * N + x] = new Vector2(u * 40f, v * 40f);
            }
        }
        var tris = new int[(N - 1) * (N - 1) * 6];
        int t = 0;
        for (int z = 0; z < N - 1; z++)
        {
            for (int x = 0; x < N - 1; x++)
            {
                int i0 = z * N + x;
                tris[t] = i0; tris[t + 1] = i0 + N; tris[t + 2] = i0 + 1;
                tris[t + 3] = i0 + 1; tris[t + 4] = i0 + N; tris[t + 5] = i0 + N + 1;
                t += 6;
            }
        }
        var mesh = new Mesh { vertices = verts, triangles = tris, uv = uvs };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, "Assets/Models/" + planetName + "TerrainMesh.asset");

        // ---- material ----
        var mat = new Material(Shader.Find("HDRP/Lit"));
        mat.SetTexture("_BaseColorMap", detail);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", bands ? 0.25f : 0.08f);
        AssetDatabase.CreateAsset(mat, "Assets/Materials/" + planetName + "Surface.mat");

        // ---- env root (built at Mars' park spot for raycast precision, moved after) ----
        var envRoot = new GameObject(envName);
        envRoot.transform.position = new Vector3(0f, -20000f, 0f);

        var terr = new GameObject("Terrain");
        terr.transform.SetParent(envRoot.transform, false);
        terr.AddComponent<MeshFilter>().sharedMesh = mesh;
        terr.AddComponent<MeshRenderer>().sharedMaterial = mat;
        terr.AddComponent<MeshCollider>().sharedMesh = mesh;
        Physics.SyncTransforms();

        // ---- water (optional) ----
        if (waterLevel > 0f)
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "Water";
            water.transform.SetParent(envRoot.transform, false);
            water.transform.localPosition = new Vector3(0f, waterLevel, 0f);
            water.transform.localScale = new Vector3(SIZE, 2f, SIZE);
            var wmat = new Material(Shader.Find("HDRP/Lit"));
            wmat.SetColor("_BaseColor", waterColor);
            wmat.SetFloat("_Smoothness", 0.92f);
            AssetDatabase.CreateAsset(wmat, "Assets/Materials/" + planetName + "Water.mat");
            water.GetComponent<Renderer>().sharedMaterial = wmat;
        }

        // ---- rocks ----
        var rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Asteroid.mat");
        for (int i = 0; i < rockCount; i++)
        {
            float r1 = Mathf.Abs(Mathf.Sin((i + seed) * 12.9898f) * 43758.5453f) % 1f;
            float r2 = Mathf.Abs(Mathf.Sin((i + seed) * 78.233f) * 12543.123f) % 1f;
            float r3 = Mathf.Abs(Mathf.Sin((i + seed) * 39.425f) * 76321.77f) % 1f;
            var rk = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rk.name = "Rock_" + i;
            rk.transform.SetParent(envRoot.transform, false);
            rk.transform.localPosition = new Vector3((r1 - 0.5f) * 3000f, amp + 100f, (r2 - 0.5f) * 3000f);
            rk.transform.localScale = new Vector3(4f + r3 * 14f, 3f + r2 * 8f, 4f + r1 * 12f);
            rk.transform.localRotation = Quaternion.Euler(r1 * 360f, r2 * 360f, r3 * 360f);
            if (rockMat != null) rk.GetComponent<Renderer>().sharedMaterial = rockMat;
            RaycastHit hit;
            if (Physics.Raycast(rk.transform.position, Vector3.down, out hit, 3000f))
                rk.transform.position = hit.point + Vector3.up * rk.transform.localScale.y * 0.25f;
        }

        // ---- spawn + park + wire ----
        var spawn = new GameObject("SurfaceSpawn");
        spawn.transform.SetParent(envRoot.transform, false);
        spawn.transform.localPosition = new Vector3(0f, 800f, 0f);

        envRoot.transform.position = parkPos;
        envRoot.SetActive(false);

        var planet = GameObject.Find(planetName);
        var zone = planet != null ? planet.GetComponent<PlanetLandingZone>() : null;
        if (zone == null) return planetName + ": ZONE NOT FOUND";
        zone.surfaceEnv = envRoot.transform;
        zone.surfaceSpawn = spawn.transform;
        EditorUtility.SetDirty(zone);
        return planetName + ": ok, landable=" + zone.Landable;
    }
}
