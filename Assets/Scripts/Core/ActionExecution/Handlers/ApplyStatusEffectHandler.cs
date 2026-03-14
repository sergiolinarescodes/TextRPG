using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class ApplyStatusEffectHandler : IActionHandler
    {
        private readonly IStatusEffectService _statusEffects;
        private readonly StatusEffectType _effectType;
        private readonly DurationMode _durationMode;

        public string ActionId { get; }

        public enum DurationMode { FromValue, Permanent, StackByValue }

        public ApplyStatusEffectHandler(string actionId, IActionHandlerContext ctx,
            StatusEffectType effectType, bool applySelf, DurationMode durationMode)
        {
            ActionId = actionId;
            _statusEffects = ctx.StatusEffects;
            _effectType = effectType;
            _durationMode = durationMode;
        }

        public void Execute(ActionContext context)
        {
            if (_durationMode == DurationMode.StackByValue)
            {
                for (int t = 0; t < context.Targets.Count; t++)
                    for (int i = 0; i < context.Value; i++)
                        _statusEffects.ApplyEffect(context.Targets[t], _effectType, StatusEffectInstance.PermanentDuration, context.Source);
                return;
            }

            int duration = _durationMode == DurationMode.Permanent
                ? StatusEffectInstance.PermanentDuration
                : context.Value;

            for (int i = 0; i < context.Targets.Count; i++)
                _statusEffects.ApplyEffect(context.Targets[i], _effectType, duration, context.Source);
        }
    }
}
