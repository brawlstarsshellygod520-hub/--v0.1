using UnityEngine;

namespace FireRescue
{
    /// <summary>How the first-rescue water packets are presented. This never owns fluid simulation.</summary>
    public enum FirstRescueEffectMode
    {
        BillboardParticles,
        DropletMeshes,
        Hybrid
    }

    /// <summary>
    /// Bounded presentation for the toilet-water rescue segment. It observes SprayEmitter and
    /// FireField only: changing, disabling, or removing this component cannot change gameplay.
    /// </summary>
    public sealed class FirstRescueEffects : MonoBehaviour
    {
        const int MaxBillboardCapacity = 500;
        const int DefaultBillboardCapacity = 320;
        const int DefaultDropletCapacity = 64;
        const int DefaultSplashCapacity = 32;
        const int DefaultFireCapacity = 48;

        [Header("Sources (optional until Initialize is called)")]
        [SerializeField] SprayEmitter emitter;
        [SerializeField] FireField field;

        [Header("Presentation")]
        [SerializeField] FirstRescueEffectMode mode = FirstRescueEffectMode.Hybrid;
        [SerializeField, Range(1, MaxBillboardCapacity)] int billboardCapacity = DefaultBillboardCapacity;
        [SerializeField, Range(1, 64)] int dropletCapacity = DefaultDropletCapacity;
        [SerializeField, Range(1, 32)] int splashCapacity = DefaultSplashCapacity;
        [SerializeField, Range(1, 512)] int fireCapacity = DefaultFireCapacity;
        [SerializeField] bool showFire = true;
        [SerializeField] bool spaciousFire;
        [SerializeField, Range(1f,3f)] float waterVisualScale=1f;
        [SerializeField, Min(.1f)] float wetMarkLifetime = 2.4f;

        ParticleSystem waterParticles;
        ParticleSystem.Particle[] particleBuffer;
        Transform[] droplets;
        Renderer[] dropletRenderers;
        ImpactVisual[] impacts;
        FireVisual[] fires;
        Material waterMaterial;
        Material wetMaterial;
        Material orangeFireMaterial;
        Material yellowFireMaterial;
        Material billboardMaterial;
        Texture2D billboardTexture;
        Mesh flameMesh;
        Mesh heatMesh;
        Material heatMaterial;
        bool subscribed;
        int nextImpact;

        public FirstRescueEffectMode Mode
        {
            get => mode;
            set => mode = value;
        }

        public int ActiveBillboardCount { get; private set; }
        public int ActiveDropletCount { get; private set; }
        public int ActiveSplashCount { get; private set; }
        public int ActiveFireVisualCount { get; private set; }
        public int ActiveFlameCount { get; private set; }
        public int ActiveHeatMarkCount { get; private set; }
        public float WaterVisualScale=>waterVisualScale;
        public bool IsSubscribed => subscribed;

        /// <summary>Idempotent explicit wiring for scene setup code and replaceable level assets.</summary>
        public void Initialize(SprayEmitter source, FireField fireField)
        {
            Unsubscribe();
            emitter = source;
            field = fireField;
            Subscribe();
        }

        void Awake()
        {
            EnsurePresentation();
        }

        void OnEnable()
        {
            EnsurePresentation();
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
            ClearVisuals();
        }

        void OnDestroy()
        {
            Unsubscribe();
            if (waterMaterial) Destroy(waterMaterial);
            if (wetMaterial) Destroy(wetMaterial);
            if (orangeFireMaterial) Destroy(orangeFireMaterial);
            if (yellowFireMaterial) Destroy(yellowFireMaterial);
            if (billboardMaterial) Destroy(billboardMaterial);
            if (billboardTexture) Destroy(billboardTexture);
            if (flameMesh) Destroy(flameMesh);
            if (heatMesh) Destroy(heatMesh);
            if (heatMaterial) Destroy(heatMaterial);
        }

        void LateUpdate()
        {
            if (!isActiveAndEnabled) return;
            EnsurePresentation();
            UpdateWater();
            UpdateImpacts(Time.deltaTime);
            UpdateFire();
        }

