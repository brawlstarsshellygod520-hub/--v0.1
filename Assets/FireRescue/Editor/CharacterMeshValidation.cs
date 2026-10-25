using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // Mesh measurements complement, but never replace, the front/side/animated visual review.
    public static class CharacterMeshValidation
    {
        [MenuItem("消防 Prototype/檢查角色比例與腳底接觸")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var results = new List<string>();
            try
            {
                foreach (var name in new[] { "Firefighter", "Resident" }) Validate(name, results);
                Directory.CreateDirectory("Logs");
                bool success = results.All(line => !line.StartsWith("FAIL:", StringComparison.Ordinal));
                string header = success ? "CHARACTER_MESH_PASS" : "CHARACTER_MESH_FAIL";
                File.WriteAllLines("Logs/character-v3-mesh-results.txt", new[] { header }.Concat(results));
                if (!success) throw new InvalidOperationException("Mesh quality criteria failed; full measurements in Logs/character-v3-mesh-results.txt.");
                Debug.Log(header + "\n" + string.Join("\n", results));
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllLines("Logs/character-v3-mesh-results.txt", new[] { "CHARACTER_MESH_FAIL" }.Concat(results).Append(exception.Message));
                throw;
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            }
        }

        static void Validate(string name, List<string> results)
        {
            var visual = ResidentialArtCatalog.Visual(name, null);
            var animator = visual.GetComponentInChildren<Animator>();
            var skins = visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            var body = skins.Single(s => s.name == name + "_Body");
            var head = skins.Single(s => s.name == name + "_HeadMesh");
            var feet = skins.Where(s => s.name.Contains("_Boot_") || s.name.Contains("_Shoe_")).ToArray();
            var hipJoints = visual.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("UpperLeg.")).ToArray();
            Require(feet.Length == 2 && hipJoints.Length == 2, name + ": two feet and hip attachment joints", results);
            animator.Rebind();
            Sample(animator, "Idle", 0);
            var shells = skins.Where(s => s.name.Contains("HelmetCrown") || s.name.Contains("HelmetBrim") || s.name.Contains("HairCap")).ToArray();
            Require(shells.Length > 0 && shells.All(s => SignedVolume(s) > .00001f),
                name + ": closed hat/hair shells have outward face winding; no double-sided shader workaround", results);
            Bounds whole = BoundsOf(skins);
            float ratio = BoundsOf(new[] { head }).size.y / whole.size.y;
            Require(Mathf.Abs(whole.size.y - 1.6f) < .035f && ratio >= .33f && ratio <= .48f,
                name + ": baked height=" + whole.size.y.ToString("F3") + "m, head fraction=" + ratio.ToString("F3"), results);
            results.Add("MEASURE: " + name + " Idle surfaces: " + string.Join("; ", skins.Select(s =>
            {
                Bounds b = BoundsOf(new[] { s });
                return s.name + "=" + b.min.y.ToString("F4") + ".." + b.max.y.ToString("F4");
            })));
            results.Add("MEASURE: " + name + " renderer scales: " + string.Join("; ", skins.Select(s =>
                s.name + "=" + s.transform.lossyScale.ToString("F4"))));
            float idleGround = feet.Max(s => BoundsOf(new[] { s }).min.y);
            Require(Mathf.Abs(idleGround) <= .015f, name + ": Idle feet contact ground, upper min=" + idleGround.ToString("F4") + "m", results);
            int contacts = 0;
            float minY = float.PositiveInfinity;
            float maxAttachmentGap = 0;
            for (int frame = 0; frame < 24; frame++)
            {
                Sample(animator, "Run", frame / 24f);
                Bounds bodyBounds = BoundsOf(new[] { body });
                foreach (var joint in hipJoints)
                    maxAttachmentGap = Mathf.Max(maxAttachmentGap, bodyBounds.min.y - joint.position.y);
                minY = Mathf.Min(minY, BoundsOf(skins).min.y);
                float footY = feet.Min(s => BoundsOf(new[] { s }).min.y);
                if (footY <= .025f) contacts++;
            }
            Require(minY >= -.015f, name + ": Run baked meshes do not penetrate floor, min=" + minY.ToString("F4") + "m", results);
            Require(contacts >= 6, name + ": Run has visible ground contact in " + contacts + "/24 sampled poses", results);
            Require(maxAttachmentGap <= .005f, name + ": hip origins remain inside torso silhouette envelope, gap=" + maxAttachmentGap.ToString("F4") + "m", results);
            if (name == "Firefighter")
            {
                int bandTriangles = skins.Sum(s => Enumerable.Range(0, s.sharedMaterials.Length)
                    .Where(i => s.sharedMaterials[i].name == "Reflector")
                    .Sum(i => (int)s.sharedMesh.GetIndexCount(i) / 3));
                Require(bandTriangles >= 40, "Firefighter: reflector colour belongs to skinned surface triangles (" + bandTriangles + ")", results);
            }
            UnityEngine.Object.DestroyImmediate(visual);
        }

        static void Sample(Animator animator, string state, float normalizedTime)
        {
            animator.SetFloat("Speed", state == "Run" ? 1 : 0);
            animator.SetFloat("CycleSpeed", 1);
            animator.Play(state, 0, normalizedTime);
            animator.Update(0);
        }

        static float SignedVolume(SkinnedMeshRenderer skin)
        {
            var mesh = new Mesh();
            try
            {
                skin.BakeMesh(mesh, true);
                var vertices = mesh.vertices;
                var indices = mesh.triangles;
                // Translating close to the origin avoids cancellation at distant level positions.
                Vector3 origin = mesh.bounds.center;
                double volume = 0;
                for (int i = 0; i < indices.Length; i += 3)
                    volume += Vector3.Dot(vertices[indices[i]] - origin,
                        Vector3.Cross(vertices[indices[i + 1]] - origin, vertices[indices[i + 2]] - origin)) / 6.0;
                return (float)volume;
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }

        static Bounds BoundsOf(IEnumerable<SkinnedMeshRenderer> skins)
        {
            var baked = new Mesh();
            var bounds = new Bounds();
            bool first = true;
            try
            {
                foreach (var skin in skins)
                {
                    // Unity 6's false/default snapshot retains the renderer's scale.
                    // Compensate first, because TransformPoint below applies it to world space.
                    skin.BakeMesh(baked, true);
                    foreach (var vertex in baked.vertices)
                    {
                        Vector3 world = skin.transform.TransformPoint(vertex);
                        if (first) { bounds = new Bounds(world, Vector3.zero); first = false; }
                        else bounds.Encapsulate(world);
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
            return bounds;
        }

        static void Require(bool condition, string detail, List<string> results)
        {
            results.Add((condition ? "PASS: " : "FAIL: ") + detail);
        }
    }
}
