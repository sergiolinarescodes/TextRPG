using System;
using System.Collections.Generic;
using TextRPG.Core.CombatGrid;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using Unidad.Core.Grid;
namespace TextRPG.Core.ActionExecution
{
    internal sealed class CombatContext : ICombatContext
    {
        private static readonly Random Rng = new();

        private EntityId _sourceEntity;
        private EntityId? _focusedTarget;
        private GridPosition? _focusedPosition;
        private IReadOnlyList<EntityId> _enemies = Array.Empty<EntityId>();
        private IReadOnlyList<EntityId> _allies = Array.Empty<EntityId>();
        private ICombatGridService _combatGrid;
        private IEntityStatsService _entityStats;
        private IStatusEffectService _statusEffects;

        public EntityId SourceEntity => _sourceEntity;
        public EntityId? FocusedTarget => _focusedTarget;
        public GridPosition? FocusedPosition => _focusedPosition;
        public ICombatGridService CombatGrid => _combatGrid;

        public void SetSourceEntity(EntityId source) => _sourceEntity = source;
        public void SetEnemies(IReadOnlyList<EntityId> enemies) => _enemies = enemies;
        public void SetAllies(IReadOnlyList<EntityId> allies) => _allies = allies;
        public void SetGrid(ICombatGridService grid) => _combatGrid = grid;
        public void SetEntityStats(IEntityStatsService entityStats) => _entityStats = entityStats;
        public void SetStatusEffects(IStatusEffectService statusEffects) => _statusEffects = statusEffects;
        public void SetFocusedTarget(EntityId target) => _focusedTarget = target;
        public void ClearFocusedTarget() => _focusedTarget = null;
        public void SetFocusedPosition(GridPosition position) => _focusedPosition = position;
        public void ClearFocusedPosition() => _focusedPosition = null;

        public IReadOnlyList<EntityId> GetTargets(TargetType targetType, int range = 0)
        {
            var enemies = range > 0 ? FilterByRange(_enemies, range) : _enemies;
            var allies = range > 0 ? FilterByRange(_allies, range) : _allies;
            return ResolveTargets(targetType, enemies, allies);
        }

        public IReadOnlyList<EntityId> ExpandArea(IReadOnlyList<EntityId> primaryTargets, AreaShape area)
        {
            if (area == AreaShape.Single || _combatGrid == null)
                return primaryTargets;

            var result = new HashSet<EntityId>();
            var casterPos = _combatGrid.GetPosition(_sourceEntity);
            int gridHeight = _combatGrid.Grid.Height;

            for (int i = 0; i < primaryTargets.Count; i++)
            {
                var targetPos = _combatGrid.GetPosition(primaryTargets[i]);
                var positions = AreaShapeResolver.GetPositions(targetPos, area, casterPos, gridHeight);

                for (int j = 0; j < positions.Count; j++)
                {
                    if (!_combatGrid.Grid.IsInBounds(positions[j]))
                        continue;

                    var entity = _combatGrid.GetEntityAt(positions[j]);
                    if (entity.HasValue)
                        result.Add(entity.Value);
                }
            }

            var list = new EntityId[result.Count];
            result.CopyTo(list);
            return list;
        }

        private IReadOnlyList<EntityId> FilterByRange(IReadOnlyList<EntityId> candidates, int range)
        {
            if (_combatGrid == null)
                return candidates;

            var sourcePos = _combatGrid.GetPosition(_sourceEntity);
            var result = new List<EntityId>();
            for (int i = 0; i < candidates.Count; i++)
            {
                var pos = _combatGrid.GetPosition(candidates[i]);
                int dist = sourcePos.ManhattanDistanceTo(pos);
                if (dist <= range)
                    result.Add(candidates[i]);
            }
            return result;
        }

