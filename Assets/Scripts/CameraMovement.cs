using System;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform to follow (usually the player)")]
    public Transform target;

    [Header("Follow Axes")]
    [Tooltip("Follow target horizontally (X)")]
    public bool followHorizontal = true;
    [Tooltip("Follow target vertically (Y)")]
    public bool followVertical = true;

    [Header("Dead Zone (camera does not move while target stays inside)")]
    [Tooltip("Width (X) and Height (Y) of the dead zone in world units, centered on the camera")]
    public Vector2 deadZone = new(2.0f, 1.5f);

    [Header("Smoothing")]
    [Tooltip("Time for the camera to smooth towards the desired position (seconds)")]
    [Min(0f)] public float smoothTime = 0.15f;
    [Tooltip("Max camera speed in world units per second (used by SmoothDamp). 0 = unlimited")]
    [Min(0f)] public float maxSpeed = 20f;

    [Tooltip("Use unscaled delta time (true) or scaled (false) for smoothing")]
    public bool useUnscaledTime = false;

    [Header("Lookahead")]
    [Tooltip("Enable lookahead - camera shifts in the direction of player movement")]
    public bool enableLookahead = true;
    [Tooltip("Maximum distance to shift camera ahead horizontally (X) and vertically (Y) in world units")]
    public Vector2 lookaheadDistance = new Vector2(2f, 1f);
    [Tooltip("Time to smooth lookahead offset changes")]
    [Min(0f)] public float lookaheadSmoothTime = 0.3f;
    [Tooltip("Minimum velocity magnitude to trigger lookahead")]
    [Min(0f)] public float lookaheadThreshold = 0.5f;

    private Vector3 _velocity; // for SmoothDamp
    private Vector2 _lookaheadOffset; // current lookahead offset
    private Vector2 _lookaheadVelocity; // for lookahead smoothing

    [Header("Vertical Limits")]
    [Tooltip("Prevent camera from showing below the ground line (orthographic cameras only)")]
    public bool limitBottom = true;
    [Tooltip("World Y of the ground surface. Camera bottom will not go below this value")]
    public float groundY = 0f;
    [Tooltip("Prevent camera from showing above the ceiling line (orthographic cameras only)")]
    public bool limitTop = false;
    [Tooltip("World Y of the ceiling. Camera top will not go above this value")]
    public float ceilingY = 10f;

    [Header("Horizontal Limits")]
    [Tooltip("Clamp camera so it does not show area left of 'leftBoundX' (orthographic cameras only)")]
    public bool limitLeft;
    [Tooltip("Clamp camera so it does not show area right of 'rightBoundX' (orthographic cameras only)")]
    public bool limitRight;
    [Tooltip("World X of the left bound of the level")] public float leftBoundX = -10f;
    [Tooltip("World X of the right bound of the level")] public float rightBoundX = 10f;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (!target) return;
        if (!_cam)
        {
            Debug.LogWarning("CameraMovement requires a Camera component on the same GameObject");
            return;
        }

        Vector3 camPos = transform.position;
        Vector3 tPos = target.position;

        // Compute offset only when target leaves the dead zone on the respective axis
        Vector2 delta = new Vector2(tPos.x - camPos.x, tPos.y - camPos.y);
        Vector2 offset = Vector2.zero;

        if (followHorizontal)
        {
            float halfW = Mathf.Max(0f, deadZone.x * 0.5f);
            if (delta.x > halfW) offset.x = delta.x - halfW;
            else if (delta.x < -halfW) offset.x = delta.x + halfW;
        }

        if (followVertical)
        {
            float halfH = Mathf.Max(0f, deadZone.y * 0.5f);
            if (delta.y > halfH) offset.y = delta.y - halfH;
            else if (delta.y < -halfH) offset.y = delta.y + halfH;
        }

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // Calculate lookahead offset based on target velocity
        Vector2 lookahead = Vector2.zero;
        if (enableLookahead && (lookaheadDistance.x > 0f || lookaheadDistance.y > 0f))
        {
            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb != null)
            {
                Vector2 velocity = targetRb.linearVelocity;
                float speed = velocity.magnitude;

                if (speed > lookaheadThreshold)
                {
                    Vector2 direction = velocity.normalized;
                    Vector2 targetLookahead = new Vector2(
                        direction.x * lookaheadDistance.x,
                        direction.y * lookaheadDistance.y
                    );

                    // Smooth the lookahead offset
                    _lookaheadOffset = Vector2.SmoothDamp(_lookaheadOffset, targetLookahead,
                        ref _lookaheadVelocity, lookaheadSmoothTime, Mathf.Infinity, dt);
                }
                else
                {
                    // Smoothly return to zero when not moving
                    _lookaheadOffset = Vector2.SmoothDamp(_lookaheadOffset, Vector2.zero,
                        ref _lookaheadVelocity, lookaheadSmoothTime, Mathf.Infinity, dt);
                }

                lookahead = _lookaheadOffset;
            }
        }

        // Desired camera position keeps the same Z and includes lookahead
        Vector3 desired = new Vector3(camPos.x + offset.x + lookahead.x, camPos.y + offset.y + lookahead.y, camPos.z);

        // Vertical clamps (bottom/top) for orthographic camera — independent booleans
        {
            if (_cam && _cam.orthographic)
            {
                float halfHeight = _cam.orthographicSize;

                // Both limits enabled: clamp between them (or center if bounds narrower than view height)
                if (limitBottom && limitTop && ceilingY > groundY)
                {
                    float available = ceilingY - groundY;
                    if (available >= halfHeight * 2f)
                    {
                        float minCenterY = groundY + halfHeight;
                        float maxCenterY = ceilingY - halfHeight;
                        desired.y = Mathf.Clamp(desired.y, minCenterY, maxCenterY);
                    }
                    else
                    {
                        // View is taller than the vertical bounds: lock to middle
                        desired.y = (groundY + ceilingY) * 0.5f;
                    }
                }
                else
                {
                    // Only bottom limit
                    if (limitBottom)
                    {
                        float minCenterY = groundY + halfHeight;
                        if (desired.y < minCenterY)
                            desired.y = minCenterY;
                    }

                    // Only top limit
                    if (limitTop)
                    {
                        float maxCenterY = ceilingY - halfHeight;
                        if (desired.y > maxCenterY)
                            desired.y = maxCenterY;
                    }
                }
            }
        }

        // Clamp horizontally to left/right bounds (independent booleans) so we don't see outside the level
        {
            if (_cam && _cam.orthographic)
            {
                float halfWidth = _cam.orthographicSize * _cam.aspect;

                bool both = limitLeft && limitRight && rightBoundX > leftBoundX;
                if (both)
                {
                    float levelWidth = rightBoundX - leftBoundX;
                    if (levelWidth >= halfWidth * 2f)
                    {
                        float minCenterX = leftBoundX + halfWidth;
                        float maxCenterX = rightBoundX - halfWidth;
                        desired.x = Mathf.Clamp(desired.x, minCenterX, maxCenterX);
                    }
                    else
                    {
                        // Level narrower than camera view: lock camera to the middle of bounds
                        desired.x = (leftBoundX + rightBoundX) * 0.5f;
                    }
                }
                else
                {
                    if (limitLeft)
                    {
                        float minCenterX = leftBoundX + halfWidth;
                        if (desired.x < minCenterX)
                            desired.x = minCenterX;
                    }
                    if (limitRight)
                    {
                        float maxCenterX = rightBoundX - halfWidth;
                        if (desired.x > maxCenterX)
                            desired.x = maxCenterX;
                    }
                }
            }
        }

        if (smoothTime <= 0f)
        {
            // No smoothing
            transform.position = desired;
        }
        else
        {
            // Smoothly move towards desired, maintaining Z
            transform.position = Vector3.SmoothDamp(camPos, desired, ref _velocity, smoothTime, (maxSpeed <= 0f ? Mathf.Infinity : maxSpeed), dt);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Ensure camera reference is available in editor
        Camera cam = _cam ? _cam : GetComponent<Camera>();
        if (!cam) return;
        // Draw the dead zone rectangle around the camera to aid tuning
        Gizmos.color = new Color(1f, 1f, 0.1f, 0.9f);
        Vector3 c = transform.position;

        float halfW = Mathf.Max(0f, deadZone.x * 0.5f);
        float halfH = Mathf.Max(0f, deadZone.y * 0.5f);

        Vector3 a = new Vector3(c.x - halfW, c.y - halfH, c.z);
        Vector3 b = new Vector3(c.x + halfW, c.y - halfH, c.z);
        Vector3 d = new Vector3(c.x + halfW, c.y + halfH, c.z);
        Vector3 e = new Vector3(c.x - halfW, c.y + halfH, c.z);

        // Follow axes: gray out lines for disabled axes
        Color enabledCol = Gizmos.color;
        Color disabledCol = new Color(0.6f, 0.6f, 0.6f, 0.6f);

        // Bottom and top depend on horizontal following for visual clarity
        Gizmos.color = followHorizontal ? enabledCol : disabledCol;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(e, d);

        // Left and right depend on vertical following
        Gizmos.color = followVertical ? enabledCol : disabledCol;
        Gizmos.DrawLine(a, e);
        Gizmos.DrawLine(b, d);

        // Draw ground line (bottom clamp) when enabled
        if (limitBottom)
        {
            if (cam && cam.orthographic)
            {
                float halfWidth = cam.orthographicSize * cam.aspect;
                Vector3 g1 = new Vector3(c.x - halfWidth, groundY, c.z);
                Vector3 g2 = new Vector3(c.x + halfWidth, groundY, c.z);
                Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
                Gizmos.DrawLine(g1, g2);
            }
        }

        // Draw ceiling line (top clamp) when enabled
        if (limitTop)
        {
            if (cam && cam.orthographic)
            {
                float halfWidth = cam.orthographicSize * cam.aspect;
                Vector3 c1 = new Vector3(c.x - halfWidth, ceilingY, c.z);
                Vector3 c2 = new Vector3(c.x + halfWidth, ceilingY, c.z);
                Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.9f);
                Gizmos.DrawLine(c1, c2);
            }
        }

        // Draw left/right bounds (horizontal clamp) when enabled
        {
            if (cam && cam.orthographic)
            {
                float halfHeight = cam.orthographicSize;

                if (limitLeft)
                {
                    Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.9f);
                    Vector3 l1 = new Vector3(leftBoundX, c.y - halfHeight, c.z);
                    Vector3 l2 = new Vector3(leftBoundX, c.y + halfHeight, c.z);
                    Gizmos.DrawLine(l1, l2);
                }

                if (limitRight)
                {
                    Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.9f);
                    Vector3 r1 = new Vector3(rightBoundX, c.y - halfHeight, c.z);
                    Vector3 r2 = new Vector3(rightBoundX, c.y + halfHeight, c.z);
                    Gizmos.DrawLine(r1, r2);
                }
            }
        }
    }
}