        void Subscribe()
        {
            if (subscribed || !emitter) return;
            emitter.ParticleImpacted += OnParticleImpacted;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed) return;
            if (emitter) emitter.ParticleImpacted -= OnParticleImpacted;
            subscribed = false;
        }

        // This event is notification-only. FireField and ElementReaction remain the gameplay writers.
        void OnParticleImpacted(SprayEmitter.ParticleImpact impact)
        {
            if (!isActiveAndEnabled || impact.Element != ElementKind.Water || impacts == null || impacts.Length == 0) return;
            ImpactVisual visual = impacts[nextImpact];
            nextImpact = (nextImpact + 1) % impacts.Length;
            visual.Age = 0f;
            visual.Lifetime = Mathf.Max(.15f, wetMarkLifetime);
            if (field && impact.Normal.y > .7f && field.TryGetCell(impact.Position, out var cell))
                visual.Lifetime = Mathf.Max(.15f, field.GetCell(cell.x, cell.y).WaterRemaining);
            visual.Position = impact.Position + impact.Normal * .008f;
            visual.Normal = impact.Normal.sqrMagnitude > .001f ? impact.Normal.normalized : Vector3.up;
            visual.Amount = Mathf.Clamp(impact.Amount * 4f, .35f, 1.25f);
            visual.Active = true;
        }

        void UpdateWater()
        {
            int count = emitter ? Mathf.Min(emitter.ActiveParticleCount, particleBuffer.Length) : 0;
            bool billboards = mode == FirstRescueEffectMode.BillboardParticles || mode == FirstRescueEffectMode.Hybrid;
            bool meshDrops = mode == FirstRescueEffectMode.DropletMeshes || mode == FirstRescueEffectMode.Hybrid;

            ActiveBillboardCount = 0;
            if (billboards && emitter)
            {
                for (int i = 0; i < count; i++)
                {
                    SprayEmitter.ParticleSample sample = emitter.GetParticle(i);
                    if (sample.Element != ElementKind.Water) continue;
                    particleBuffer[ActiveBillboardCount++] = new ParticleSystem.Particle
                    {
                        position = sample.Position,
                        startColor = new Color(.57f, .88f, 1f, .92f),
                        startSize = Mathf.Max(.06f, sample.Radius * 1.8f)*waterVisualScale,
                        remainingLifetime = 1f,
                        startLifetime = 1f,
                        velocity = sample.Velocity
                    };
                }
            }
            waterParticles.SetParticles(particleBuffer, ActiveBillboardCount);

            ActiveDropletCount = 0;
            if (meshDrops && emitter)
            {
                int step = Mathf.Max(1, Mathf.CeilToInt((float)Mathf.Max(1, count) / droplets.Length));
                for (int i = 0; i < count && ActiveDropletCount < droplets.Length; i += step)
                {
                    SprayEmitter.ParticleSample sample = emitter.GetParticle(i);
                    if (sample.Element != ElementKind.Water) continue;
                    Transform drop = droplets[ActiveDropletCount];
                    Vector3 velocity = sample.Velocity;
                    drop.position = sample.Position;
                    if (velocity.sqrMagnitude > .0001f) drop.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
                    float radius = Mathf.Max(.035f, sample.Radius * 1.15f)*waterVisualScale;
                    drop.localScale = new Vector3(radius, radius, Mathf.Max(radius * 1.8f, velocity.magnitude * .025f));
                    dropletRenderers[ActiveDropletCount].enabled = true;
                    ActiveDropletCount++;
                }
            }
            for (int i = ActiveDropletCount; i < dropletRenderers.Length; i++) dropletRenderers[i].enabled = false;
        }

