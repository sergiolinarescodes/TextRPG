using System;
using System.Collections.Generic;

namespace TextRPG.Core.EventEncounter.Reactions
{
    internal sealed class TagReactionRegistry
    {
        private readonly Dictionary<(string Tag, string ActionId), List<InteractionReaction>> _reactions = new(TagActionComparer.Instance);

        public void Register(string tag, string actionId, InteractionReaction reaction)
        {
            var key = (tag, actionId);
            if (!_reactions.TryGetValue(key, out var list))
            {
                list = new List<InteractionReaction>();
                _reactions[key] = list;
            }
            list.Add(reaction);
        }

        public IReadOnlyList<InteractionReaction> GetReactions(string tag, string actionId)
        {
            return _reactions.TryGetValue((tag, actionId), out var list) ? list : Array.Empty<InteractionReaction>();
        }

        private sealed class TagActionComparer : IEqualityComparer<(string Tag, string ActionId)>
        {
            public static readonly TagActionComparer Instance = new();

            public bool Equals((string Tag, string ActionId) x, (string Tag, string ActionId) y) =>
                string.Equals(x.Tag, y.Tag, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.ActionId, y.ActionId, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string Tag, string ActionId) obj) =>
                HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Tag),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ActionId));
        }
    }
}
