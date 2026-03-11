using System.Collections.Generic;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.EventEncounter.Reactions.Tags
{
    internal sealed class TagStateStore
    {
        private readonly Dictionary<(string TagId, EntityId Entity), Dictionary<string, int>> _state = new();

        public int Get(string tagId, EntityId entity, string key)
        {
            if (_state.TryGetValue((tagId, entity), out var dict) && dict.TryGetValue(key, out var val))
                return val;
            return 0;
        }

        public void Set(string tagId, EntityId entity, string key, int value)
        {
            var stateKey = (tagId, entity);
            if (!_state.TryGetValue(stateKey, out var dict))
            {
                dict = new Dictionary<string, int>();
                _state[stateKey] = dict;
            }
            dict[key] = value;
        }

        public int Increment(string tagId, EntityId entity, string key, int amount = 1)
        {
            var stateKey = (tagId, entity);
            if (!_state.TryGetValue(stateKey, out var dict))
            {
                dict = new Dictionary<string, int>();
                _state[stateKey] = dict;
            }

            dict.TryGetValue(key, out var current);
            var newValue = current + amount;
            dict[key] = newValue;
            return newValue;
        }

        public void ClearEntity(EntityId entity)
        {
            var toRemove = new List<(string, EntityId)>();
            foreach (var key in _state.Keys)
            {
                if (key.Entity.Equals(entity))
                    toRemove.Add(key);
            }
            for (int i = 0; i < toRemove.Count; i++)
                _state.Remove(toRemove[i]);
        }

        public void ClearAll() => _state.Clear();
    }
}
