using System;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Luck;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.StatusEffect
{
    internal sealed class AnxietyService : SystemServiceBase, IAnxietyService
    {
        private readonly IWordTagResolver _tagResolver;
        private readonly ILuckService _luckService;
        private IStatusEffectService _statusEffects;

        public AnxietyService(IEventBus eventBus, IWordTagResolver tagResolver, ILuckService luckService = null) : base(eventBus)
        {
            _tagResolver = tagResolver;
            _luckService = luckService;

            Subscribe<ActionResolvedEvent>(OnActionResolved);
        }

        public void SetStatusEffects(IStatusEffectService statusEffects)
        {
            _statusEffects = statusEffects;
        }

        public bool TryInterceptTurn(EntityId entityId, out string word)
        {
            word = null;
            if (_statusEffects == null) return false;

            int stacks = _statusEffects.GetStackCount(entityId, StatusEffectType.Anxiety);
            if (stacks <= 0) return false;

            float chance = Math.Min(40, 10 + (stacks - 1) * 30 / 8) / 100f;
            if (_luckService != null)
                chance = _luckService.AdjustChance(chance, entityId, false);
            if (UnityEngine.Random.value >= chance) return false;

            word = _tagResolver.GetRandomWordByTag("THOUGHTS");
            return word != null;
        }

        private void OnActionResolved(ActionResolvedEvent e)
        {
            if (_statusEffects == null) return;
            if (!_tagResolver.HasTag(e.Word, "RELAX")) return;
            if (!_statusEffects.HasEffect(e.Source, StatusEffectType.Anxiety)) return;

            _statusEffects.DecrementStack(e.Source, StatusEffectType.Anxiety);
        }
    }
}
