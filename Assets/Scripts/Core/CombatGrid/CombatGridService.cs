using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.UnitRendering;
using Unidad.Core.EventBus;
using Unidad.Core.Grid;
using Unidad.Core.Systems;

namespace TextRPG.Core.CombatGrid
{
    internal sealed class CombatGridService : SystemServiceBase, ICombatGridService
    {
        private readonly IUnitService _unitService;
        private IGrid<UnitId?> _grid;
        private readonly Dictionary<EntityId, UnitId> _entityToUnit = new();
        private readonly Dictionary<UnitId, EntityId> _unitToEntity = new();

        public IGrid<UnitId?> Grid => _grid;

        public CombatGridService(IEventBus eventBus, IUnitService unitService)
            : base(eventBus)
        {
            _unitService = unitService;
        }

        public void Initialize(int width, int height)
        {
            _grid = new GridFactory(EventBus).Create<UnitId?>(width, height, 1f);
            _entityToUnit.Clear();
            _unitToEntity.Clear();
            Publish(new CombatGridInitializedEvent(width, height));
        }

        public void RegisterCombatant(EntityId entityId, UnitDefinition unitDef, GridPosition position)
        {
            if (_grid == null)
                throw new InvalidOperationException("Grid has not been initialized.");

            _unitService.PlaceUnit(unitDef, position, _grid);
            _entityToUnit[entityId] = unitDef.Id;
            _unitToEntity[unitDef.Id] = entityId;
            Publish(new CombatantRegisteredEvent(entityId, position));
        }

        public void RemoveCombatant(EntityId entityId)
        {
            if (!_entityToUnit.TryGetValue(entityId, out var unitId))
                throw new KeyNotFoundException($"Entity '{entityId.Value}' is not a combatant.");

            var instance = _unitService.GetUnit(unitId);
            var pos = instance.Position;
            _unitService.RemoveUnit(unitId, _grid);
            _entityToUnit.Remove(entityId);
            _unitToEntity.Remove(unitId);
            Publish(new CombatantRemovedEvent(entityId, pos));
        }

        public GridPosition GetPosition(EntityId entityId)
        {
            if (!_entityToUnit.TryGetValue(entityId, out var unitId))
                throw new KeyNotFoundException($"Entity '{entityId.Value}' is not a combatant.");
            return _unitService.GetUnit(unitId).Position;
        }

        public EntityId? GetEntityAt(GridPosition position)
        {
            if (_grid == null || !_grid.IsInBounds(position))
                return null;

            var unitId = _grid.Get(position);
            if (unitId == null)
                return null;

            return _unitToEntity.TryGetValue(unitId.Value, out var entityId) ? entityId : null;
        }

        public IReadOnlyList<EntityId> GetEntitiesInRange(GridPosition center, int range)
        {
            var result = new List<EntityId>();
            if (_grid == null) return result;

            foreach (var pos in _grid.AllPositions)
            {
                if (center.ManhattanDistanceTo(pos) <= range)
                {
                    var unitId = _grid.Get(pos);
                    if (unitId != null && _unitToEntity.TryGetValue(unitId.Value, out var entityId))
                        result.Add(entityId);
                }
            }
            return result;
        }

        public IReadOnlyList<EntityId> GetAdjacentEntities(GridPosition position)
        {
            var result = new List<EntityId>();
            if (_grid == null) return result;

            foreach (var neighbor in _grid.GetNeighbors(position, NeighborMode.Cardinal))
            {
                var unitId = _grid.Get(neighbor);
                if (unitId != null && _unitToEntity.TryGetValue(unitId.Value, out var entityId))
                    result.Add(entityId);
            }
            return result;
        }

        public int GetDistance(EntityId a, EntityId b)
        {
            var posA = GetPosition(a);
            var posB = GetPosition(b);
            return posA.ManhattanDistanceTo(posB);
        }

        public bool CanMoveTo(GridPosition position)
        {
            return _grid != null && _grid.IsInBounds(position) && _grid.Get(position) == null;
        }

        public void MoveEntity(EntityId entityId, GridPosition to)
        {
            if (!_entityToUnit.TryGetValue(entityId, out var unitId))
                throw new KeyNotFoundException($"Entity '{entityId.Value}' is not a combatant.");

            var from = _unitService.GetUnit(unitId).Position;
            _unitService.MoveUnit(unitId, to, _grid);
            Publish(new CombatantMovedEvent(entityId, from, to));
        }

    }
}
