using UnityEngine;

namespace FireRescue
{
    /// <summary>Presentation-only locomotion sampling. Gameplay movement remains owned by its motor.</summary>
    public sealed class CharacterLocomotionAnimation : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int CycleSpeed = Animator.StringToHash("CycleSpeed");
        private const float RunEnterSpeed = .06f;
        private const float RunExitSpeed = .04f;
        private const float TeleportSpeed = 16f;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform actorRoot;
        [SerializeField, Min(.01f)] private float referenceSpeed = 4f;
        private Vector3 lastPosition;
        private bool hasSample;
        private bool isMoving;
        private float sampledSpeed;
        public bool IsMoving => isMoving;
        public float SampledSpeed => sampledSpeed;
        private void Awake()
        {
            if (actorRoot == null) actorRoot = transform;
            DisableRootMotion();
        }

        private void OnEnable()
        {
            DisableRootMotion();
            ResetSampling();
            SetIdle();
        }
        private void LateUpdate() => TickPresentation(Time.deltaTime);
        private void OnDisable() => SetIdle();
        /// <summary>May be called repeatedly when an actor or Animator is reconfigured.</summary>
        public void Initialize(Animator target, Transform root, float actorReferenceSpeed)
        {
            animator = target;
            actorRoot = root != null ? root : transform;
            referenceSpeed = Mathf.Max(.01f, actorReferenceSpeed);
            DisableRootMotion();
            ResetSampling();
            SetIdle();
        }
        /// <summary>Shared with Play Mode tests; samples resolved transform motion after movement.</summary>
        public void TickPresentation(float deltaTime)
        {
            Transform root = actorRoot != null ? actorRoot : transform;
            Vector3 currentPosition = root.position;
            if (deltaTime <= 0f || !hasSample)
            {
                lastPosition = currentPosition;
                hasSample = true;
                SetIdle();
                return;
            }
            Vector3 displacement = currentPosition - lastPosition;
            displacement.y = 0f;
            lastPosition = currentPosition;
            sampledSpeed = displacement.magnitude / deltaTime;
            if (sampledSpeed > TeleportSpeed)
            {
                SetIdle();
                return;
            }

            float normalizedSpeed = Mathf.Clamp(sampledSpeed / referenceSpeed, 0f, 1.4f);
            isMoving = isMoving ? normalizedSpeed >= RunExitSpeed : normalizedSpeed > RunEnterSpeed;
            SetAnimatorValues(normalizedSpeed);
        }
        private void ResetSampling()
        {
            lastPosition = (actorRoot != null ? actorRoot : transform).position;
            hasSample = true;
            sampledSpeed = 0f;
            isMoving = false;
        }
        private void SetIdle()
        {
            sampledSpeed = 0f;
            isMoving = false;
            SetAnimatorValues(0f);
        }

        private void SetAnimatorValues(float normalizedSpeed)
        {
            if (animator == null || !animator.isActiveAndEnabled || !animator.isInitialized) return;
            animator.SetFloat(Speed, normalizedSpeed);
            animator.SetFloat(CycleSpeed, Mathf.Clamp(normalizedSpeed, .55f, 1.4f));
        }

        private void DisableRootMotion()
        {
            if (animator != null) animator.applyRootMotion = false;
        }
    }
}
