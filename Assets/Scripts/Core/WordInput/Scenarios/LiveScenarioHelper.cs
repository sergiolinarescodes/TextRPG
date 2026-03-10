using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Consumable;
using TextRPG.Core.Encounter;
using TextRPG.Core.Equipment;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.Passive;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.UI.Components;
using Unidad.Core.UI.TextAnimation.ElementAnimation;
using Unidad.Core.UI.Tooltip;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.WordInput.Scenarios
{
    internal sealed class LiveScenarioServices
    {
        public IEventBus EventBus;
        public DrunkLetterService DrunkLetterService;
        public IWordInputService WordInputService;
        public IUnitService UnitService;
        public IEntityStatsService EntityStats;
        public WordActionData WordActionData;
        public IWordResolver WordResolver;
        public IWordResolver AmmoResolver;
        public IWordMatchService WordMatchService;
        public IWordMatchService AmmoMatchService;
        public ICombatSlotService SlotService;
        public ICombatContext CombatContext;
        public ITurnService TurnService;
        public IStatusEffectService StatusEffects;
        public IWeaponService WeaponService;
        public IConsumableService ConsumableService;
        public ConsumableActionExecutor ConsumableExecutor;
        public IItemRegistry ItemRegistry;
        public IInventoryService InventoryService;
        public InventoryId PlayerInventoryId;
        public ActionHandlerRegistry HandlerRegistry;
        public IActionExecutionService ActionExecution;
        public IWeaponActionExecutor WeaponExecutor;
        public ActionAnimationService AnimationService;
        public IPassiveService PassiveService;
        public IEquipmentService EquipmentService;
        public ILootRewardService LootRewardService;
        public ITargetingPreviewService PreviewService;
        public ITargetingPreviewService AmmoPreviewService;
        public StatusEffectVisualService StatusVisualService;
        public ITooltipService FrameworkTooltipService;
        public EntityTooltipService TooltipService;
        public IEncounterService EncounterAdapter;
        public IEventEncounterService EventEncounterService;
        public IWordResolver EnemyResolver;
        public EntityId PlayerId;
        public IAnimationResolver ScenarioAnimResolver;
        public FloatingMessagePool MessagePool;
        public Func<EntityId, Vector3> PositionProvider;
    }

    internal sealed class LiveScenarioLayout
    {
        public AnimatedCodeField CodeField;
        public CombatSlotVisual SlotVisual;
        public EquipmentBarVisual LeftBar;
        public EquipmentBarVisual RightBar;
        public UnidadProgressBar HpBar;
        public Label HpLabel;
        public UnidadProgressBar ManaBar;
        public Label ManaLabel;
        public VisualElement ManaCostOverlay;
        public VisualElement MainTextPanel;
        public VisualElement StatsBar;
        public VisualElement LinesContainer;
        public VisualElement AllyRow;
        public VisualElement WeaponSlot;
        public Label WeaponDurabilityLabel;
        public VisualElement ConsumableSlot;
        public Label ConsumableDurabilityLabel;
        public VisualElement TooltipLayer;
        public VisualElement LootOverlay;
        public VisualElement LootTooltip;
        public int HighlightedSlotIndex = -1;
        public VisualElement DragElement;
        public string DragItemWord;
        public int DragSourceSlot = -1;
        public bool DragFromEquipment;
        public string LastMatchedWord = "";
        public bool IsPreviewingManaCost;
        public IVisualElementScheduledItem DrunkWobbleSchedule;
    }

    internal static class LiveScenarioHelper
    {
        private static readonly Color HighlightEnemy = new(1f, 0.3f, 0.3f, 0.4f);
        private static readonly Color HighlightSelf = new(0.3f, 1f, 0.3f, 0.4f);

        public static LiveScenarioServices CreateCoreServices(EntityId playerId, GameObject sceneRoot)
        {
            var svc = new LiveScenarioServices();
            svc.PlayerId = playerId;
            svc.EventBus = new EventBus();
            svc.DrunkLetterService = new DrunkLetterService(svc.EventBus, playerId);
            svc.WordInputService = new WordInputService(svc.EventBus, svc.DrunkLetterService);
            svc.UnitService = new UnitService(svc.EventBus);
            svc.EntityStats = new EntityStatsService(svc.EventBus);
            svc.WordActionData = WordActionDatabaseLoader.Load();
            svc.WordResolver = new FilteredWordResolver(svc.WordActionData.Resolver, svc.WordActionData.AmmoWordSet);
            svc.AmmoResolver = svc.WordActionData.AmmoResolver;
            svc.WordMatchService = new WordMatchService(svc.WordResolver, svc.WordActionData.ActionRegistry);
            svc.AmmoMatchService = new WordMatchService(svc.AmmoResolver, svc.WordActionData.ActionRegistry);

            svc.SlotService = new CombatSlotService(svc.EventBus);
            svc.CombatContext = new CombatContext();
            ((CombatContext)svc.CombatContext).SetEntityStats(svc.EntityStats);
            ((CombatContext)svc.CombatContext).SetSlotService(svc.SlotService);

            svc.TurnService = new TurnService(svc.EventBus);

            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(svc.EntityStats, svc.TurnService, svc.EventBus);
            var statusEffects = new StatusEffectService(svc.EventBus, svc.EntityStats, svc.TurnService,
                effectHandlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)statusEffects;
            svc.StatusEffects = statusEffects;

            var weaponRegistry = WeaponSystemInstaller.BuildWeaponRegistry(svc.WordActionData);
            svc.WeaponService = new WeaponService(svc.EventBus, weaponRegistry);

            svc.ItemRegistry = EquipmentSystemInstaller.BuildItemRegistry(svc.WordActionData);
            svc.InventoryService = new InventoryService(svc.EventBus);
            svc.PlayerInventoryId = new InventoryId("player");

            var actionHandlerCtx = new ActionHandlerContext(svc.EntityStats, svc.EventBus, svc.CombatContext,
                statusEffects, svc.TurnService, svc.WeaponService, slotService: svc.SlotService);
            svc.HandlerRegistry = ActionHandlerRegistryFactory.CreateDefault(actionHandlerCtx);

            PlayerDefaults.Register(svc.EntityStats, playerId);
            svc.InventoryService.Create(svc.PlayerInventoryId, new InventoryDefinition(EquipmentConstants.InventorySlotCount));

            foreach (var itemWord in svc.ItemRegistry.Keys)
            {
                if (svc.ItemRegistry.TryGet(itemWord, out var itemDef))
                    svc.InventoryService.DefineItem(new Unidad.Core.Inventory.ItemDefinition(
                        new ItemId(itemWord), itemDef.DisplayName, 1));
            }

            svc.PreviewService = new TargetingPreviewService(svc.WordResolver, svc.CombatContext);
            svc.AmmoPreviewService = new TargetingPreviewService(svc.AmmoResolver, svc.CombatContext);

            svc.ScenarioAnimResolver = new LiveAnimationResolver();

            var consumableRegistry = ConsumableSystemInstaller.BuildConsumableRegistry(svc.ItemRegistry);
            svc.ConsumableService = new ConsumableService(svc.EventBus, consumableRegistry);

            svc.AnimationService = new ActionAnimationService(svc.EventBus, svc.ScenarioAnimResolver,
                svc.HandlerRegistry, svc.EntityStats);

            svc.DrunkLetterService.SetStatusEffects(svc.StatusEffects);

            svc.StatusVisualService = new StatusEffectVisualService(svc.EventBus);

            var tickRunner = sceneRoot.AddComponent<TickRunner>();
            tickRunner.Initialize(new UnityTimeProvider(), new ITickable[] { svc.AnimationService });

            return svc;
        }

        public static void CreateActionExecution(LiveScenarioServices svc, IWordResolver compositeResolver)
        {
            svc.ActionExecution = new ActionExecutionService(svc.EventBus, compositeResolver,
                svc.HandlerRegistry, svc.CombatContext, svc.EntityStats, svc.StatusEffects, svc.ScenarioAnimResolver);

            svc.WeaponExecutor = new WeaponActionExecutor(svc.EventBus, svc.WeaponService, svc.AmmoResolver,
                svc.HandlerRegistry, svc.CombatContext, svc.ScenarioAnimResolver);

            svc.ConsumableExecutor = new ConsumableActionExecutor(svc.EventBus, svc.ConsumableService,
                svc.AmmoResolver, svc.HandlerRegistry, svc.CombatContext, svc.ScenarioAnimResolver);
        }

        public static void CreatePassiveService(LiveScenarioServices svc, IEncounterService encounterService,
            Dictionary<string, EntityDefinition> allUnits)
        {
            var triggerRegistry = PassiveSystemInstaller.CreateTriggerRegistry();
            var effectRegistry = PassiveSystemInstaller.CreateEffectRegistry();
            var targetResolver = new PassiveTargetResolver();
            var passiveContext = new PassiveContext(svc.EntityStats, svc.SlotService, svc.EventBus,
                encounterService, animationService: svc.AnimationService);
            svc.PassiveService = new PassiveService(svc.EventBus, triggerRegistry, effectRegistry,
                targetResolver, passiveContext, allUnits);
        }

        public static void CreateEquipmentAndLoot(LiveScenarioServices svc)
        {
            svc.EquipmentService = new EquipmentService(svc.EventBus, svc.ItemRegistry, svc.EntityStats,
                svc.PassiveService, svc.WeaponService, svc.ConsumableService);

            svc.HandlerRegistry.Register("Item", new ItemActionHandler(
                new ActionHandlerContext(svc.EntityStats, svc.EventBus, svc.CombatContext,
                    svc.StatusEffects, svc.TurnService, svc.WeaponService, slotService: svc.SlotService),
                svc.InventoryService, svc.PlayerInventoryId, svc.EquipmentService, svc.ItemRegistry));

            svc.LootRewardService = new LootRewardService(svc.EventBus, svc.ItemRegistry,
                svc.InventoryService, svc.PlayerInventoryId, svc.PlayerId);
        }

        public static LiveScenarioLayout BuildLayout(VisualElement root, LiveScenarioServices svc,
            float vibrationAmplitude, float fontScaleFactor)
        {
            var layout = new LiveScenarioLayout();

            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);

            // Enemy row
            var enemyRow = new VisualElement();
            enemyRow.style.flexDirection = FlexDirection.Row;
            enemyRow.style.justifyContent = Justify.Center;
            enemyRow.style.alignItems = Align.Center;
            enemyRow.style.height = Length.Percent(20);
            enemyRow.style.backgroundColor = Color.black;
            root.Add(enemyRow);

            layout.SlotVisual = new CombatSlotVisual(svc.UnitService, svc.EntityStats, svc.SlotService);
            layout.SlotVisual.BuildEnemyRow(enemyRow);

            // Middle area
            var middleArea = new VisualElement();
            middleArea.style.flexDirection = FlexDirection.Row;
            middleArea.style.flexGrow = 1;

            layout.LeftBar = new EquipmentBarVisual(EquipmentConstants.InventorySlotCount);
            layout.LeftBar.BuildColumn(middleArea);

            layout.MainTextPanel = new VisualElement();
            layout.MainTextPanel.style.flexGrow = 1;
            layout.MainTextPanel.style.backgroundColor = Color.black;
            layout.MainTextPanel.style.justifyContent = Justify.Center;
            layout.MainTextPanel.style.alignItems = Align.Stretch;
            layout.MainTextPanel.style.overflow = Overflow.Hidden;
            middleArea.Add(layout.MainTextPanel);

            layout.RightBar = new EquipmentBarVisual(EquipmentConstants.SlotCount);
            layout.RightBar.BuildColumn(middleArea);

            layout.RightBar.SetSlotBackground(0, "HEAD", SlotColors.Placeholder);
            layout.RightBar.SetSlotBackground(1, "WEAR", SlotColors.Placeholder);
            layout.RightBar.SetSlotBackground(2, "ACCESSORY", SlotColors.Placeholder);
            layout.RightBar.SetSlotBackground(3, "USE", SlotColors.Placeholder);
            layout.RightBar.SetSlotBackground(4, "WEAPON", SlotColors.Placeholder);

            for (int i = 0; i < EquipmentConstants.InventorySlotCount; i++)
                layout.LeftBar.SetSlotBackground(i, "INVENTORY", SlotColors.Placeholder);

            root.Add(middleArea);

            // AnimatedCodeField
            layout.CodeField = new AnimatedCodeField();
            layout.CodeField.multiline = false;
            layout.CodeField.PersistentFocus = true;
            layout.CodeField.TypingAnimationAmplitude = vibrationAmplitude;
            layout.CodeField.style.width = Length.Percent(100);
            layout.CodeField.style.flexGrow = 1;
            layout.CodeField.style.paddingTop = 0;
            layout.CodeField.style.paddingBottom = 0;
            layout.CodeField.style.paddingLeft = 0;
            layout.CodeField.style.paddingRight = 0;
            layout.CodeField.style.marginTop = 0;
            layout.CodeField.style.marginBottom = 0;
            layout.CodeField.style.marginLeft = 0;
            layout.CodeField.style.marginRight = 0;
            layout.CodeField.style.color = Color.white;
            layout.MainTextPanel.Add(layout.CodeField);

            layout.LinesContainer = layout.CodeField.Q(className: "animated-code-field__lines");
            if (layout.LinesContainer != null)
            {
                layout.LinesContainer.style.paddingLeft = 0;
                layout.LinesContainer.style.paddingTop = 0;
                layout.LinesContainer.style.paddingRight = 0;
                layout.LinesContainer.style.paddingBottom = 0;
                layout.LinesContainer.style.justifyContent = Justify.Center;
            }

            // Ally row
            layout.AllyRow = new VisualElement();
            layout.AllyRow.pickingMode = PickingMode.Ignore;
            layout.AllyRow.style.position = Position.Absolute;
            layout.AllyRow.style.bottom = 10;
            layout.AllyRow.style.left = 0;
            layout.AllyRow.style.right = 0;
            layout.AllyRow.style.flexDirection = FlexDirection.Row;
            layout.AllyRow.style.justifyContent = Justify.Center;
            layout.AllyRow.style.alignItems = Align.Center;
            layout.AllyRow.style.height = 100;
            layout.AllyRow.style.paddingTop = 4;
            layout.AllyRow.style.paddingBottom = 4;
            layout.MainTextPanel.Add(layout.AllyRow);

            layout.SlotVisual.BuildAllyRow(layout.AllyRow);

            // Stats bar
            layout.StatsBar = new VisualElement();
            layout.StatsBar.style.flexGrow = 0;
            layout.StatsBar.style.flexShrink = 0;
            layout.StatsBar.style.height = 150;
            layout.StatsBar.style.backgroundColor = Color.black;
            layout.StatsBar.style.flexDirection = FlexDirection.Column;
            layout.StatsBar.style.justifyContent = Justify.Center;
            layout.StatsBar.style.paddingLeft = 10;
            layout.StatsBar.style.paddingRight = 10;
            root.Add(layout.StatsBar);

            BuildHpBar(layout, svc);
            BuildManaBar(layout, svc);
            BuildStatsRow(layout);

            // Projectile overlay
            var projectileOverlay = ProjectilePool.CreateOverlay();
            root.Add(projectileOverlay);

            Func<EntityId, Vector3> positionProvider = entityId =>
            {
                if (entityId.Equals(svc.PlayerId))
                {
                    var hpCenter = layout.HpBar.worldBound.center;
                    return new Vector3(hpCenter.x, hpCenter.y, 0f);
                }
                var element = layout.SlotVisual.GetSlotElement(entityId);
                if (element != null)
                {
                    var center = element.worldBound.center;
                    return new Vector3(center.x, center.y, 0f);
                }
                return Vector3.zero;
            };
            svc.PositionProvider = positionProvider;
            svc.AnimationService.Initialize(positionProvider, projectileOverlay);

            // Status effect overlays
            var statusTextOverlay = StatusEffectFloatingTextPool.CreateOverlay();
            root.Add(statusTextOverlay);

            // Floating message overlay (generic arc+bounce messages)
            var messageOverlay = FloatingMessagePool.CreateOverlay();
            root.Add(messageOverlay);
            svc.MessagePool = new FloatingMessagePool();
            svc.MessagePool.Initialize(messageOverlay);

            layout.TooltipLayer = new VisualElement { name = "tooltip-layer" };
            layout.TooltipLayer.style.position = Position.Absolute;
            layout.TooltipLayer.style.left = 0;
            layout.TooltipLayer.style.top = 0;
            layout.TooltipLayer.style.right = 0;
            layout.TooltipLayer.style.bottom = 0;
            layout.TooltipLayer.pickingMode = PickingMode.Ignore;
            root.Add(layout.TooltipLayer);

            svc.StatusVisualService.Initialize(positionProvider, statusTextOverlay, layout.SlotVisual);

            // Framework tooltip service
            var elementAnimator = new ElementAnimator();
            svc.FrameworkTooltipService = new TooltipService(svc.EventBus, elementAnimator);
            svc.FrameworkTooltipService.SetTooltipLayer(layout.TooltipLayer);

            // Entity tooltip service — handles hover tooltips for combat, equipment, and inventory slots
            svc.TooltipService = new EntityTooltipService(svc.EventBus);
            svc.TooltipService.Initialize(
                layout.SlotVisual.GetAllSlotElements(),
                layout.RightBar.GetAllSlotElements(),
                layout.LeftBar.GetAllSlotElements(),
                svc.FrameworkTooltipService,
                svc.SlotService, svc.StatusEffects, svc.UnitService, svc.PassiveService,
                svc.EncounterAdapter, svc.WordActionData.ActionRegistry, svc.HandlerRegistry, svc.EnemyResolver,
                svc.AmmoResolver, svc.WeaponService, svc.ConsumableService, svc.EquipmentService,
                svc.ItemRegistry, svc.EntityStats, svc.InventoryService, svc.PlayerInventoryId, svc.PlayerId,
                svc.EventEncounterService);

            // Weapon slot
            layout.WeaponSlot = layout.RightBar.GetSlotElement(4);
            layout.WeaponSlot.pickingMode = PickingMode.Position;

            layout.WeaponDurabilityLabel = new Label();
            layout.WeaponDurabilityLabel.style.color = new Color(1f, 0.8f, 0.2f);
            layout.WeaponDurabilityLabel.style.fontSize = 20;
            layout.WeaponDurabilityLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            layout.WeaponDurabilityLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            layout.WeaponDurabilityLabel.style.position = Position.Absolute;
            layout.WeaponDurabilityLabel.style.bottom = 2;
            layout.WeaponDurabilityLabel.style.left = 4;

            // Consumable slot
            layout.ConsumableSlot = layout.RightBar.GetSlotElement(3);
            layout.ConsumableSlot.pickingMode = PickingMode.Position;

            layout.ConsumableDurabilityLabel = new Label();
            layout.ConsumableDurabilityLabel.style.color = new Color(1f, 0.85f, 0.2f);
            layout.ConsumableDurabilityLabel.style.fontSize = 20;
            layout.ConsumableDurabilityLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            layout.ConsumableDurabilityLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            layout.ConsumableDurabilityLabel.style.position = Position.Absolute;
            layout.ConsumableDurabilityLabel.style.bottom = 2;
            layout.ConsumableDurabilityLabel.style.left = 4;

            layout.MainTextPanel.RegisterCallback<GeometryChangedEvent>(_ =>
                RecalculateFontSize(layout, fontScaleFactor));

            layout.CodeField.schedule.Execute(() => layout.CodeField?.Focus());

            return layout;
        }

        public static void SubscribeCommonEvents(LiveScenarioServices svc, LiveScenarioLayout layout,
            List<IDisposable> subs, Func<bool> isPlayerTurn, Dictionary<string, EntityDefinition> allUnits)
        {
            var bus = svc.EventBus;
            var playerId = svc.PlayerId;

            // Weapon events
            subs.Add(bus.Subscribe<WeaponEquippedEvent>(evt =>
            {
                if (!evt.Entity.Equals(playerId)) return;
                layout.RightBar?.SetSlotContent(4, evt.Weapon.DisplayName, Color.white);
                layout.WeaponSlot?.Add(layout.WeaponDurabilityLabel);
                layout.WeaponDurabilityLabel.text = evt.Weapon.Durability.ToString();
            }));
            subs.Add(bus.Subscribe<WeaponDurabilityChangedEvent>(evt =>
            {
                if (!evt.Entity.Equals(playerId)) return;
                layout.WeaponDurabilityLabel.text = evt.CurrentDurability.ToString();
            }));
            subs.Add(bus.Subscribe<WeaponDestroyedEvent>(evt =>
            {
                if (!evt.Entity.Equals(playerId)) return;
                layout.RightBar?.ClearSlotContent(4);
                if (layout.WeaponDurabilityLabel?.parent != null)
                    layout.WeaponDurabilityLabel.RemoveFromHierarchy();
            }));

            // Consumable events
            subs.Add(bus.Subscribe<ConsumableEquippedEvent>(evt =>
            {
                if (!evt.Entity.Equals(playerId)) return;
                layout.RightBar?.SetSlotContent(3, evt.Consumable.DisplayName, new Color(1f, 0.85f, 0.2f));
                layout.ConsumableDurabilityLabel.text = evt.Consumable.Durability.ToString();
                layout.ConsumableSlot?.Add(layout.ConsumableDurabilityLabel);
            }));
            subs.Add(bus.Subscribe<ConsumableDurabilityChangedEvent>(evt =>
            {
                if (!evt.Entity.Equals(playerId)) return;
                layout.ConsumableDurabilityLabel.text = evt.CurrentDurability.ToString();
            }));
            subs.Add(bus.Subscribe<ConsumableDestroyedEvent>(evt =>
            {
                if (!evt.Entity.Equals(playerId)) return;
                layout.RightBar?.ClearSlotContent(3);
                if (layout.ConsumableDurabilityLabel?.parent != null)
                    layout.ConsumableDurabilityLabel.RemoveFromHierarchy();
            }));

            // Drunk letter changes
            subs.Add(bus.Subscribe<DrunkLettersChangedEvent>(_ => RefreshDrunkVisuals(svc, layout)));

            // Word events
            subs.Add(bus.Subscribe<WordSubmittedEvent>(evt =>
            {
                var word = evt.Word.ToLowerInvariant();
                var actions = svc.WordResolver.Resolve(word);
                var meta = svc.WordResolver.GetStats(word);
                if (actions.Count > 0)
                {
                    var actionList = string.Join(", ", actions.Select(a => $"{a.ActionId}({a.Value})"));
                    Debug.Log($"[LiveScenario] \"{evt.Word}\" → {meta.Target} cost={meta.Cost} | {actionList}");
                }
            }));

            // HP/slot refresh
            subs.Add(bus.Subscribe<DamageTakenEvent>(evt =>
            {
                layout.SlotVisual?.RefreshSlot(evt.EntityId);
                if (evt.EntityId.Equals(playerId)) UpdatePlayerHpBar(layout, svc);
            }));
            subs.Add(bus.Subscribe<HealedEvent>(evt =>
            {
                layout.SlotVisual?.RefreshSlot(evt.EntityId);
                if (evt.EntityId.Equals(playerId)) UpdatePlayerHpBar(layout, svc);
            }));
            subs.Add(bus.Subscribe<EntityDiedEvent>(evt =>
            {
                if (evt.EntityId.Equals(playerId))
                    layout.SlotVisual?.RefreshSlot(evt.EntityId);
                else
                    layout.SlotVisual?.PlayDeathAnimation(evt.EntityId);
            }));

            // Mana bar
            subs.Add(bus.Subscribe<ManaChangedEvent>(evt =>
            {
                if (evt.EntityId.Equals(playerId))
                {
                    if (layout.IsPreviewingManaCost)
                        ClearManaCostPreview(layout, svc);
                    else
                        UpdatePlayerManaBar(layout, svc);
                }
                else
                {
                    layout.SlotVisual?.RefreshSlot(evt.EntityId);
                }
            }));
            subs.Add(bus.Subscribe<WordRejectedEvent>(evt =>
                Debug.Log($"[LiveScenario] Insufficient mana for \"{evt.Word}\" (cost={evt.ManaCost})")));

            // Passive triggers
            subs.Add(bus.Subscribe<PassiveTriggeredEvent>(evt =>
                Debug.Log($"[Passive] {evt.TriggerId}+{evt.EffectId} from {evt.SourceEntity.Value} → " +
                          $"value={evt.Value} affected={evt.AffectedEntity?.Value ?? "none"}")));

            // Enemy actions
            subs.Add(bus.Subscribe<ActionHandlerExecutedEvent>(evt =>
            {
                if (!isPlayerTurn())
                    Debug.Log($"[Turn:Enemy] {evt.ActionId}({evt.Value}) → {evt.Targets.Count} target(s)");
            }));

            // Summon registration
            subs.Add(bus.Subscribe<UnitSummonedEvent>(evt =>
            {
                var word = evt.Word.ToLowerInvariant();
                var uid = new UnitId(evt.EntityId.Value);
                if (allUnits.TryGetValue(word, out var unitDef))
                {
                    svc.UnitService.Register(uid,
                        new UnitDefinition(uid, unitDef.Name,
                            unitDef.MaxHealth, unitDef.Strength, unitDef.PhysicalDefense, unitDef.Luck, unitDef.Color));
                }
                else
                {
                    svc.UnitService.Register(uid,
                        new UnitDefinition(uid, evt.EntityId.Value.ToUpperInvariant(),
                            svc.EntityStats.GetStat(evt.EntityId, StatType.MaxHealth), 0, 0, 0, Color.white));
                }
            }));

            // Slot visual registration
            subs.Add(bus.Subscribe<SlotEntityRegisteredEvent>(evt =>
            {
                if (layout.SlotVisual == null) return;
                var slotElements = layout.SlotVisual.GetAllSlotElements();
                int visualIndex = evt.Slot.Type == SlotType.Enemy ? evt.Slot.Index : 3 + evt.Slot.Index;
                if (visualIndex >= 0 && visualIndex < slotElements.Count)
                    layout.SlotVisual.RegisterEntity(evt.EntityId, slotElements[visualIndex]);
            }));

            // Loot reward
            subs.Add(bus.Subscribe<LootRewardOfferedEvent>(evt =>
                ShowLootSelection(layout, svc, evt.Options)));
            subs.Add(bus.Subscribe<LootRewardSelectedEvent>(_ =>
                HideLootSelection(layout)));

            // Inventory/equipment visual
            subs.Add(bus.Subscribe<Unidad.Core.Inventory.SlotChangedEvent>(evt =>
            {
                if (evt.InventoryId != svc.PlayerInventoryId) return;
                if (evt.NewSlot.IsEmpty)
                {
                    layout.LeftBar?.ClearSlotContent(evt.SlotIndex);
                }
                else
                {
                    var itemWord = evt.NewSlot.ItemId.Value;
                    if (svc.ItemRegistry.TryGet(itemWord, out var itemDef))
                        layout.LeftBar?.SetSlotContent(evt.SlotIndex, itemDef.DisplayName, itemDef.Color);
                    else
                        layout.LeftBar?.SetSlotContent(evt.SlotIndex, itemWord.ToUpperInvariant(), Color.white);
                }
            }));
            subs.Add(bus.Subscribe<ItemEquippedEvent>(evt =>
            {
                if (!evt.Entity.Equals(playerId)) return;
                int slotIndex = (int)evt.Slot;
                layout.RightBar?.SetSlotContent(slotIndex, evt.Item.DisplayName, evt.Item.Color);
            }));
            subs.Add(bus.Subscribe<ItemUnequippedEvent>(evt =>
            {
                if (!evt.Entity.Equals(playerId)) return;
                int slotIndex = (int)evt.Slot;
                layout.RightBar?.ClearSlotContent(slotIndex);
            }));
        }

        public static void SetupInputHandling(LiveScenarioLayout layout, LiveScenarioServices svc,
            Func<string, WordSubmitResult> submitFunc, Func<bool> canFireWeapon,
            Func<bool> canUseConsumable, Func<bool> isEncounterActive,
            float fontScaleFactor, List<IDisposable> subs)
        {
            layout.CodeField.RegisterValueChangedCallback(evt =>
                OnTextChanged(evt, layout, svc, fontScaleFactor));

            layout.CodeField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    // Use remapped word (after drunk letter scramble) so submit matches preview
                    var word = svc.WordInputService.CurrentWord?.Trim() ?? "";
                    if (word.Length == 0) return;

                    var result = submitFunc(word);
                    if (result == WordSubmitResult.InsufficientMana)
                    {
                        PlayManaRejection(layout);
                        layout.CodeField?.schedule.Execute(() => layout.CodeField?.Focus());
                        evt.StopImmediatePropagation();
                        evt.PreventDefault();
                        return;
                    }
                    if (result == WordSubmitResult.Accepted)
                    {
                        ClearManaCostPreview(layout, svc);
                        svc.WordInputService.Clear();
                        layout.CodeField.value = "";
                        RecalculateFontSize(layout, fontScaleFactor);
                        ClearTargetingPreview(layout);
                        layout.CodeField?.schedule.Execute(() => layout.CodeField?.Focus());
                    }
                    evt.StopImmediatePropagation();
                    evt.PreventDefault();
                }
            }, TrickleDown.TrickleDown);

            // Weapon slot click
            layout.WeaponSlot.RegisterCallback<ClickEvent>(_ =>
            {
                if (!canFireWeapon()) return;
                svc.WordInputService.Clear();
                layout.CodeField.value = "";
                ClearTargetingPreview(layout);
                ClearManaCostPreview(layout, svc);
                layout.CodeField?.schedule.Execute(() => layout.CodeField?.Focus());
            });

            // Consumable slot click
            layout.ConsumableSlot.RegisterCallback<ClickEvent>(_ =>
            {
                if (!canUseConsumable()) return;
                svc.WordInputService.Clear();
                layout.CodeField.value = "";
                ClearTargetingPreview(layout);
                ClearManaCostPreview(layout, svc);
                layout.CodeField?.schedule.Execute(() => layout.CodeField?.Focus());
            });

            // Drag-and-drop
            RegisterSlotDragHandlers(layout.LeftBar, EquipmentConstants.InventorySlotCount,
                (evt, slotIndex) =>
                {
                    if (isEncounterActive()) return;
                    if (svc.InventoryService == null) return;
                    var slot = svc.InventoryService.GetSlot(svc.PlayerInventoryId, slotIndex);
                    if (slot.IsEmpty) return;
                    layout.DragItemWord = slot.ItemId.Value;
                    layout.DragSourceSlot = slotIndex;
                    layout.DragFromEquipment = false;
                    StartDrag(layout, evt.position, layout.CodeField.panel.visualTree);
                    evt.StopPropagation();
                });

            RegisterSlotDragHandlers(layout.RightBar, EquipmentConstants.SlotCount,
                (evt, slotIndex) =>
                {
                    if (isEncounterActive()) return;
                    if (svc.EquipmentService == null) return;
                    var slotType = (EquipmentSlotType)slotIndex;
                    var equipped = svc.EquipmentService.GetEquipped(svc.PlayerId, slotType);
                    if (equipped == null) return;
                    layout.DragItemWord = equipped.ItemWord;
                    layout.DragSourceSlot = slotIndex;
                    layout.DragFromEquipment = true;
                    StartDrag(layout, evt.position, layout.CodeField.panel.visualTree);
                    evt.StopPropagation();
                });

            var rootElement = layout.CodeField.panel?.visualTree ?? layout.MainTextPanel.parent;
            rootElement.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (layout.DragElement == null) return;
                UpdateDragPosition(layout, evt.position);
            });
            rootElement.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (layout.DragElement == null || layout.DragItemWord == null) return;

                bool handled;
                if (layout.DragFromEquipment)
                    handled = svc.EquipmentService.UnequipToInventory(
                        svc.PlayerId, (EquipmentSlotType)layout.DragSourceSlot, svc.InventoryService, svc.PlayerInventoryId);
                else
                    handled = TryDropToEquipmentSlot(layout, svc, evt.position);

                CancelDrag(layout);
            });
        }

        public static void SetInputEnabled(LiveScenarioLayout layout, bool enabled)
        {
            if (layout.CodeField == null) return;
            layout.CodeField.PersistentFocus = enabled;
            layout.CodeField.SetEnabled(enabled);
            if (enabled)
                layout.CodeField.schedule.Execute(() => layout.CodeField?.Focus());
        }

        public static void CleanupServices(LiveScenarioServices svc, LiveScenarioLayout layout, List<IDisposable> subs)
        {
            foreach (var sub in subs) sub.Dispose();
            subs.Clear();

            if (svc != null)
            {
                svc.TooltipService?.Dispose();
                (svc.FrameworkTooltipService as IDisposable)?.Dispose();
                svc.MessagePool?.Dispose();
                svc.StatusVisualService?.Dispose();
                svc.AnimationService?.Dispose();
                (svc.PassiveService as IDisposable)?.Dispose();
                (svc.TurnService as IDisposable)?.Dispose();
                (svc.ConsumableService as IDisposable)?.Dispose();
                (svc.ConsumableExecutor as IDisposable)?.Dispose();
                (svc.LootRewardService as IDisposable)?.Dispose();
                (svc.EquipmentService as IDisposable)?.Dispose();
                (svc.InventoryService as IDisposable)?.Dispose();
                (svc.DrunkLetterService as IDisposable)?.Dispose();
                svc.EventBus?.ClearAllSubscriptions();
            }

            if (layout != null)
            {
                layout.DrunkWobbleSchedule?.Pause();
                CancelDrag(layout);
                layout.LootOverlay?.RemoveFromHierarchy();
                HideLootTooltip(layout);
            }
        }

        // --- Private helpers ---

        private static bool IsAmmoWord(LiveScenarioServices svc, string word) =>
            (svc.WeaponService != null && svc.WeaponService.HasWeapon(svc.PlayerId)
             && svc.WeaponService.IsAmmoForEquipped(svc.PlayerId, word))
            || (svc.ConsumableService != null && svc.ConsumableService.HasConsumable(svc.PlayerId)
                && svc.ConsumableService.IsAmmoForEquipped(svc.PlayerId, word));

        private static void OnTextChanged(ChangeEvent<string> evt, LiveScenarioLayout layout,
            LiveScenarioServices svc, float fontScaleFactor)
        {
            var newText = evt.newValue ?? "";
            svc.WordInputService.Clear();
            foreach (var c in newText)
                svc.WordInputService.AppendCharacter(c);

            // Use the remapped word (after drunk letter scramble) for matching and preview
            var matchText = svc.WordInputService.CurrentWord;

            RecalculateFontSize(layout, fontScaleFactor);

            bool isAmmo = IsAmmoWord(svc, matchText);
            var matchService = isAmmo ? svc.AmmoMatchService : svc.WordMatchService;
            var wasMatched = matchService.IsMatched;
            var colors = matchService.CheckMatch(matchText);
            if (colors.Count > 0)
            {
                var labels = layout.CodeField.CharLabels;
                for (int i = 0; i < colors.Count && i < labels.Count; i++)
                    labels[i].style.color = colors[i].Color;

                if (!wasMatched)
                {
                    var indices = new List<int>();
                    for (int i = 0; i < colors.Count; i++)
                        indices.Add(i);
                    layout.CodeField.PlayHighlightAnimation(indices);
                }

                var currentWord = matchText.ToLowerInvariant();
                if (currentWord != layout.LastMatchedWord)
                {
                    layout.LastMatchedWord = currentWord;
                    ShowTargetingPreview(layout, svc, matchText);
                    if (!isAmmo)
                        ShowManaCostPreview(layout, svc, currentWord);
                    else
                        ClearManaCostPreview(layout, svc);
                }
            }
            else
            {
                var labels = layout.CodeField.CharLabels;
                for (int i = 0; i < labels.Count; i++)
                    labels[i].style.color = Color.white;

                layout.LastMatchedWord = "";
                ClearTargetingPreview(layout);
                ClearManaCostPreview(layout, svc);
            }

            // Drunk visual override
            if (svc.DrunkLetterService != null && svc.DrunkLetterService.IsActive)
            {
                var drunkText = layout.CodeField.value ?? "";
                var drunkLabels = layout.CodeField.CharLabels;
                for (int i = 0; i < drunkLabels.Count && i < drunkText.Length; i++)
                {
                    var c = char.ToLowerInvariant(drunkText[i]);
                    if (svc.DrunkLetterService.IsRemappedChar(c))
                    {
                        drunkLabels[i].style.color = new Color(1f, 0.85f, 0.2f);
                        drunkLabels[i].AddToClassList("drunk-letter");
                    }
                    else
                    {
                        drunkLabels[i].RemoveFromClassList("drunk-letter");
                    }
                }
                UpdateDrunkWobble(layout, svc);
            }
        }

        private static void ShowTargetingPreview(LiveScenarioLayout layout, LiveScenarioServices svc, string text)
        {
            ClearTargetingPreview(layout);
            if (svc.PreviewService == null) return;

            var word = text.ToLowerInvariant();
            var previewService = IsAmmoWord(svc, word) ? svc.AmmoPreviewService : svc.PreviewService;
            var preview = previewService.PreviewWord(word);
            if (preview.ActionPreviews.Count == 0) return;

            var sourceEntity = svc.CombatContext.SourceEntity;
            foreach (var actionPreview in preview.ActionPreviews)
            {
                foreach (var entityId in actionPreview.AffectedEntities)
                {
                    var element = layout.SlotVisual.GetSlotElement(entityId);
                    if (element == null) continue;
                    var color = entityId.Equals(sourceEntity) ? HighlightSelf : HighlightEnemy;
                    element.style.backgroundColor = color;
                }
            }
        }

        private static void ClearTargetingPreview(LiveScenarioLayout layout)
        {
            var elements = layout.SlotVisual?.GetAllSlotElements();
            if (elements == null) return;
            for (int i = 0; i < elements.Count; i++)
                elements[i].style.backgroundColor = Color.black;
        }

        private static void RecalculateFontSize(LiveScenarioLayout layout, float fontScaleFactor)
        {
            if (layout.CodeField == null || layout.MainTextPanel == null) return;

            var widthSource = layout.LinesContainer ?? layout.MainTextPanel;
            var panelWidth = widthSource.resolvedStyle.width;
            if (float.IsNaN(panelWidth) || panelWidth <= 0) return;

            var text = layout.CodeField.value ?? "";
            var charCount = Mathf.Max(text.Length, 1);

            var ratio = layout.CodeField.BaseCharWidthRatio;
            var fontSize = panelWidth / (charCount * ratio);
            fontSize = Mathf.Clamp(fontSize, 12f, 800f);
            layout.CodeField.SetCharFontSize(fontSize);

            if (charCount > 0 && text.Length > 0)
            {
                var labels = layout.CodeField.CharLabels;
                if (labels.Count == 0) return;

                EventCallback<GeometryChangedEvent> correctionCallback = null;
                var localFontSize = fontSize;
                var localPanelWidth = panelWidth;
                correctionCallback = _ =>
                {
                    labels[0].UnregisterCallback(correctionCallback);
                    if (layout.CodeField == null || layout.MainTextPanel == null) return;

                    var currentLabels = layout.CodeField.CharLabels;
                    if (currentLabels.Count == 0) return;

                    float totalWidth = 0;
                    foreach (var label in currentLabels)
                    {
                        var w = label.resolvedStyle.width;
                        if (!float.IsNaN(w) && w > 0) totalWidth += w;
                    }

                    if (totalWidth <= 0) return;

                    var targetWidth = localPanelWidth * fontScaleFactor;
                    var correction = targetWidth / totalWidth;
                    var correctedSize = localFontSize * correction;
                    correctedSize = Mathf.Clamp(correctedSize, 12f, 800f);
                    layout.CodeField.SetCharFontSize(correctedSize);
                };
                labels[0].RegisterCallback(correctionCallback);
            }
        }

        private static void BuildHpBar(LiveScenarioLayout layout, LiveScenarioServices svc)
        {
            var hpBarWrapper = new VisualElement();
            hpBarWrapper.style.width = Length.Percent(100);
            hpBarWrapper.style.height = 48;
            hpBarWrapper.style.marginBottom = 8;
            hpBarWrapper.style.justifyContent = Justify.Center;

            layout.HpBar = new UnidadProgressBar(1f);
            layout.HpBar.SetVariant(ProgressVariant.Success);
            layout.HpBar.style.width = Length.Percent(100);
            layout.HpBar.style.height = 16;
            hpBarWrapper.Add(layout.HpBar);

            int hp = svc.EntityStats.GetCurrentHealth(svc.PlayerId);
            int maxHp = svc.EntityStats.GetStat(svc.PlayerId, StatType.MaxHealth);
            layout.HpLabel = new Label($"{hp}/{maxHp}");
            layout.HpLabel.style.position = Position.Absolute;
            layout.HpLabel.style.top = 0;
            layout.HpLabel.style.left = 0;
            layout.HpLabel.style.right = 0;
            layout.HpLabel.style.bottom = 0;
            layout.HpLabel.style.fontSize = 36;
            layout.HpLabel.style.color = Color.white;
            layout.HpLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            layout.HpLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            hpBarWrapper.Add(layout.HpLabel);

            layout.StatsBar.Add(hpBarWrapper);
        }

        private static void BuildManaBar(LiveScenarioLayout layout, LiveScenarioServices svc)
        {
            var manaBarWrapper = new VisualElement();
            manaBarWrapper.style.width = Length.Percent(100);
            manaBarWrapper.style.height = 48;
            manaBarWrapper.style.marginBottom = 8;
            manaBarWrapper.style.justifyContent = Justify.Center;

            layout.ManaBar = new UnidadProgressBar(0.5f);
            layout.ManaBar.SetVariant(ProgressVariant.Info);
            layout.ManaBar.style.width = Length.Percent(100);
            layout.ManaBar.style.height = 16;
            manaBarWrapper.Add(layout.ManaBar);

            layout.ManaCostOverlay = new VisualElement();
            layout.ManaCostOverlay.style.position = Position.Absolute;
            layout.ManaCostOverlay.style.top = 0;
            layout.ManaCostOverlay.style.bottom = 0;
            layout.ManaCostOverlay.style.display = DisplayStyle.None;
            layout.ManaCostOverlay.pickingMode = PickingMode.Ignore;
            layout.ManaBar.Add(layout.ManaCostOverlay);

            int mana = svc.EntityStats.GetCurrentMana(svc.PlayerId);
            int maxMana = svc.EntityStats.GetStat(svc.PlayerId, StatType.MaxMana);
            layout.ManaLabel = new Label($"{mana}/{maxMana}");
            layout.ManaLabel.style.position = Position.Absolute;
            layout.ManaLabel.style.top = 0;
            layout.ManaLabel.style.left = 0;
            layout.ManaLabel.style.right = 0;
            layout.ManaLabel.style.bottom = 0;
            layout.ManaLabel.style.fontSize = 36;
            layout.ManaLabel.style.color = Color.white;
            layout.ManaLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            layout.ManaLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            manaBarWrapper.Add(layout.ManaLabel);

            layout.StatsBar.Add(manaBarWrapper);
        }

        private static void BuildStatsRow(LiveScenarioLayout layout)
        {
            var statsRow = new VisualElement();
            statsRow.style.flexDirection = FlexDirection.Row;
            statsRow.style.alignItems = Align.Center;
            layout.StatsBar.Add(statsRow);

            var statsLabel = new Label("STR: 10  DEX: 8  INT: 12");
            statsLabel.style.color = Color.white;
            statsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            statsLabel.style.fontSize = 38;
            statsLabel.style.marginRight = 20;
            statsRow.Add(statsLabel);

            var statusLabel = new Label("Status: Normal");
            statusLabel.style.color = Color.white;
            statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusLabel.style.fontSize = 38;
            statsRow.Add(statusLabel);
        }

        private static void UpdatePlayerHpBar(LiveScenarioLayout layout, LiveScenarioServices svc)
        {
            if (layout.HpBar == null || layout.HpLabel == null) return;
            int hp = svc.EntityStats.GetCurrentHealth(svc.PlayerId);
            int maxHp = svc.EntityStats.GetStat(svc.PlayerId, StatType.MaxHealth);
            layout.HpBar.Value = (float)hp / maxHp;
            layout.HpLabel.text = $"{hp}/{maxHp}";
        }

        private static void UpdatePlayerManaBar(LiveScenarioLayout layout, LiveScenarioServices svc)
        {
            if (layout.ManaBar == null || layout.ManaLabel == null) return;
            int mana = svc.EntityStats.GetCurrentMana(svc.PlayerId);
            int maxMana = svc.EntityStats.GetStat(svc.PlayerId, StatType.MaxMana);
            layout.ManaBar.Value = maxMana > 0 ? (float)mana / maxMana : 0f;
            layout.ManaLabel.text = $"{mana}/{maxMana}";
        }

        private static void ShowManaCostPreview(LiveScenarioLayout layout, LiveScenarioServices svc, string word)
        {
            if (layout.ManaBar == null || layout.ManaLabel == null || layout.ManaCostOverlay == null) return;

            var meta = svc.WordResolver.GetStats(word);
            int cost = meta.Cost;
            if (cost <= 0) { ClearManaCostPreview(layout, svc); return; }

            layout.IsPreviewingManaCost = true;
            int currentMana = svc.EntityStats.GetCurrentMana(svc.PlayerId);
            int maxMana = svc.EntityStats.GetStat(svc.PlayerId, StatType.MaxMana);
            if (maxMana <= 0) return;

            float currentRatio = (float)currentMana / maxMana;
            int previewMana = currentMana - cost;
            float previewRatio = (float)previewMana / maxMana;

            layout.ManaBar.Value = Mathf.Clamp01(previewRatio);

            float overlayLeft = Mathf.Max(0f, previewRatio);
            float overlayWidth = currentRatio - overlayLeft;
            layout.ManaCostOverlay.style.left = Length.Percent(overlayLeft * 100f);
            layout.ManaCostOverlay.style.width = Length.Percent(overlayWidth * 100f);
            layout.ManaCostOverlay.style.display = DisplayStyle.Flex;

            bool canAfford = previewMana >= 0;
            layout.ManaCostOverlay.style.backgroundColor = canAfford
                ? new Color(1f, 0.6f, 0f, 0.5f)
                : new Color(1f, 0f, 0f, 0.5f);

            layout.ManaLabel.text = $"{previewMana}/{maxMana} (-{cost})";
            layout.ManaLabel.style.color = canAfford ? Color.white : Color.red;
        }

        private static void ClearManaCostPreview(LiveScenarioLayout layout, LiveScenarioServices svc)
        {
            if (!layout.IsPreviewingManaCost) return;
            layout.IsPreviewingManaCost = false;
            if (layout.ManaCostOverlay != null)
                layout.ManaCostOverlay.style.display = DisplayStyle.None;
            UpdatePlayerManaBar(layout, svc);
            if (layout.ManaLabel != null)
                layout.ManaLabel.style.color = Color.white;
        }

        private static void PlayManaRejection(LiveScenarioLayout layout)
        {
            var labels = layout.CodeField.CharLabels;
            var indices = new List<int>();
            for (int i = 0; i < labels.Count; i++)
                indices.Add(i);
            layout.CodeField.PlayRejectionAnimation(indices);

            layout.ManaBar.SetVariant(ProgressVariant.Danger);
            layout.ManaBar.schedule.Execute(() => layout.ManaBar?.SetVariant(ProgressVariant.Info)).ExecuteLater(500);
        }

        private static void RefreshDrunkVisuals(LiveScenarioServices svc, LiveScenarioLayout layout)
        {
            if (layout.CodeField == null) return;
            var labels = layout.CodeField.CharLabels;
            if (labels.Count == 0) return;

            var text = layout.CodeField.value ?? "";
            for (int i = 0; i < labels.Count && i < text.Length; i++)
            {
                var c = char.ToLowerInvariant(text[i]);
                if (svc.DrunkLetterService != null && svc.DrunkLetterService.IsRemappedChar(c))
                {
                    labels[i].style.color = new Color(1f, 0.85f, 0.2f);
                    labels[i].AddToClassList("drunk-letter");
                }
                else
                {
                    labels[i].RemoveFromClassList("drunk-letter");
                }
            }
            UpdateDrunkWobble(layout, svc);
        }

        private static void UpdateDrunkWobble(LiveScenarioLayout layout, LiveScenarioServices svc)
        {
            if (svc.DrunkLetterService == null || !svc.DrunkLetterService.IsActive)
            {
                layout.DrunkWobbleSchedule?.Pause();
                layout.DrunkWobbleSchedule = null;
                if (layout.CodeField != null)
                {
                    var labels = layout.CodeField.CharLabels;
                    for (int i = 0; i < labels.Count; i++)
                    {
                        labels[i].style.translate = new Translate(0, 0);
                        labels[i].RemoveFromClassList("drunk-letter");
                    }
                }
                return;
            }

            if (layout.DrunkWobbleSchedule != null) return;

            layout.DrunkWobbleSchedule = layout.CodeField.schedule.Execute(() =>
            {
                if (layout.CodeField == null || svc.DrunkLetterService == null) return;
                var labels = layout.CodeField.CharLabels;
                var text = layout.CodeField.value ?? "";
                float time = Time.time;
                for (int i = 0; i < labels.Count && i < text.Length; i++)
                {
                    if (!labels[i].ClassListContains("drunk-letter")) continue;
                    float offsetX = Mathf.Sin(time * 8 + i) * 3f;
                    float offsetY = Mathf.Cos(time * 6 + i * 0.5f) * 2f;
                    labels[i].style.translate = new Translate(offsetX, offsetY);
                }
            }).Every(16);
        }

        // --- Drag-and-drop helpers ---

        private static void RegisterSlotDragHandlers(EquipmentBarVisual bar, int count,
            Action<PointerDownEvent, int> handler)
        {
            for (int i = 0; i < count; i++)
            {
                var slotIndex = i;
                var slotElement = bar.GetSlotElement(i);
                slotElement.pickingMode = PickingMode.Position;
                slotElement.RegisterCallback<PointerDownEvent>(evt => handler(evt, slotIndex));
            }
        }

        private static void StartDrag(LiveScenarioLayout layout, Vector2 position, VisualElement rootElement)
        {
            if (layout.DragElement != null) return;

            layout.DragElement = new VisualElement();
            layout.DragElement.style.position = Position.Absolute;
            layout.DragElement.style.width = 100;
            layout.DragElement.style.height = 80;
            layout.DragElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 0.85f);
            layout.DragElement.style.borderTopWidth = 2;
            layout.DragElement.style.borderBottomWidth = 2;
            layout.DragElement.style.borderLeftWidth = 2;
            layout.DragElement.style.borderRightWidth = 2;
            layout.DragElement.style.borderTopColor = Color.yellow;
            layout.DragElement.style.borderBottomColor = Color.yellow;
            layout.DragElement.style.borderLeftColor = Color.yellow;
            layout.DragElement.style.borderRightColor = Color.yellow;
            layout.DragElement.style.justifyContent = Justify.Center;
            layout.DragElement.style.alignItems = Align.Center;
            layout.DragElement.pickingMode = PickingMode.Ignore;

            var label = new Label(layout.DragItemWord.ToUpperInvariant());
            label.style.color = Color.white;
            label.style.fontSize = 16;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.pickingMode = PickingMode.Ignore;
            layout.DragElement.Add(label);

            rootElement.Add(layout.DragElement);
            UpdateDragPosition(layout, position);
        }

        private static void UpdateDragPosition(LiveScenarioLayout layout, Vector2 position)
        {
            if (layout.DragElement == null) return;
            layout.DragElement.style.left = position.x - 50;
            layout.DragElement.style.top = position.y - 40;
        }

        private static bool TryDropToEquipmentSlot(LiveScenarioLayout layout, LiveScenarioServices svc, Vector2 dropPos)
        {
            for (int i = 0; i < EquipmentConstants.SlotCount; i++)
            {
                var slotElement = layout.RightBar.GetSlotElement(i);
                if (slotElement == null || !slotElement.worldBound.Contains(dropPos)) continue;

                var targetSlotType = (EquipmentSlotType)i;
                var itemSlotType = svc.EquipmentService.GetSlotTypeForItem(layout.DragItemWord);
                if (itemSlotType == null || itemSlotType.Value != targetSlotType) return false;

                return svc.EquipmentService.EquipFromInventory(
                    svc.PlayerId, layout.DragItemWord, svc.InventoryService, svc.PlayerInventoryId);
            }
            return false;
        }

        private static void CancelDrag(LiveScenarioLayout layout)
        {
            layout.DragElement?.RemoveFromHierarchy();
            layout.DragElement = null;
            layout.DragItemWord = null;
            layout.DragSourceSlot = -1;
            layout.DragFromEquipment = false;
        }

        // --- Loot overlay ---

        private static void ShowLootSelection(LiveScenarioLayout layout, LiveScenarioServices svc,
            EquipmentItemDefinition[] options)
        {
            SetInputEnabled(layout, false);

            layout.LootOverlay = new VisualElement();
            layout.LootOverlay.style.position = Position.Absolute;
            layout.LootOverlay.style.left = 0;
            layout.LootOverlay.style.top = 0;
            layout.LootOverlay.style.right = 0;
            layout.LootOverlay.style.bottom = 0;
            layout.LootOverlay.style.justifyContent = Justify.Center;
            layout.LootOverlay.style.alignItems = Align.Center;
            layout.LootOverlay.pickingMode = PickingMode.Position;

            var title = new Label("Choose a reward");
            title.style.fontSize = 32;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 24;
            title.pickingMode = PickingMode.Ignore;
            layout.LootOverlay.Add(title);

            var cardRow = new VisualElement();
            cardRow.style.flexDirection = FlexDirection.Row;
            cardRow.style.justifyContent = Justify.Center;
            cardRow.style.alignItems = Align.FlexStart;
            cardRow.pickingMode = PickingMode.Ignore;
            layout.LootOverlay.Add(cardRow);

            for (int i = 0; i < options.Length; i++)
            {
                var item = options[i];
                var cardIndex = i;
                var slotIndex = (int)item.SlotType;

                var card = new VisualElement();
                card.style.marginLeft = 12;
                card.style.marginRight = 12;
                card.style.alignItems = Align.Center;
                card.pickingMode = PickingMode.Position;

                card.Add(TooltipContentBuilder.CreateMiniWordBox(item.DisplayName, item.Color, 120, 100));

                card.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    ShowLootTooltip(layout, item, card);
                    HighlightEquipmentSlot(layout, slotIndex);
                });
                card.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    HideLootTooltip(layout);
                    ClearEquipmentSlotHighlight(layout);
                });
                card.RegisterCallback<ClickEvent>(_ => svc.LootRewardService.SelectReward(cardIndex));

                cardRow.Add(card);
            }

            var rootVe = layout.CodeField.panel?.visualTree ?? layout.MainTextPanel.parent;
            rootVe.Add(layout.LootOverlay);
        }

        private static void HideLootSelection(LiveScenarioLayout layout)
        {
            layout.LootOverlay?.RemoveFromHierarchy();
            layout.LootOverlay = null;
            HideLootTooltip(layout);
            ClearEquipmentSlotHighlight(layout);
        }

        private static void ShowLootTooltip(LiveScenarioLayout layout, EquipmentItemDefinition item, VisualElement card)
        {
            HideLootTooltip(layout);

            layout.LootTooltip = new VisualElement();
            layout.LootTooltip.style.position = Position.Absolute;
            layout.LootTooltip.style.backgroundColor = Color.black;
            layout.LootTooltip.style.borderTopWidth = 1;
            layout.LootTooltip.style.borderBottomWidth = 1;
            layout.LootTooltip.style.borderLeftWidth = 1;
            layout.LootTooltip.style.borderRightWidth = 1;
            layout.LootTooltip.style.borderTopColor = Color.white;
            layout.LootTooltip.style.borderBottomColor = Color.white;
            layout.LootTooltip.style.borderLeftColor = Color.white;
            layout.LootTooltip.style.borderRightColor = Color.white;
            layout.LootTooltip.style.paddingLeft = 16;
            layout.LootTooltip.style.paddingRight = 16;
            layout.LootTooltip.style.paddingTop = 12;
            layout.LootTooltip.style.paddingBottom = 12;
            layout.LootTooltip.pickingMode = PickingMode.Ignore;

            layout.LootTooltip.Add(TooltipContentBuilder.BuildHeader(item.DisplayName, item.Color, item.SlotType.ToString()));
            layout.LootTooltip.Add(TooltipContentBuilder.BuildArmorContent(item));

            var rootVe = layout.CodeField.panel?.visualTree ?? layout.MainTextPanel.parent;
            rootVe.Add(layout.LootTooltip);

            var cardBound = card.worldBound;
            layout.LootTooltip.style.left = cardBound.x + cardBound.width + 12;
            layout.LootTooltip.style.top = cardBound.y;
        }

        private static void HideLootTooltip(LiveScenarioLayout layout)
        {
            layout.LootTooltip?.RemoveFromHierarchy();
            layout.LootTooltip = null;
        }

        private static void HighlightEquipmentSlot(LiveScenarioLayout layout, int slotIndex)
        {
            ClearEquipmentSlotHighlight(layout);
            layout.HighlightedSlotIndex = slotIndex;
            var slot = layout.RightBar.GetSlotElement(slotIndex);
            var green = new Color(0.3f, 1f, 0.3f);
            slot.style.borderTopColor = green;
            slot.style.borderBottomColor = green;
            slot.style.borderLeftColor = green;
            slot.style.borderRightColor = green;
        }

        private static void ClearEquipmentSlotHighlight(LiveScenarioLayout layout)
        {
            if (layout.HighlightedSlotIndex < 0) return;
            var slot = layout.RightBar.GetSlotElement(layout.HighlightedSlotIndex);
            slot.style.borderTopColor = Color.white;
            slot.style.borderBottomColor = Color.white;
            slot.style.borderLeftColor = Color.white;
            slot.style.borderRightColor = Color.white;
            layout.HighlightedSlotIndex = -1;
        }

        private sealed class LiveAnimationResolver : IAnimationResolver
        {
            public bool IsInstant => false;
            public void Play(string animationId, Action onComplete = null) => onComplete?.Invoke();
        }
    }
}
