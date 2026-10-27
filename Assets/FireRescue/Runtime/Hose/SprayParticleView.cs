using UnityEngine;

namespace FireRescue
{
    // This component only renders snapshots. ParticleSystem collisions/emission never apply gameplay effects.
    public sealed class SprayParticleView : MonoBehaviour
    {
        [SerializeField] SprayEmitter emitter;
        ParticleSystem particles;
        ParticleSystem.Particle[] buffer;
        Material material;
        Texture2D texture;
        public void Initialize(SprayEmitter value) => emitter = value;

        void Awake()
        {
            var root = new GameObject("Directional spray particles"); root.transform.SetParent(transform, false);
            particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false; main.loop = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = SprayEmitter.ParticleCapacity; main.simulationSpeed = 0;
            var emission = particles.emission; emission.enabled = false;
            var shape = particles.shape; shape.enabled = false;
            var collision = particles.collision; collision.enabled = false;
            buffer = new ParticleSystem.Particle[SprayEmitter.ParticleCapacity];
            texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                float distance = new Vector2((x - 7.5f) / 7.5f, (y - 7.5f) / 7.5f).magnitude;
                texture.SetPixel(x, y, new Color(1, 1, 1, 1f - Mathf.SmoothStep(.65f, 1f, distance)));
            }
            texture.Apply();
            material = new Material(Shader.Find("Sprites/Default")) { mainTexture = texture };
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material; renderer.renderMode = ParticleSystemRenderMode.Billboard;
            particles.Play();
        }

        void LateUpdate()
        {
            if (!particles) return;
            int count = emitter && emitter.UsesDirectionalSpray ? emitter.ActiveParticleCount : 0;
            for (int i = 0; i < count; i++)
            {
                var sample = emitter.GetParticle(i);
                bool powder = sample.Element == ElementKind.DryPowder;
                Color color = sample.Element == ElementKind.Water ? new Color(.22f, .75f, 1f, .92f) :
                    powder ? new Color(1f, .97f, .8f, .68f) : new Color(.9f, .55f, .08f, .95f);
                buffer[i] = new ParticleSystem.Particle
                {
                    position = sample.Position, velocity = Vector3.zero, startColor = color,
                    startSize = sample.Radius * (powder ? Mathf.Lerp(2f, 4f, sample.NormalizedAge) : 2.2f),
                    startLifetime = 1, remainingLifetime = 1
                };
            }
            particles.SetParticles(buffer, count);
        }
        void OnDisable() { if (particles) particles.Clear(); }
        void OnDestroy() { if (material) Destroy(material); if (texture) Destroy(texture); }
    }
}
