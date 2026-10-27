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
        // Opt-in for the tutorial: locomotion owns facing except while an actual tool action is active.
        // False preserves existing point-aim scenes and scripted command callers.
        [SerializeField] bool faceMovementWhenIdle;
        [SerializeField] PlayerCarryInteraction carry;
        [SerializeField] FirstReviewMission mission;
        public void SetMissionInteraction(PlayerCarryInteraction interaction, FirstReviewMission state)
        {carry=interaction;mission=state;}
        const float MinimumAimDistance = .75f;
        bool inputFocused = true;
        bool applicationPaused;

        // Tests can submit commands directly without having Update read a physical keyboard or mouse.
        public bool ReadDevices { get; set; } = true;
        public FluidSource NearbySource { get; private set; }

        public void SetLocomotionFacingDuringPointerInput(bool enabled)
        {
            faceMovementWhenIdle = enabled;
        }

        // The survivors argument is retained for existing scene builders; NPCs now own their escape without input.
        public void Initialize(FirefighterMotor movement, HoseConnection connection, SprayEmitter spray,
            Camera camera, FluidSource[] outlets, RescueNpc[] survivors)
        { motor = movement; hose = connection; emitter = spray; view = camera; sources = outlets; }

        public void SetTools(FirefighterTools value)
        {
            if (tools && tools != value) tools.Stop();
            tools = value;
        }

        // Shared by the R device edge and automated restart validation, in every round state.
        public bool TryRestartRound()
        {
            if (!isActiveAndEnabled || !mission) return false;
            StopActions();mission.RestartRound();return true;
        }

        void Update()
        {
            if (!ReadDevices) return;
            if (Input.GetKeyDown(KeyCode.R) && TryRestartRound()) return;
            if (mission && !mission.AllowsInput) { StopActions(); return; }
            if (!Application.isFocused)
            {
                SetInputFocus(false);
                if (motor && motor.isActiveAndEnabled) motor.Move(Vector3.zero, Time.deltaTime);
                return;
            }
            inputFocused = true;
            if(carry)carry.RefreshNearby();
            Vector2 input = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            // Ground targeting makes spraying at burning floor patches easy in the angled view.
            Vector3 target = transform.position + transform.forward * 4;
            if (view)
            {
                target = ResolveAimPoint(Input.mousePosition);
            }
            if (tools)
                TickPointerCommand(input,target,Input.GetKeyDown(KeyCode.E),false,
                    Input.GetMouseButtonDown(0),Input.GetMouseButton(0),
                    Input.GetMouseButtonDown(1),Input.GetMouseButton(1),Time.deltaTime);
            else
                TickCommand(input,target,Input.GetKeyDown(KeyCode.E),false,
                    Input.GetMouseButton(0),Time.deltaTime);
        }

        // Directional mode projects the cursor onto nozzle height for a screen-readable heading.
        // It never snaps to a receiver or uses cursor distance to control the trajectory.
        // Historical scenes retain their ground/raised-target point aim.
        public Vector3 ResolveAimPoint(Vector2 screenPoint)
        {
            Vector3 target = transform.position + transform.forward * 4;
            if (!view) return target;
            Ray ray = view.ScreenPointToRay(screenPoint);
            if ((!emitter || !emitter.UsesDirectionalSpray) &&
                Physics.Raycast(ray, out RaycastHit hit, 100f, ~((1 << 9) | (1 << 11)), QueryTriggerInteraction.Ignore))
            {
                var receiver = hit.collider.GetComponentInParent<ElementReceiver>();
                if (receiver && receiver.isActiveAndEnabled) return hit.point;
            }
            float aimHeight = emitter && emitter.UsesDirectionalSpray ? emitter.NozzlePosition.y : .04f;
            if (new Plane(Vector3.up, Vector3.up * aimHeight).Raycast(ray, out float distance)) target = ray.GetPoint(distance);
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
            if (!isActiveAndEnabled || !inputFocused || applicationPaused || dt <= 0 || (mission && !mission.AllowsInput))
            {
                StopActions();
                return;
            }
            if (carry) carry.RefreshNearby();
            // E is one contextual action, never both a pickup/drop and a hose toggle.
            if (interact && InteractContext())
            {
                axePressed=axeHeld=hosePressed=hoseHeld=false;
            }
            if(carry && carry.IsCarrying)
            {
                if(tools)tools.Stow();
                RunCommand(movement,ref aimPoint,false,false,dt,true,false);
                return;
            }
            // Establish action heading before the first axe trace or spray packet is produced.
            bool actionAiming = tools && tools.WillPointerAimAtCursor(axePressed, axeHeld, hosePressed, hoseHeld);
            RunCommand(movement, ref aimPoint, false, hoseHeld, dt, true, actionAiming);
            if (tools) tools.TickPointer(aimPoint, axePressed, axeHeld, hosePressed, hoseHeld, NearbySource, dt);
            else if (emitter && emitter.isActiveAndEnabled) emitter.Tick(aimPoint, hoseHeld, dt);
        }

        bool InteractContext()
        {
            // Holding, delivery and an actually safe pickup have priority. A blocked drop
            // must not fall through and accidentally disconnect a different interaction.
            if(carry && (carry.IsCarrying || carry.NearbyItem))
            {
                bool held=carry.IsCarrying;
                if(carry.Interact() && !held && carry.IsCarrying && hose)hose.Disconnect();
                // Picking up stows the hose, so the player is not tethered with both hands full.
                return true;
            }
            if(!hose || !hose.isActiveAndEnabled)return false;
            if(hose.IsConnected)
            { if(tools)tools.Stop();hose.Disconnect();return true; }
            var source=hose.FindNearestSource(sources);
            if(!source || !hose.TryConnect(source))return false;
            if(tools)tools.EquipHose();
            return true;
        }

        void RunCommand(Vector2 movement, ref Vector3 aimPoint, bool interact, bool sprayHeld, float dt,
            bool isPointerCommand = false, bool pointerActionAiming = false)
        {
            Vector3 planarAim = aimPoint - transform.position;
            planarAim.y = 0;
            if (planarAim.sqrMagnitude < MinimumAimDistance * MinimumAimDistance)
            {
                float aimHeight = aimPoint.y;
                Vector3 heading = emitter && emitter.UsesDirectionalSpray && planarAim.sqrMagnitude > .000001f
                    ? planarAim.normalized : transform.forward;
                aimPoint = transform.position + heading * MinimumAimDistance;
                aimPoint.y = aimHeight;
            }

            if (motor && motor.isActiveAndEnabled)
            {
                Vector3 direction = view ? ViewRelativeDirection(movement,view.transform) : Vector3.zero;
                motor.Move(direction,dt);
                if (!isPointerCommand || !faceMovementWhenIdle)
                {
                    // Historical command paths retain point aim, including their immediate hose turn.
                    motor.Face(aimPoint,dt,sprayHeld);
                }
                else if (pointerActionAiming)
                {
                    // Tool gameplay traces transform.forward, so action aim settles before TickPointer.
                    motor.Face(aimPoint,dt,true);
                }
                else if (direction.sqrMagnitude > .0001f)
                {
                    // No input leaves the previous heading intact and avoids idle jitter.
                    motor.Face(transform.position + direction,dt,false);
                }
            }
            if (interact && hose && hose.isActiveAndEnabled)
            {
                if(isPointerCommand)hose.Disconnect();
                else hose.Interact(sources,hoseWindows); // Historical low-level window/interaction commands.
            }
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
