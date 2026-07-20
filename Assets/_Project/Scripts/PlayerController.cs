using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float runSpeed = 7f;
    [SerializeField] float crouchSpeed = 2.2f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float standingHeight = 1.8f;
    [SerializeField] float crouchingHeight = 1.05f;

    CharacterController controller;
    Transform cameraTransform;
    float verticalSpeed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
    }

    void Update()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        bool crouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.JoystickButton8);
        bool running = !crouching && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.JoystickButton9));
        float targetHeight = crouching ? crouchingHeight : standingHeight;
        controller.height = Mathf.MoveTowards(controller.height, targetHeight, Time.deltaTime * 5f);
        controller.center = Vector3.up * controller.height * 0.5f;

        Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 forward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : transform.forward;
        Vector3 right = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized : transform.right;
        Vector3 move = forward * input.y + right * input.x;

        if (move.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Slerp(transform.forward, move.normalized, 14f * Time.deltaTime);

        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;
        float speed = crouching ? crouchSpeed : running ? runSpeed : walkSpeed;
        controller.Move((move * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
    }
}
