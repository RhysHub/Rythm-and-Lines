using UnityEngine;

public class GroundDetector : MonoBehaviour
{
    public bool isGrounded { get; private set; } = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground")) // Make sure the ground has the "Ground" tag
        {
            isGrounded = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
