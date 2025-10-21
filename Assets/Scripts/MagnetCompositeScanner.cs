using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum MagnetPolarity { Red = +1, Blue = -1 }

[DisallowMultipleComponent]
public class MagnetCompositeScanner : MonoBehaviour
{
    public MagnetPolarity polarity = MagnetPolarity.Red;
    public CompositeCollider2D composite;
    public Tilemap tilemap;
    [Tooltip("Strength coefficient per tile (base, multiplied by area/tile count)")]
    public float baseStrengthPerTile = 1f;

    [Serializable]
    public class Region
    {
        public MagnetPolarity polarity;
        public Vector2 centroid;
        public float areaWorld;
        public int tileCount;
        public float effectiveRadius;
        public float strength;      
    }
    public List<Region> regions { get; private set; } = new();
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (isActiveAndEnabled) Rebuild();
    }

    private void OnTransformParentChanged() { if (isActiveAndEnabled) Rebuild(); }
    #endif


    private void Reset()
    {
        composite = GetComponent<CompositeCollider2D>();
        tilemap   = GetComponent<Tilemap>();
    }

    private void OnEnable()
    {
        StartCoroutine(DelayedRebuild());
    }

    private IEnumerator DelayedRebuild()
    {
        yield return null;
        Rebuild();
    }


    public void Rebuild()
    {
        regions.Clear();

        if (!composite)
        {
            Debug.LogWarning($"No composite for {name}");
            return;
        }

        if (!tilemap)
        {
            Debug.LogWarning($"No tilemap for {name}");
            return;
        }
            

        int pathCount = composite.pathCount;
        if (pathCount <= 0) return;
        
        Vector3 cellSize = tilemap ? tilemap.cellSize : Vector3.one;
        Vector3 lossy    = composite.transform.lossyScale;
        float tileArea   = Mathf.Abs(cellSize.x * lossy.x * cellSize.y * lossy.y);
        if (tileArea <= Mathf.Epsilon) tileArea = 1f;

        Vector2[] buffer = new Vector2[256];

        for (int i = 0; i < pathCount; i++)
        {
            int n = composite.GetPathPointCount(i);
            if (n <= 2) continue;

            if (buffer.Length < n) buffer = new Vector2[n];
            int got = composite.GetPath(i, buffer);
            if (got != n) continue;

            // Copy of local vertices (for safety, to avoid modifying the buffer)
            Vector2[] pathLocal = new Vector2[n];
            Array.Copy(buffer, pathLocal, n);
            
            // Debug.Log($"Path {i}, {polarity.ToString()} array content: {string.Join(", ", pathLocal)}");
            
            if (!TryPolygonCentroid(pathLocal, n, worldSpace: true, composite.transform,
                                    out Vector2 centroidWorld, out float signedArea))
                continue;

            float absArea = Mathf.Abs(signedArea);
            if (absArea <= Mathf.Epsilon) continue;

            int   tiles     = Mathf.Max(1, Mathf.RoundToInt(absArea / tileArea));
            float strength  = baseStrengthPerTile * tiles;
            float effRadius = Mathf.Sqrt(absArea / Mathf.PI);
            
            // Debug.Log($"Path {i}, {polarity.ToString()}: {tiles} tiles, {absArea} area, {strength} strength, {effRadius} effective radius");

            regions.Add(new Region
            {
                polarity        = polarity,
                centroid        = centroidWorld,
                areaWorld       = absArea,
                tileCount       = tiles,
                effectiveRadius = effRadius,
                strength        = strength
            });
        }
        
        MagnetFieldManager.Instance?.Unregister(this);
        MagnetFieldManager.Instance?.Register(this);
    }


    private void OnDisable()
    {
        MagnetFieldManager.Instance?.Unregister(this);
    }

    // For visualizing centroids
    private void OnDrawGizmosSelected()
    {
        if (regions == null) return;
        Gizmos.color = polarity == MagnetPolarity.Red ? Color.red : Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        foreach (var r in regions)
        {
            Gizmos.DrawSphere(r.centroid, r.effectiveRadius);
        }
    }
    
    /// Centroid of an arbitrary (possibly concave) polygon.
    /// Returns true on success.
    /// centroid — in local or world space (worldSpace), signedArea — oriented area (world/local respectively).
    /// Formulas: A = 1/2 * Σ cross,  C = (1/(6A)) * Σ ( (xi+xj, yi+yj) * cross ).
    /// Here A2 = Σ cross = 2A  => divide by 3*A2 (equivalent to 1/(6A)).
    private static bool TryPolygonCentroid(
        Vector2[] pts, int n,
        bool worldSpace,
        Transform tr,
        out Vector2 centroid,
        out float signedArea)
    {
        centroid = default;
        signedArea = 0f;
        if (pts == null || n < 3) return false;

        double A2 = 0.0;   // Σ cross = 2A
        double Cx = 0.0, Cy = 0.0;

        for (int i = 0; i < n; i++)
        {
            Vector2 a = pts[i];
            Vector2 b = pts[(i + 1) % n];

            if (worldSpace && tr)
            {
                a = tr.TransformPoint(a);
                b = tr.TransformPoint(b);
            }

            double cross = (double)a.x * b.y - (double)b.x * a.y;
            A2 += cross;
            Cx += (a.x + b.x) * cross;
            Cy += (a.y + b.y) * cross;
        }

        // Degenerate case — area is nearly zero
        if (Math.Abs(A2) < 1e-12)
        {
            double sx = 0.0, sy = 0.0;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = pts[i];
                if (worldSpace && tr) p = tr.TransformPoint(p);
                sx += p.x; sy += p.y;
            }
            centroid = new Vector2((float)(sx / n), (float)(sy / n));
            signedArea = 0f;
            return true;
        }

        // A  = A2 / 2
        // Cx = (1/(6A)) Σ (xi + xj) * cross  =>  (1/(3*A2)) Σ (...)
        double k = 1.0 / (3.0 * A2);
        centroid = new Vector2((float)(Cx * k), (float)(Cy * k));
        signedArea = (float)(0.5 * A2);
        return true;
    }

}
