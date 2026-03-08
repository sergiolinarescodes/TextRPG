using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.Passive
{
    internal sealed class PassiveService : SystemServiceBase, IPassiveService
    {
        private readonly IReadOnlyDictionary<string, IPassiveHandler> _handlerRegistry;
        private readonly IPassiveContext _context;
        private readonly IReadOnlyDictionary<string, EnemyDefinition> _unitRegistry;
        private readonly Dictionary<EntityId, List<PassiveEntry>> _activePassives = new();

        public PassiveService(
            IEventBus eventBus,
            IReadOnlyDictionary<string, IPassiveHandler> handlerRegistry,
            IPassiveContext context,
            IReadOnlyDictionary<string, EnemyDefinition> unitRegistry = null)
            : base(eventBus)
        {
            _handlerRegistry = handlerRegistry;
            _context = context;
            _unitRegistry = unitRegistry;

            Subscribe<EntityDiedEvent>(OnEntityDied);
            Subscribe<UnitSummonedEvent>(OnUnitSummoned);
        }

        public void RegisterPassives(EntityId entityId, PassiveEntry[] passives)
        {
            if (passives == null || passives.Length == 0) return;

            var list = new List<PassiveEntry>();
            foreach (var entry in passives)
            {
                if (_handlerRegistry.TryGetValue(entry.PassiveId, out var handler))
                {
                    handler.Register(entityId, entry.Value, _context);
                    list.Add(entry);
                }
            }

            if (list.Count > 0)
                _activePassives[entityId] = list;
        }

        public void RemovePassives(EntityId entityId)
        {
            if (!_activePassives.TryGetValue(entityId, out var list)) return;

            foreach (var entry in list)
            {
                if (_handlerRegistry.TryGetValue(entry.PassiveId, out var handler))
                    handler.Unregister(entityId, _context);
            }

            _activePassives.Remove(entityId);
        }

        public bool HasPassives(EntityId entityId) => _activePassives.ContainsKey(entityId);

        public IReadOnlyList<PassiveEntry> GetPassives(EntityId entityId)
        {
            if (!_activePassives.TryGetValue(entityId, out var list))
                return Array.Empty<PassiveEntry>();
            return list;
        }

        private void OnEntityDied(EntityDiedEvent e) => RemovePassives(e.EntityId);

        private void OnUnitSummoned(UnitSummonedEvent e)
        {
            if (_unitRegistry == null) return;
            if (e.Word.Length == 0) return;

            var key = e.Word.ToLowerInvariant();
            if (!_unitRegistry.TryGetValue(key, out var def)) return;
            if (def.Passives == null || def.Passives.Length == 0) return;

            RegisterPassives(e.EntityId, def.Passives);
        }
    }
}
