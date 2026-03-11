using TextRPG.Core.CombatSlot;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class ScrambleActionHandler : IActionHandler
    {
        private readonly ICombatSlotService _slotService;
        private readonly IEventBus _eventBus;

        public string ActionId => ActionNames.Scramble;

        public ScrambleActionHandler(IActionHandlerContext ctx)
        {
            _slotService = ctx.SlotService;
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var targetSlot = _slotService.GetSlot(target);
                if (targetSlot == null) continue;

                var slot = targetSlot.Value;
                var arr = slot.Type == SlotType.Enemy
                    ? _slotService.GetAllEnemies()
                    : _slotService.GetAllAllies();

                if (arr.Count < 2) continue;

                // Find a different occupied slot to swap with
                int? swapIndex = null;
                var maxSlots = slot.Type == SlotType.Enemy ? 3 : 2;
                for (int offset = 1; offset < maxSlots; offset++)
                {
                    var candidateIdx = (slot.Index + offset) % maxSlots;
                    var occupant = _slotService.GetEntityAt(slot.Type, candidateIdx);
                    if (occupant.HasValue && !occupant.Value.Equals(target))
                    {
                        swapIndex = candidateIdx;
                        break;
                    }
                }

                if (swapIndex == null)
                {
                    // No other unit to swap with — move to an empty slot if available
                    var emptySlot = _slotService.FindFirstEmptySlot(slot.Type);
                    if (emptySlot == null || emptySlot.Value == slot.Index) continue;

                    _slotService.RemoveEntity(target);
                    if (slot.Type == SlotType.Enemy)
                        _slotService.RegisterEnemy(target, emptySlot.Value);
                    else
                        _slotService.RegisterAlly(target, emptySlot.Value);
                    continue;
                }

                // Swap the two entities
                var otherEntity = _slotService.GetEntityAt(slot.Type, swapIndex.Value);
                if (!otherEntity.HasValue) continue;

                var entityA = target;
                var entityB = otherEntity.Value;
                var indexA = slot.Index;
                var indexB = swapIndex.Value;

                _slotService.RemoveEntity(entityA);
                _slotService.RemoveEntity(entityB);

                if (slot.Type == SlotType.Enemy)
                {
                    _slotService.RegisterEnemy(entityA, indexB);
                    _slotService.RegisterEnemy(entityB, indexA);
                }
                else
                {
                    _slotService.RegisterAlly(entityA, indexB);
                    _slotService.RegisterAlly(entityB, indexA);
                }
            }
        }
    }
}
