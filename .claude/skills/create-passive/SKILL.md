---
name: create-passive
description: Guide for creating new passive triggers and effects in the composable Passive system. Use when adding a new passive trigger, passive effect, or composable passive behavior for units or items.
user-invocable: false
---

# Creating New Passives

Composable design: each passive = **Trigger** + **Effect** + **Target**. No code needed per combination — just create new triggers or effects independently, and they compose with all existing counterparts.

Both triggers and effects use `[AutoScan]` — no manual registration.

## Data Structure

```csharp
public sealed record PassiveEntry(
    string TriggerId,     // e.g., "on_self_hit"
    string TriggerParam,  // optional param (e.g., word length threshold, tag name)
    string EffectId,      // e.g., "damage", "heal"
    string EffectParam,   // optional param (e.g., status effect name for apply_status)
    int Value,            // numeric value (damage amount, heal amount, stacks)
    string Target         // "Self", "AllAllies", "AllEnemies", "Injured", "Attacker"
);
```

## Triggers

Triggers detect when a passive should fire, subscribing to events and calling back with context.

**Interface:**
```csharp
public interface IPassiveTrigger
{
    string TriggerId { get; }
    IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                          Action<PassiveTriggerContext> onTriggered);
}
```

**Steps:**
1. Create `internal sealed class` in `Passive/Triggers/`
2. Implement `IPassiveTrigger`
3. Add `[AutoScan]` attribute
4. Done — `AssemblyScanner.FindAll<IPassiveTrigger, string>(t => t.TriggerId)` discovers it

**Example — OnSelfHitTrigger:**
```csharp
[AutoScan]
internal sealed class OnSelfHitTrigger : IPassiveTrigger
{
    public string TriggerId => "on_self_hit";

    public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                  Action<PassiveTriggerContext> onTriggered)
    {
        return ctx.EventBus.Subscribe<DamageTakenEvent>(evt =>
        {
            if (!evt.EntityId.Equals(owner)) return;
            if (!ctx.EntityStats.HasEntity(owner) || ctx.EntityStats.GetCurrentHealth(owner) <= 0) return;

            onTriggered(new PassiveTriggerContext(owner, evt.EntityId, evt.DamageSource, null));
        });
    }
}
```

**PassiveTriggerContext:**
```csharp
public readonly record struct PassiveTriggerContext(
    EntityId Owner,          // entity that owns the passive
    EntityId? EventEntity,   // entity involved in triggering event (e.g., damaged ally)
    EntityId? EventSource,   // source of event (e.g., attacker)
    string Word              // relevant word (null for most triggers)
);
```

**Existing triggers:** on_self_hit, on_ally_hit, on_kill, on_round_start, on_round_end, on_turn_start, on_turn_end, on_word_played, on_word_length, on_word_tag

Special: `taunt` is a marker passive (no trigger/effect) — checked by CombatContext and CombatAIService.

---

## Effects

Effects execute on resolved targets when a trigger fires.

**Interface:**
```csharp
public interface IPassiveEffect
{
    string EffectId { get; }
    void Execute(EntityId owner, int value, string effectParam,
                 IReadOnlyList<EntityId> targets, IPassiveContext ctx);
}
```

**Steps:**
1. Create `internal sealed class` in `Passive/Effects/`
2. Implement `IPassiveEffect`
3. Add `[AutoScan]` attribute
4. Done — `AssemblyScanner.FindAll<IPassiveEffect, string>(e => e.EffectId)` discovers it

**Example — DamageEffect:**
```csharp
[AutoScan]
internal sealed class DamageEffect : IPassiveEffect
{
    public string EffectId => "damage";

    public void Execute(EntityId owner, int value, string effectParam,
                        IReadOnlyList<EntityId> targets, IPassiveContext ctx)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            if (!ctx.EntityStats.HasEntity(target) || ctx.EntityStats.GetCurrentHealth(target) <= 0)
                continue;
            ctx.EntityStats.ApplyDamage(target, value, owner);
        }
    }
}
```

**Existing effects:** heal, damage, shield, mana, apply_status

---

## Safety Patterns

Always guard against dead/removed entities in both triggers and effects:
```csharp
if (!ctx.EntityStats.HasEntity(target) || ctx.EntityStats.GetCurrentHealth(target) <= 0)
    continue; // or return
```

`PassiveService` has a `_isProcessing` re-entrancy guard — effects that trigger other passives are queued.

---

## Target Resolution

`PassiveTargetResolver` maps target strings using the trigger context:

| Target | Resolves to |
|--------|-------------|
| `"Self"` | Owner entity |
| `"Injured"` | `EventEntity` from trigger context |
| `"Attacker"` | `EventSource` from trigger context |
| `"AllAllies"` | All living allies (faction-aware via slot type) |
| `"AllEnemies"` | All living enemies (faction-aware via slot type) |