        private IReadOnlyList<EntityId> ResolveTargets(TargetType targetType,
            IReadOnlyList<EntityId> enemies, IReadOnlyList<EntityId> allies)
        {
            return targetType switch
            {
                // Basic
                TargetType.Self => new[] { _sourceEntity },
                TargetType.SingleEnemy => PickSingleEnemy(enemies),
                TargetType.AllEnemies => enemies,
                TargetType.All => BuildAll(enemies, allies),
                TargetType.AllAllies => allies,
                TargetType.AllAlliesAndSelf => BuildAlliesAndSelf(allies),

                // Positional
                TargetType.Melee => GetMeleeTargets(enemies),
                TargetType.Area => enemies,

                // Random
                TargetType.RandomEnemy => PickRandom(enemies),
                TargetType.RandomAlly => PickRandom(allies),
                TargetType.RandomAny => PickRandomFromAll(enemies, allies),

                // Stat-based single
                TargetType.LowestHealthEnemy => PickByStat(enemies, StatType.Health, lowest: true),
                TargetType.HighestHealthEnemy => PickByStat(enemies, StatType.Health, lowest: false),
                TargetType.LowestDefenseEnemy => PickByStat(enemies, StatType.PhysicalDefense, lowest: true),
                TargetType.HighestDefenseEnemy => PickByStat(enemies, StatType.PhysicalDefense, lowest: false),
                TargetType.LowestStrengthEnemy => PickByStat(enemies, StatType.Strength, lowest: true),
                TargetType.HighestStrengthEnemy => PickByStat(enemies, StatType.Strength, lowest: false),
                TargetType.LowestMagicEnemy => PickByStat(enemies, StatType.MagicPower, lowest: true),
                TargetType.HighestMagicEnemy => PickByStat(enemies, StatType.MagicPower, lowest: false),

                // Random + stat tiebreakers
                TargetType.RandomLowestHealthEnemy => PickRandomByStat(enemies, StatType.Health, lowest: true),
                TargetType.RandomHighestHealthEnemy => PickRandomByStat(enemies, StatType.Health, lowest: false),

                // Status-based: any status
                TargetType.RandomEnemyWithStatus => PickRandomWithAnyStatus(enemies, hasStatus: true),
                TargetType.RandomEnemyWithoutStatus => PickRandomWithAnyStatus(enemies, hasStatus: false),
                TargetType.AllEnemiesWithStatus => FilterByAnyStatus(enemies, hasStatus: true),
                TargetType.AllEnemiesWithoutStatus => FilterByAnyStatus(enemies, hasStatus: false),

                // Status-based: specific effects
                TargetType.AllBurningEnemies => FilterByStatus(enemies, StatusEffectType.Burning),
                TargetType.AllWetEnemies => FilterByStatus(enemies, StatusEffectType.Wet),
                TargetType.AllPoisonedEnemies => FilterByStatus(enemies, StatusEffectType.Poisoned),
                TargetType.AllFrozenEnemies => FilterByStatus(enemies, StatusEffectType.Frozen),
                TargetType.AllStunnedEnemies => FilterByStatus(enemies, StatusEffectType.Stun),
                TargetType.AllCursedEnemies => FilterByStatus(enemies, StatusEffectType.Cursed),
                TargetType.AllFearfulEnemies => FilterByStatus(enemies, StatusEffectType.Fear),

                // Status + random
                TargetType.RandomBurningEnemy => PickRandomWithStatus(enemies, StatusEffectType.Burning),
                TargetType.RandomWetEnemy => PickRandomWithStatus(enemies, StatusEffectType.Wet),
                TargetType.RandomPoisonedEnemy => PickRandomWithStatus(enemies, StatusEffectType.Poisoned),
                TargetType.RandomFrozenEnemy => PickRandomWithStatus(enemies, StatusEffectType.Frozen),
                TargetType.RandomStunnedEnemy => PickRandomWithStatus(enemies, StatusEffectType.Stun),

                // Status + stat
                TargetType.LowestHealthBurningEnemy => PickByStatWithStatus(enemies, StatType.Health, StatusEffectType.Burning),
                TargetType.LowestHealthPoisonedEnemy => PickByStatWithStatus(enemies, StatType.Health, StatusEffectType.Poisoned),
                TargetType.LowestHealthWetEnemy => PickByStatWithStatus(enemies, StatType.Health, StatusEffectType.Wet),

                // Subset
                TargetType.HalfEnemiesRandom => PickRandomN(enemies, Math.Max(1, enemies.Count / 2)),
                TargetType.TwoRandomEnemies => PickRandomN(enemies, 2),
                TargetType.ThreeRandomEnemies => PickRandomN(enemies, 3),

                _ => Array.Empty<EntityId>()
            };
        }

        // === Single target with focus ===

