using System;
using Unidad.Core.EventBus;
using Unidad.Core.Grid;
using Unidad.Core.Registry;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class UnitService : IUnitService
    {
        private readonly IEventBus _eventBus;
        private readonly RegistryBase<UnitId, UnitInstance> _registry = new();

        public UnitService(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public UnitInstance PlaceUnit(UnitDefinition definition, GridPosition position, IGrid<UnitId?> grid)
        {
            if (!grid.IsInBounds(position))
                throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is out of bounds.");

            if (grid.Get(position) != null)
                throw new InvalidOperationException($"Position {position} is already occupied.");

            var instance = new UnitInstance(definition, position);
            _registry.Register(definition.Id, instance);
            grid.Set(position, definition.Id);
            _eventBus.Publish(new UnitPlacedEvent(definition.Id, position));
            return instance;
        }

        public void RemoveUnit(UnitId id, IGrid<UnitId?> grid)
        {
            var instance = _registry.Get(id);
            grid.Set(instance.Position, null);
            _registry.Remove(id);
            _eventBus.Publish(new UnitRemovedEvent(id, instance.Position));
        }

        public void MoveUnit(UnitId id, GridPosition to, IGrid<UnitId?> grid)
        {
            if (!grid.IsInBounds(to))
                throw new ArgumentOutOfRangeException(nameof(to), $"Position {to} is out of bounds.");

            if (grid.Get(to) != null)
                throw new InvalidOperationException($"Position {to} is already occupied.");

            var instance = _registry.Get(id);
            var from = instance.Position;
            grid.Set(from, null);
            grid.Set(to, id);
            instance.Position = to;
            _eventBus.Publish(new UnitMovedEvent(id, from, to));
        }

        public UnitInstance GetUnit(UnitId id) => _registry.Get(id);

        public bool TryGetUnit(UnitId id, out UnitInstance instance) => _registry.TryGet(id, out instance);

        public bool HasUnitAt(GridPosition position, IGrid<UnitId?> grid) => grid.Get(position) != null;

        public UnitInstance GetUnitAt(GridPosition position, IGrid<UnitId?> grid)
        {
            var unitId = grid.Get(position);
            if (unitId == null)
                throw new InvalidOperationException($"No unit at position {position}.");
            return _registry.Get(unitId.Value);
        }
    }
}