---

## IPassiveContext Services

Available to both triggers and effects:
- `IEntityStatsService EntityStats`
- `ICombatSlotService SlotService`
- `IEventBus EventBus`
- `IEncounterService EncounterService`
- `IStatusEffectService StatusEffects`
- `IWordTagResolver TagResolver`
- `ITurnService TurnService`
- `IActionAnimationService AnimationService`

---

## Display Text

`PassiveDefinitions.Generate(PassiveEntry)` auto-generates human-readable descriptions from trigger+effect+target. When adding new triggers/effects, add a mapping in `PassiveDefinitions` for display text and color.

---

## DB Tables

**Unit passives:**
```sql
CREATE TABLE unit_passives (
    unit_id       TEXT NOT NULL,
    trigger_id    TEXT NOT NULL,
    trigger_param TEXT,              -- optional (e.g., "6" for on_word_length, "NATURE" for on_word_tag)
    effect_id     TEXT NOT NULL,
    effect_param  TEXT,              -- optional (e.g., "Burning" for apply_status)
    value         INTEGER NOT NULL DEFAULT 1,
    target        TEXT NOT NULL DEFAULT 'Self',
    seq           INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (unit_id, trigger_id, effect_id, target, seq)
);

-- Example: golem heals allies for 2 HP at end of each round
INSERT INTO unit_passives(unit_id, trigger_id, effect_id, value, target)
VALUES ('golem', 'on_round_end', 'heal', 2, 'AllAllies');

-- Example: pyre applies Burning to all enemies at round start
INSERT INTO unit_passives(unit_id, trigger_id, effect_id, effect_param, value, target)
VALUES ('pyre', 'on_round_start', 'apply_status', 'Burning', 2, 'AllEnemies');
```

**Item passives:**
```sql
CREATE TABLE item_passives (
    item_id       TEXT NOT NULL,
    trigger_id    TEXT NOT NULL,
    trigger_param TEXT,
    effect_id     TEXT NOT NULL,
    effect_param  TEXT,
    value         INTEGER NOT NULL DEFAULT 1,
    target        TEXT NOT NULL DEFAULT 'Self',
    seq           INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (item_id, trigger_id, effect_id, target, seq)
);

-- Example: ring grants 1 shield when owner takes a hit
INSERT INTO item_passives(item_id, trigger_id, effect_id, value, target)
VALUES ('ring', 'on_self_hit', 'shield', 1, 'Self');
```

---

## Updating ralph-prompt.md

When adding a new **trigger**, update these sections in `ralph-prompt.md`:
1. **Available triggers table** (under UNIT PASSIVES) — add row with Trigger, trigger_param, and "When it fires" description
2. **Passive design archetypes table** — add a row showing example usage if relevant

When adding a new **effect**, update these sections:
1. **Available effects table** (under UNIT PASSIVES) — add row with Effect, effect_param, and "What it does" description
2. If the effect uses `effect_param`, document valid values (e.g., status names for `apply_status`)

When adding a new **target string**, update:
1. **Available passive targets table** — add row with Target and "Resolves to" description
2. `PassiveTargetResolver.cs` — add resolution logic

## Key Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Core/Passive/IPassiveTrigger.cs` | Trigger interface |
| `Assets/Scripts/Core/Passive/IPassiveEffect.cs` | Effect interface |
| `Assets/Scripts/Core/Passive/PassiveTriggerContext.cs` | Trigger callback context |
| `Assets/Scripts/Core/Passive/IPassiveContext.cs` | Service dependencies |
| `Assets/Scripts/Core/Passive/Triggers/` | All trigger implementations |
| `Assets/Scripts/Core/Passive/Effects/` | All effect implementations |
| `Assets/Scripts/Core/Passive/PassiveDefinitions.cs` | Display text generation |
| `Assets/Scripts/Core/Passive/PassiveTargetResolver.cs` | Target string resolution |
| `Assets/Scripts/Core/Passive/Scenarios/PassiveVerificationScenario.cs` | Verification tests |
| `Assets/Scripts/Core/Services/AutoScanAttribute.cs` | The `[AutoScan]` attribute |

## Checklist

- [ ] Create new class in `Triggers/` or `Effects/`
- [ ] Implement `IPassiveTrigger` or `IPassiveEffect`
- [ ] Mark with `[AutoScan]` attribute
- [ ] Add entity/alive guards in handlers
- [ ] Add display mapping in `PassiveDefinitions.cs`
- [ ] Add DB rows (`unit_passives` or `item_passives`) with SQL INSERT
- [ ] Update `ralph-prompt.md` — add to Available triggers/effects table + passive design archetypes
- [ ] Add verification checks in `PassiveVerificationScenario`