        private EntityId[] PickSingleEnemy(IReadOnlyList<EntityId> enemies)
        {
            if (enemies.Count == 0) return Array.Empty<EntityId>();
            if (_focusedTarget.HasValue)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i].Equals(_focusedTarget.Value))
                        return new[] { _focusedTarget.Value };
                }
            }
            return new[] { enemies[0] };
        }

        // === Positional ===

        private EntityId[] GetMeleeTargets(IReadOnlyList<EntityId> enemies)
        {
            if (_combatGrid == null)
                return enemies.Count > 0 ? new[] { enemies[0] } : Array.Empty<EntityId>();

            var sourcePos = _combatGrid.GetPosition(_sourceEntity);
            var adjacent = _combatGrid.GetAdjacentEntities(sourcePos);
            var result = new List<EntityId>();
            foreach (var adj in adjacent)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i].Equals(adj))
                    {
                        result.Add(adj);
                        break;
                    }
                }
            }
            return result.ToArray();
        }

        // === Random ===

        private static EntityId[] PickRandom(IReadOnlyList<EntityId> list)
        {
            if (list.Count == 0) return Array.Empty<EntityId>();
            return new[] { list[Rng.Next(list.Count)] };
        }

        private EntityId[] PickRandomFromAll(IReadOnlyList<EntityId> enemies, IReadOnlyList<EntityId> allies)
        {
            var all = BuildAll(enemies, allies);
            if (all.Length == 0) return Array.Empty<EntityId>();
            return new[] { all[Rng.Next(all.Length)] };
        }

        private static EntityId[] PickRandomN(IReadOnlyList<EntityId> list, int n)
        {
            if (list.Count == 0) return Array.Empty<EntityId>();
            n = Math.Min(n, list.Count);

            var indices = new List<int>();
            for (int i = 0; i < list.Count; i++)
                indices.Add(i);

            var result = new EntityId[n];
            for (int i = 0; i < n; i++)
            {
                var idx = Rng.Next(indices.Count);
                result[i] = list[indices[idx]];
                indices.RemoveAt(idx);
            }
            return result;
        }

        // === Stat-based ===

        private EntityId[] PickByStat(IReadOnlyList<EntityId> list, StatType stat, bool lowest)
        {
            if (list.Count == 0 || _entityStats == null) return Array.Empty<EntityId>();

            var best = list[0];
            var bestVal = GetStatValue(best, stat);

            for (int i = 1; i < list.Count; i++)
            {
                var val = GetStatValue(list[i], stat);
                if (lowest ? val < bestVal : val > bestVal)
                {
                    best = list[i];
                    bestVal = val;
                }
            }
            return new[] { best };
        }

        private EntityId[] PickRandomByStat(IReadOnlyList<EntityId> list, StatType stat, bool lowest)
        {
            if (list.Count == 0 || _entityStats == null) return Array.Empty<EntityId>();

            var bestVal = GetStatValue(list[0], stat);
            for (int i = 1; i < list.Count; i++)
            {
                var val = GetStatValue(list[i], stat);
                if (lowest ? val < bestVal : val > bestVal)
                    bestVal = val;
            }

            var tied = new List<EntityId>();
            for (int i = 0; i < list.Count; i++)
            {
                if (GetStatValue(list[i], stat) == bestVal)
                    tied.Add(list[i]);
            }

            return new[] { tied[Rng.Next(tied.Count)] };
        }

        // === Status-based ===

        private EntityId[] FilterByStatus(IReadOnlyList<EntityId> list, StatusEffectType effectType)
        {
            if (_statusEffects == null) return Array.Empty<EntityId>();
            var result = new List<EntityId>();
            for (int i = 0; i < list.Count; i++)
            {
                if (_statusEffects.HasEffect(list[i], effectType))
                    result.Add(list[i]);
            }
            return result.ToArray();
        }

        private EntityId[] FilterByAnyStatus(IReadOnlyList<EntityId> list, bool hasStatus)
        {
            if (_statusEffects == null) return hasStatus ? Array.Empty<EntityId>() : ToArray(list);
            var result = new List<EntityId>();
            for (int i = 0; i < list.Count; i++)
            {
                var entityHasEffects = _statusEffects.GetEffects(list[i]).Count > 0;
                if (entityHasEffects == hasStatus)
                    result.Add(list[i]);
            }
            return result.ToArray();
        }

        private EntityId[] PickRandomWithStatus(IReadOnlyList<EntityId> list, StatusEffectType effectType)
        {
            var filtered = FilterByStatus(list, effectType);
            return PickRandom(filtered);
        }

        private EntityId[] PickRandomWithAnyStatus(IReadOnlyList<EntityId> list, bool hasStatus)
        {
            var filtered = FilterByAnyStatus(list, hasStatus);
            return PickRandom(filtered);
        }

        private EntityId[] PickByStatWithStatus(IReadOnlyList<EntityId> list, StatType stat, StatusEffectType effectType)
        {
            var filtered = FilterByStatus(list, effectType);
            return PickByStat(filtered, stat, lowest: true);
        }

        // === Helpers ===

        private int GetStatValue(EntityId entity, StatType stat)
        {
            return stat == StatType.Health
                ? _entityStats.GetCurrentHealth(entity)
                : _entityStats.GetStat(entity, stat);
        }

        private EntityId[] BuildAll(IReadOnlyList<EntityId> enemies, IReadOnlyList<EntityId> allies)
        {
            var result = new List<EntityId>(enemies.Count + allies.Count + 1);
            for (int i = 0; i < enemies.Count; i++)
                result.Add(enemies[i]);
            for (int i = 0; i < allies.Count; i++)
                result.Add(allies[i]);
            result.Add(_sourceEntity);
            return result.ToArray();
        }

        private EntityId[] BuildAlliesAndSelf(IReadOnlyList<EntityId> allies)
        {
            var result = new List<EntityId>(allies.Count + 1);
            for (int i = 0; i < allies.Count; i++)
                result.Add(allies[i]);
            result.Add(_sourceEntity);
            return result.ToArray();
        }

        private static EntityId[] ToArray(IReadOnlyList<EntityId> list)
        {
            var result = new EntityId[list.Count];
            for (int i = 0; i < list.Count; i++)
                result[i] = list[i];
            return result;
        }
    }
}
