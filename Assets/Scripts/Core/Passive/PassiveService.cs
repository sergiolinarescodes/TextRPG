using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.Passive
{
    internal sealed class PassiveService : SystemServiceBase, IPassiveService
    {
        private readonly IReadOnlyDictionary<string, IPassiveTrigger> _triggerRegistry;
        private readonly IReadOnlyDictionary<string, IPassiveEffect> _effectRegistry;
        private readonly PassiveTargetResolver _targetResolver;
        private readonly IPassiveContext _context;
        private readonly IReadOnlyDictionary<string, EntityDefinition> _unitRegistry;
        private readonly Dictionary<EntityId, List<ActivePassive>> _activePassives = new();
        private readonly Dictionary<EntityId, List<PassiveEntry>> _passiveEntryCache = new();
        private bool _isProcessing;

        private const string DefaultSource = "__default";
        private sealed record ActivePassive(PassiveEntry Entry, IDisposable Subscription, string Source);

        public PassiveService(
            IEventBus eventBus,
            IReadOnlyDictionary<string, IPassiveTrigger> triggerRegistry,
            IReadOnlyDictionary<string, IPassiveEffect> effectRegistry,
            PassiveTargetResolver targetResolver,
            IPassiveContext context,
            IReadOnlyDictionary<string, EntityDefinition> unitRegistry = null)
            : base(eventBus)
        {
            _triggerRegistry = triggerRegistry;
            _effectRegistry = effectRegistry;
            _targetResolver = targetResolver;
            _context = context;
            _unitRegistry = unitRegistry;

            Subscribe<EntityDiedEvent>(OnEntityDied);
            Subscribe<UnitSummonedEvent>(OnUnitSummoned);
        }

        public void RegisterPassives(EntityId entityId, PassiveEntry[] passives) =>
            RegisterPassives(entityId, DefaultSource, passives);

        public void RegisterPassives(EntityId entityId, string source, PassiveEntry[] passives)
        {
            if (passives == null || passives.Length == 0) return;
            source ??= DefaultSource;

            if (!_activePassives.TryGetValue(entityId, out var list))
            {
                list = new List<ActivePassive>();
                _activePassives[entityId] = list;
            }

            _passiveEntryCache.Remove(entityId);

            foreach (var entry in passives)
            {
                IDisposable subscription = null;

                if (entry.TriggerId != null
                    && entry.EffectId != null
                    && _triggerRegistry.TryGetValue(entry.TriggerId, out var trigger)
                    && _effectRegistry.TryGetValue(entry.EffectId, out var effect))
                {
                    var capturedEntry = entry;
                    var capturedEffect = effect;
                    subscription = trigger.Subscribe(entityId, entry.TriggerParam, _context, triggerCtx =>
                    {
                        if (_isProcessing) return;
                        _isProcessing = true;
                        try
                        {
                            var targets = _targetResolver.Resolve(capturedEntry.Target, triggerCtx, entityId, _context);

                            var effectiveValue = triggerCtx.OverrideValue ?? capturedEntry.Value;

                            if (_context.AnimationService?.IsAnimating == true)
                            {
                                UnityEngine.Debug.Log($"[Passive] DEFERRED {capturedEntry.TriggerId}→{capturedEntry.EffectId} owner={entityId} targets={targets.Count} animSvc={_context.AnimationService != null}");
                                var targetsCopy = new List<EntityId>(targets);
                                var capturedValue = effectiveValue;
                                _context.AnimationService.EnqueuePassiveAnimation(
                                    entityId, capturedEntry.EffectId, capturedValue, targetsCopy, () =>
                                    {
                                        UnityEngine.Debug.Log($"[Passive] ARRIVAL {capturedEntry.EffectId} owner={entityId} targets={targetsCopy.Count}");
                                        capturedEffect.Execute(entityId, capturedValue, capturedEntry.EffectParam, targetsCopy, _context);
                                        var a = targetsCopy.Count > 0 ? targetsCopy[0] : (EntityId?)null;
                                        _context.EventBus.Publish(new PassiveTriggeredEvent(
                                            entityId, capturedEntry.TriggerId, capturedEntry.EffectId,
                                            capturedValue, a));
                                    });
                            }
                            else
                            {
                                UnityEngine.Debug.Log($"[Passive] SYNC {capturedEntry.TriggerId}→{capturedEntry.EffectId} owner={entityId} targets={targets.Count} animSvc={_context.AnimationService != null} isAnimating={_context.AnimationService?.IsAnimating}");
                                capturedEffect.Execute(entityId, effectiveValue, capturedEntry.EffectParam, targets, _context);
                                var affected = targets.Count > 0 ? targets[0] : (EntityId?)null;
                                _context.EventBus.Publish(new PassiveTriggeredEvent(
                                    entityId, capturedEntry.TriggerId, capturedEntry.EffectId,
                                    effectiveValue, affected));
                            }
                        }
                        finally
                        {
                            _isProcessing = false;
                        }
                    });
                }

                list.Add(new ActivePassive(entry, subscription, source));
            }
        }

        public void RemovePassives(EntityId entityId) => RemovePassives(entityId, null);

        public void RemovePassives(EntityId entityId, string source)
        {
            if (!_activePassives.TryGetValue(entityId, out var list)) return;

            if (source == null)
            {
                // Remove all passives for entity
                foreach (var active in list)
                    active.Subscription?.Dispose();
                _activePassives.Remove(entityId);
            }
            else
            {
                // Remove only passives from the specified source
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].Source == source)
                    {
                        list[i].Subscription?.Dispose();
                        list.RemoveAt(i);
                    }
                }
                if (list.Count == 0)
                    _activePassives.Remove(entityId);
            }

            _passiveEntryCache.Remove(entityId);
        }

        public bool HasPassives(EntityId entityId) => _activePassives.ContainsKey(entityId);

        public IReadOnlyList<PassiveEntry> GetPassives(EntityId entityId)
        {
            if (!_activePassives.TryGetValue(entityId, out var list))
                return Array.Empty<PassiveEntry>();

            if (!_passiveEntryCache.TryGetValue(entityId, out var cached) || cached.Count != list.Count)
            {
                cached = new List<PassiveEntry>(list.Count);
                for (int i = 0; i < list.Count; i++)
                    cached.Add(list[i].Entry);
                _passiveEntryCache[entityId] = cached;
            }
            return cached;
        }

        public bool HasTaunt(EntityId entityId)
        {
            if (!_activePassives.TryGetValue(entityId, out var list)) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Entry.TriggerId == PassiveConstants.Taunt)
                    return true;
            }
            return false;
        }

        private void OnEntityDied(EntityDiedEvent e)
        {
            EventBus.Publish(new PassiveDeathTriggerEvent(e.EntityId));
            RemovePassives(e.EntityId);
        }

        private void OnUnitSummoned(UnitSummonedEvent e)
        {
            if (_unitRegistry == null) return;
            if (e.Word.Length == 0) return;

            var key = e.Word.ToLowerInvariant();
            if (!_unitRegistry.TryGetValue(key, out var def)) return;
            if (def.Passives == null || def.Passives.Length == 0) return;

            RegisterPassives(e.EntityId, def.Passives);
        }
    }
}
