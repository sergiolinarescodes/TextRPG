using System;
using System.Collections.Generic;
using System.Linq;
using PrimeTween;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.Luck;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Lockpick
{
    internal sealed class LockpickService : SystemServiceBase, ILockpickService
    {
        private static readonly string[] Messages =
        {
            "...", "*click*", "almost there...", "the pin catches...",
            "turning...", "*snap*", "nearly got it..."
        };

        private readonly IEntityStatsService _entityStats;
        private readonly ILuckService _luckService;
        private IEventEncounterService _encounterService;

        private bool _isLockpicking;
        public bool IsLockpicking => _isLockpicking;

        public LockpickService(IEventBus eventBus, IEntityStatsService entityStats,
            ILuckService luckService) : base(eventBus)
        {
            _entityStats = entityStats;
            _luckService = luckService;

            Subscribe<LockpickAttemptEvent>(OnLockpickAttempt);
        }

        internal void SetEncounterService(IEventEncounterService encounterService)
        {
            _encounterService = encounterService;
        }

        private void OnLockpickAttempt(LockpickAttemptEvent evt)
        {
            if (_isLockpicking) return;

            // Find a lockpickable target
            EntityId? lockpickTarget = null;
            if (_encounterService != null)
            {
                foreach (var target in evt.Targets)
                {
                    try
                    {
                        var def = _encounterService.GetEntityDefinition(target);
                        if (def.Tags != null && Array.Exists(def.Tags,
                            t => string.Equals(t, "lockpickable", StringComparison.OrdinalIgnoreCase)))
                        {
                            lockpickTarget = target;
                            break;
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        // Not an interactable entity, skip
                    }
                }
            }

            if (!lockpickTarget.HasValue)
            {
                Publish(new InteractionMessageEvent("Nothing to lockpick here.", evt.Source));
                Publish(new ActionAnimationCompletedEvent("lockpick"));
                return;
            }

            _isLockpicking = true;
            var target_ = lockpickTarget.Value;
            var source = evt.Source;

            // Compute parameters based on Dexterity
            int dex = _entityStats.GetStat(source, StatType.Dexterity);
            int totalSteps = Math.Max(2, 5 - dex / 3);
            float baseChance = 0.4f + dex * 0.05f;
            float chance = _luckService.AdjustChance(baseChance, source, true);

            // Pick random messages (no repeats)
            var selectedMessages = PickRandomMessages(totalSteps);

            // Chain delayed messages
            RunLockpickSequence(source, target_, selectedMessages, chance, 0);
        }

        private void RunLockpickSequence(EntityId source, EntityId target,
            List<string> messages, float chance, int step)
        {
            if (step < messages.Count)
            {
                Tween.Delay(0.8f).OnComplete(() =>
                {
                    Publish(new InteractionMessageEvent(messages[step], target));
                    RunLockpickSequence(source, target, messages, chance, step + 1);
                });
            }
            else
            {
                // Final step: roll success
                Tween.Delay(0.8f).OnComplete(() =>
                {
                    bool success = UnityEngine.Random.value <= chance;

                    if (success)
                    {
                        Publish(new InteractionMessageEvent("The lock opens!", target));
                        // Trigger the Open reaction chain (same as typing "open")
                        Publish(new InteractionActionEvent(
                            source, "Open", new[] { target }, 0, "lockpick"));
                    }
                    else
                    {
                        Publish(new InteractionMessageEvent("The lock resists...", target));
                    }

                    Publish(new LockpickCompletedEvent(source, target, success));
                    _isLockpicking = false;
                    Publish(new ActionAnimationCompletedEvent("lockpick"));
                });
            }
        }

        private static List<string> PickRandomMessages(int count)
        {
            var pool = new List<string>(Messages);
            var result = new List<string>(count);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
