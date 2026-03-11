using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using Unidad.Core.EventBus;
using Unidad.Core.Resource;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.EventEncounter.Reactions.Tags
{
    public sealed class TagReactionContext
    {
        public EntityId Source { get; }
        public EntityId Target { get; }
        public string ActionId { get; }
        public int Value { get; }

        public IEntityStatsService EntityStats { get; }
        public IEventBus EventBus { get; }
        public ICombatSlotService SlotService { get; }
        public IStatusEffectService StatusEffects { get; }
        public IResourceService Resources { get; }
        public IEventEncounterService EncounterService { get; }

        private readonly TagStateStore _stateStore;
        private readonly string _tagId;

        internal TagReactionContext(
            EntityId source,
            EntityId target,
            string actionId,
            int value,
            IEventEncounterContext ctx,
            TagStateStore stateStore,
            string tagId)
        {
            Source = source;
            Target = target;
            ActionId = actionId;
            Value = value;

            EntityStats = ctx.EntityStats;
            EventBus = ctx.EventBus;
            SlotService = ctx.SlotService;
            StatusEffects = ctx.StatusEffects;
            Resources = ctx.ResourceService;
            EncounterService = ctx.EncounterService;

            _stateStore = stateStore;
            _tagId = tagId;
        }

        public int GetState(string key) => _stateStore.Get(_tagId, Target, key);
        public void SetState(string key, int value) => _stateStore.Set(_tagId, Target, key, value);
        public int IncrementState(string key, int amount = 1) => _stateStore.Increment(_tagId, Target, key, amount);
    }
}
