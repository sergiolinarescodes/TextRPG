using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect
{
    public sealed class StatusEffectInteractionTable
    {
        private readonly Dictionary<StatusEffectType, StatusEffectType[]> _removals = new();
        private readonly Dictionary<(string ActionId, StatusEffectType), float> _damageMultipliers = new();

        public StatusEffectInteractionTable()
        {
            AddRemoval(StatusEffectType.Burning, StatusEffectType.Frozen);
            AddRemoval(StatusEffectType.Burning, StatusEffectType.Frostbitten);
            AddRemoval(StatusEffectType.Frostbitten, StatusEffectType.Burning);
            AddRemoval(StatusEffectType.Frozen, StatusEffectType.Burning);
            AddDamageMultiplier("Shock", StatusEffectType.Wet, 2f);
        }

        public StatusEffectType[] GetRemovals(StatusEffectType applied)
        {
            return _removals.TryGetValue(applied, out var removals) ? removals : null;
        }

        public float GetDamageMultiplier(string actionId, EntityId target, IStatusEffectService effects)
        {
            float multiplier = 1f;
            foreach (var entry in _damageMultipliers)
            {
                if (entry.Key.ActionId == actionId && effects.HasEffect(target, entry.Key.Item2))
                    multiplier *= entry.Value;
            }
            return multiplier;
        }

        private void AddRemoval(StatusEffectType applied, StatusEffectType removes)
        {
            if (_removals.TryGetValue(applied, out var existing))
            {
                var expanded = new StatusEffectType[existing.Length + 1];
                existing.CopyTo(expanded, 0);
                expanded[existing.Length] = removes;
                _removals[applied] = expanded;
            }
            else
            {
                _removals[applied] = new[] { removes };
            }
        }

        private void AddDamageMultiplier(string actionId, StatusEffectType requiredEffect, float multiplier)
        {
            _damageMultipliers[(actionId, requiredEffect)] = multiplier;
        }
    }
}
