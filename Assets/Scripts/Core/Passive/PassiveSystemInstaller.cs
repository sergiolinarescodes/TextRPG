using System.Collections.Generic;
using Reflex.Core;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive.Effects;
using TextRPG.Core.Passive.Triggers;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.WordAction;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.Passive
{
    public sealed class PassiveSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var entityStats = container.Resolve<IEntityStatsService>();
                var slotService = container.Resolve<ICombatSlotService>();
                var encounterService = container.Resolve<IEncounterService>();
                var statusEffects = container.Resolve<IStatusEffectService>();
                var turnService = container.Resolve<ITurnService>();

                IWordTagResolver tagResolver = null;
                try { tagResolver = container.Resolve<IWordTagResolver>(); } catch { /* optional */ }

                IActionAnimationService animationService = null;
                try { animationService = container.Resolve<IActionAnimationService>(); } catch { /* optional */ }

                var triggerRegistry = CreateTriggerRegistry();
                var effectRegistry = CreateEffectRegistry();
                var targetResolver = new PassiveTargetResolver();
                var context = new PassiveContext(entityStats, slotService, eventBus, encounterService,
                    statusEffects, tagResolver, turnService, animationService);

                return (IPassiveService)new PassiveService(eventBus, triggerRegistry, effectRegistry,
                    targetResolver, context);
            }, typeof(IPassiveService));
        }

        public ISystemTestFactory CreateTestFactory() => new PassiveTestFactory();

        internal static Dictionary<string, IPassiveTrigger> CreateTriggerRegistry()
        {
            return new Dictionary<string, IPassiveTrigger>
            {
                ["on_ally_hit"] = new OnAllyHitTrigger(),
                ["on_self_hit"] = new OnSelfHitTrigger(),
                ["on_round_end"] = new OnRoundEndTrigger(),
                ["on_round_start"] = new OnRoundStartTrigger(),
                ["on_turn_start"] = new OnTurnStartTrigger(),
                ["on_turn_end"] = new OnTurnEndTrigger(),
                ["on_word_played"] = new OnWordPlayedTrigger(),
                ["on_word_length"] = new OnWordLengthTrigger(),
                ["on_word_tag"] = new OnWordTagTrigger(),
                ["on_kill"] = new OnKillTrigger(),
            };
        }

        internal static Dictionary<string, IPassiveEffect> CreateEffectRegistry()
        {
            return new Dictionary<string, IPassiveEffect>
            {
                ["heal"] = new HealEffect(),
                ["damage"] = new DamageEffect(),
                ["shield"] = new ShieldEffect(),
                ["mana"] = new ManaEffect(),
                ["apply_status"] = new ApplyStatusPassiveEffect(),
            };
        }
    }
}
