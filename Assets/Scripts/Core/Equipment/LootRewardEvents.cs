namespace TextRPG.Core.Equipment
{
    public readonly record struct LootRewardOfferedEvent(EquipmentItemDefinition[] Options);
    public readonly record struct LootRewardSelectedEvent(EquipmentItemDefinition Selected);
}
