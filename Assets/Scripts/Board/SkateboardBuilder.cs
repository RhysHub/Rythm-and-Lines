using UnityEngine;

/// <summary>
/// Builds a 3D skateboard from Unity primitives.
/// Attach to an empty GameObject to generate the board at runtime or in editor.
/// </summary>
public class SkateboardBuilder : MonoBehaviour
{
    [Header("Deck Dimensions")]
    [Tooltip("Length of the deck (Z-axis)")]
    public float deckLength = 0.8f;
    [Tooltip("Width of the deck (X-axis)")]
    public float deckWidth = 0.2f;
    [Tooltip("Thickness of the deck (Y-axis)")]
    public float deckThickness = 0.02f;

    [Header("Truck Dimensions")]
    [Tooltip("Width of the truck hanger")]
    public float truckWidth = 0.18f;
    [Tooltip("Height of the truck")]
    public float truckHeight = 0.025f;
    [Tooltip("Distance from center to truck mount")]
    public float wheelbaseOffset = 0.28f;

    [Header("Wheel Dimensions")]
    [Tooltip("Radius of the wheels")]
    public float wheelRadius = 0.026f;
    [Tooltip("Width of the wheels")]
    public float wheelWidth = 0.018f;

    [Header("Materials")]
    public Material deckMaterial;
    public Material truckMaterial;
    public Material wheelMaterial;

    [Header("Colors (if no materials assigned)")]
    public Color deckColor = new Color(0.4f, 0.25f, 0.1f); // Brown wood
    public Color truckColor = new Color(0.7f, 0.7f, 0.7f); // Silver metal
    public Color wheelColor = new Color(0.95f, 0.95f, 0.9f); // Off-white

    [Header("Generated Reference")]
    [SerializeField] private Transform boardVisuals;

    /// <summary>
    /// The root transform of the generated skateboard visuals.
    /// Use this as the animation target.
    /// </summary>
    public Transform BoardVisuals => boardVisuals;

    [ContextMenu("Build Skateboard")]
    public GameObject BuildSkateboard()
    {
        // Clean up existing board if any
        if (boardVisuals != null)
        {
            if (Application.isPlaying)
                Destroy(boardVisuals.gameObject);
            else
                DestroyImmediate(boardVisuals.gameObject);
        }

        // Create root container
        GameObject root = new GameObject("BoardVisuals");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        boardVisuals = root.transform;

        // Create deck
        CreateDeck(root.transform);

        // Create trucks and wheels
        CreateTruckAssembly(root.transform, true);  // Front truck
        CreateTruckAssembly(root.transform, false); // Back truck

        return root;
    }

    private void CreateDeck(Transform parent)
    {
        GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.name = "Deck";
        deck.transform.SetParent(parent);
        deck.transform.localPosition = Vector3.zero;
        deck.transform.localRotation = Quaternion.identity;
        deck.transform.localScale = new Vector3(deckWidth, deckThickness, deckLength);

        // Remove collider (we'll handle collision separately)
        var collider = deck.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }

        // Apply material
        var renderer = deck.GetComponent<MeshRenderer>();
        if (deckMaterial != null)
        {
            renderer.material = deckMaterial;
        }
        else
        {
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = deckColor;
        }

        // Create nose kick (subtle upturn at front)
        CreateDeckKick(parent, true);
        // Create tail kick (subtle upturn at back)
        CreateDeckKick(parent, false);
    }

    private void CreateDeckKick(Transform parent, bool isFront)
    {
        GameObject kick = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kick.name = isFront ? "NoseKick" : "TailKick";
        kick.transform.SetParent(parent);

        float zPos = isFront ? deckLength * 0.45f : -deckLength * 0.45f;
        float kickAngle = isFront ? -15f : 15f;

        kick.transform.localPosition = new Vector3(0, deckThickness * 0.3f, zPos);
        kick.transform.localRotation = Quaternion.Euler(kickAngle, 0, 0);
        kick.transform.localScale = new Vector3(deckWidth * 0.95f, deckThickness, deckLength * 0.15f);

        // Remove collider
        var collider = kick.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }

        // Apply material
        var renderer = kick.GetComponent<MeshRenderer>();
        if (deckMaterial != null)
        {
            renderer.material = deckMaterial;
        }
        else
        {
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = deckColor;
        }
    }

    private void CreateTruckAssembly(Transform parent, bool isFront)
    {
        // Create truck container
        GameObject truck = new GameObject(isFront ? "FrontTruck" : "BackTruck");
        truck.transform.SetParent(parent);

        float zPos = isFront ? wheelbaseOffset : -wheelbaseOffset;
        float yPos = -(deckThickness * 0.5f + truckHeight * 0.5f);
        truck.transform.localPosition = new Vector3(0, yPos, zPos);
        truck.transform.localRotation = Quaternion.identity;

        // Create hanger (the main truck body)
        CreateTruckHanger(truck.transform);

        // Create wheels
        float wheelOffset = (truckWidth * 0.5f) + (wheelWidth * 0.5f);
        CreateWheel(truck.transform, new Vector3(-wheelOffset, -truckHeight * 0.5f, 0), "LeftWheel");
        CreateWheel(truck.transform, new Vector3(wheelOffset, -truckHeight * 0.5f, 0), "RightWheel");
    }

    private void CreateTruckHanger(Transform parent)
    {
        GameObject hanger = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hanger.name = "Hanger";
        hanger.transform.SetParent(parent);
        hanger.transform.localPosition = Vector3.zero;
        hanger.transform.localRotation = Quaternion.identity;
        hanger.transform.localScale = new Vector3(truckWidth, truckHeight, truckHeight);

        // Remove collider
        var collider = hanger.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }

        // Apply material
        var renderer = hanger.GetComponent<MeshRenderer>();
        if (truckMaterial != null)
        {
            renderer.material = truckMaterial;
        }
        else
        {
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = truckColor;
        }
    }

    private void CreateWheel(Transform parent, Vector3 localPos, string name)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = name;
        wheel.transform.SetParent(parent);
        wheel.transform.localPosition = localPos;
        // Rotate cylinder to align with X-axis (wheels roll along Z)
        wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
        wheel.transform.localScale = new Vector3(wheelRadius * 2, wheelWidth * 0.5f, wheelRadius * 2);

        // Remove collider
        var collider = wheel.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }

        // Apply material
        var renderer = wheel.GetComponent<MeshRenderer>();
        if (wheelMaterial != null)
        {
            renderer.material = wheelMaterial;
        }
        else
        {
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = wheelColor;
        }
    }

    /// <summary>
    /// Returns the total height from ground to top of deck when sitting flat
    /// </summary>
    public float GetBoardHeight()
    {
        return (wheelRadius * 2) + truckHeight + (deckThickness * 0.5f);
    }

    /// <summary>
    /// Returns the Y position the board should be at to sit on the ground
    /// </summary>
    public float GetGroundedYPosition()
    {
        return GetBoardHeight();
    }

    private void Awake()
    {
        // Auto-build if no visuals exist
        if (boardVisuals == null)
        {
            BuildSkateboard();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Rebuild in editor when values change
        if (!Application.isPlaying && boardVisuals != null)
        {
            // Delay rebuild to avoid issues during serialization
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                    BuildSkateboard();
            };
        }
    }
#endif
}
