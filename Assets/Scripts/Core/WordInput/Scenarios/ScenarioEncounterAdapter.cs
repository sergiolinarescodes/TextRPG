using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;

namespace TextRPG.Core.WordInput.Scenarios
{
    internal sealed class ScenarioEncounterAdapter : IEncounterService
    {
        private readonly List<EntityId> _enemies = new();
        private readonly Dictionary<EntityId, EntityDefinition> _definitions = new();
        private readonly HashSet<EntityId> _dead = new();
        private bool _active;
        private EntityId _player;
        private IEventBus _eventBus;

        public bool IsEncounterActive => _active;
        public IReadOnlyList<EntityId> EnemyEntities => _enemies;
        public EntityId PlayerEntity => _player;

        public void SetPlayer(EntityId player) => _player = player;
        public void SetEventBus(IEventBus eventBus) => _eventBus = eventBus;

        public void Activate() => _active = true;

        public void RegisterEnemy(EntityId id, EntityDefinition definition = null)
        {
            if (_definitions.ContainsKey(id)) return;
            definition ??= new EntityDefinition("SUMMON", 10, 1, 0, 0, 0, 0,
                UnityEngine.Color.red, new[] { "scratch" });
            _enemies.Add(id);
            _definitions[id] = definition;
        }

        public void MarkDead(EntityId id)
        {
            _dead.Add(id);

            if (_active && _enemies.All(e => _dead.Contains(e)))
            {
                _active = false;
                _eventBus?.Publish(new EncounterEndedEvent("scenario", true));
            }
        }

        public bool IsEnemy(EntityId id) =>
            _definitions.ContainsKey(id) && !_dead.Contains(id);

        public EntityDefinition GetEntityDefinition(EntityId id) =>
            _definitions.TryGetValue(id, out var def)
                ? def
                : throw new KeyNotFoundException($"'{id.Value}' is not an enemy.");

        // No-ops — scenario manages slots/entities directly
        public void StartEncounter(EncounterDefinition e, EntityId p) { }
        public void EndEncounter() { _active = false; }
    }
}
