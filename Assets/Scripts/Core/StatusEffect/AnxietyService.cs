using System;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
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
        private IStatusEffectService _statusEffects;

        public AnxietyService(IEventBus eventBus, IWordTagResolver tagResolver) : base(eventBus)
        {
            _tagResolver = tagResolver;

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

            int chance = Math.Min(40, 10 + (stacks - 1) * 30 / 8);
            if (UnityEngine.Random.Range(0, 100) >= chance) return false;

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
