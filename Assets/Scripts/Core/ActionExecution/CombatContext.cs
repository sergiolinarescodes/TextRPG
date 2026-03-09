using System;
using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive;
using TextRPG.Core.StatusEffect;
namespace TextRPG.Core.ActionExecution
{
    internal sealed class CombatContext : ICombatContext
    {
        private static readonly Random Rng = new();

        private EntityId _sourceEntity;
        private EntityId? _focusedTarget;
        private IReadOnlyList<EntityId> _enemies = Array.Empty<EntityId>();
        private IReadOnlyList<EntityId> _allies = Array.Empty<EntityId>();
        private ICombatSlotService _slotService;
        private IEntityStatsService _entityStats;
        private IStatusEffectService _statusEffects;
        private IPassiveService _passiveService;

        public EntityId SourceEntity => _sourceEntity;
        public EntityId? FocusedTarget => _focusedTarget;
        public ICombatSlotService SlotService => _slotService;

        public void SetSourceEntity(EntityId source) => _sourceEntity = source;
        public void SetEnemies(IReadOnlyList<EntityId> enemies) => _enemies = enemies;
        public void SetAllies(IReadOnlyList<EntityId> allies) => _allies = allies;
        public void SetSlotService(ICombatSlotService slotService) => _slotService = slotService;
        public void SetEntityStats(IEntityStatsService entityStats) => _entityStats = entityStats;
        public void SetStatusEffects(IStatusEffectService statusEffects) => _statusEffects = statusEffects;
        public void SetFocusedTarget(EntityId target) => _focusedTarget = target;
        public void ClearFocusedTarget() => _focusedTarget = null;
        public void SetPassiveService(IPassiveService passiveService) => _passiveService = passiveService;

        public IReadOnlyList<EntityId> GetTargets(TargetType targetType, int range = 0, StatusEffectType? statusFilter = null)
        {
            if (IsSingleEnemyTarget(targetType))
            {
                var taunt = FindTauntTarget(_enemies);
                if (taunt.HasValue) return new[] { taunt.Value };
            }

            var enemies = _enemies;
            var allies = _allies;
            if (statusFilter.HasValue)
                enemies = FilterByStatus(enemies, statusFilter.Value);
            return ResolveTargets(targetType, enemies, allies);
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

                // Positional (slot-based)
                TargetType.FrontEnemy => PickSlotEnemy(enemies, 0),
                TargetType.MiddleEnemy => PickSlotEnemy(enemies, 1),
                TargetType.BackEnemy => PickSlotEnemy(enemies, 2),
                TargetType.Melee => PickSlotEnemy(enemies, 0),
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

                // Subset
                TargetType.HalfEnemiesRandom => PickRandomN(enemies, Math.Max(1, enemies.Count / 2)),
                TargetType.TwoRandomEnemies => PickRandomN(enemies, 2),
                TargetType.ThreeRandomEnemies => PickRandomN(enemies, 3),

                _ => Array.Empty<EntityId>()
            };
        }

        // === Weighted targeting ===

        private const float PlayerTargetWeight = 0.7f;

        private bool IsSourceEnemy()
        {
            if (_slotService == null) return false;
            var slot = _slotService.GetSlot(_sourceEntity);
            return slot.HasValue && slot.Value.Type == SlotType.Enemy;
        }

        private EntityId[] PickWeightedTarget(IReadOnlyList<EntityId> enemies)
        {
            if (enemies.Count == 0) return Array.Empty<EntityId>();
            if (enemies.Count == 1) return new[] { enemies[0] };
            if (!IsSourceEnemy()) return new[] { enemies[0] };

            // enemies[0] = player, enemies[1..n] = summons
            if (Rng.NextDouble() < PlayerTargetWeight)
                return new[] { enemies[0] };

            return new[] { enemies[1 + Rng.Next(enemies.Count - 1)] };
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
            return PickWeightedTarget(enemies);
        }

        // === Slot-based positional ===

        private EntityId[] PickSlotEnemy(IReadOnlyList<EntityId> enemies, int slotIndex)
        {
            if (_slotService != null)
            {
                var sourceSlot = _slotService.GetSlot(_sourceEntity);
                if (sourceSlot.HasValue && sourceSlot.Value.Type == SlotType.Enemy)
                {
                    // Enemy attacking: weighted random between player and summons
                    return PickWeightedTarget(enemies);
                }

                // Player using melee: target enemy slots positionally
                var entity = _slotService.FindNearestOccupiedSlot(SlotType.Enemy, slotIndex);
                if (entity.HasValue) return new[] { entity.Value };
            }
            return enemies.Count > 0 ? new[] { enemies[0] } : Array.Empty<EntityId>();
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

        private EntityId[] PickRandomWithAnyStatus(IReadOnlyList<EntityId> list, bool hasStatus)
        {
            var filtered = FilterByAnyStatus(list, hasStatus);
            return PickRandom(filtered);
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

        // === Taunt ===

        private EntityId? FindTauntTarget(IReadOnlyList<EntityId> enemies)
        {
            if (_passiveService == null) return null;
            for (int i = 0; i < enemies.Count; i++)
            {
                var entity = enemies[i];
                if (_entityStats == null || !_entityStats.HasEntity(entity) || _entityStats.GetCurrentHealth(entity) <= 0)
                    continue;
                if (_passiveService.HasTaunt(entity))
                    return entity;
            }
            return null;
        }

        private static bool IsSingleEnemyTarget(TargetType targetType)
        {
            return targetType switch
            {
                TargetType.SingleEnemy => true,
                TargetType.FrontEnemy => true,
                TargetType.MiddleEnemy => true,
                TargetType.BackEnemy => true,
                TargetType.Melee => true,
                TargetType.RandomEnemy => true,
                TargetType.LowestHealthEnemy => true,
                TargetType.HighestHealthEnemy => true,
                TargetType.LowestDefenseEnemy => true,
                TargetType.HighestDefenseEnemy => true,
                TargetType.LowestStrengthEnemy => true,
                TargetType.HighestStrengthEnemy => true,
                TargetType.LowestMagicEnemy => true,
                TargetType.HighestMagicEnemy => true,
                TargetType.RandomLowestHealthEnemy => true,
                TargetType.RandomHighestHealthEnemy => true,
                TargetType.RandomEnemyWithStatus => true,
                TargetType.RandomEnemyWithoutStatus => true,
                _ => false
            };
        }
    }
}
