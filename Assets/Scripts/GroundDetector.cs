using UnityEngine;

/// <summary>
/// Detects if the skateboard wheels are touching the ground.
/// Uses both raycast and trigger detection for reliability.
/// </summary>
public class GroundDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Distance to raycast downward for ground detection")]
    public float raycastDistance = 0.2f;

    [Tooltip("Layer mask for ground detection (if not set, uses 'Ground' tag)")]
    public LayerMask groundLayer = -1; // -1 = Everything

    [Tooltip("Use raycast detection (more reliable)")]
    public bool useRaycast = true;

    [Tooltip("Use trigger detection (backup)")]
    public bool useTrigger = true;

    [Header("Debug")]
    public bool showDebug = false;

    // State
    private bool raycastGrounded = false;
    private bool triggerGrounded = false;

    /// <summary>
    /// Whether this detector is touching the ground.
    /// </summary>
    public bool isGrounded
    {
        get
        {
            if (useRaycast && useTrigger)
                return raycastGrounded || triggerGrounded;
            else if (useRaycast)
                return raycastGrounded;
            else if (useTrigger)
                return triggerGrounded;
            return true; // Default to grounded if no detection method
        }
    }

    private void Update()
    {
        if (useRaycast)
        {
            CheckRaycastGround();
        }
    }

    private void CheckRaycastGround()
    {
        // Cast a ray downward from this position
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance, groundLayer))
        {
            // Hit something below - count as grounded
            // Also check for Ground tag if it exists
            bool isTaggedGround = false;
            try { isTaggedGround = hit.collider.CompareTag("Ground"); } catch { }

            if (isTaggedGround || hit.point.y < transform.position.y)
            {
                raycastGrounded = true;
                if (showDebug) Debug.DrawLine(transform.position, hit.point, Color.green);
                return;
            }
        }

        raycastGrounded = false;
        if (showDebug) Debug.DrawRay(transform.position, Vector3.down * raycastDistance, Color.red);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;

        if (other.CompareTag("Ground"))
        {
            triggerGrounded = true;
            if (showDebug) Debug.Log($"[GroundDetector] Trigger entered ground: {other.name}");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!useTrigger) return;

        if (other.CompareTag("Ground"))
        {
            triggerGrounded = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!useTrigger) return;

        if (other.CompareTag("Ground"))
        {
            triggerGrounded = false;
            if (showDebug) Debug.Log($"[GroundDetector] Trigger exited ground: {other.name}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw raycast range
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * raycastDistance);
        Gizmos.DrawWireSphere(transform.position + Vector3.down * raycastDistance, 0.02f);
    }
}
