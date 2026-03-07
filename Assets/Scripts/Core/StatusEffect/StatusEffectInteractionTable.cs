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
            _removals[applied] = new[] { removes };
        }

        private void AddDamageMultiplier(string actionId, StatusEffectType requiredEffect, float multiplier)
        {
            _damageMultipliers[(actionId, requiredEffect)] = multiplier;
        }
    }
}
