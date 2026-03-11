using System.Collections.Generic;
using Reflex.Core;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Effects;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
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
                var effectRegistry = CreateEffectRegistry(null);
                var targetResolver = new PassiveTargetResolver();
                var context = new PassiveContext(entityStats, slotService, eventBus, encounterService,
                    statusEffects, tagResolver, turnService, animationService);

                return (IPassiveService)new PassiveService(eventBus, triggerRegistry, effectRegistry,
                    targetResolver, context);
            }, typeof(IPassiveService));
        }

        public ISystemTestFactory CreateTestFactory() => new PassiveTestFactory();

        internal static Dictionary<string, IPassiveTrigger> CreateTriggerRegistry()
            => AssemblyScanner.FindAll<IPassiveTrigger, string>(t => t.TriggerId);

        internal static Dictionary<string, IPassiveEffect> CreateEffectRegistry(IGameServices services)
        {
            var effectRegistry = CreateGameEffectRegistry();

            if (services != null)
            {
                var result = new Dictionary<string, IPassiveEffect>();
                foreach (var effect in effectRegistry.Values)
                    result[effect.EffectId] = new PassiveEffectAdapter(effect, services);
                return result;
            }

            // Fallback: standalone effects (no IGameServices available)
            return AssemblyScanner.FindAll<IPassiveEffect, string>(e => e.EffectId);
        }

        internal static GameEffectRegistry CreateGameEffectRegistry()
            => AssemblyScanner.BuildRegistry<GameEffectRegistry, IGameEffect>();
    }
}
