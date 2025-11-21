using UnityEngine;

public class MagnetGizmoViewer : MonoBehaviour
{
    [Header("Gizmos (visualization only)")]
    public bool drawGizmos = true;
    public Color gizmoColor = new(1, 1, 1, 0.35f);
    [Tooltip("Рисовать внешний радиус влияния (maxInfluenceRadius + effectiveRadius)")]
    public bool drawInfluenceRadius = true;
    public Color influenceColor = new(1f, 0.8f, 0.2f, 0.3f);

    [Header("Directional Gizmos")]
    [Tooltip("Рисовать направление для FixedDirectional источников")]
    public bool drawDirection = true;
    public Color directionColor = new(0.2f, 1f, 0.2f, 0.9f);
    [Tooltip("Длина стрелки направления (в единицах мира)")]
    public float directionArrowLength = 2.0f;

    // This component is now gizmo-only: no physics, no global coefficients, no registry.
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        var scanners = FindObjectsByType<MagnetFieldSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (scanners == null || scanners.Length == 0) return;

        foreach (var s in scanners)
        {
            if (!s || s.Regions == null) continue;
            foreach (var r in s.Regions)
            {
                // Внутренний «геометрический» радиус региона
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(r.centroid, r.effectiveRadius);

                // Внешний радиус влияния поля для данного региона
                if (!drawInfluenceRadius || !(s.maxInfluenceRadius > 0f)) continue;
                float outer = r.effectiveRadius + s.maxInfluenceRadius;
                Gizmos.color = influenceColor;
                Gizmos.DrawWireSphere(r.centroid, outer);

                // Направление для FixedDirectional
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
                        // Простая «стрелка» — две короткие засечки
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
