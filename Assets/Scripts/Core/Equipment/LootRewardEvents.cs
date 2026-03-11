namespace TextRPG.Core.Equipment
{
    public readonly record struct LootRewardOfferedEvent(LootRewardOption[] Options);
    public readonly record struct LootRewardSelectedEvent(LootRewardOption Selected);
}
