using System;
using System.Collections.Generic;
using Unidad.Core.Grid;

namespace TextRPG.Core.ActionExecution
{
    internal static class AreaShapeResolver
    {
        public static IReadOnlyList<GridPosition> GetPositions(GridPosition center, AreaShape shape,
            GridPosition? casterPos, int gridHeight = 0)
        {
            return shape switch
            {
                AreaShape.Single => new[] { center },
                AreaShape.Cross => GetCross(center),
                AreaShape.Square3x3 => GetSquare3x3(center),
                AreaShape.Diamond2 => GetDiamond2(center),
                AreaShape.Line3 => GetLine3(center, casterPos),
                AreaShape.VerticalLine => GetVerticalLine(center, gridHeight),
                _ => new[] { center }
            };
        }

        private static GridPosition[] GetCross(GridPosition center)
        {
            return new[]
            {
                center,
                new GridPosition(center.X, center.Y + 1),
                new GridPosition(center.X + 1, center.Y),
                new GridPosition(center.X, center.Y - 1),
                new GridPosition(center.X - 1, center.Y),
            };
        }

        private static GridPosition[] GetSquare3x3(GridPosition center)
        {
            var result = new GridPosition[9];
            int idx = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    result[idx++] = new GridPosition(center.X + dx, center.Y + dy);
                }
            }
            return result;
        }

        private static GridPosition[] GetDiamond2(GridPosition center)
        {
            var result = new List<GridPosition>();
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    if (Math.Abs(dx) + Math.Abs(dy) <= 2)
                        result.Add(new GridPosition(center.X + dx, center.Y + dy));
                }
            }
            return result.ToArray();
        }

        private static GridPosition[] GetLine3(GridPosition center, GridPosition? casterPos)
        {
            if (casterPos == null)
                return new[] { center };

            var cp = casterPos.Value;
            int rawDx = center.X - cp.X;
            int rawDy = center.Y - cp.Y;

            int dirX = Math.Sign(rawDx);
            int dirY = Math.Sign(rawDy);

            if (dirX == 0 && dirY == 0)
                return new[] { center };

            var result = new GridPosition[3];
            for (int i = 0; i < 3; i++)
            {
                result[i] = new GridPosition(center.X + dirX * i, center.Y + dirY * i);
            }
            return result;
        }

        private static GridPosition[] GetVerticalLine(GridPosition center, int gridHeight)
        {
            if (gridHeight <= 0)
                return new[] { center };

            // Full vertical line at target's X column, from row 0 to grid height
            var result = new GridPosition[gridHeight];
            for (int y = 0; y < gridHeight; y++)
            {
                result[y] = new GridPosition(center.X, y);
            }
            return result;
        }
    }
}
