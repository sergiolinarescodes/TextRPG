using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.Services;

namespace TextRPG.Core.Effects
{
    internal sealed class OutcomeEffectAdapter : IInteractionOutcome
    {
        private readonly IGameEffect _effect;
        private readonly IGameServices _services;
        private readonly bool _swapSourceTarget;

        public string OutcomeId => _effect.EffectId;

        public OutcomeEffectAdapter(IGameEffect effect, IGameServices services, bool swapSourceTarget = false)
        {
            _effect = effect;
            _services = services;
            _swapSourceTarget = swapSourceTarget;
        }

        public void Execute(InteractionOutcomeContext context)
        {
            var source = _swapSourceTarget ? context.Target : context.Source;
            var target = _swapSourceTarget ? context.Source : context.Target;

            _effect.Execute(new EffectContext(
                source,
                new[] { target },
                context.Value,
                context.OutcomeParam,
                _services));
        }
    }
}
