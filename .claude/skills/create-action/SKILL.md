---
name: create-action
description: Guide for creating new action handlers in the ActionExecution system. Use when adding a new combat action, damage type, status effect action, stat modifier, or registering a new action handler.
user-invocable: false
---

# Creating a New Action Handler

## Two Paths

### Path A: Template-Based (Preferred)

Add one line to `ActionDefinitionTable.cs`. No new C# class needed. Available templates:

| Template | Param1 | Param2 | Description |
|----------|--------|--------|-------------|
| `scaled_damage` | Offensive `StatType` | Defensive `StatType` | `Max(1, base + off/3 - def/3)` |
| `apply_status` | `StatusEffectType` | `FromValue` / `Permanent` / `StackByValue` | Apply status to targets (`ApplySelf: true` for self-buffs) |
| `stat_modifier` | `StatType` | `"buff"` / `"debuff"` | Temporary stat change |
| `heal` | — | — | `base + MagicPower/3` |
| `shield` | — | — | `base + PhysicalDefense/3` |
| `mana_self` | — | — | Restore mana to caster |
| `noop` | — | — | Does nothing (e.g., Pay) |
| `push` | — | — | Push target back a slot |
| `fire` | — | — | Fire-element combo trigger |

**Steps:**
1. Add a `const string` to `ActionNames.cs`
2. Add an `ActionTemplateDef` entry to `ActionDefinitionTable.cs`
3. Add word rows to the SQLite database (`word_actions` table)
4. Add verification checks to `CombatActionVerificationScenario`
5. Update `ralph-prompt.md` with the new action

**Example — new scaled damage type:**
```csharp
// ActionNames.cs
public const string HolyDamage = "HolyDamage";

// ActionDefinitionTable.cs — add to Definitions array
new(HolyDamage, "scaled_damage", "MagicPower", "MagicDefense"),
```

**Example — new self-buff status action:**
```csharp
// ActionNames.cs
public const string Fortify = "Fortify";

// ActionDefinitionTable.cs
new(Fortify, "apply_status", "Hardening", "StackByValue", ApplySelf: true),
```

### Path B: Custom Handler

For actions with logic that doesn't fit any template (e.g., Shock chains, Concentrate with weak scaling, Summon).

**Steps:**
1. Add a `const string` to `ActionNames.cs`
2. Create a new `internal sealed class` implementing `IActionHandler` in `Handlers/`
3. Register it in `ActionHandlerFactory.CreateDefault()` under the "Complex handlers" section
4. Add DB rows + verification checks + update `ralph-prompt.md`

**IActionHandler interface:**
```csharp
public interface IActionHandler
{
    string ActionId { get; }
    void Execute(ActionContext context);
}
```

**ActionContext fields:**
```csharp
public readonly record struct ActionContext(
    EntityId Source,
    IReadOnlyList<EntityId> Targets,
    int Value,
    string Word,
    string AssocWord = ""
);
```

**IActionHandlerContext services** (injected into handler constructors):
- `IEntityStatsService EntityStats` — read/modify stats, apply damage
- `IEventBus EventBus` — publish events
- `ICombatContext CombatContext` — combat state
- `IStatusEffectService StatusEffects` — apply/remove effects
- `ITurnService TurnService` — turn tracking
- `IWeaponService WeaponService` — weapon resolution
- `ICombatSlotService SlotService` — slot positions
- `StatusEffectInteractionTable Interactions` — effect interactions

**Example — custom handler class:**
```csharp
internal sealed class MyActionHandler : IActionHandler
{
    private readonly IEntityStatsService _entityStats;
    public string ActionId => ActionNames.MyAction;

    public MyActionHandler(IActionHandlerContext ctx)
    {
        _entityStats = ctx.EntityStats;
    }

    public void Execute(ActionContext context)
    {
        for (int i = 0; i < context.Targets.Count; i++)
        {
            var target = context.Targets[i];
            _entityStats.ApplyDamage(target, context.Value, context.Source);
        }
    }
}
```

**Register in ActionHandlerFactory.CreateDefault():**
```csharp
// 2. Complex handlers section
registry.Register(ActionNames.MyAction, new MyActionHandler(ctx));
```

