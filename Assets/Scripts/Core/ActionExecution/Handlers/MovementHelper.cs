using System.Collections.Generic;
using TextRPG.Core.CombatGrid;
using TextRPG.Core.EntityStats;
using Unidad.Core.Grid;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal static class MovementHelper
    {
        public static GridPosition StepToward(ICombatGridService grid, EntityId entityId, GridPosition target, int maxSteps)
        {
            for (int step = 0; step < maxSteps; step++)
            {
                var currentPos = grid.GetPosition(entityId);
                if (currentPos.ManhattanDistanceTo(target) <= 1)
                    break;

                var bestPos = currentPos;
                var bestDist = int.MaxValue;

                foreach (var neighbor in grid.Grid.GetNeighbors(currentPos, NeighborMode.Cardinal))
                {
                    if (!grid.CanMoveTo(neighbor))
                        continue;

                    var dist = neighbor.ManhattanDistanceTo(target);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestPos = neighbor;
                    }
                }

                if (bestPos.Equals(currentPos))
                    break;

                grid.MoveEntity(entityId, bestPos);
            }

            return grid.GetPosition(entityId);
        }

        public static List<GridPosition> FindEmptyAdjacentTo(ICombatGridService grid, GridPosition position)
        {
            var result = new List<GridPosition>();
            foreach (var neighbor in grid.Grid.GetNeighbors(position, NeighborMode.Cardinal))
            {
                if (grid.CanMoveTo(neighbor))
                    result.Add(neighbor);
            }
            return result;
        }

        public static EntityId? FindNearestEntity(ICombatGridService grid, GridPosition from, IReadOnlyList<EntityId> entities)
        {
            EntityId? nearest = null;
            var bestDist = int.MaxValue;

            for (int i = 0; i < entities.Count; i++)
            {
                var pos = grid.GetPosition(entities[i]);
                var dist = from.ManhattanDistanceTo(pos);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = entities[i];
                }
            }

            return nearest;
        }

        public static GridPosition FindBehindPosition(GridPosition source, GridPosition target)
        {
            var dx = target.X - source.X;
            var dy = target.Y - source.Y;

            var stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
            var stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

            if (stepX == 0 && stepY == 0)
                return new GridPosition(target.X + 1, target.Y);

            return new GridPosition(target.X + stepX, target.Y + stepY);
        }

        public static List<GridPosition> CollectEmptyInRange(ICombatGridService grid, GridPosition center, int range)
        {
            var result = new List<GridPosition>();
            foreach (var pos in grid.Grid.AllPositions)
            {
                if (center.ManhattanDistanceTo(pos) <= range && grid.CanMoveTo(pos))
                    result.Add(pos);
            }
            return result;
        }
    }
}
