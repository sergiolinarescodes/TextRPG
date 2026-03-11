using System;
using System.Collections.Generic;
using TextRPG.Core.EventEncounter.Reactions.Tags;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.EventEncounter.Reactions
{
    internal sealed class TagReactionRegistry
    {
        private readonly Dictionary<string, ITagDefinition> _tags = new(StringComparer.OrdinalIgnoreCase);
        private readonly TagStateStore _stateStore = new();

        public void Register(ITagDefinition tag)
        {
            _tags[tag.TagId] = tag;
        }

        public bool TryGet(string tagId, out ITagDefinition tag) => _tags.TryGetValue(tagId, out tag);

        public TagStateStore StateStore => _stateStore;

        public void ClearEntityState(EntityId entity) => _stateStore.ClearEntity(entity);

        public void ClearAllState() => _stateStore.ClearAll();
    }
}
