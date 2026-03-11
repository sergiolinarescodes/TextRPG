using Unidad.Core.EventBus;

namespace TextRPG.Core.WordCooldown
{
    internal static class WordCooldownHelper
    {
        public static bool TryRejectCooldown(IWordCooldownService cooldown, string word, int round, IEventBus eventBus)
        {
            if (cooldown == null || cooldown.CanUseWord(word, round)) return false;
            var remaining = cooldown.GetRemainingCooldown(word, round);
            eventBus.Publish(new WordCooldownEvent(word, remaining, cooldown.IsPermanentlyBanned(word)));
            return true;
        }
    }
}
