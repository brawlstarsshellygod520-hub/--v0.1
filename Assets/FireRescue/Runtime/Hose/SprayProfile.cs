using System;
using UnityEngine;

namespace FireRescue
{
    [Serializable]
    public struct SprayEffectSettings
    {
        [Range(0f, 4f), Tooltip("滅火力倍率；不改流量或濕度，也不讓油變成滅火劑。")]
        public float ExtinguishMultiplier;
        [Range(0f, 4f), Tooltip("水對玩家／動態物件的推力倍率；油與乾粉仍不推物。")]
        public float PushMultiplier;
        [Range(0f, 4f), Tooltip("每次命中的濕度／油量／粉末量倍率，不改殘留秒數。0不新增或刷新殘留。")]
        public float ResidueMultiplier;

        public static SprayEffectSettings Neutral => new SprayEffectSettings
            { ExtinguishMultiplier = 1f, PushMultiplier = 1f, ResidueMultiplier = 1f };
        public SprayEffectSettings Validated() => new SprayEffectSettings
        {
            ExtinguishMultiplier = Safe(ExtinguishMultiplier),
            PushMultiplier = Safe(PushMultiplier), ResidueMultiplier = Safe(ResidueMultiplier)
        };
        static float Safe(float v) => Mathf.Clamp(float.IsNaN(v) || float.IsInfinity(v) ? 1f : v, 0f, 4f);
    }

    [Serializable]
    public struct SpraySettings
    {
        [Min(.1f), Tooltip("作者曲線模式的最近落點（m）；物理模式改由初速、發射角、重力決定。")]
        public float MinimumRange;
        [Min(.1f), Tooltip("作者曲線模式的最遠落點（m）；物理模式改由初速、發射角、重力決定。")]
        public float MaximumRange;
        [Min(.05f), Tooltip("最遠噴射物的空中存活秒數；撞到物件會提早結束。")]
        public float AirLifetime;
        [Min(0f), Tooltip("落地殘留秒數；再次噴到會刷新，油也可能提早燒完。")]
        public float GroundLifetime;
        [Range(0f, 60f), Tooltip("水平擴散的完整角度。")]
        public float SpreadAngle;
        [Min(.01f)] public float CollisionRadius;
        [Min(.05f)] public float ImpactRadius;
        [Min(0f), Tooltip("拋物線額外抬升（m）；0仍會自然下降至地面。")]
        public float ArcHeight;
        [Range(1f, 60f)] public float PacketsPerSecond;

        [Tooltip("以初速、重力與空氣阻力飛行；關閉時保留舊關卡的作者曲線。")]
        public bool PhysicalBallistics;
        [Min(.1f), Tooltip("物理模式的最高初速（m/s）。")]
        public float MuzzleSpeed;
        [Range(.1f,1f),Tooltip("最低水滴初速比例；缺省0沿用舊版65%。可用低速水滴補近端落點。")]
        public float MinimumSpeedFraction;
        [Range(.1f,1f),Tooltip("一般水滴的速度範圍上限比例；最遠一組仍用最高初速。缺省0沿用1。")]
        public float MainStreamSpeedFraction;
        [Range(-15f, 45f)] public float LaunchAngle;
        [Min(.1f)] public float Gravity;
        [Min(0f)] public float AirDrag;

        public SpraySettings Validated()
        {
            var s = this;
            s.MaximumRange = Safe(s.MaximumRange, .1f, 30f, 5.5f);
            s.MinimumRange = Safe(s.MinimumRange, .1f, s.MaximumRange, 1.5f);
            s.AirLifetime = Safe(s.AirLifetime, .05f, 3f, .65f);
            s.GroundLifetime = Safe(s.GroundLifetime, 0f, 120f, 6f);
            s.SpreadAngle = Safe(s.SpreadAngle, 0f, 60f, 4f);
            s.CollisionRadius = Safe(s.CollisionRadius, .01f, .5f, .07f);
            s.ImpactRadius = Safe(s.ImpactRadius, .05f, 3f, .85f);
            s.ArcHeight = Safe(s.ArcHeight, 0f, 3f, 0f);
            s.PacketsPerSecond = Safe(s.PacketsPerSecond, 1f, 60f, 40f);
            s.MuzzleSpeed = Safe(s.MuzzleSpeed <= 0 ? 12f : s.MuzzleSpeed, .1f, 30f, 12f);
            s.MinimumSpeedFraction = Safe(s.MinimumSpeedFraction<=0?.65f:s.MinimumSpeedFraction,.1f,1f,.65f);
            s.MainStreamSpeedFraction=Safe(s.MainStreamSpeedFraction<=0?1:s.MainStreamSpeedFraction,s.MinimumSpeedFraction,1,1);
            s.LaunchAngle = Safe(s.LaunchAngle, -15f, 45f, 3f);
            s.Gravity = Safe(s.Gravity <= 0 ? 9.81f : s.Gravity, .1f, 30f, 9.81f);
            s.AirDrag = Safe(s.AirDrag, 0f, 5f, .08f);
            return s;
        }

        static float Safe(float v, float min, float max, float fallback) =>
            Mathf.Clamp(float.IsNaN(v) || float.IsInfinity(v) ? fallback : v, min, max);

        public static SpraySettings DefaultFor(ElementKind kind)
        {
            var s = new SpraySettings { MinimumRange = 1.5f, MaximumRange = 5.5f, AirLifetime = .65f,
                GroundLifetime = 6f, SpreadAngle = 4f, CollisionRadius = .07f, ImpactRadius = .85f,
                ArcHeight = 0f, PacketsPerSecond = 40f,
                MuzzleSpeed = 12f, MinimumSpeedFraction=.65f, MainStreamSpeedFraction=1, LaunchAngle = 3f, Gravity = 9.81f, AirDrag = .08f };
            if (kind == ElementKind.Oil)
            {
                s.MinimumRange = .8f; s.MaximumRange = 3.2f; s.AirLifetime = 1.1f;
                s.GroundLifetime = 18f; s.SpreadAngle = 12f; s.CollisionRadius = .11f;
                s.ImpactRadius = .65f; s.ArcHeight = .12f; s.PacketsPerSecond = 24f;
            }
            else if (kind == ElementKind.DryPowder)
            {
                s.MinimumRange = 1f; s.MaximumRange = 3.5f; s.AirLifetime = .85f;
                s.GroundLifetime = 4f; s.SpreadAngle = 26f; s.CollisionRadius = .16f;
                s.ImpactRadius = 1.1f; s.ArcHeight = .3f; s.PacketsPerSecond = 36f;
            }
            return s;
        }
    }

    [CreateAssetMenu(menuName = "FireRescue/Spray Profile", fileName = "SprayProfile")]
    public sealed class SprayProfile : ScriptableObject
    {
        [SerializeField] SpraySettings settings = SpraySettings.DefaultFor(ElementKind.Water);
        // Separate initialized field: old profile assets missing this section keep neutral behavior.
        [SerializeField] SprayEffectSettings effects = SprayEffectSettings.Neutral;
        public SpraySettings Settings => settings.Validated();
        public SprayEffectSettings Effects => effects.Validated();
        public void Configure(SpraySettings value) => settings = value.Validated();
        public void ConfigureEffects(SprayEffectSettings value) => effects = value.Validated();
        void OnValidate() { settings = settings.Validated(); effects = effects.Validated(); }
    }
}
