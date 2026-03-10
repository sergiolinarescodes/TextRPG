# Unidad Framework — AI Development Rules

## Project Structure

- **Framework**: `Packages/com.unidad.core/` (UPM local package)
- **Game code**: `Assets/Scripts/` (game-specific systems)
- **Single bootstrap scene**: One scene with a single MonoBehaviour extending `UnidadBootstrap`
- **No manual scene work**: All GameObjects are created code-driven via `IGameObjectFactory`

## Architecture Rules

### DI (Dependency Injection)
- Use **Reflex** (`com.gustavopsantos.reflex`) for all dependency injection
- Every service is a plain C# class, injected via interfaces
- The ONLY MonoBehaviour is the Bootstrap (and TickRunner, spawned automatically)
- Never use `FindObjectOfType`, `GetComponent` at runtime for service access — inject via constructor

### Events
- All events MUST be `struct` (value types) — enforced by `where T : struct` constraint
- Use `IEventBus.Publish<T>()` / `IEventBus.Subscribe<T>()` for all inter-system communication
- Never use Unity's `SendMessage` or `BroadcastMessage`
- **NEVER** subscribe to an event with a stub/log-only handler. If you subscribe, the handler MUST perform real logic. If the feature isn't ready, don't subscribe — leave a `// TODO:` comment instead. Silent stub handlers appear functional but are bugs.

### Systems
- Every system MUST implement `ISystemInstaller` which forces `CreateTestFactory()`
- Service implementations MUST be `internal` — only interfaces are `public`
- Extend `SystemServiceBase` for auto-subscription lifecycle management
- Systems that need per-frame updates MUST implement `ITickable` — never use `MonoBehaviour.Update()`

### Time
- NEVER read `UnityEngine.Time.deltaTime` directly — inject `ITimeProvider` instead
- For tick-based systems, accept `float deltaTime` as parameter in `Tick()`

### Object Creation
- Always use `IGameObjectFactory` for creating GameObjects — enables tracking and cleanup
- For frequently instantiated objects, ALWAYS use `GameObjectPool<T>` via `PoolRegistry`
- Never call `new GameObject()` or `Object.Instantiate()` directly in services

### Logic vs Presentation
- **Game logic** (rules, state changes, validation) MUST live in the service layer — never in scenarios
- Scenarios handle ONLY **presentation**: visuals, animations, floating text, UI overlays
- Services must be testable independently of any scenario
- Example: energy-starved module stopping a turn = service logic; orange ghost animation = scenario presentation

### CRITICAL: Always Use Unidad Framework Components
- **NEVER** reimplement functionality that already exists in `Packages/com.unidad.core/`
- **BEFORE** writing any new utility, pattern, or helper, search the framework package for existing implementations
- Use `EventBus` (from `Unidad.Core.EventBus`) in scenarios — never create custom event bus implementations
- Use `ModifierStack<T>` for stackable stat/value modifiers — never roll your own
- Use `SystemServiceBase` for all services — never manually manage event subscriptions
- Use `DataDrivenScenario` for all scenarios — never implement `ITestScenario` directly
- Use `IGameObjectFactory`, `PoolRegistry`, `RegistryBase`, `StateMachine`, `IContributor` etc. from the framework
- If you think a framework component is missing, ask the user before creating a new one

### Patterns — Use Advanced Solutions
- **AI**: Build scorer-based systems using `IContributor<TContext>` pattern, not simple if/else
- **State machines**: Use `IState<TContext>` / `StateMachine<TContext>`, not enum switches
- **Modifiers**: Use `IModifier<TValue>` for stackable effects, not hardcoded calculations
- **Pooling**: Always pool objects that are created/destroyed frequently
- **Registries**: Use `RegistryBase<TKey, TValue>` for any collection of game entities

## Testing Rules

