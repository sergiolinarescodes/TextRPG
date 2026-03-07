using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.ActionExecution.Handlers;
using TextRPG.Core.CombatGrid;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordInput;
using Unidad.Core.EventBus;
using Unidad.Core.Grid;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Weapon.Scenarios
{
    internal sealed class WeaponScenario : DataDrivenScenario
    {
        private IEventBus _eventBus;
        private IWeaponService _weaponService;
        private IWeaponActionExecutor _weaponExecutor;
        private IWordResolver _ammoResolver;
        private IWordMatchService _wordMatchService;
        private IEntityStatsService _entityStats;
        private ICombatGridService _combatGrid;
        private ICombatContext _combatContext;
        private IUnitService _unitService;
        private IGrid<UnitId?> _grid;

        private VisualElement _weaponSlot;
        private Label _weaponDurabilityLabel;
        private VisualElement _weaponNameContainer;
        private bool _isWeaponMode;
        private readonly List<IDisposable> _subscriptions = new();

        private static readonly Color WeaponSlotBorderDefault = new(0.5f, 0.5f, 0.5f);
        private static readonly Color WeaponSlotBorderActive = new(1f, 0.8f, 0.2f);
        private static readonly Color WeaponNameColor = new(1f, 0.85f, 0.3f);

        public WeaponScenario() : base(new TestScenarioDefinition(
            "weapon",
            "Weapon System",
            "Equip a weapon and use ammo words. Type GUN or SWORD to equip, " +
            "then click the weapon slot to enter weapon mode and type ammo words.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _eventBus = new EventBus();
            _unitService = new UnitService(_eventBus);
            _entityStats = new EntityStatsService(_eventBus);

            var wordActionData = WordActionDatabaseLoader.Load();

            // Wrap main resolver to exclude ammo words
            var filteredResolver = new FilteredWordResolver(wordActionData.Resolver, wordActionData.AmmoWordSet);
            _ammoResolver = wordActionData.AmmoResolver;
            _wordMatchService = new WordMatchService(filteredResolver, wordActionData.ActionRegistry);

            // Combat grid
            _combatGrid = new CombatGridService(_eventBus, _unitService);
            _combatGrid.Initialize(3, 8);
            _grid = _combatGrid.Grid;

            _combatContext = new CombatContext();
            _combatContext.SetEntityStats(_entityStats);
            _combatContext.SetGrid(_combatGrid);

            // Player
            var playerId = new EntityId("player");
            _entityStats.RegisterEntity(playerId, 100, 10, 8, 5, 4, 3);
            _combatGrid.RegisterCombatant(playerId,
                new UnitDefinition(new UnitId("player"), "YOU", 100, 10, 8, 12, Color.white),
                new GridPosition(1, 0));

            // Enemy
            var enemyId = new EntityId("goblin");
            _entityStats.RegisterEntity(enemyId, 50, 6, 4, 3, 2, 2);
            _combatGrid.RegisterCombatant(enemyId,
                new UnitDefinition(new UnitId("goblin"), "GOB", 50, 6, 5, 4, new Color(0.2f, 0.8f, 0.2f)),
                new GridPosition(1, 7));

            _combatContext.SetSourceEntity(playerId);
            _combatContext.SetEnemies(new[] { enemyId });
            _combatContext.SetAllies(Array.Empty<EntityId>());

            // Weapon + Action services
            var weaponRegistry = WeaponSystemInstaller.BuildWeaponRegistry(wordActionData);
            _weaponService = new WeaponService(_eventBus, weaponRegistry);

            var actionHandlerCtx = new ActionHandlerContext(_entityStats, _eventBus, _combatContext,
                weaponService: _weaponService);
            var handlerRegistry = new ActionHandlerRegistry();
            handlerRegistry.Register("Damage", new DamageActionHandler(actionHandlerCtx));
            handlerRegistry.Register("Weapon", new WeaponActionHandler(actionHandlerCtx));

            _weaponExecutor = new WeaponActionExecutor(
                _eventBus, _weaponService, _ammoResolver, handlerRegistry, _combatContext);

            // ActionExecution for normal words
            var actionExecution = new ActionExecutionService(_eventBus, filteredResolver, handlerRegistry, _combatContext);

            // Subscribe to events
            _subscriptions.Add(_eventBus.Subscribe<WeaponEquippedEvent>(OnWeaponEquipped));
            _subscriptions.Add(_eventBus.Subscribe<WeaponDurabilityChangedEvent>(OnDurabilityChanged));
            _subscriptions.Add(_eventBus.Subscribe<WeaponDestroyedEvent>(OnWeaponDestroyed));
            _subscriptions.Add(_eventBus.Subscribe<ActionExecutionCompletedEvent>(evt =>
            {
                Debug.Log($"[WeaponScenario] Action completed: {evt.Word}");
            }));

            BuildUI();
            Debug.Log("[WeaponScenario] Started — type GUN or SWORD to equip a weapon");
        }

        private void BuildUI()
        {
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.backgroundColor = Color.black;

            var infoLabel = new Label("Type GUN or SWORD to equip. Click weapon slot to enter weapon mode.");
            infoLabel.style.color = Color.white;
            infoLabel.style.fontSize = 14;
            infoLabel.style.paddingLeft = 10;
            infoLabel.style.paddingTop = 5;
            root.Add(infoLabel);

            // Weapon slot (bottom-right, initially hidden)
            _weaponSlot = new VisualElement();
            _weaponSlot.style.position = Position.Absolute;
            _weaponSlot.style.bottom = 10;
            _weaponSlot.style.right = 10;
            _weaponSlot.style.width = 80;
            _weaponSlot.style.height = 80;
            _weaponSlot.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            _weaponSlot.style.borderTopWidth = 2;
            _weaponSlot.style.borderBottomWidth = 2;
            _weaponSlot.style.borderLeftWidth = 2;
            _weaponSlot.style.borderRightWidth = 2;
            _weaponSlot.style.borderTopColor = WeaponSlotBorderDefault;
            _weaponSlot.style.borderBottomColor = WeaponSlotBorderDefault;
            _weaponSlot.style.borderLeftColor = WeaponSlotBorderDefault;
            _weaponSlot.style.borderRightColor = WeaponSlotBorderDefault;
            _weaponSlot.style.flexDirection = FlexDirection.Column;
            _weaponSlot.style.justifyContent = Justify.Center;
            _weaponSlot.style.alignItems = Align.Center;
            _weaponSlot.style.overflow = Overflow.Hidden;
            _weaponSlot.style.display = DisplayStyle.None;
            _weaponSlot.pickingMode = PickingMode.Position;

            _weaponNameContainer = new VisualElement();
            _weaponNameContainer.style.flexDirection = FlexDirection.Column;
            _weaponNameContainer.style.justifyContent = Justify.Center;
            _weaponNameContainer.style.alignItems = Align.Center;
            _weaponNameContainer.style.flexGrow = 1;
            _weaponSlot.Add(_weaponNameContainer);

            _weaponDurabilityLabel = new Label();
            _weaponDurabilityLabel.style.color = Color.white;
            _weaponDurabilityLabel.style.fontSize = 10;
            _weaponDurabilityLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _weaponDurabilityLabel.style.position = Position.Absolute;
            _weaponDurabilityLabel.style.bottom = 2;
            _weaponDurabilityLabel.style.left = 0;
            _weaponDurabilityLabel.style.right = 0;
            _weaponSlot.Add(_weaponDurabilityLabel);

            _weaponSlot.RegisterCallback<ClickEvent>(_ =>
            {
                _isWeaponMode = !_isWeaponMode;
                UpdateWeaponSlotBorder();
                Debug.Log($"[WeaponScenario] Weapon mode: {(_isWeaponMode ? "ON" : "OFF")}");
            });

            root.Add(_weaponSlot);
        }

        private void OnWeaponEquipped(WeaponEquippedEvent evt)
        {
            Debug.Log($"[WeaponScenario] Equipped: {evt.Weapon.DisplayName} (dur={evt.Weapon.Durability})");
            _weaponSlot.style.display = DisplayStyle.Flex;
            RenderWeaponName(evt.Weapon.DisplayName);
            _weaponDurabilityLabel.text = $"{evt.Weapon.Durability}/{evt.Weapon.Durability}";
        }

        private void RenderWeaponName(string name)
        {
            _weaponNameContainer.Clear();

            float cellWidth = 76;
            float cellHeight = 56;
            var layout = UnitTextLayout.Calculate(name, cellWidth, cellHeight);

            foreach (var rowText in layout.Rows)
            {
                var label = new Label(rowText);
                label.style.fontSize = layout.FontSize;
                label.style.color = WeaponNameColor;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginTop = 0;
                label.style.marginBottom = 0;
                label.style.paddingTop = 0;
                label.style.paddingBottom = 0;
                _weaponNameContainer.Add(label);
            }
        }

        private void OnDurabilityChanged(WeaponDurabilityChangedEvent evt)
        {
            Debug.Log($"[WeaponScenario] Durability: {evt.CurrentDurability}/{evt.MaxDurability}");
            _weaponDurabilityLabel.text = $"{evt.CurrentDurability}/{evt.MaxDurability}";
        }

        private void OnWeaponDestroyed(WeaponDestroyedEvent evt)
        {
            Debug.Log($"[WeaponScenario] Weapon destroyed: {evt.WeaponWord}");
            _weaponSlot.style.display = DisplayStyle.None;
            _isWeaponMode = false;
            _weaponNameContainer.Clear();
        }

        private void UpdateWeaponSlotBorder()
        {
            var color = _isWeaponMode ? WeaponSlotBorderActive : WeaponSlotBorderDefault;
            _weaponSlot.style.borderTopColor = color;
            _weaponSlot.style.borderBottomColor = color;
            _weaponSlot.style.borderLeftColor = color;
            _weaponSlot.style.borderRightColor = color;
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("Weapon slot element exists", _weaponSlot != null,
                    _weaponSlot != null ? null : "Weapon slot is null"),
                new("Weapon service initialized", _weaponService != null,
                    _weaponService != null ? null : "Weapon service is null"),
                new("Ammo resolver initialized", _ammoResolver != null,
                    _ammoResolver != null ? null : "Ammo resolver is null"),
            };
            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();

            _eventBus?.ClearAllSubscriptions();
            _eventBus = null;
            _weaponService = null;
            _weaponExecutor = null;
            _ammoResolver = null;
            _wordMatchService = null;
            _entityStats = null;
            _combatGrid = null;
            _combatContext = null;
            _unitService = null;
            _grid = null;
            _weaponSlot = null;
            _weaponDurabilityLabel = null;
            _weaponNameContainer = null;
        }
    }
}
