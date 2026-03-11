---
name: create-reaction
description: Guide for creating new event encounter reactions — tag definitions and interaction outcomes. Use when adding a new tag reaction, interaction outcome, or event encounter behavior.
user-invocable: false
---

# Creating New Reactions

Two extensible components, both using `[AutoScan]` — no manual registration needed.

## Tag Definitions

Tags define how interactables react to specific actions (e.g., "breakable" objects shatter after 2 hits).

**Interface:**
```csharp
public interface ITagDefinition
{
    string TagId { get; }
    void React(TagReactionContext context);
}
```

**Steps:**
1. Create `internal sealed class` in `Reactions/Tags/Definitions/`
2. Implement `ITagDefinition`
3. Add `[AutoScan]` attribute
4. Done — `AssemblyScanner` discovers it automatically

**Example — BreakableTagDefinition:**
```csharp
[AutoScan]
internal sealed class BreakableTagDefinition : ITagDefinition
{
    private static readonly HashSet<string> ReactActions =
        new(StringComparer.OrdinalIgnoreCase) { "Damage", "Smash" };

    public string TagId => "breakable";

    public void React(TagReactionContext ctx)
    {
        if (!ReactActions.Contains(ctx.ActionId)) return;

        var hits = ctx.IncrementState("hits");
        if (hits >= 2)
        {
            var hp = ctx.EntityStats.GetStat(ctx.Target, EntityStats.StatType.Health);
            ctx.EntityStats.ApplyDamage(ctx.Target, hp, ctx.Source);
            ctx.EventBus.Publish(new InteractionMessageEvent("It shatters!", ctx.Target));
        }
        else
        {
            ctx.EventBus.Publish(new InteractionMessageEvent("It cracks under the force!", ctx.Target));
        }
    }
}
```

**TagReactionContext fields:**
- `Source` / `Target` — `EntityId` of actor and interactable
- `ActionId` — action that triggered the reaction
- `Value` — numeric value from the action
- `EntityStats`, `EventBus`, `SlotService`, `StatusEffects`, `Resources`, `EncounterService` — services
- `GetState(key)` / `SetState(key, value)` / `IncrementState(key)` — per-tag, per-entity persistent state

**Existing tags:** flammable, breakable, conductive, meltable, mercenary, social

**DB — unit_tags:**
```sql
CREATE TABLE unit_tags (
    unit_id TEXT NOT NULL,
    tag     TEXT NOT NULL,
    PRIMARY KEY (unit_id, tag),
    FOREIGN KEY (unit_id) REFERENCES units(unit_id)
);

-- Example: make "golem" breakable
INSERT INTO unit_tags(unit_id, tag) VALUES ('golem', 'breakable');
```

---

## Interaction Outcomes

Outcomes define what happens when a reaction fires (damage player, heal, show message, transition, etc.).

**Interface:**
```csharp
public interface IInteractionOutcome
{
    string OutcomeId { get; }
    void Execute(InteractionOutcomeContext context);
}
```

**Steps:**
1. Create `internal sealed class` in `Reactions/Outcomes/`
2. Implement `IInteractionOutcome`
3. Add `[AutoScan]` attribute
4. Done — `AssemblyScanner` discovers it automatically

**Example — DamageOutcome:**
```csharp
[AutoScan]
internal sealed class DamageOutcome : IInteractionOutcome
{
    public string OutcomeId => "damage";

    public void Execute(InteractionOutcomeContext context)
    {
        context.Ctx.EntityStats.ApplyDamage(context.Source, context.Value, context.Target);
    }
}
```

**InteractionOutcomeContext fields:**
```csharp
public readonly record struct InteractionOutcomeContext(
    EntityId Source,       // player or actor
    EntityId Target,       // interactable
    string ActionId,       // triggering action
    int Value,             // numeric value from reaction definition
    string OutcomeParam,   // extra parameter string (e.g., status effect name)
    IEventEncounterContext Ctx  // all services via .Ctx
);
```

