using System.Collections.Generic;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.WordInput.Scenarios
{
    internal sealed class ScenarioEncounterAdapter : IEncounterService
    {
        private readonly List<EntityId> _enemies = new();
        private readonly Dictionary<EntityId, EnemyDefinition> _definitions = new();
        private readonly HashSet<EntityId> _dead = new();
        private bool _active;
        private EntityId _player;

        public bool IsEncounterActive => _active;
        public IReadOnlyList<EntityId> EnemyEntities => _enemies;
        public EntityId PlayerEntity => _player;

        public void SetPlayer(EntityId player) => _player = player;

        public void Activate() => _active = true;

        public void RegisterEnemy(EntityId id, EnemyDefinition definition = null)
        {
            if (_definitions.ContainsKey(id)) return;
            definition ??= new EnemyDefinition("SUMMON", 10, 1, 0, 0, 0, 0,
                UnityEngine.Color.red, new[] { "scratch" });
            _enemies.Add(id);
            _definitions[id] = definition;
        }

        public void MarkDead(EntityId id) => _dead.Add(id);

        public bool IsEnemy(EntityId id) =>
            _definitions.ContainsKey(id) && !_dead.Contains(id);

        public EnemyDefinition GetEnemyDefinition(EntityId id) =>
            _definitions.TryGetValue(id, out var def)
                ? def
                : throw new KeyNotFoundException($"'{id.Value}' is not an enemy.");

        // No-ops — scenario manages slots/entities directly
        public void StartEncounter(EncounterDefinition e, EntityId p) { }
        public void EndEncounter() { _active = false; }
    }
}
