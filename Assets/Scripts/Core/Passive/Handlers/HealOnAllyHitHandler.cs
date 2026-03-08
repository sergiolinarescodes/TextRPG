using System;
using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive.Handlers
{
    internal sealed class HealOnAllyHitHandler : IPassiveHandler
    {
        public string PassiveId => "heal_on_ally_hit";

        private readonly Dictionary<EntityId, (int Value, IDisposable Subscription)> _registrations = new();

        public void Register(EntityId owner, int value, IPassiveContext context)
        {
            var sub = context.EventBus.Subscribe<DamageTakenEvent>(evt =>
            {
                if (evt.EntityId.Equals(owner)) return;
                if (!context.EntityStats.HasEntity(owner) || context.EntityStats.GetCurrentHealth(owner) <= 0) return;

                // Check if damaged entity is in the same faction as owner
                var ownerSlot = context.SlotService.GetSlot(owner);
                var targetSlot = context.SlotService.GetSlot(evt.EntityId);
                if (!ownerSlot.HasValue || !targetSlot.HasValue) return;
                if (ownerSlot.Value.Type != targetSlot.Value.Type) return;

                context.EntityStats.ApplyHeal(evt.EntityId, value);
                context.EventBus.Publish(new PassiveTriggeredEvent(owner, "heal_on_ally_hit", value, evt.EntityId));
            });

            _registrations[owner] = (value, sub);
        }

        public void Unregister(EntityId owner, IPassiveContext context)
        {
            if (_registrations.TryGetValue(owner, out var entry))
            {
                entry.Subscription.Dispose();
                _registrations.Remove(owner);
            }
        }
    }
}
