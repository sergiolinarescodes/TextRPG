using System;
using System.Collections.Generic;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using Unidad.Core.Grid;

namespace TextRPG.Core.WordInput.Scenarios
{
    internal sealed class ScenarioEncounterAdapter : IEncounterService
    {
        private readonly List<EntityId> _enemies = new();
        private readonly Dictionary<EntityId, EnemyDefinition> _definitions = new();
        private readonly HashSet<EntityId> _dead = new();
        private bool _active;

        public bool IsEncounterActive => _active;
        public IReadOnlyList<EntityId> EnemyEntities => _enemies;

        public void Activate() => _active = true;

        public void RegisterEnemy(EntityId id, EnemyDefinition def)
        {
            _enemies.Add(id);
            _definitions[id] = def;
        }

        public void MarkDead(EntityId id) => _dead.Add(id);

        public bool IsEnemy(EntityId id) =>
            _definitions.ContainsKey(id) && !_dead.Contains(id);

        public EnemyDefinition GetEnemyDefinition(EntityId id) =>
            _definitions.TryGetValue(id, out var def)
                ? def
                : throw new KeyNotFoundException($"'{id.Value}' is not an enemy.");

        // No-ops — scenario manages grid/entities directly
        public void StartEncounter(EncounterDefinition e, EntityId p, GridPosition pos) { }
        public void EndEncounter() { _active = false; }
    }
}
