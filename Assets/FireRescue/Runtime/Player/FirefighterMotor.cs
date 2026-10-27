using UnityEngine;

namespace FireRescue
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirefighterMotor : MonoBehaviour
    {
        [SerializeField] FireField field;
        [SerializeField] HoseConnection hose;
        [SerializeField] float speed = 4;
        [SerializeField] float dryAcceleration = 32f;
        [SerializeField] float dryBraking = 32f;
        [SerializeField] float turnSpeed = 720f;
        [SerializeField, Min(.1f)] float oilAcceleration = 6;
        [SerializeField, Min(.1f)] float oilBraking = 3;
        CharacterController controller;
        Vector3 velocity;
        Vector3 externalVelocity;
        float gravity;
        public Vector3 Velocity => velocity + externalVelocity;
        public bool IsSlipping => field && field.GetOilAt(transform.position) > .08f;
        public float MaximumSpeed => speed;
        public void ConfigureLocomotion(float maxSpeed,float acceleration,float braking,float degreesPerSecond)
        {speed=Mathf.Max(.1f,maxSpeed);dryAcceleration=Mathf.Max(.1f,acceleration);dryBraking=Mathf.Max(.1f,braking);turnSpeed=Mathf.Max(1,degreesPerSecond);}

        public void Initialize(FireField fireField, HoseConnection connection) { field = fireField; hose = connection; }
        void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.minMoveDistance = 0;
        }

        public void Move(Vector3 direction, float dt)
        {
            if (dt <= 0) return;
            direction.y = 0;
            float acceleration = IsSlipping ? (direction.sqrMagnitude > .0001f ? oilAcceleration : oilBraking) :
                (direction.sqrMagnitude > .0001f ? dryAcceleration : dryBraking);
            velocity = Vector3.MoveTowards(velocity, Vector3.ClampMagnitude(direction, 1) * speed, acceleration * dt);
            Vector3 desired = transform.position + (velocity + externalVelocity) * dt;
            if (hose)
            {
                Vector3 requested = desired;
                desired = hose.ConstrainPosition(desired);
                Vector3 blocked = requested - desired; blocked.y = 0;
                if (blocked.sqrMagnitude > .00000001f)
                {
                    // Remove stored outward momentum instead of fighting the tether every frame.
                    Vector3 normal = blocked.normalized;
                    float outward = Vector3.Dot(velocity, normal);
                    if (outward > 0) velocity -= normal * outward;
                    outward = Vector3.Dot(externalVelocity, normal);
                    if (outward > 0) externalVelocity -= normal * outward;
                }
            }
            if (controller.isGrounded && gravity < 0) gravity = -2;
            gravity = Mathf.Max(-20, gravity - 22 * dt);
            controller.Move(desired - transform.position + Vector3.up * gravity * dt);
            externalVelocity = Vector3.MoveTowards(externalVelocity, Vector3.zero, (IsSlipping ? 1.2f : 7f) * dt);
        }

        public void Face(Vector3 point, float dt, bool immediate = false)
        {
            if (dt <= 0) return;
            Vector3 direction = point - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > .001f)
                transform.rotation = immediate ? Quaternion.LookRotation(direction) :
                    Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * dt);
            // Facing moves the independent inlet around the body. Keep that movement under
            // the same wrapped length/collision constraint; the motor remains the position owner.
            if(hose && hose.AutomaticWallWrapping && hose.IsConnected)
                controller.Move(hose.ConstrainPosition(transform.position)-transform.position);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Vector3 normal = hit.normal; normal.y = 0;
            if (normal.sqrMagnitude < .01f) return;
            normal.Normalize();
            // Keep tangential sliding, but do not store momentum pushing into a wall after contact.
            float inward = Vector3.Dot(velocity,normal);
            if (inward < 0) velocity -= normal * inward;
            inward = Vector3.Dot(externalVelocity,normal);
            if (inward < 0) externalVelocity -= normal * inward;
        }

        public void AddImpulse(Vector3 impulse)
        {
            impulse.y = 0;
            externalVelocity = Vector3.ClampMagnitude(externalVelocity + impulse, 8);
        }
    }
}
