using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordCooldown;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.Scroll
{
    internal sealed class SpellService : SystemServiceBase, ISpellService
    {
        private readonly IWordResolver _baseResolver;
        private readonly IWordCooldownService _cooldownService;
        private readonly SpellWordResolver _spellResolver;
        private readonly IWordTagResolver _tagResolver;
        private readonly HashSet<string> _learnedSpells = new();
        private readonly HashSet<string> _offeredOriginals = new();

        public IReadOnlyList<string> LearnedSpells => _learnedSpells.ToList();
        public HashSet<string> OfferedOriginals => _offeredOriginals;

        public SpellService(
            IEventBus eventBus,
            IWordResolver baseResolver,
            IWordCooldownService cooldownService,
            SpellWordResolver spellResolver,
            IWordTagResolver tagResolver = null) : base(eventBus)
        {
            _baseResolver = baseResolver;
            _cooldownService = cooldownService;
            _spellResolver = spellResolver;
            _tagResolver = tagResolver;
        }

        public void LearnSpell(ScrollDefinition scroll)
        {
            var originalActions = _baseResolver.Resolve(scroll.OriginalWord);
            if (originalActions.Count == 0) return;

            var actions = new List<WordActionMapping>(originalActions);
            var originalMeta = _baseResolver.GetStats(scroll.OriginalWord);
            var meta = new WordMeta(originalMeta.Target, scroll.ManaCost, originalMeta.Range, originalMeta.Area);

            _spellResolver.RegisterWord(scroll.ScrambledWord, actions, meta);
            _cooldownService.RegisterFixedCooldown(scroll.ScrambledWord, 2);
            _learnedSpells.Add(scroll.ScrambledWord);
            _offeredOriginals.Add(scroll.OriginalWord);

            _tagResolver?.AddTag(scroll.ScrambledWord, "SPELL");

            Publish(new SpellLearnedEvent(scroll.ScrambledWord, scroll.OriginalWord, scroll.ManaCost));
        }

        public bool IsSpell(string word)
        {
            return word != null && _learnedSpells.Contains(word);
        }
    }
}