**Services via `context.Ctx`:** EntityStats, EventBus, SlotService, StatusEffects, ResourceService, EncounterService

**Existing outcomes:** heal, damage, damage_target, shield, mana, message, reward, apply_status, transition, spawn_combat, consume, leave, recruit, give_item

**DB — interactable_reactions:**
```sql
CREATE TABLE interactable_reactions (
    encounter_id    TEXT NOT NULL,
    interactable_id TEXT NOT NULL,
    action_id       TEXT NOT NULL,
    outcome_id      TEXT NOT NULL,
    outcome_param   TEXT,             -- extra param (e.g., status name for apply_status)
    value           INTEGER NOT NULL DEFAULT 0,
    chance          REAL NOT NULL DEFAULT 1.0, -- 0.0-1.0 probability
    seq             INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (encounter_id, interactable_id, action_id, outcome_id, seq)
);

-- Example: shrine heals 5 HP when player prays (100% chance)
INSERT INTO interactable_reactions(encounter_id, interactable_id, action_id, outcome_id, value)
VALUES ('ancient_shrine', 'altar', 'Pray', 'heal', 5);

-- Example: chest has 50% chance to spawn combat when opened
INSERT INTO interactable_reactions(encounter_id, interactable_id, action_id, outcome_id, outcome_param, value, chance)
VALUES ('trapped_chest', 'chest', 'Open', 'spawn_combat', 'goblin_ambush', 0, 0.5);
```

---

## AutoScan Attribute

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class AutoScanAttribute : Attribute { }
```

Both tag definitions and outcomes require a **parameterless constructor**. `AssemblyScanner` instantiates them via `Activator.CreateInstance()` at startup.

---

## Key Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Core/EventEncounter/Reactions/Tags/ITagDefinition.cs` | Tag interface |
| `Assets/Scripts/Core/EventEncounter/Reactions/Tags/TagReactionContext.cs` | Tag context with state API |
| `Assets/Scripts/Core/EventEncounter/Reactions/Tags/Definitions/` | All tag implementations |
| `Assets/Scripts/Core/EventEncounter/Reactions/IInteractionOutcome.cs` | Outcome interface |
| `Assets/Scripts/Core/EventEncounter/Reactions/InteractionOutcomeContext.cs` | Outcome context record |
| `Assets/Scripts/Core/EventEncounter/Reactions/Outcomes/` | All outcome implementations |
| `Assets/Scripts/Core/EventEncounter/Reactions/InteractionOutcomeRegistry.cs` | Auto-scan registry |
| `Assets/Scripts/Core/EventEncounter/Reactions/TagReactionRegistry.cs` | Auto-scan registry |
| `Assets/Scripts/Core/Services/AutoScanAttribute.cs` | The `[AutoScan]` attribute |

## Updating ralph-prompt.md

**New tag definition**: No ralph-prompt.md changes needed (tags are assigned via DB, not word classification).

**New outcome**: No ralph-prompt.md changes needed (outcomes are wired via `interactable_reactions` DB rows, not word classification).

**New interaction action** (e.g., adding "Bribe" alongside Enter/Talk/Steal/etc.):
1. Add to `ActionNames.InteractionActions` array in C#
2. Add row to `ralph-prompt.md` INTERACTION ACTIONS table with ActionId, Usage, and Example words
3. Add to `ralph-prompt.md` "When to classify as interaction action" guidelines

## Checklist

- [ ] Create new class in the appropriate `Definitions/` or `Outcomes/` directory
- [ ] Implement `ITagDefinition` or `IInteractionOutcome`
- [ ] Mark with `[AutoScan]` attribute
- [ ] Add DB rows (`unit_tags` for tags, `interactable_reactions` for entity reactions)
- [ ] If new interaction action: update `ActionNames.InteractionActions` + `ralph-prompt.md` INTERACTION ACTIONS table
