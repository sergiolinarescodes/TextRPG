using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive;
using TextRPG.Core.Services;

namespace TextRPG.Core.Effects
{
    internal sealed class PassiveEffectAdapter : IPassiveEffect
    {
        private readonly IGameEffect _effect;
        private readonly IGameServices _services;

        public string EffectId => _effect.EffectId;

        public PassiveEffectAdapter(IGameEffect effect, IGameServices services)
        {
            _effect = effect;
            _services = services;
        }

        public void Execute(EntityId owner, int value, string effectParam,
                            IReadOnlyList<EntityId> targets, IPassiveContext ctx)
        {
            _effect.Execute(new EffectContext(owner, targets, value, effectParam, _services));
        }
    }
}
