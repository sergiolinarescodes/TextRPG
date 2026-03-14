using System;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
using TextRPG.Core.Weapon;
using Unidad.Core.EventBus;

namespace TextRPG.Core.Passive.Triggers
{
    [AutoScan]
    internal sealed class OnWeaponFireTrigger : IPassiveTrigger
    {
        public string TriggerId => "on_weapon_fire";

        public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                      Action<PassiveTriggerContext> onTriggered)
        {
            return ctx.EventBus.Subscribe<WeaponAmmoSubmittedEvent>(evt =>
            {
                if (!ctx.EntityStats.HasEntity(owner) || ctx.EntityStats.GetCurrentHealth(owner) <= 0) return;
                if (!PassiveTargetResolver.IsSameFaction(owner, evt.Source, ctx)) return;

                onTriggered(new PassiveTriggerContext(owner, null, evt.Source, evt.AmmoWord));
            });
        }
    }
}
