using System;
using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive.Handlers
{
    internal sealed class DamageOnAllyHitHandler : IPassiveHandler
    {
        public string PassiveId => "damage_on_ally_hit";

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

                // Retaliate against the attacker
                if (!evt.DamageSource.HasValue) return;
                var attacker = evt.DamageSource.Value;
                if (!context.EntityStats.HasEntity(attacker) || context.EntityStats.GetCurrentHealth(attacker) <= 0) return;

                context.EntityStats.ApplyDamage(attacker, value, owner);
                context.EventBus.Publish(new PassiveTriggeredEvent(owner, "damage_on_ally_hit", value, attacker));
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
