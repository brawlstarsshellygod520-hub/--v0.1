using UnityEngine;

namespace FireRescue
{
    // Owns tool gameplay state. Views can observe it, but never drive its swing or spray rules.
    public sealed class FirefighterTools : MonoBehaviour
    {
        const float AxeDamage = 1f;

        public enum Tool { None, Axe, Hose }

        [SerializeField] SprayEmitter emitter;
        [SerializeField, Min(.05f)] float swingDuration = .55f;
        [SerializeField, Range(0.05f, .95f)] float hitProgress = .45f;
        [SerializeField, Min(.01f)] float axeRange = 1.3f;
        [SerializeField, Min(.01f)] float axeRadius = .16f;
        readonly RaycastHit[] axeHits = new RaycastHit[16];
        float swingElapsed;
        bool hitThisSwing;
        bool axeRepeatArmed;
        bool sprayArmed;
        FluidSource firingSource;

        public Tool SelectedTool { get; private set; }
        public bool IsSwinging { get; private set; }
        public float SwingProgress { get; private set; }
        public int SwingCount { get; private set; }
        public int HitCount { get; private set; }
        public RaycastHit LastHit { get; private set; }

        // Input adapters use this before TickPointer so an axe trace or the first spray packet
        // is emitted after the actor has already turned. It only mirrors pointer-state gates;
        // it never advances tool state.
        public bool WillPointerAimAtCursor(bool axePressed, bool axeHeld, bool hosePressed, bool hoseHeld)
        {
            if (!isActiveAndEnabled) return false;

            // Right input has the same priority as TickPointer. Equipping and connecting are
            // interactions, not a spray action.
            if (hosePressed || hoseHeld)
            {
                HoseConnection connection = emitter ? emitter.Connection : null;
                if (hosePressed)
                    return SelectedTool == Tool.Hose && connection && connection.IsConnected && hoseHeld;
                return SelectedTool == Tool.Hose && sprayArmed && connection &&
                    connection.IsConnected && connection.Source == firingSource && hoseHeld;
            }

            // A click starts a stroke only after the axe was already equipped. Once started,
            // release still completes that stroke and therefore continues to target the cursor.
            if (IsSwinging) return true;
            return axePressed && SelectedTool == Tool.Axe;
        }

        public void Initialize(SprayEmitter spray)
        {
            if (emitter && emitter != spray) emitter.Stop();
            emitter = spray;
            Stop();
        }

        public void Tick(Vector3 aimPoint, bool axeHeld, bool sprayHeld, float dt)
        {
            if (!isActiveAndEnabled || dt <= 0)
            {
                Stop();
                return;
            }

            // Right-click hose priority makes simultaneous holds deterministic.
            if (sprayHeld)
            {
                SelectedTool = Tool.Hose;
                StopSwing();
                TickSpray(aimPoint,dt);
                return;
            }

            if (axeHeld)
            {
                SelectedTool = Tool.Axe;
                if (emitter) emitter.Stop();
                TickSwing(dt);
                return;
            }

            Stop();
        }

        // Explicit edges keep the equip/connect click separate from the next action.
        // No device polling here: local and future remote players can submit the same commands.
        public void TickPointer(Vector3 aimPoint, bool axePressed, bool axeHeld,
            bool hosePressed, bool hoseHeld, FluidSource nearbySource, float dt)
        {
            if (!isActiveAndEnabled || dt <= 0) { Stop(); return; }
            HoseConnection connection = emitter ? emitter.Connection : null;
            if (sprayArmed && (!connection || connection.Source != firingSource)) sprayArmed = false;

            // Right input wins; holding an equip/connect click never advances to spray.
            if (hosePressed || hoseHeld)
            {
                if (hosePressed)
                {
                    if (SelectedTool != Tool.Hose)
                    {
                        SelectedTool = Tool.Hose;
                        Stop();
                        return;
                    }
                    if (!connection || !connection.IsConnected)
                    {
                        Stop();
                        if (connection && nearbySource) connection.TryConnect(nearbySource);
                        return;
                    }
                    firingSource = connection.Source;
                    sprayArmed = true;
                }
                StopSwing();
                axeRepeatArmed = false;
                if (SelectedTool == Tool.Hose && sprayArmed && hoseHeld) TickSpray(aimPoint, dt);
                else if (emitter) emitter.Stop();
                return;
            }

            sprayArmed = false;
            firingSource = null;
            if (emitter) emitter.Stop();
            if (axePressed)
            {
                if (SelectedTool != Tool.Axe)
                {
                    SelectedTool = Tool.Axe;
                    Stop();
                    return;
                }
                axeRepeatArmed = true;
                if (!IsSwinging) StartSwing();
            }
            if (!axeHeld) axeRepeatArmed = false;
            // Release finishes the already-started stroke; it does not queue another one.
            if (SelectedTool == Tool.Axe && IsSwinging) TickSwing(dt, axeRepeatArmed && axeHeld);
        }

        public void Stop()
        {
            axeRepeatArmed = false;
            sprayArmed = false;
            firingSource = null;
            StopSwing();
            if (emitter) emitter.Stop();
        }

        public void Stow() { Stop(); SelectedTool = Tool.None; }

        void TickSpray(Vector3 aimPoint, float dt)
        {
            if (emitter && emitter.isActiveAndEnabled) emitter.Tick(aimPoint,true,dt);
        }

        void TickSwing(float dt, bool repeat = true)
        {
            if (!IsSwinging) StartSwing();

            float remaining = dt;
            float duration = Mathf.Max(.05f,swingDuration);
            float strikeTime = duration * hitProgress;
            while (remaining > 0)
            {
                float toCycleEnd = duration - swingElapsed;
                float step = Mathf.Min(remaining,toCycleEnd);
                if (!hitThisSwing && swingElapsed < strikeTime && swingElapsed + step >= strikeTime)
                {
                    hitThisSwing = true;
                    QueryAxeHit();
                }

                swingElapsed += step;
                remaining -= step;
                if (swingElapsed + .00001f < duration) continue;

                if (!repeat) { StopSwing(); return; }
                swingElapsed = 0;
                hitThisSwing = false;
                StartSwing();
            }

            SwingProgress = Mathf.Clamp01(swingElapsed / duration);
        }

        void StartSwing()
        {
            IsSwinging = true;
            SwingCount++;
        }

        void StopSwing()
        {
            IsSwinging = false;
            swingElapsed = 0;
            hitThisSwing = false;
            SwingProgress = 0;
        }

        void QueryAxeHit()
        {
            Vector3 origin = transform.position + Vector3.up * .8f;
            Vector3 direction = transform.forward;
            direction.y = 0;
            if (direction.sqrMagnitude < .0001f) return;
            direction.Normalize();

            int count = Physics.SphereCastNonAlloc(origin,axeRadius,direction,axeHits,axeRange,~0,QueryTriggerInteraction.Ignore);
            float closest = float.MaxValue;
            RaycastHit closestHit = default;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                Collider collider = axeHits[i].collider;
                if (!collider || collider.transform.IsChildOf(transform) || axeHits[i].distance >= closest) continue;
                closest = axeHits[i].distance;
                closestHit = axeHits[i];
                found = true;
            }

            if (!found) return;
            LastHit = closestHit;
            HitCount++;

            // The tool owns target selection, while the target owns its own damage and
            // break state. This keeps axe range independent from any door visual.
            AxeDamageReceiver receiver = closestHit.collider.GetComponentInParent<AxeDamageReceiver>();
            if (receiver) receiver.ReceiveAxeHit(AxeDamage);
        }

        void OnDisable() => Stop();
    }
}
