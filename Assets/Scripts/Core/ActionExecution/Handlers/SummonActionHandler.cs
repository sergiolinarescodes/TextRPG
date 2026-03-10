using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.UnitRendering;
using Unidad.Core.EventBus;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class SummonActionHandler : IActionHandler
    {
        private readonly ICombatContext _combatContext;
        private readonly IEntityStatsService _entityStats;
        private readonly IEventBus _eventBus;
        private readonly IReadOnlyDictionary<string, EntityDefinition> _unitRegistry;
        private int _summonCounter;

        public string ActionId => ActionNames.Summon;

        public SummonActionHandler(IActionHandlerContext ctx)
        {
            _combatContext = ctx.CombatContext;
            _entityStats = ctx.EntityStats;
            _eventBus = ctx.EventBus;
            _unitRegistry = ctx.UnitRegistry;
        }

        public void Execute(ActionContext context)
        {
            var slotService = _combatContext.SlotService;
            if (slotService == null) return;

            // Determine if this is an enemy or ally summon
            var sourceSlot = slotService.GetSlot(context.Source);
            bool isEnemySummon = sourceSlot.HasValue && sourceSlot.Value.Type == SlotType.Enemy;
            var slotType = isEnemySummon ? SlotType.Enemy : SlotType.Ally;

            var emptySlot = slotService.FindFirstEmptySlot(slotType);
            if (emptySlot == null) return;

            var id = $"summon_{_summonCounter++}";
            var entityId = new EntityId(id);

            var unitType = "enemy";
            var summonKey = !string.IsNullOrEmpty(context.AssocWord)
                ? context.AssocWord.ToLowerInvariant()
                : context.Word.ToLowerInvariant();

            if (_unitRegistry != null && _unitRegistry.TryGetValue(summonKey, out var unitDef))
            {
                EntityRegistrationHelper.RegisterFromDefinition(_entityStats, entityId, unitDef);
                unitType = unitDef.UnitType;
            }
            else
            {
                var hp = context.Value * 5;
                _entityStats.RegisterEntity(entityId, maxHealth: hp, strength: context.Value,
                    magicPower: 0, physicalDefense: context.Value / 2, magicDefense: 0, luck: 0);
            }

            var slot = new CombatSlot.CombatSlot(slotType, emptySlot.Value);

            // Publish summon event before slot registration so subscribers
            // can register unit visuals before SlotEntityRegisteredEvent triggers rendering
            _eventBus.Publish(new UnitSummonedEvent(entityId, context.Source, slot, unitType, summonKey));

            if (isEnemySummon)
                slotService.RegisterEnemy(entityId, emptySlot.Value);
            else
                slotService.RegisterAlly(entityId, emptySlot.Value);
        }
    }
}
