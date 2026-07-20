using UnityEngine;

public sealed class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float distance = 4.5f;
    [SerializeField] float sensitivity = 130f;
    float yaw = 25f;
    float pitch = 18f;

    public void SetTarget(Transform value) => target = value;

    void LateUpdate()
    {
        if (!target) return;
        float mouseX = Input.GetMouseButton(1) ? Input.GetAxis("Mouse X") : 0f;
        float mouseY = Input.GetMouseButton(1) ? Input.GetAxis("Mouse Y") : 0f;
        float stickX = Input.GetAxisRaw("Gamepad Look X");
        float stickY = Input.GetAxisRaw("Gamepad Look Y");
        yaw += (mouseX * 2.2f + stickX) * sensitivity * Time.deltaTime;
        pitch -= (mouseY * 2.2f + stickY) * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -10f, 65f);
        distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * 0.35f, 2.2f, 7f);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * 1.15f;
        transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
    }
}
