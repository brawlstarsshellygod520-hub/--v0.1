using System;
using System.Collections.Generic;
using UnityEngine;

namespace FireRescue
{
    public sealed class RescueGameManager : MonoBehaviour
    {
        [SerializeField] FireField field;
        [SerializeField] RescueNpc[] survivors;
        [SerializeField] ObjectFire[] objectFires = Array.Empty<ObjectFire>();

        // This snapshot is deliberately separate from the serialized list.  It prevents a
        // duplicate inspector reference from changing completion state or adding the same
        // rescue callback more than once.
        readonly List<RescueNpc> subscribedSurvivors = new List<RescueNpc>();
        readonly List<ObjectFire> uniqueObjectFires = new List<ObjectFire>();
        bool objectFireCacheValid;

        public int RescuedCount { get; private set; }
        public bool RescueComplete { get; private set; }
        public bool AllFiresOut
        {
            get
            {
                if (!field || field.BurningCount != 0)
                    return false;

                EnsureObjectFireCache();
                for (int i = 0; i < uniqueObjectFires.Count; i++)
                {
                    ObjectFire fire = uniqueObjectFires[i];
                    // An explicitly configured but disabled ObjectFire is not part of the
                    // live level. Disabling a renderer or a separate visual component does
                    // not affect this check: the ObjectFire itself remains active/enabled.
                    if (fire && fire.isActiveAndEnabled && fire.IsBurning)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// A read-only whole-level observation. Rescue completion itself intentionally does
        /// not require every fire to be out, so evacuation can remain its own gameplay goal.
        /// </summary>
        public bool LevelComplete => RescueComplete && AllFiresOut;

        public event Action RescueCompleted;

        /// <summary>Supplies the floor-fire and residents while preserving the original API.</summary>
        public void Initialize(FireField fireField, RescueNpc[] npcs)
        {
            field = fireField;
            survivors = npcs ?? Array.Empty<RescueNpc>();
            RefreshSurvivorSubscriptions();
        }

        /// <summary>
        /// Supplies the complete authored fire set. Only this explicit set is counted: the
        /// manager deliberately does not search the scene for ObjectFire components.
        /// </summary>
        public void Initialize(FireField fireField, RescueNpc[] npcs, ObjectFire[] fires)
        {
            Initialize(fireField, npcs);
            SetObjectFires(fires);
        }

        /// <summary>
        /// Replaces the explicit object-fire set. Null and duplicate entries are ignored;
        /// inactive or disabled ObjectFire components are excluded from live fire state.
        /// </summary>
        public void SetObjectFires(ObjectFire[] fires)
        {
            objectFires = fires ?? Array.Empty<ObjectFire>();
            RebuildObjectFireCache();
        }

        void Awake()
        {
            RebuildObjectFireCache();
        }

        void OnEnable()
        {
            RefreshSurvivorSubscriptions();
        }

        void OnDisable()
        {
            UnsubscribeSurvivors();
        }

        void OnValidate()
        {
            RebuildObjectFireCache();
        }

        void OnRescued(RescueNpc survivor)
        {
            RescuedCount = CountRescuedSurvivors(out int survivorCount);
            if (!RescueComplete && survivorCount > 0 && RescuedCount == survivorCount)
            {
                RescueComplete = true;
                Debug.Log("RESCUE_COMPLETE: 所有 NPC 已自行抵達出口。可繼續測試水與油；停止再 Play 重開。");
                RescueCompleted?.Invoke();
            }
        }

        private void RefreshSurvivorSubscriptions()
        {
            UnsubscribeSurvivors();
            if (!isActiveAndEnabled || survivors == null)
                return;

            for (int i = 0; i < survivors.Length; i++)
            {
                RescueNpc npc = survivors[i];
                if (!npc || subscribedSurvivors.Contains(npc))
                    continue;

                npc.Rescued += OnRescued;
                subscribedSurvivors.Add(npc);
            }
        }

        private void UnsubscribeSurvivors()
        {
            for (int i = 0; i < subscribedSurvivors.Count; i++)
            {
                RescueNpc npc = subscribedSurvivors[i];
                if (npc)
                    npc.Rescued -= OnRescued;
            }

            subscribedSurvivors.Clear();
        }

        private int CountRescuedSurvivors(out int survivorCount)
        {
            survivorCount = 0;
            int rescuedCount = 0;
            if (survivors == null)
                return rescuedCount;

            var seen = new HashSet<RescueNpc>();
            for (int i = 0; i < survivors.Length; i++)
            {
                RescueNpc npc = survivors[i];
                if (!npc || !seen.Add(npc))
                    continue;

                survivorCount++;
                if (npc.IsRescued)
                    rescuedCount++;
            }

            return rescuedCount;
        }

        private void EnsureObjectFireCache()
        {
            // Serialized arrays can be changed by an editor or scene deserialization without
            // using SetObjectFires, so build lazily before the public status is observed.
            if (!objectFireCacheValid)
                RebuildObjectFireCache();
        }

        private void RebuildObjectFireCache()
        {
            objectFireCacheValid = true;
            uniqueObjectFires.Clear();
            if (objectFires == null)
                return;

            var seen = new HashSet<ObjectFire>();
            for (int i = 0; i < objectFires.Length; i++)
            {
                ObjectFire fire = objectFires[i];
                if (fire && seen.Add(fire))
                    uniqueObjectFires.Add(fire);
            }
        }
    }
}
