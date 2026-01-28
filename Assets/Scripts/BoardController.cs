using UnityEngine;
using UnityEngine.InputSystem;

public class BoardController : MonoBehaviour
{
    public float moveSpeed = 5f;        // Speed of movement for the rigidbodies
    public float maxDistance = 5f;      // Max radius distance from the root object
    public Rigidbody frontHalfRb;       // Front rigidbody
    public Rigidbody backHalfRb;        // Back rigidbody
    public Transform deckMesh;          // The deck mesh that rotates
    public Transform rootObject;        // The root object that the rigidbodies are constrained around

    public GroundDetector frontWheels;  // Reference to front wheel ground detector
    public GroundDetector backWheels;   // Reference to back wheel ground detector

    public float verticalForce = 2f;    // Vertical force applied by shoulder buttons/triggers
    public float maxVerticalDistance = 1f;  // Max vertical distance the board can move (positive and negative)
    private float frontVerticalTarget = 0f;  // Target vertical position for front half
    private float backVerticalTarget = 0f;   // Target vertical position for back half
    private float lerpSpeed = 5f;           // Speed at which the vertical position is lerped

    private Vector3 frontHalfVelocity = Vector3.zero;  // To smooth front half movement
    private Vector3 backHalfVelocity = Vector3.zero;   // To smooth back half movement

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Get thumbstick input (X and Y movement)
        Vector2 leftStick = Gamepad.current.leftStick.ReadValue();   // Front rigidbody control
        Vector2 rightStick = Gamepad.current.rightStick.ReadValue(); // Back rigidbody control

        // Move the front and back rigidbodies on a flat plane ONLY if they are in the air
        if (!frontWheels.isGrounded)
            MoveOnPlane(frontHalfRb, leftStick, ref frontHalfVelocity);

        if (!backWheels.isGrounded)
            MoveOnPlane(backHalfRb, rightStick, ref backHalfVelocity);

        // Rotate the deck mesh based on the angle between the two rigidbodies
        RotateDeckMesh();

        // Set the deck position to the midpoint between the two rigidbodies
        MoveDeckToMidpoint();

        // Get the state of the left and right shoulder buttons
        bool leftShoulderPressed = Gamepad.current.leftShoulder.isPressed;
        bool rightShoulderPressed = Gamepad.current.rightShoulder.isPressed;

        // Get the values of the left and right triggers (L2, R2)
        float leftTriggerValue = Gamepad.current.leftTrigger.ReadValue();
        float rightTriggerValue = Gamepad.current.rightTrigger.ReadValue();

        // Apply vertical force based on shoulder buttons or triggers
        ApplyVerticalForce(leftShoulderPressed, rightShoulderPressed, leftTriggerValue, rightTriggerValue);

        // Lerp vertical movement
        LerpVerticalMovement();
    }

    void MoveOnPlane(Rigidbody rb, Vector2 stickInput, ref Vector3 velocity)
    {
        // Get the movement direction based on the thumbstick input
        Vector3 direction = new Vector3(stickInput.x, 0, stickInput.y).normalized;

        // Calculate the target position with smoothing (using Lerp for damping effect)
        Vector3 targetPosition = rb.position + direction * moveSpeed * Time.deltaTime;

        // Ensure the rigidbody stays within the max distance from the root object
        Vector3 directionFromRoot = targetPosition - rootObject.position;
        if (directionFromRoot.magnitude > maxDistance)
        {
            targetPosition = rootObject.position + directionFromRoot.normalized * maxDistance;
        }

        // Smooth the movement using Lerp to apply damping
        rb.position = Vector3.SmoothDamp(rb.position, targetPosition, ref velocity, 0.1f);  // Small smoothing for horizontal movement
    }

    void RotateDeckMesh()
    {
        // Calculate the direction from the front to the back rigidbody
        Vector3 frontToBack = backHalfRb.position - frontHalfRb.position;

        // Calculate the rotation of the deck based on this direction
        Quaternion targetRotation = Quaternion.LookRotation(frontToBack);

        // Apply the rotation to the deck mesh
        deckMesh.rotation = targetRotation;
    }

    void MoveDeckToMidpoint()
    {
        // Calculate the midpoint between the front and back rigidbodies
        Vector3 midpoint = (frontHalfRb.position + backHalfRb.position) / 2;

        // Move the deck mesh to this midpoint
        deckMesh.position = midpoint;
    }

    void ApplyVerticalForce(bool leftShoulderPressed, bool rightShoulderPressed, float leftTriggerValue, float rightTriggerValue)
    {
        // Apply vertical force to the rigidbodies when the shoulder buttons or triggers are pressed
        if (leftShoulderPressed)
        {
            frontVerticalTarget = maxVerticalDistance;  // Move up
        }
        else if (leftTriggerValue > 0)
        {
            frontVerticalTarget = -maxVerticalDistance;  // Move down
        }
        else
        {
            frontVerticalTarget = 0f;  // No input, stay in the middle
        }

        if (rightShoulderPressed)
        {
            backVerticalTarget = maxVerticalDistance;  // Move up
        }
        else if (rightTriggerValue > 0)
        {
            backVerticalTarget = -maxVerticalDistance;  // Move down
        }
        else
        {
            backVerticalTarget = 0f;  // No input, stay in the middle
        }
    }

    void LerpVerticalMovement()
    {
        // Lerp front and back rigidbodies' vertical positions to their target positions
        Vector3 frontPosition = frontHalfRb.position;
        Vector3 backPosition = backHalfRb.position;

        frontPosition.y = Mathf.Lerp(frontPosition.y, frontVerticalTarget, Time.deltaTime * lerpSpeed);
        backPosition.y = Mathf.Lerp(backPosition.y, backVerticalTarget, Time.deltaTime * lerpSpeed);

        // Apply the new position with the lerped Y value
        frontHalfRb.position = frontPosition;
        backHalfRb.position = backPosition;
    }
}