using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.StatusEffect
{
    internal sealed class StatusEffectService : SystemServiceBase, IStatusEffectService
    {
        private readonly IEntityStatsService _entityStats;
        private readonly ITurnService _turnService;
        private readonly IStatusEffectHandlerRegistry _handlerRegistry;
        private readonly IStatusEffectHandlerContext _handlerContext;
        private readonly StatusEffectInteractionTable _interactionTable;
        private readonly Dictionary<EntityId, List<StatusEffectInstance>> _effects = new();
        private int _modifierCounter;

        public StatusEffectService(IEventBus eventBus, IEntityStatsService entityStats, ITurnService turnService,
            IStatusEffectHandlerRegistry handlerRegistry, IStatusEffectHandlerContext handlerContext,
            StatusEffectInteractionTable interactionTable = null)
            : base(eventBus)
        {
            _entityStats = entityStats;
            _turnService = turnService;
            _handlerRegistry = handlerRegistry;
            _handlerContext = handlerContext;
            _interactionTable = interactionTable ?? new StatusEffectInteractionTable();
            Subscribe<TurnEndedEvent>(OnTurnEnded);
            Subscribe<TurnStartedEvent>(OnTurnStarted);
            Subscribe<EntityStats.HealedEvent>(OnHealed);
            Subscribe<EntityStats.DamageTakenEvent>(OnDamageTaken);
        }

        public void ApplyEffect(EntityId target, StatusEffectType type, int duration, EntityId source)
        {
            var definition = StatusEffectDefinitions.Get(type);

            if (!_effects.TryGetValue(target, out var list))
            {
                list = new List<StatusEffectInstance>();
                _effects[target] = list;
            }

            var existing = list.Find(e => e.Type == type);

            if (existing != null)
            {
                switch (definition.StackPolicy)
                {
                    case StackPolicy.RefreshDuration:
                        existing.RemainingDuration = duration;
                        Publish(new StatusEffectAppliedEvent(target, type, duration, source));
                        return;

                    case StackPolicy.StackIntensity:
                        if (existing.IsPermanent || duration < 0)
                            existing.RemainingDuration = StatusEffectInstance.PermanentDuration;
                        else
                            existing.RemainingDuration = Math.Max(existing.RemainingDuration, duration);
                        existing.StackCount++;
                        if (_handlerRegistry.TryGet(type, out var stackHandler))
                            stackHandler.OnApply(target, existing, _handlerContext);
                        Publish(new StatusEffectAppliedEvent(target, type, existing.RemainingDuration, source));
                        return;

                    case StackPolicy.Ignore:
                        return;
                }
            }

            var modifierIds = AddStatModifiers(target, definition);
            var instance = new StatusEffectInstance(type, duration, source, modifierIds);
            list.Add(instance);
            Publish(new StatusEffectAppliedEvent(target, type, duration, source));

            if (_handlerRegistry.TryGet(type, out var handler))
                handler.OnApply(target, instance, _handlerContext);

            var removals = _interactionTable.GetRemovals(type);
            if (removals != null)
            {
                foreach (var toRemove in removals)
                {
                    if (HasEffect(target, toRemove))
                        RemoveEffect(target, toRemove);
                }
            }
        }

        public void RemoveEffect(EntityId target, StatusEffectType type)
        {
            if (!_effects.TryGetValue(target, out var list))
                return;

            var instance = list.Find(e => e.Type == type);
            if (instance == null)
                return;

            if (_handlerRegistry.TryGet(type, out var handler))
                handler.OnRemove(target, instance, _handlerContext);

            RemoveStatModifiers(target, instance);
            list.Remove(instance);
            Publish(new StatusEffectRemovedEvent(target, type));
        }

        public void RemoveAllEffects(EntityId target)
        {
            if (!_effects.TryGetValue(target, out var list))
                return;

            foreach (var instance in list)
            {
                if (_handlerRegistry.TryGet(instance.Type, out var handler))
                    handler.OnRemove(target, instance, _handlerContext);
                RemoveStatModifiers(target, instance);
            }

            list.Clear();
        }

        public bool HasEffect(EntityId target, StatusEffectType type)
        {
            return _effects.TryGetValue(target, out var list) && list.Exists(e => e.Type == type);
        }

        public IReadOnlyList<StatusEffectInstance> GetEffects(EntityId target)
        {
            return _effects.TryGetValue(target, out var list)
                ? list
                : Array.Empty<StatusEffectInstance>();
        }

        public int GetStackCount(EntityId target, StatusEffectType type)
        {
            if (!_effects.TryGetValue(target, out var list))
                return 0;
            var instance = list.Find(e => e.Type == type);
            return instance?.StackCount ?? 0;
        }

        public void DecrementStack(EntityId target, StatusEffectType type)
        {
            if (!_effects.TryGetValue(target, out var list))
                return;
            var instance = list.Find(e => e.Type == type);
            if (instance == null)
                return;
            instance.StackCount--;
            if (instance.StackCount <= 0)
                RemoveEffect(target, type);
        }

        private void OnDamageTaken(EntityStats.DamageTakenEvent e)
        {
            if (e.DamageSource == null) return;
            if (!HasEffect(e.EntityId, StatusEffectType.Thorns)) return;
            var stacks = GetStackCount(e.EntityId, StatusEffectType.Thorns);
            _entityStats.ApplyDamage(e.DamageSource.Value, stacks);
        }

        private void OnHealed(EntityStats.HealedEvent e)
        {
            if (!_effects.TryGetValue(e.EntityId, out var list))
                return;
            var bleeding = list.Find(x => x.Type == StatusEffectType.Bleeding);
            if (bleeding != null)
                bleeding.WasHealedThisTurn = true;
        }

        private void OnTurnStarted(TurnStartedEvent e)
        {
            var entity = e.EntityId;
            if (HasEffect(entity, StatusEffectType.Stun) || HasEffect(entity, StatusEffectType.Frozen))
                _turnService.EndTurn();
        }

        private void OnTurnEnded(TurnEndedEvent e)
        {
            var entity = e.EntityId;
            if (!_effects.TryGetValue(entity, out var list))
                return;

            var snapshot = new List<StatusEffectInstance>(list);
            var expired = new List<StatusEffectInstance>();

            foreach (var instance in snapshot)
            {
                // Handler's OnTick may have removed this instance (e.g. BleedingHandler)
                if (!list.Contains(instance))
                    continue;

                if (_handlerRegistry.TryGet(instance.Type, out var handler))
                    handler.OnTick(entity, instance, _handlerContext);

                // Re-check after OnTick in case handler removed the instance
                if (!list.Contains(instance))
                    continue;

                if (instance.IsPermanent)
                {
                    Publish(new StatusEffectTickedEvent(entity, instance.Type, instance.RemainingDuration));
                    continue;
                }

                instance.RemainingDuration--;
                if (instance.RemainingDuration <= 0)
                {
                    expired.Add(instance);
                }
                else
                {
                    Publish(new StatusEffectTickedEvent(entity, instance.Type, instance.RemainingDuration));
                }
            }

            foreach (var instance in expired)
            {
                if (!list.Contains(instance))
                    continue;

                if (_handlerRegistry.TryGet(instance.Type, out var handler))
                    handler.OnExpire(entity, instance, _handlerContext);

                RemoveStatModifiers(entity, instance);
                list.Remove(instance);
                Publish(new StatusEffectExpiredEvent(entity, instance.Type));
            }
        }

        private string[] AddStatModifiers(EntityId target, StatusEffectDefinition definition)
        {
            var mods = definition.StatModifiers;
            if (mods.Length == 0)
                return Array.Empty<string>();

            var ids = new string[mods.Length];
            for (int i = 0; i < mods.Length; i++)
            {
                var id = $"status_{definition.Type}_{mods[i].Stat}_{_modifierCounter++}";
                var modifier = new StatusEffectModifier(id, mods[i].Amount);
                _entityStats.AddModifier(target, mods[i].Stat, modifier);
                ids[i] = id;
            }
            return ids;
        }

        private void RemoveStatModifiers(EntityId target, StatusEffectInstance instance)
        {
            var definition = StatusEffectDefinitions.Get(instance.Type);
            for (int i = 0; i < instance.ModifierIds.Length; i++)
            {
                _entityStats.RemoveModifier(target, definition.StatModifiers[i].Stat, instance.ModifierIds[i]);
            }
        }
    }
}
