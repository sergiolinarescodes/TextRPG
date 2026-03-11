using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class CombatVisualController : IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new();
        private CombatSlotVisual _slotVisual;

        public CombatVisualController(IEventBus eventBus, IEntityStatsService entityStats,
            IUnitService unitService, CombatSlotVisual slotVisual,
            Dictionary<string, EntityDefinition> allUnits, EntityId playerId)
        {
            _slotVisual = slotVisual;

            _subscriptions.Add(eventBus.Subscribe<DamageTakenEvent>(evt =>
            {
                _slotVisual?.RefreshSlot(evt.EntityId);
            }));
            _subscriptions.Add(eventBus.Subscribe<HealedEvent>(evt =>
            {
                _slotVisual?.RefreshSlot(evt.EntityId);
            }));
            _subscriptions.Add(eventBus.Subscribe<EntityDiedEvent>(evt =>
            {
                if (evt.EntityId.Equals(playerId))
                    _slotVisual?.RefreshSlot(evt.EntityId);
                else
                    _slotVisual?.PlayDeathAnimation(evt.EntityId);
            }));

            // Refresh non-player mana bars
            _subscriptions.Add(eventBus.Subscribe<ManaChangedEvent>(evt =>
            {
                if (!evt.EntityId.Equals(playerId))
                    _slotVisual?.RefreshSlot(evt.EntityId);
            }));

            // Summon unit registration
            _subscriptions.Add(eventBus.Subscribe<UnitSummonedEvent>(evt =>
            {
                var uid = new UnitId(evt.EntityId.Value);
                if (unitService.TryGetUnit(uid, out _)) return;

                var word = evt.Word.ToLowerInvariant();
                if (allUnits.TryGetValue(word, out var unitDef))
                {
                    unitService.Register(uid,
                        new UnitDefinition(uid, unitDef.Name,
                            unitDef.MaxHealth, unitDef.Strength, unitDef.PhysicalDefense, unitDef.Luck, unitDef.Color));
                }
                else
                {
                    unitService.Register(uid,
                        new UnitDefinition(uid, evt.EntityId.Value.ToUpperInvariant(),
                            entityStats.GetStat(evt.EntityId, StatType.MaxHealth), 0, 0, 0, Color.white));
                }
            }));

            // Slot visual registration
            _subscriptions.Add(eventBus.Subscribe<SlotEntityRegisteredEvent>(evt =>
            {
                if (_slotVisual == null) return;
                var slotElements = _slotVisual.GetAllSlotElements();
                int visualIndex = evt.Slot.Type == SlotType.Enemy ? evt.Slot.Index : 3 + evt.Slot.Index;
                if (visualIndex >= 0 && visualIndex < slotElements.Count)
                    _slotVisual.RegisterEntity(evt.EntityId, slotElements[visualIndex]);
            }));

            // Slot visual removal for recruitment (not death)
            _subscriptions.Add(eventBus.Subscribe<SlotEntityRemovedEvent>(evt =>
            {
                if (_slotVisual == null) return;
                if (entityStats.GetCurrentHealth(evt.EntityId) <= 0) return;
                _slotVisual.UnregisterEntity(evt.EntityId);
            }));
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            _slotVisual = null;
        }
    }
}
