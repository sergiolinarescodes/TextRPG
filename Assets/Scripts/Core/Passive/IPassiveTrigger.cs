using System;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive
{
    public interface IPassiveTrigger
    {
        string TriggerId { get; }
        IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                              Action<PassiveTriggerContext> onTriggered);
    }
}