## Stat Scaling Reference

```csharp
// Offensive: Max(1, base + off/3 - def/3)
StatScaling.OffensiveScale(baseValue, offense, defense);

// Support: base + stat/3
StatScaling.SupportScale(baseValue, scalingStat);

// Weak scaling (divisor=6, used by Concentrate): base + stat/6
StatScaling.SupportScale(baseValue, scalingStat, StatScaling.WeakDivisor);
```

## Database: word_actions Table

```sql
CREATE TABLE word_actions (
    word        TEXT NOT NULL COLLATE NOCASE,
    action_name TEXT NOT NULL,
    value       INTEGER NOT NULL CHECK(value BETWEEN 1 AND 10),
    target      TEXT DEFAULT NULL,       -- per-action override (null = inherit word-level)
    range       INTEGER DEFAULT NULL,
    area        TEXT DEFAULT NULL,
    assoc_word  TEXT NOT NULL DEFAULT '', -- weapon/consumable ammo link
    seq         INTEGER NOT NULL DEFAULT 0, -- enables duplicate actions (e.g. Damage x2)
    PRIMARY KEY (word, action_name, seq, assoc_word)
);
```

Add rows for test words manually or via the Python pipeline:
```bash
# Insert via pipeline (preferred for batch operations)
echo '[{"word":"smite","target":"SingleEnemy","cost":3,"range":0,"area":"Single","tags":["HOLY","OFFENSIVE"],"actions":[{"action":"HolyDamage","value":4}]}]' | py Tools/WordAction/batch_insert.py

# Or insert directly for quick testing
sqlite3 Assets/StreamingAssets/wordactions.db "INSERT INTO word_actions(word, action_name, value) VALUES ('smite', 'HolyDamage', 4);"
```

Pipeline scripts: `Tools/WordAction/batch_next.py`, `batch_insert.py`, `stats.py`, `audit.py`

## Updating pipeline reference data

When adding a new action, update these files:

1. **`Tools/WordAction/batch_insert.py`** — add to `VALID_ACTIONS` set
2. **`Tools/WordAction/context.py`** — add entry to `ACTION_DESCRIPTIONS` dict with a concise description
3. **`ralph-prompt.md`** — if the action has special classification rules (always paired with a tag, specific targeting), add a bullet point under CLASSIFICATION GUIDELINES

If adding a new **interaction action**, also add to `ActionNames.InteractionActions` in C#.

If adding a new **status effect**, add to `VALID_STATUS_EFFECTS` in `batch_insert.py`.

## Key Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Core/ActionExecution/ActionNames.cs` | All action name constants |
| `Assets/Scripts/Core/ActionExecution/ActionDefinitionTable.cs` | Template-based action definitions |
| `Assets/Scripts/Core/ActionExecution/ActionHandlerFactory.cs` | Factory that builds handler registry |
| `Assets/Scripts/Core/ActionExecution/ActionDefinition.cs` | `ActionTemplateDef` record |
| `Assets/Scripts/Core/ActionExecution/IActionHandler.cs` | Handler interface |
| `Assets/Scripts/Core/ActionExecution/ActionContext.cs` | Execution context record |
| `Assets/Scripts/Core/ActionExecution/IActionHandlerContext.cs` | Service dependencies |
| `Assets/Scripts/Core/ActionExecution/StatScaling.cs` | Scaling formulas |
| `Assets/Scripts/Core/ActionExecution/Handlers/ScaledDamageHandler.cs` | Reference handler impl |
| `Assets/Scripts/Core/ActionExecution/Scenarios/CombatActionVerificationScenario.cs` | Verification tests |

## Checklist

- [ ] Add constant to `ActionNames.cs`
- [ ] Add template def to `ActionDefinitionTable.cs` OR create custom handler + register in `ActionHandlerFactory`
- [ ] Add `word_actions` rows in SQLite DB (via pipeline or direct SQL)
- [ ] Add verification checks in `CombatActionVerificationScenario` (exact values, not just invocation)
- [ ] Update `batch_insert.py` VALID_ACTIONS + `context.py` ACTION_DESCRIPTIONS
- [ ] If new status effect: update `batch_insert.py` VALID_STATUS_EFFECTS
