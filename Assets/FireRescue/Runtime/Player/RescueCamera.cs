using UnityEngine;

namespace FireRescue
{
    [RequireComponent(typeof(Camera))]
    public sealed class RescueCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        // Legacy scenes retain 50/40; individual scenes can align the view without rotating gameplay.
        [SerializeField] Vector2 viewAngles = new Vector2(50, 40);
        Vector3 focus;
        Vector3 velocity;
        public void Initialize(Transform player) { target = player; Snap(); }
        public void ConfigureView(float pitch, float yaw)
        { viewAngles = new Vector2(Mathf.Clamp(pitch, 10, 90), yaw); Snap(); }
        void Awake() => Snap();
        void Snap() { if (target) focus = target.position + Vector3.up * .7f; Apply(); }
        void LateUpdate()
        {
            if (!target) return;
            focus = Vector3.SmoothDamp(focus, target.position + Vector3.up * .7f, ref velocity, .18f);
            Apply();
        }
        void Apply()
        {
            Quaternion rotation = Quaternion.Euler(viewAngles.x, viewAngles.y, 0);
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * 22, rotation);
        }
    }
}
