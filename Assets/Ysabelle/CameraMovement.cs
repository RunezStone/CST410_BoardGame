using UnityEngine;

/// <summary>
/// Free-fly "omnipresent" camera, like the Sims free camera. Attach to your Main Camera.
///
///   Hold right mouse ..... look around
///   WASD / Arrows ........ fly forward / back / left / right (where you're looking)
///   E / Space ............ fly up
///   Q / Left Ctrl ........ fly down
///   Shift ................ fly faster
///   Mouse wheel .......... change fly speed
///
/// Left click is never used, so tile clicking in PlayerManager still works.
/// </summary>
public class CameraMovement : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] float flySpeed = 5f;
    [SerializeField] float minFlySpeed = 0.5f;
    [SerializeField] float maxFlySpeed = 30f;
    [SerializeField] float fastMultiplier = 3f;
    [Tooltip("Higher = snappier starts/stops, lower = floatier.")]
    [SerializeField] float acceleration = 8f;

    [Header("Look")]
    [SerializeField] float lookSensitivity = 2f;

    [Header("Height Limits")]
    [Tooltip("Height of the board surface (its Y position).")]
    [SerializeField] float groundHeight = 0f;
    [Tooltip("The camera never goes lower than this above the board.")]
    [SerializeField] float minHeightAboveBoard = 0.25f;
    [SerializeField] float maxHeight = 30f;

    [Header("Bounds (XZ)")]
    [SerializeField] bool clampToBounds = true;
    [SerializeField] Vector2 boundsMin = new Vector2(-11f, -18f);
    [SerializeField] Vector2 boundsMax = new Vector2(11f, 8f);

    float yaw, pitch;
    Vector3 velocity;

    void Start()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x > 180f ? euler.x - 360f : euler.x;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Look (hold right mouse; cursor hidden while looking)
        bool looking = Input.GetMouseButton(1);
        Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !looking;

        if (looking)
        {
            yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
        }
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Scroll changes fly speed
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
            flySpeed = Mathf.Clamp(flySpeed * (1f + scroll * 0.1f), minFlySpeed, maxFlySpeed);

        // Movement relative to where you're looking
        Vector3 dir = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dir += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dir -= transform.forward;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir += transform.right;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir -= transform.right;
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) dir += Vector3.up;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) dir -= Vector3.up;

        float speed = flySpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= fastMultiplier;

        Vector3 desired = dir.sqrMagnitude > 0f ? dir.normalized * speed : Vector3.zero;
        velocity = Vector3.Lerp(velocity, desired, 1f - Mathf.Exp(-acceleration * Time.deltaTime));

        Vector3 pos = transform.position + velocity * Time.deltaTime;

        if (clampToBounds)
        {
            pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
            pos.z = Mathf.Clamp(pos.z, boundsMin.y, boundsMax.y);
        }
        pos.y = Mathf.Clamp(pos.y, groundHeight + minHeightAboveBoard, maxHeight);

        transform.position = pos;
    }

    /// <summary>Instantly move and aim the camera at a point (e.g. the active player).</summary>
    public void LookAtPoint(Vector3 point, float distance = 6f, float angle = 50f)
    {
        Quaternion rot = Quaternion.Euler(angle, yaw, 0f);
        transform.position = point - rot * Vector3.forward * distance;
        pitch = angle;
        transform.rotation = rot;
        velocity = Vector3.zero;
    }

    void OnDrawGizmosSelected()
    {
        if (!clampToBounds) return;
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((boundsMin.x + boundsMax.x) / 2f, groundHeight, (boundsMin.y + boundsMax.y) / 2f);
        Vector3 size = new Vector3(boundsMax.x - boundsMin.x, 0.01f, boundsMax.y - boundsMin.y);
        Gizmos.DrawWireCube(center, size);
    }
}