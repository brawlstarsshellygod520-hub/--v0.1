using UnityEngine;

namespace FireRescue
{
    // This is the only runtime component reading devices. Additional players supply the same motor/hose commands.
    public sealed class LocalPlayerInput : MonoBehaviour
    {
        [SerializeField] FirefighterMotor motor;
        [SerializeField] HoseConnection hose;
        [SerializeField] SprayEmitter emitter;
        // Optional: the tutorial equips tools, while the original residential scene keeps legacy left-click spray.
        [SerializeField] FirefighterTools tools;
        [SerializeField] Camera view;
        [SerializeField] FluidSource[] sources;
        [SerializeField] HoseWindow[] hoseWindows;
        const float MinimumAimDistance = .75f;
        bool inputFocused = true;
        bool applicationPaused;

        // Tests can submit commands directly without having Update read a physical keyboard or mouse.
        public bool ReadDevices { get; set; } = true;
        public FluidSource NearbySource { get; private set; }

        // The survivors argument is retained for existing scene builders; NPCs now own their escape without input.
        public void Initialize(FirefighterMotor movement, HoseConnection connection, SprayEmitter spray,
            Camera camera, FluidSource[] outlets, RescueNpc[] survivors)
        { motor = movement; hose = connection; emitter = spray; view = camera; sources = outlets; }

        public void SetTools(FirefighterTools value)
        {
            if (tools && tools != value) tools.Stop();
            tools = value;
        }

        void Update()
        {
            if (!ReadDevices) return;
            if (!Application.isFocused)
            {
                SetInputFocus(false);
                if (motor && motor.isActiveAndEnabled) motor.Move(Vector3.zero, Time.deltaTime);
                return;
            }
            inputFocused = true;
            Vector2 input = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            // Ground targeting makes spraying at burning floor patches easy in the angled view.
            Vector3 target = transform.position + transform.forward * 4;
            if (view)
            {
                target = ResolveAimPoint(Input.mousePosition);
            }
            if (tools)
                TickPointerCommand(input,target,Input.GetKeyDown(KeyCode.F),false,
                    Input.GetMouseButtonDown(0),Input.GetMouseButton(0),
                    Input.GetMouseButtonDown(1),Input.GetMouseButton(1),Time.deltaTime);
            else
                TickCommand(input,target,Input.GetKeyDown(KeyCode.F),false,
                    Input.GetMouseButton(0),Time.deltaTime);
        }

        // Raised targets (e.g. a pan on a counter) can be aimed at directly. Keep
        // the established ground-plane aim everywhere else; walls still occlude selection.
        public Vector3 ResolveAimPoint(Vector2 screenPoint)
        {
            Vector3 target = transform.position + transform.forward * 4;
            if (!view) return target;
            Ray ray = view.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~((1 << 9) | (1 << 11)), QueryTriggerInteraction.Ignore))
            {
                var receiver = hit.collider.GetComponentInParent<ElementReceiver>();
                if (receiver && receiver.isActiveAndEnabled) return hit.point;
            }
            if (new Plane(Vector3.up, Vector3.up * .04f).Raycast(ray, out float distance)) target = ray.GetPoint(distance);
            return target;
        }

        public static Vector3 ViewRelativeDirection(Vector2 input, Transform cameraTransform)
        {
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward,Vector3.up).normalized;
            return Vector3.ClampMagnitude(forward * input.y + Vector3.Cross(Vector3.up,forward) * input.x,1);
        }

        // The device reader and automated checks use the same command path; gameplay modules stay device-independent.
        // The legacy calm argument in command overloads is ignored; no interaction is required for NPC evacuation.
        public void TickCommand(Vector2 movement,Vector3 aimPoint,bool interact,bool calm,bool sprayHeld,float dt)
        {
            // Preserve the established ResidentialFire test contract: legacy commands remain callable while this
            // adapter is disabled and do not require an application-focus callback.
            if (dt <= 0)
            {
                if (emitter) emitter.Stop();
                return;
            }
            RunCommand(movement,ref aimPoint,interact,sprayHeld,dt);
            if (emitter && emitter.isActiveAndEnabled) emitter.Tick(aimPoint,sprayHeld,dt);
        }

        // Legacy low-level action command, retained for existing scripted callers and historical tests.
        public void TickCommand(Vector2 movement,Vector3 aimPoint,bool interact,bool calm,bool axeHeld,bool sprayHeld,float dt)
        {
            if (!isActiveAndEnabled || !inputFocused || applicationPaused || dt <= 0)
            {
                StopActions();
                return;
            }

            RunCommand(movement,ref aimPoint,interact,sprayHeld,dt);
            if (tools)
                tools.Tick(aimPoint,axeHeld,sprayHeld,dt);
            else if (emitter && emitter.isActiveAndEnabled)
                emitter.Tick(aimPoint,sprayHeld,dt);
        }

        public void SetInputFocus(bool focused)
        {
            inputFocused = focused;
            if (!focused) StopActions();
        }

        // Current tutorial pointer contract: equip, then connect/act on separate presses.
        public void TickPointerCommand(Vector2 movement, Vector3 aimPoint, bool interact, bool calm,
            bool axePressed, bool axeHeld, bool hosePressed, bool hoseHeld, float dt)
        {
            if (!isActiveAndEnabled || !inputFocused || applicationPaused || dt <= 0)
            {
                StopActions();
                return;
            }
            RunCommand(movement, ref aimPoint, interact, hoseHeld, dt);
            if (tools) tools.TickPointer(aimPoint, axePressed, axeHeld, hosePressed, hoseHeld, NearbySource, dt);
            else if (emitter && emitter.isActiveAndEnabled) emitter.Tick(aimPoint, hoseHeld, dt);
        }

        void RunCommand(Vector2 movement, ref Vector3 aimPoint, bool interact, bool sprayHeld, float dt)
        {
            Vector3 planarAim = aimPoint - transform.position;
            planarAim.y = 0;
            if (planarAim.sqrMagnitude < MinimumAimDistance * MinimumAimDistance)
            {
                float aimHeight = aimPoint.y;
                aimPoint = transform.position + transform.forward * MinimumAimDistance;
                aimPoint.y = aimHeight;
            }

            if (motor && motor.isActiveAndEnabled)
            {
                Vector3 direction = view ? ViewRelativeDirection(movement,view.transform) : Vector3.zero;
                motor.Move(direction,dt);
                // While using the hose, align the mesh/nozzle immediately rather than spraying during a turn.
                motor.Face(aimPoint,dt,sprayHeld);
            }
            if (interact && hose && hose.isActiveAndEnabled) hose.Interact(sources, hoseWindows);
            NearbySource = hose && hose.isActiveAndEnabled ? hose.FindNearestSource(sources) : null;
        }

        void StopActions()
        {
            NearbySource = null;
            if (tools) tools.Stop();
            if (emitter) emitter.Stop();
        }

        void OnDisable() => StopActions();
        void OnApplicationFocus(bool focused) => SetInputFocus(focused);
        void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            if (paused) StopActions();
        }
    }
}
