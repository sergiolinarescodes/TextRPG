using System;
using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.TurnSystem;

namespace TextRPG.Core.Passive.Handlers
{
    internal sealed class HealOnRoundEndHandler : IPassiveHandler
    {
        public string PassiveId => "heal_on_round_end";

        private readonly Dictionary<EntityId, (int Value, IDisposable Subscription)> _registrations = new();

        public void Register(EntityId owner, int value, IPassiveContext context)
        {
            var sub = context.EventBus.Subscribe<RoundEndedEvent>(_ =>
            {
                if (!context.EntityStats.HasEntity(owner) || context.EntityStats.GetCurrentHealth(owner) <= 0) return;

                var ownerSlot = context.SlotService.GetSlot(owner);
                if (!ownerSlot.HasValue) return;

                // Heal all allies in same faction
                var allies = ownerSlot.Value.Type == SlotType.Ally
                    ? context.SlotService.GetAllAllies()
                    : context.SlotService.GetAllEnemies();

                foreach (var ally in allies)
                {
                    if (!context.EntityStats.HasEntity(ally) || context.EntityStats.GetCurrentHealth(ally) <= 0) continue;
                    context.EntityStats.ApplyHeal(ally, value);
                    context.EventBus.Publish(new PassiveTriggeredEvent(owner, "heal_on_round_end", value, ally));
                }

                // Also heal the player if owner is an ally-faction structure
                if (ownerSlot.Value.Type == SlotType.Ally && context.EncounterService != null)
                {
                    var player = context.EncounterService.PlayerEntity;
                    if (context.EntityStats.HasEntity(player) && context.EntityStats.GetCurrentHealth(player) > 0)
                    {
                        context.EntityStats.ApplyHeal(player, value);
                        context.EventBus.Publish(new PassiveTriggeredEvent(owner, "heal_on_round_end", value, player));
                    }
                }
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
