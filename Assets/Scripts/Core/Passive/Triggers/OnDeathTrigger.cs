using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
using Unidad.Core.EventBus;

namespace TextRPG.Core.Passive.Triggers
{
    [AutoScan]
    internal sealed class OnDeathTrigger : IPassiveTrigger
    {
        public string TriggerId => "on_death";

        public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                      Action<PassiveTriggerContext> onTriggered)
        {
            if (triggerParam == "siphon")
                return SubscribeSiphonAccumulator(owner, ctx, onTriggered);

            var sub = ctx.EventBus.Subscribe<PassiveDeathTriggerEvent>(evt =>
            {
                if (!evt.EntityId.Equals(owner)) return;
                onTriggered(new PassiveTriggerContext(owner, null, null, null));
            });
            return sub;
        }

        private static IDisposable SubscribeSiphonAccumulator(EntityId owner, IPassiveContext ctx,
                                                                Action<PassiveTriggerContext> onTriggered)
        {
            int accumulated = 0;

            var siphonSub = ctx.EventBus.Subscribe<StatSiphonedEvent>(evt =>
            {
                if (evt.Source.Equals(owner))
                    accumulated += evt.Amount;
            });

            var deathSub = ctx.EventBus.Subscribe<PassiveDeathTriggerEvent>(evt =>
            {
                if (!evt.EntityId.Equals(owner)) return;
                var value = accumulated > 0 ? accumulated : (int?)null;
                onTriggered(new PassiveTriggerContext(owner, null, null, null, value));
            });

            return new CompositeDisposable(siphonSub, deathSub);
        }

        private sealed class CompositeDisposable : IDisposable
        {
            private readonly IDisposable _a;
            private readonly IDisposable _b;

            public CompositeDisposable(IDisposable a, IDisposable b)
            {
                _a = a;
                _b = b;
            }

            public void Dispose()
            {
                _a?.Dispose();
                _b?.Dispose();
            }
        }
    }
}