        void UpdateImpacts(float dt)
        {
            ActiveSplashCount = 0;
            for (int i = 0; i < impacts.Length; i++)
            {
                ImpactVisual visual = impacts[i];
                if (!visual.Active) continue;
                visual.Age += dt;
                if (visual.Age >= visual.Lifetime)
                {
                    visual.Active = false;
                    visual.SplashRenderer.enabled = false;
                    visual.WetRenderer.enabled = false;
                    continue;
                }

                ActiveSplashCount++;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, visual.Normal);
                float normalized = visual.Age / visual.Lifetime;
                float wetScale = Mathf.Lerp(.1f, .5f, Mathf.Clamp01(normalized * 2f)) * visual.Amount;
                visual.Wet.position = visual.Position;
                visual.Wet.rotation = rotation;
                visual.Wet.localScale = new Vector3(wetScale, .008f, wetScale);
                visual.WetRenderer.enabled = true;

                bool splashVisible = visual.Age < .24f;
                visual.SplashRenderer.enabled = splashVisible;
                if (splashVisible)
                {
                    float splashScale = Mathf.Lerp(.06f, .24f, visual.Age / .24f) * visual.Amount;
                    visual.Splash.position = visual.Position + visual.Normal * .018f;
                    visual.Splash.rotation = rotation;
                    visual.Splash.localScale = new Vector3(splashScale, .03f, splashScale);
                }
            }
        }

        void UpdateFire()
        {
            ActiveFireVisualCount = ActiveFlameCount = ActiveHeatMarkCount = 0;
            if (!showFire || !field)
            {
                HideFires(0);
                return;
            }

            for (int z = 0; z < field.Height && ActiveFireVisualCount < fires.Length; z++)
            {
                for (int x = 0; x < field.Width && ActiveFireVisualCount < fires.Length; x++)
                {
                    FireCellState state = field.GetCell(x, z);
                    if (!state.IsBurning) continue;
                    FireVisual visual = fires[ActiveFireVisualCount++];
                    float strength = Mathf.Clamp01(state.Intensity);
                    // Presentation-only movement: FireField intensity remains the sole flame authority.
                    float phase = Time.time * (5.5f + strength * 2f) + visual.Seed;
                    float flicker = 1f + Mathf.Sin(phase) * .09f + Mathf.Sin(phase * 1.73f) * .055f;
                    float height = (.18f + strength * (spaciousFire ? .25f : .48f)) * flicker;
                    Vector3 center = field.CellCenter(x, z);
                    Vector3 dance = new Vector3(Mathf.Sin(phase * 1.31f), 0f, Mathf.Cos(phase * 1.67f)) * (.018f + strength * .022f);
                    // Flame mesh is authored from y=0 upward, so its wide base remains on the floor.
                    visual.Outer.position = center + dance;
                    float width = field.CellSize * (spaciousFire ? .32f + strength * .06f : .55f + strength * .35f) * flicker;
                    visual.Outer.localScale = new Vector3(width, height, width);
                    // Opaque nested meshes hide their own core. Offset it toward the fixed gameplay camera.
                    visual.Inner.position = center + dance * .55f + new Vector3(field.CellSize * .035f, .025f, -width * .8f);
                    visual.Inner.localScale = visual.Outer.localScale * .56f;
                    // Keep isolated/small fronts visible; dense interiors use stable staggered thirds,
                    // not random per-frame popping. Every live cell retains a warm floor footprint.
                    bool hasNeighbor = Burning(x-1,z)||Burning(x+1,z)||Burning(x,z-1)||Burning(x,z+1);
                    bool flame = !spaciousFire || (x+z*2)%3==0 || !hasNeighbor;
                    visual.OuterRenderer.enabled = flame;
                    visual.InnerRenderer.enabled = flame;
                    if(flame)ActiveFlameCount++;
                    if(spaciousFire)
                    {
                        visual.Heat.position=center+Vector3.up*.035f;
                        visual.Heat.localScale=Vector3.one*field.CellSize;
                        visual.HeatRenderer.enabled=true;ActiveHeatMarkCount++;
                    }
                    else visual.HeatRenderer.enabled=false;
                }
            }
            HideFires(ActiveFireVisualCount);
        }

        bool Burning(int x,int z)=>x>=0&&z>=0&&x<field.Width&&z<field.Height&&field.GetCell(x,z).IsBurning;

        void HideFires(int from)
        {
            for (int i = from; i < fires.Length; i++)
            {
                fires[i].OuterRenderer.enabled = false;
                fires[i].InnerRenderer.enabled = false;
                fires[i].HeatRenderer.enabled = false;
            }
        }

        void ClearVisuals()
        {
            ActiveBillboardCount = ActiveDropletCount = ActiveSplashCount = ActiveFireVisualCount = ActiveFlameCount = ActiveHeatMarkCount = 0;
            if (waterParticles) waterParticles.Clear(true);
            if (dropletRenderers != null) for (int i = 0; i < dropletRenderers.Length; i++) dropletRenderers[i].enabled = false;
            if (impacts != null) for (int i = 0; i < impacts.Length; i++)
            {
                impacts[i].Active = false;
                impacts[i].SplashRenderer.enabled = false;
                impacts[i].WetRenderer.enabled = false;
            }
            if (fires != null) HideFires(0);
        }

        void EnsurePresentation()
        {
            if (particleBuffer != null) return;
            billboardCapacity = Mathf.Clamp(billboardCapacity, 1, MaxBillboardCapacity);
            dropletCapacity = Mathf.Clamp(dropletCapacity, 1, 64);
            splashCapacity = Mathf.Clamp(splashCapacity, 1, 32);
            fireCapacity = Mathf.Clamp(fireCapacity, 1, 512);
            CreateMaterials();
            flameMesh = CreateFlameMesh();
            heatMaterial=new Material(Shader.Find("Sprites/Default")){color=new Color(1f,.23f,.025f,.18f)};
            heatMesh=new Mesh{name="Fire danger footprint"};heatMesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)};
            heatMesh.triangles=new[]{0,1,2,0,2,3};heatMesh.RecalculateNormals();heatMesh.RecalculateBounds();
            particleBuffer = new ParticleSystem.Particle[billboardCapacity];
            waterParticles = CreateWaterParticles();
            droplets = new Transform[dropletCapacity];
            dropletRenderers = new Renderer[dropletCapacity];
            for (int i = 0; i < droplets.Length; i++) CreateDrop(i);
            impacts = new ImpactVisual[splashCapacity];
            for (int i = 0; i < impacts.Length; i++) impacts[i] = CreateImpact(i);
            fires = new FireVisual[fireCapacity];
            for (int i = 0; i < fires.Length; i++) fires[i] = CreateFire(i);
        }

        void CreateMaterials()
        {
            waterMaterial = CreateStandardMaterial(new Color(.25f, .72f, 1f, 1f), true);
            wetMaterial = CreateStandardMaterial(new Color(.08f, .34f, .62f, 1f), false);
            orangeFireMaterial = CreateStandardMaterial(new Color(1f, .18f, .015f, 1f), true);
            yellowFireMaterial = CreateStandardMaterial(new Color(1f, .74f, .04f, 1f), true);
        }

        static Material CreateStandardMaterial(Color color, bool emission)
        {
            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = color };
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * .7f);
            }
            return material;
        }

        ParticleSystem CreateWaterParticles()
        {
            GameObject root = new GameObject("Water Billboards");
            root.transform.SetParent(transform, false);
            ParticleSystem system = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = billboardCapacity;
            main.simulationSpeed = 0f;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.25f;
            renderer.velocityScale = .035f;
            billboardTexture = CreateRadialTexture();
            billboardMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = billboardTexture };
            renderer.sharedMaterial = billboardMaterial;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play();
            return system;
        }

        void CreateDrop(int index)
        {
            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drop.name = "Water Droplet " + index;
            drop.transform.SetParent(transform, false);
            drop.GetComponent<Collider>().enabled = false;
            Object.Destroy(drop.GetComponent<Collider>());
            Renderer renderer = drop.GetComponent<Renderer>();
            renderer.sharedMaterial = waterMaterial;
            renderer.enabled = false;
            droplets[index] = drop.transform;
            dropletRenderers[index] = renderer;
        }

        ImpactVisual CreateImpact(int index)
        {
            GameObject root = new GameObject("Water Impact " + index);
            root.transform.SetParent(transform, false);
            Transform wet = CreateVisualPrimitive(root.transform, "Wet Mark", PrimitiveType.Sphere, wetMaterial, out Renderer wetRenderer);
            Transform splash = CreateVisualPrimitive(root.transform, "Splash", PrimitiveType.Sphere, waterMaterial, out Renderer splashRenderer);
            wetRenderer.enabled = false;
            splashRenderer.enabled = false;
            return new ImpactVisual { Wet = wet, Splash = splash, WetRenderer = wetRenderer, SplashRenderer = splashRenderer };
        }

        FireVisual CreateFire(int index)
        {
            GameObject root = new GameObject("Fire Cluster " + index);
            root.transform.SetParent(transform, false);
            Transform outer = CreateFlameVisual(root.transform, "Orange Flame", orangeFireMaterial, out Renderer outerRenderer);
            Transform inner = CreateFlameVisual(root.transform, "Yellow Flame", yellowFireMaterial, out Renderer innerRenderer);
            outerRenderer.enabled = false;
            innerRenderer.enabled = false;
            var heat=new GameObject("Burning floor footprint");heat.transform.SetParent(root.transform,false);heat.AddComponent<MeshFilter>().sharedMesh=heatMesh;
            var hr=heat.AddComponent<MeshRenderer>();hr.sharedMaterial=heatMaterial;hr.enabled=false;hr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;hr.receiveShadows=false;
            return new FireVisual { Outer = outer, Inner = inner, OuterRenderer = outerRenderer, InnerRenderer = innerRenderer, Heat=heat.transform,HeatRenderer=hr, Seed = index * 1.618f };
        }

        Transform CreateFlameVisual(Transform parent, string name, Material material, out Renderer renderer)
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(parent, false);
            MeshFilter filter = visual.AddComponent<MeshFilter>();
            filter.sharedMesh = flameMesh;
            MeshRenderer meshRenderer = visual.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            renderer = meshRenderer;
            return visual.transform;
        }

        static Transform CreateVisualPrimitive(Transform parent, string name, PrimitiveType type, Material material, out Renderer renderer)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.GetComponent<Collider>().enabled = false;
            Object.Destroy(visual.GetComponent<Collider>());
            renderer = visual.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return visual.transform;
        }

        static Texture2D CreateRadialTexture()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "FirstRescueWaterDrop",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size * 2f - 1f;
                float v = (y + .5f) / size * 2f - 1f;
                float distance = Mathf.Sqrt(u * u + v * v);
                float alpha = 1f - Mathf.SmoothStep(.45f, 1f, distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply(false, true);
            return texture;
        }

        // One low-poly asymmetric teardrop shared by every orange/yellow fire renderer.
        // Its base lies on y=0; the tip is intentionally offset so it reads as a flame, not a sphere.
        static Mesh CreateFlameMesh()
        {
            const int sides = 6;
            Vector3[] vertices = new Vector3[sides * 2 + 1];
            int[] triangles = new int[sides * 9];
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                float x = Mathf.Cos(angle);
                float z = Mathf.Sin(angle);
                vertices[i] = new Vector3(x, 0f, z);
                vertices[sides + i] = new Vector3(x * .72f, .43f, z * .72f);
            }
            vertices[sides * 2] = new Vector3(.24f, 1f, -.1f);

            int write = 0;
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                triangles[write++] = i;
                triangles[write++] = sides + next;
                triangles[write++] = next;
                triangles[write++] = i;
                triangles[write++] = sides + i;
                triangles[write++] = sides + next;
                triangles[write++] = sides + i;
                triangles[write++] = sides * 2;
                triangles[write++] = sides + next;
            }
            Mesh mesh = new Mesh { name = "FirstRescueLowPolyFlame" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        sealed class ImpactVisual
        {
            public Transform Wet;
            public Transform Splash;
            public Renderer WetRenderer;
            public Renderer SplashRenderer;
            public Vector3 Position;
            public Vector3 Normal;
            public float Amount;
            public float Age;
            public float Lifetime;
            public bool Active;
        }

        sealed class FireVisual
        {
            public Transform Outer;
            public Transform Inner;
            public Renderer OuterRenderer;
            public Renderer InnerRenderer;
            public Transform Heat;
            public Renderer HeatRenderer;
            public float Seed;
        }
    }
}
