using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FireRescue.Editor
{
    // Reusable Play Mode fixture. CharacterSwapValidation owns the batch driver so this
    // check remains focused on command-to-facing behavior rather than scene setup.
    public static class CharacterFacingValidation
    {
        const float Dt = 1f / 30f;

        public static IEnumerator Run(List<string> checks)
        {
            GameObject root = new GameObject("Character facing fixture");
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.SetParent(root.transform);
            ground.transform.position = new Vector3(1000, -.1f, 1000);
            ground.transform.localScale = new Vector3(30, .2f, 30);

            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            player.transform.position = new Vector3(1000, 0, 1000);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.6f; controller.radius = .28f; controller.center = new Vector3(0, .8f, 0);
            HoseConnection hose = player.AddComponent<HoseConnection>();
            FirefighterMotor motor = player.AddComponent<FirefighterMotor>(); motor.Initialize(null, hose);
            ElementReaction reactions = player.AddComponent<ElementReaction>();
            SprayEmitter emitter = player.AddComponent<SprayEmitter>(); emitter.Initialize(hose, reactions);
            emitter.EnableDirectionalSpray(true); emitter.AutomaticSimulation = false;
            FirefighterTools tools = player.AddComponent<FirefighterTools>(); tools.Initialize(emitter);

            GameObject cameraObject = new GameObject("Facing test view");
            cameraObject.transform.position = player.transform.position + new Vector3(0, 8, -8);
            cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0, -.7f, 1));
            Camera camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            LocalPlayerInput input = player.AddComponent<LocalPlayerInput>();

            GameObject sourceObject = new GameObject("Water source");
            sourceObject.transform.SetParent(root.transform);
            sourceObject.transform.position = player.transform.position + Vector3.right * .2f;
            FluidSource source = sourceObject.AddComponent<FluidSource>(); source.Configure(ElementKind.Water);
            SprayProfile profile = ScriptableObject.CreateInstance<SprayProfile>();
            SpraySettings settings = SpraySettings.DefaultFor(ElementKind.Water);
            settings.SpreadAngle = 0f; settings.ArcHeight = 0f; settings.PacketsPerSecond = 20f;
            profile.Configure(settings); source.SetSprayProfile(profile);
            input.Initialize(motor, hose, emitter, camera, new[] { source }, Array.Empty<RescueNpc>());
            input.SetTools(tools);
            input.SetLocomotionFacingDuringPointerInput(true);
            input.ReadDevices = false;
            Physics.SyncTransforms();
            yield return null;

            Vector3 cursorLeft = player.transform.position + Vector3.left * 4f;
            for (int i = 0; i < 12; i++) { input.TickPointerCommand(Vector2.up, cursorLeft, false, false, false, false, false, false, Dt); yield return null; }
            Check(Faces(player.transform, Vector3.forward), checks, "Walking faces screen-forward movement rather than the cursor");
            yield return MoveAndFace(input, Vector2.left, cursorLeft, 12);
            Check(Faces(player.transform, Vector3.left), checks, "Walking left faces screen-left movement");
            yield return MoveAndFace(input, Vector2.down, cursorLeft, 12);
            Check(Faces(player.transform, Vector3.back), checks, "Walking down faces screen-back movement");
            yield return MoveAndFace(input, Vector2.right, cursorLeft, 12);
            Check(Faces(player.transform, Vector3.right), checks, "Walking right faces screen-right movement");
            yield return MoveAndFace(input, Vector2.up, cursorLeft, 12);
            cursorLeft = player.transform.position + Vector3.left * 4f;
            Quaternion idleHeading = player.transform.rotation;
            input.TickPointerCommand(Vector2.zero, cursorLeft, false, false, false, false, false, false, Dt);
            yield return null;
            Check(Quaternion.Angle(idleHeading, player.transform.rotation) < .1f, checks, "Idle preserves the previous locomotion heading");

            // First left click equips only; it must still follow movement.
            // Keep a held equip input for several frames: it must not turn toward the cursor.
            for (int i = 0; i < 8; i++) { input.TickPointerCommand(Vector2.up, cursorLeft, false, false, i == 0, true, false, false, Dt); yield return null; }
            Check(Faces(player.transform, Vector3.forward) && !tools.IsSwinging, checks, "Axe equip click does not cursor-aim or swing");
            cursorLeft = player.transform.position + Vector3.left * 4f;
            input.TickPointerCommand(Vector2.zero, cursorLeft, false, false, true, true, false, false, Dt);
            yield return null;
            Check(tools.IsSwinging && Faces(player.transform, Vector3.left), checks, "Active axe stroke cursor-aims before its trace");
            input.TickPointerCommand(Vector2.zero, cursorLeft, false, false, false, false, false, false, Dt);
            yield return null;
            Check(Faces(player.transform, Vector3.left), checks, "Released axe stroke keeps cursor heading until it ends");
            for (int i = 0; i < 20; i++) { input.TickPointerCommand(Vector2.zero, cursorLeft, false, false, false, false, false, false, Dt); yield return null; }

            // First right click equips, second connects, third sprays. Only the third cursor-aims.
            for (int i = 0; i < 8; i++) { input.TickPointerCommand(Vector2.up, cursorLeft, false, false, false, false, i == 0, true, Dt); yield return null; }
            Check(Faces(player.transform, Vector3.forward) && tools.SelectedTool == FirefighterTools.Tool.Hose, checks, "Hose equip click follows movement rather than cursor aim");
            sourceObject.transform.position = player.transform.position + Vector3.right * .2f;
            Physics.SyncTransforms();
            for (int i = 0; i < 8; i++) { input.TickPointerCommand(Vector2.up, cursorLeft, false, false, false, false, i == 0, true, Dt); yield return null; }
            Check(hose.IsConnected && Faces(player.transform, Vector3.forward), checks, "Hose connect click follows movement rather than cursor aim");
            cursorLeft = player.transform.position + Vector3.left * 4f;
            input.TickPointerCommand(Vector2.zero, cursorLeft, false, false, false, false, true, true, Dt);
            yield return null;
            Check(Faces(player.transform, Vector3.left) && emitter.ActiveParticleCount > 0 &&
                Vector3.Dot(emitter.GetParticle(0).Velocity.normalized, Vector3.left) > .97f, checks,
                "Actual hose spray cursor-aims before its first directional packet");
            yield return MoveAndFace(input, Vector2.right, cursorLeft, 8);
            Check(Faces(player.transform, Vector3.right), checks, "After spray stops, movement retakes facing ownership");

            input.SetLocomotionFacingDuringPointerInput(false);
            for (int i = 0; i < 12; i++) { input.TickPointerCommand(Vector2.up, player.transform.position + Vector3.left * 4, false, false, false, false, false, false, Dt); yield return null; }
            Check(Faces(player.transform, Vector3.left), checks, "Default compatibility mode retains legacy cursor point aim");
            input.SetLocomotionFacingDuringPointerInput(true);
            yield return MoveAndFace(input, Vector2.up, cursorLeft, 12);
            for (int i = 0; i < 12; i++) { input.TickCommand(Vector2.right, player.transform.position + Vector3.left * 4, false, false, false, Dt); yield return null; }
            Check(Faces(player.transform, Vector3.left), checks, "Legacy TickCommand retains cursor aim even when tutorial facing is enabled");
            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(cameraObject);
            UnityEngine.Object.Destroy(profile);
            yield return null;
        }

        static IEnumerator MoveAndFace(LocalPlayerInput input, Vector2 movement, Vector3 aim, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                input.TickPointerCommand(movement, aim, false, false, false, false, false, false, Dt);
                yield return null;
            }
        }

        static bool Faces(Transform transform, Vector3 direction)
        {
            direction.y = 0;
            return Vector3.Dot(transform.forward.normalized, direction.normalized) > .97f;
        }

        static void Check(bool condition, List<string> checks, string label)
        {
            if (!condition) throw new InvalidOperationException("Checks before failure:\n" + string.Join("\n", checks) + "\nFAIL " + label);
            checks.Add("PASS " + label);
        }
    }
}
