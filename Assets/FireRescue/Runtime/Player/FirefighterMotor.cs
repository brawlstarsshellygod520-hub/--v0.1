using UnityEngine;

namespace FireRescue
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirefighterMotor : MonoBehaviour
    {
        [SerializeField] FireField field;
        [SerializeField] HoseConnection hose;
        [SerializeField] float speed = 4;
        [SerializeField, Min(.1f)] float oilAcceleration = 6;
        [SerializeField, Min(.1f)] float oilBraking = 3;
        CharacterController controller;
        Vector3 velocity;
        Vector3 externalVelocity;
        float gravity;
        public Vector3 Velocity => velocity + externalVelocity;
        public bool IsSlipping => field && field.GetOilAt(transform.position) > .08f;

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
            float acceleration = IsSlipping ? (direction.sqrMagnitude > .0001f ? oilAcceleration : oilBraking) : 32f;
            velocity = Vector3.MoveTowards(velocity, Vector3.ClampMagnitude(direction, 1) * speed, acceleration * dt);
            Vector3 desired = transform.position + (velocity + externalVelocity) * dt;
            if (hose) desired = hose.ConstrainPosition(desired);
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
                    Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 720 * dt);
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
