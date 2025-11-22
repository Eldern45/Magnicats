using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum MagnetPolarity { Red = +1, Blue = -1 }

public enum MagnetFieldMode
{
    Radial,
    FixedDirectional
}

[DisallowMultipleComponent]
public class MagnetFieldSource : MonoBehaviour
{
    public MagnetPolarity polarity = MagnetPolarity.Red;
    public CompositeCollider2D composite;
    public Tilemap tilemap;
    [Tooltip("Strength coefficient per tile (base, multiplied by area/tile count)")]
    public float baseStrengthPerTile = 1f;

    [Header("Field Mode")]
    [Tooltip("Field type: Radial — current radial behavior; FixedDirectional — fixed force direction")]
    public MagnetFieldMode fieldMode = MagnetFieldMode.Radial;

    [Tooltip("Force direction for FixedDirectional mode")] 
    public Vector2 fixedDirection = Vector2.right;

    [Tooltip("Interpret fixedDirection in the source's local space")] 
    public bool directionsInLocalSpace = true;

    [Header("Raycast")]
    [Tooltip("Physics2D layer mask used for the gating raycast (works in FixedDirectional mode)")]
    public LayerMask raycastLayerMask = Physics2D.DefaultRaycastLayers;

    [Header("Field Physics")]
    [Tooltip("Additional strength scale for this magnet (replaces globalK)")]
    public float strengthScale = 20f;

    [Tooltip("Distance falloff exponent (1 = 1/r, 2 = 1/r^2, etc.)")] 
    public float falloffPower = 2f;

    [Tooltip("Maximum influence distance (from region centroid). 0 = no limit")] 
    public float maxInfluenceRadius = 12f;

    [Tooltip("Minimum distance in the formula to avoid singularity")] 
    public float minDistance = 0.25f;

    [Serializable]
    public class Region
    {
        public MagnetPolarity polarity;
        public Vector2 centroid;
        public float areaWorld;
        public int tileCount;
        public float effectiveRadius;
        public float strength;
        // Polygon path vertices in WORLD space (for distance-to-boundary computation)
        public Vector2[] worldPath;
    }
    public List<Region> Regions { get; private set; } = new();
    
    private void OnValidate()
    {
        if (isActiveAndEnabled) 
            Rebuild();

        // Normalize direction (and guard against zero vector)
        if (fixedDirection.sqrMagnitude < 1e-6f)
            fixedDirection = Vector2.right;
        else
            fixedDirection = fixedDirection.normalized;
    }

    private void OnTransformParentChanged()
    {
        if (isActiveAndEnabled) 
            Rebuild();
    }


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
        Regions.Clear();

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

            // Also prepare polygon vertices in world space for distance-to-boundary calculation
            Vector2[] pathWorld = new Vector2[n];
            for (int k = 0; k < n; k++)
                pathWorld[k] = composite.transform.TransformPoint(pathLocal[k]);

            Regions.Add(new Region
            {
                polarity        = polarity,
                centroid        = centroidWorld,
                areaWorld       = absArea,
                tileCount       = tiles,
                effectiveRadius = effRadius,
                strength        = strength,
                worldPath       = pathWorld
            });
        }
        // Debug.Log($"effRadiuses of each magnet: {string.Join(", ", Regions.ConvertAll(r => r.effectiveRadius.ToString("F2")))}, strengthScale: {strengthScale}");
    }


    private void OnDisable()
    {
        StopAllCoroutines();
    }
    
    /// Calculates this scanner's magnetic force contribution at position 'pos',
    /// taking into account the hero polarity (+1/-1) and local scanner settings.
    public Vector2 GetForceAt(Vector2 pos, int heroPolarity)
    {
        Vector2 sum = Vector2.zero;
        if (Regions == null || Regions.Count == 0 || heroPolarity == 0) return sum;

        foreach (var r in Regions)
        {
            if (fieldMode == MagnetFieldMode.Radial)
            {
                
                Vector2 fromRegion = pos - r.centroid;
                float distToCentroid = fromRegion.magnitude;

                // normalize the direction (away from the region)
                Vector2 dirN = distToCentroid > Mathf.Epsilon ? (fromRegion / distToCentroid) : Vector2.right;
                
                if (distToCentroid < Mathf.Epsilon) continue;
                if (maxInfluenceRadius > 0f && distToCentroid > maxInfluenceRadius + r.effectiveRadius)
                    continue;
                
                // SAME polarities => +1 (push along dirN), OPPOSITE => -1 (pull against dirN)
                int regionSign = (r.polarity == MagnetPolarity.Red) ? +1 : -1;
                float sign = (heroPolarity == regionSign) ? +1f : -1f;
                float distSoft = Mathf.Max(minDistance, distToCentroid - r.effectiveRadius * 0.5f);
                float falloff = 1f / Mathf.Pow(1f + distSoft, Mathf.Max(0.0001f, falloffPower));
                float magnitude = strengthScale * r.strength * falloff;
                sum += dirN * (sign * magnitude);
            }
            else // FixedDirectional
            {
                // Override force direction with a fixed vector
                Vector3 dirWorld = new Vector3(fixedDirection.x, fixedDirection.y, 0f);
                if (directionsInLocalSpace && transform)
                    dirWorld = transform.TransformDirection(dirWorld);
                Vector2 push = new Vector2(dirWorld.x, dirWorld.y);
                Vector2 dirN = (push.sqrMagnitude > 1e-6f) ? push.normalized : Vector2.right;
                
                // Always one-sided via Physics2D Raycast along -dirN
                float maxRay = (maxInfluenceRadius > 0f) ? maxInfluenceRadius : 1000f;
                RaycastHit2D hit = Physics2D.Raycast(pos, -dirN, maxRay, raycastLayerMask);
                if (!hit)
                    continue; // no surface in front — no force

                // If a composite is assigned, ensure we hit exactly it (avoid foreign colliders)
                if (composite && hit.collider != composite)
                    continue;

                float rayDistance = hit.distance;

                int regionSign = (r.polarity == MagnetPolarity.Red) ? +1 : -1;
                float sign = (heroPolarity == regionSign) ? +1f : -1f;
                float d = Mathf.Max(minDistance, float.IsInfinity(rayDistance) ? 0f : rayDistance);
                float falloff = 1f / Mathf.Pow(1f + d, Mathf.Max(0.0001f, falloffPower));
                float magnitude = strengthScale * r.strength * falloff;
                sum += dirN * (sign * magnitude);
            }
        }

        return sum;
    }

    // Geometric utilities for distance to boundary
    private static float DistanceToPolygonEdges(Vector2 p, Vector2[] verts)
    {
        if (verts == null || verts.Length < 2)
            return float.PositiveInfinity;

        float minSq = float.PositiveInfinity;
        int n = verts.Length;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = verts[i];
            Vector2 b = verts[(i + 1) % n];
            float sq = DistancePointToSegmentSqr(p, a, b);
            if (sq < minSq) minSq = sq;
        }
        return Mathf.Sqrt(minSq);
    }

    private static float DistancePointToSegmentSqr(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float abLenSq = Vector2.SqrMagnitude(ab);
        if (abLenSq < 1e-12f)
            return Vector2.SqrMagnitude(p - a);

        float t = Vector2.Dot(p - a, ab) / abLenSq;
        t = Mathf.Clamp01(t);
        Vector2 proj = a + t * ab;
        return Vector2.SqrMagnitude(p - proj);
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
