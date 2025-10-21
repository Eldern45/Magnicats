using System.Collections.Generic;
using UnityEngine;

public class MagnetFieldManager : MonoBehaviour
{
    public static MagnetFieldManager Instance { get; private set; }

    [Header("Field Physics")]
    [Tooltip("Global coefficient for all magnets")]
    public float globalK = 20f;

    [Tooltip("Distance falloff exponent (1 = 1/r, 2 = 1/r^2, etc.)")]
    public float falloffPower = 2f;

    [Tooltip("Maximum influence distance (from region centroid). 0 = no limit")]
    public float maxInfluenceRadius = 12f;

    [Tooltip("Minimum distance in the formula to avoid singularity")]
    public float minDistance = 0.25f;

    private readonly HashSet<MagnetCompositeScanner> scanners = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Pick up all scanners in the scene
        var scannersNow = FindObjectsByType<MagnetCompositeScanner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in scannersNow)
        {
            // Safety: let them rebuild when the manager is alive
            s.Rebuild(); // inside Rebuild() it will call Unregister+Register by itself
        }
    }


    public void Register(MagnetCompositeScanner s) => scanners.Add(s);
    public void Unregister(MagnetCompositeScanner s) => scanners.Remove(s);

    /// <summary>
    /// Calculates the total magnetic force at point pos for a hero with polarity heroPolarity (+1/-1).
    /// </summary>
    public Vector2 GetForceAt(Vector2 pos, int heroPolarity)
    {
        Vector2 sum = Vector2.zero;
        if (scanners.Count == 0) return sum;

        foreach (var s in scanners)
        {
            if (!s || s.regions == null) continue;

            foreach (var r in s.regions)
            {
                Vector2 dir = r.centroid - pos;
                float dist = dir.magnitude;
                if (dist < Mathf.Epsilon) continue;

                // Influence radius limitation
                if (maxInfluenceRadius > 0f && dist > maxInfluenceRadius + r.effectiveRadius)
                    continue;

                float sign = (heroPolarity) * (r.polarity == MagnetPolarity.Red ? +1 : -1); 
                // sign = +1 attraction, -1 repulsion (you can change the rule)

                float d = Mathf.Max(minDistance, dist - r.effectiveRadius * 0.5f); 
                
                // slightly "soften" as if the force is not taken from the geometric center
                float falloff = 1f / Mathf.Pow(d, Mathf.Max(0.5f, falloffPower)); // >= 0.5 to avoid being "too flat"

                float magnitude = globalK * r.strength * falloff; // the main formula
                sum += (dir / dist) * (sign * magnitude);
            }
        }

        return sum;
    }
    
    
    public bool drawGizmos;
    public Color gizmoColor = new(1,1,1,0.35f);

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || Instance != this) return;
        if (scanners == null) return;

        Gizmos.color = gizmoColor;
        foreach (var s in scanners)
        {
            if (s?.regions == null) continue;
            foreach (var r in s.regions)
            {
                Gizmos.DrawWireSphere(r.centroid, r.effectiveRadius);
            }
        }
    }

}