### Mandatory Test Coverage
- Every `ISystemInstaller` MUST return a non-null `ISystemTestFactory` from `CreateTestFactory()`
- Every `ISystemTestFactory` MUST provide at least one `ITestScenario`
- `AllSystemScenariosTests` auto-discovers and runs ALL scenarios — no scenario can be skipped
- Every `INativeFunction` MUST have unit tests covering normal operation and edge cases (invalid args, out-of-bounds, no energy, etc.)
- When adding native functions to a system, also add integration tests verifying they work end-to-end via script execution
- Every `IActionHandler` MUST have verification checks in `CombatActionVerificationScenario` — when adding or modifying an action handler, add checks that verify **correctness** (exact damage values, HP changes, status effects applied, stat modifications, event payloads) not just invocation
- New action handlers MUST include checks for: normal operation, edge cases (overkill, capped healing, empty targets), and combo interactions with other handlers

### Test Parity
- Scenarios run identically in NUnit (Test Runner) and in-game (Scenario Browser Editor Window)
- Use `ManualTimeProvider` in tests to control time deterministically
- Use `InstantAnimationResolver` in tests to skip animation waits
- Use `MockEventBus` for unit tests, `TestEventBus` for integration tests with history

### Test Structure
- **Unit tests**: Single service + `MockEventBus`, no real dependencies
- **Integration tests**: Multiple real services + `TestEventBus`, verify event sequences
- **Scenario tests**: `DataDrivenScenario` with `TestScenarioDefinition`, editable parameters

### Scenario Tests — Visual Requirements
- Every scenario MUST extend `DataDrivenScenario` — never implement `ITestScenario` directly
- Scenarios MUST be **visual**: spawn real GameObjects visible in the Scene/Game view
- For UI-centric scenarios, use `RootVisualElement` for overlays (theme, dialog, etc.)
- For real-time systems (physics, animation, AI), log live events to the **Console** via `Debug.Log` with a `[SystemNameScenario]` prefix — do NOT build in-game log overlays
- Scenario parameters (`ScenarioParameter`) let users tweak values and Re-run from the Scenario Browser
- `Verify()` MUST only check **deterministic setup** (objects spawned, components attached, IDs valid) — NEVER assert on runtime-dependent results (collision counts, timing). Tests must always pass at the moment `Verify()` runs
- Store `IDisposable` subscriptions in a list and dispose them in `OnCleanup()` — do not rely on `ClearAllSubscriptions()` alone
- Cleanup: override `OnCleanup()` to dispose subscriptions and null out references — `DataDrivenScenario` handles `SceneRoot` destruction
- Place scenario files in `Runtime/{SystemName}/Scenarios/` (not in Tests/) so they're accessible from both NUnit and Scenario Browser

### Convention Tests
- Events must be structs
- Service implementations must be internal
- No static mutable state in services
- All installers must implement `ISystemInstaller`

## File Naming Conventions

- Interfaces: `I{Name}.cs` (e.g., `IJumpService.cs`)
- Implementations: `{Name}.cs` (e.g., `JumpService.cs`) — always `internal sealed`
- Installers: `{SystemName}Installer.cs` (e.g., `JumpSystemInstaller.cs`)
- Test factories: `{SystemName}TestFactory.cs` (e.g., `JumpTestFactory.cs`)
- Events: `{Name}Event.cs` as `public readonly record struct`

## C# Version
- C# 10 enabled via `Assets/csc.rsp` (`-langversion:10`)
- `record struct` is available — use for events and data types
- `IsExternalInit` polyfill exists in the framework package

## Debug
- Systems that expose debug info should implement `IDebugProvider` (namespace: `Unidad.Core.Debugger`)
- Register providers with `DebugModeService`
- Use Scenario Browser (`Window > Unidad > Scenario Browser`) for in-editor testing

## Build & Test

### Compilation Check
- Run `dotnet build TextRPG.sln` to verify compilation (0 errors expected)
- Unity editor must NOT be open for batch mode commands

## Git Rules
- NEVER add `Co-Authored-By` lines to commits
- NEVER amend commits unless explicitly asked
