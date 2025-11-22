using UnityEngine;

public class MagnetGizmoViewer : MonoBehaviour
{
    [Header("Gizmos (visualization only)")]
    public bool drawGizmos = true;
    public Color gizmoColor = new(1, 1, 1, 0.35f);
    [Tooltip("Draw outer influence radius (effectiveRadius + maxInfluenceRadius)")]
    public bool drawInfluenceRadius = true;
    public Color influenceColor = new(1f, 0.8f, 0.2f, 0.3f);

    [Header("Directional Gizmos")]
    [Tooltip("Draw direction arrow for FixedDirectional sources")]
    public bool drawDirection = true;
    public Color directionColor = new(0.2f, 1f, 0.2f, 0.9f);
    [Tooltip("Direction arrow length (world units)")]
    public float directionArrowLength = 2.0f;

    // This component is gizmo-only: no physics, no global coefficients, no registry.
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        var scanners = FindObjectsByType<MagnetFieldSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (scanners == null || scanners.Length == 0) return;

        foreach (var s in scanners)
        {
            if (!s) continue;
            // Ensure regions exist in Edit Mode (if not built yet)
            if (s.Regions == null || s.Regions.Count == 0)
            {
                s.Rebuild();
                if (s.Regions == null || s.Regions.Count == 0)
                    continue;
            }
            foreach (var r in s.Regions)
            {
                // Inner "geometric" radius of the region
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(r.centroid, r.effectiveRadius);

                // Precompute outer influence radius for later use (even if we don't draw it)
                float outerInfluence = (s.maxInfluenceRadius > 0f)
                    ? r.effectiveRadius + s.maxInfluenceRadius
                    : r.effectiveRadius;

                // Outer field influence radius for this region (optional)
                if (drawInfluenceRadius && s.maxInfluenceRadius > 0f)
                {
                    Gizmos.color = influenceColor;
                    Gizmos.DrawWireSphere(r.centroid, outerInfluence);
                }

                // Direction for FixedDirectional
                if (drawDirection && s.fieldMode == MagnetFieldMode.FixedDirectional)
                {
                    Vector3 dirWorld = new Vector3(s.fixedDirection.x, s.fixedDirection.y, 0f);
                    if (s.directionsInLocalSpace && s.transform)
                        dirWorld = s.transform.TransformDirection(dirWorld);

                    if (dirWorld.sqrMagnitude > 1e-6f)
                    {
                        Vector3 start = new Vector3(r.centroid.x, r.centroid.y, 0f);
                        Vector3 end = start + dirWorld.normalized * Mathf.Max(0.1f, directionArrowLength);
                        Gizmos.color = directionColor;
                        Gizmos.DrawLine(start, end);
                        // Simple arrow head — two short ticks
                        Vector3 side = Quaternion.AngleAxis(25f, Vector3.forward) * (-dirWorld.normalized);
                        Gizmos.DrawLine(end, end + side * 0.3f * Mathf.Max(0.1f, directionArrowLength));
                        side = Quaternion.AngleAxis(-25f, Vector3.forward) * (-dirWorld.normalized);
                        Gizmos.DrawLine(end, end + side * 0.3f * Mathf.Max(0.1f, directionArrowLength));
                    }
                }
            }
        }
    }
}
