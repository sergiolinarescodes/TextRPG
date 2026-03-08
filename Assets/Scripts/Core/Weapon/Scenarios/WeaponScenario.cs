using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.ActionExecution.Handlers;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordInput;
using Unidad.Core.EventBus;
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
        private ICombatSlotService _slotService;
        private ICombatContext _combatContext;

        private VisualElement _weaponSlot;
        private Label _weaponDurabilityLabel;
        private VisualElement _weaponNameContainer;
        private EntityId _playerId;
        private readonly List<IDisposable> _subscriptions = new();

        private static readonly Color WeaponSlotBorderDefault = new(0.5f, 0.5f, 0.5f);
        private static readonly Color WeaponNameColor = new(1f, 0.85f, 0.3f);

        public WeaponScenario() : base(new TestScenarioDefinition(
            "weapon",
            "Weapon System",
            "Equip a weapon and use ammo words. Type GUN or SWORD to equip, " +
            "then type ammo words to auto-fire at enemies.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);

            var wordActionData = WordActionDatabaseLoader.Load();

            // Wrap main resolver to exclude ammo words
            var filteredResolver = new FilteredWordResolver(wordActionData.Resolver, wordActionData.AmmoWordSet);
            _ammoResolver = wordActionData.AmmoResolver;
            _wordMatchService = new WordMatchService(filteredResolver, wordActionData.ActionRegistry);

            // CombatSlot
            _slotService = new CombatSlotService(_eventBus);
            _slotService.Initialize();

            _combatContext = new CombatContext();
            _combatContext.SetEntityStats(_entityStats);
            _combatContext.SetSlotService(_slotService);

            // Player
            _playerId = new EntityId("player");
            var playerId = _playerId;
            _entityStats.RegisterEntity(playerId, 100, 10, 8, 5, 4, 3);

            // Enemy
            var enemyId = new EntityId("goblin");
            _entityStats.RegisterEntity(enemyId, 50, 6, 4, 3, 2, 2);
            _slotService.RegisterEnemy(enemyId, 0);

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

            var infoLabel = new Label("Type GUN or SWORD to equip. Then type ammo words to auto-fire.");
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

            _weaponSlot.RegisterCallback<ClickEvent>(_ => FireWeapon());

            root.Add(_weaponSlot);
        }

        private void FireWeapon()
        {
            if (_weaponService == null || !_weaponService.HasWeapon(_playerId)) return;

            var ammoWords = _weaponService.GetAmmoWords(_playerId);
            if (ammoWords.Count == 0) return;

            var ammo = ammoWords[UnityEngine.Random.Range(0, ammoWords.Count)];
            Debug.Log($"[WeaponScenario] Fire weapon: {ammo}");
            _eventBus.Publish(new WeaponAmmoSubmittedEvent(_playerId, ammo));
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

            UnitTextLabels.AddTo(layout, WeaponNameColor, _weaponNameContainer);
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
            _weaponNameContainer.Clear();
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
            _slotService = null;
            _combatContext = null;
            _weaponSlot = null;
            _weaponDurabilityLabel = null;
            _weaponNameContainer = null;
        }
    }
}
