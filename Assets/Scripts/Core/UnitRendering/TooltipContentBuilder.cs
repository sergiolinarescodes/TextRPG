using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.Encounter;
using TextRPG.Core.Equipment;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive;
using TextRPG.Core.Scroll;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.WordAction;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.UnitRendering
{
    internal static class TooltipContentBuilder
    {
        private const int FontBase = 26;
        private const int FontSmall = 22;
        private static readonly Color Dim = Color.white;
        private static readonly Color StatGreen = new(0.3f, 1f, 0.3f);
        private static readonly HashSet<string> MetaActions = new() { "Item", "Weapon" };

        private static List<WordActionMapping> FilterActions(IReadOnlyList<WordActionMapping> actions, IActionHandlerRegistry handlerRegistry)
        {
            var result = new List<WordActionMapping>();
            foreach (var a in actions)
            {
                if (MetaActions.Contains(a.ActionId)) continue;
                if (handlerRegistry != null && !handlerRegistry.TryGet(a.ActionId, out _)) continue;
                result.Add(a);
            }
            return result;
        }

        public static VisualElement CreateMiniWordBox(string word, Color color, float width = 84, float height = 54)
        {
            var box = new VisualElement();
            box.style.width = width;
            box.style.height = height;
            box.style.backgroundColor = Color.black;
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderTopColor = color;
            box.style.borderBottomColor = color;
            box.style.borderLeftColor = color;
            box.style.borderRightColor = color;
            box.style.justifyContent = Justify.Center;
            box.style.alignItems = Align.Center;
            box.style.overflow = Overflow.Hidden;
            box.pickingMode = PickingMode.Ignore;

            var layout = UnitTextLayout.Calculate(word.ToUpperInvariant(), width - 4, height - 4);
            UnitTextLabels.AddTo(layout, color, box);

            return box;
        }

        public static VisualElement BuildEquipmentContent(EquipmentItemDefinition item, int durability,
            IReadOnlyList<string> ammoWords, IWordResolver ammoResolver, IActionRegistry registry,
            IActionHandlerRegistry handlerRegistry = null)
        {
            var root = new VisualElement();
            root.pickingMode = PickingMode.Ignore;

            if (ammoWords != null && ammoWords.Count > 0)
            {
                foreach (var word in ammoWords)
                {
                    var actions = ammoResolver?.Resolve(word);
                    var realActions = actions != null ? FilterActions(actions, handlerRegistry) : new List<WordActionMapping>();
                    var color = Color.white;
                    if (realActions.Count > 0)
                        color = ActionDescriptions.GetColor(realActions[0].ActionId, registry);

                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.style.marginBottom = 4;
                    row.pickingMode = PickingMode.Ignore;

                    var letsYou = new Label("Lets you");
                    letsYou.style.color = Color.white;
                    letsYou.style.fontSize = FontBase;
                    letsYou.style.unityFontStyleAndWeight = FontStyle.Bold;
                    letsYou.style.marginRight = 8;
                    letsYou.pickingMode = PickingMode.Ignore;
                    row.Add(letsYou);

                    row.Add(CreateMiniWordBox(word, color));

                    var times = new Label($"{durability} times");
                    times.style.color = Color.white;
                    times.style.fontSize = FontBase;
                    times.style.unityFontStyleAndWeight = FontStyle.Bold;
                    times.style.marginLeft = 8;
                    times.pickingMode = PickingMode.Ignore;
                    row.Add(times);

                    root.Add(row);

                    if (realActions.Count > 0)
                    {
                        var actionTexts = realActions.Select(ActionDescriptions.FormatNatural);
                        var descLabel = new Label(string.Join(", ", actionTexts));
                        descLabel.style.color = color;
                        descLabel.style.fontSize = FontSmall;
                        descLabel.style.marginLeft = 4;
                        descLabel.style.marginBottom = 2;
                        descLabel.pickingMode = PickingMode.Ignore;
                        root.Add(descLabel);
                    }
                }
            }

            if (item.Passives != null && item.Passives.Length > 0)
                AddPassivesSection(root, item.Passives);

            return root;
        }

        public static VisualElement BuildArmorContent(EquipmentItemDefinition item)
        {
            var root = new VisualElement();
            root.pickingMode = PickingMode.Ignore;

            AddEquipmentStats(root, item.Stats);

            if (item.Passives != null && item.Passives.Length > 0)
                AddPassivesSection(root, item.Passives);

            return root;
        }

        public static VisualElement BuildScrollContent(ScrollDefinition scroll)
        {
            var root = new VisualElement();
            root.pickingMode = PickingMode.Ignore;

            var spellLabel = new Label($"Spell: {scroll.OriginalWord}");
            spellLabel.style.color = Color.white;
            spellLabel.style.fontSize = 14;
            spellLabel.style.marginTop = 4;
            spellLabel.pickingMode = PickingMode.Ignore;
            root.Add(spellLabel);

            var costLabel = new Label($"Mana cost: {scroll.ManaCost}");
            costLabel.style.color = new Color(0.4f, 0.7f, 1f);
            costLabel.style.fontSize = 14;
            costLabel.style.marginTop = 2;
            costLabel.pickingMode = PickingMode.Ignore;
            root.Add(costLabel);

            var cdLabel = new Label("Cooldown: 2 rounds (fixed)");
            cdLabel.style.color = new Color(1f, 0.8f, 0.4f);
            cdLabel.style.fontSize = 14;
            cdLabel.style.marginTop = 2;
            cdLabel.pickingMode = PickingMode.Ignore;
            root.Add(cdLabel);

            var typeLabel = new Label("MagicDamage");
            typeLabel.style.color = new Color(0.8f, 0.5f, 1f);
            typeLabel.style.fontSize = 14;
            typeLabel.style.marginTop = 2;
            typeLabel.pickingMode = PickingMode.Ignore;
            root.Add(typeLabel);

            return root;
        }

        public static VisualElement BuildHeader(string displayName, Color nameColor, string subtitle = null)
        {
            var container = new VisualElement();
            container.style.marginBottom = 4;
            container.pickingMode = PickingMode.Ignore;

            var nameLabel = new Label(displayName);
            nameLabel.style.color = nameColor;
            nameLabel.style.fontSize = 28;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.pickingMode = PickingMode.Ignore;
            container.Add(nameLabel);

            if (subtitle != null)
            {
                var sub = new Label(subtitle);
                sub.style.color = Dim;
                sub.style.fontSize = FontSmall;
                sub.pickingMode = PickingMode.Ignore;
                container.Add(sub);
            }

            return container;
        }

        public static void AddStatsRow(VisualElement parent, IEntityStatsService stats, EntityId entityId)
        {
            if (!stats.HasEntity(entityId)) return;

            int hp = stats.GetCurrentHealth(entityId);
            int maxHp = stats.GetStat(entityId, StatType.MaxHealth);
            int str = stats.GetStat(entityId, StatType.Strength);
            int def = stats.GetStat(entityId, StatType.PhysicalDefense);
            int mag = stats.GetStat(entityId, StatType.MagicPower);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2;
            row.pickingMode = PickingMode.Ignore;

            AddStatChip(row, $"{hp}/{maxHp}", Color.green);
            AddStatChip(row, $"STR {str}", new Color(1f, 0.6f, 0.3f));
            AddStatChip(row, $"DEF {def}", new Color(0.6f, 0.7f, 0.8f));
            if (mag > 0) AddStatChip(row, $"MAG {mag}", new Color(0.6f, 0.4f, 1f));

            int mana = stats.GetCurrentMana(entityId);
            int maxMana = stats.GetStat(entityId, StatType.MaxMana);
            if (maxMana > 0)
                AddStatChip(row, $"MP {mana}/{maxMana}", new Color(0.3f, 0.5f, 1f));

            parent.Add(row);
        }

        private static void AddStatChip(VisualElement row, string text, Color color)
        {
            var label = new Label(text);
            label.style.color = color;
            label.style.fontSize = FontSmall;
            label.style.marginRight = 8;
            label.pickingMode = PickingMode.Ignore;
            row.Add(label);
        }

        public static void AddEquipmentStats(VisualElement parent, StatBonus stats)
        {
            var parts = new List<string>();
            if (stats.Strength > 0) parts.Add($"+{stats.Strength} STR");
            if (stats.MagicPower > 0) parts.Add($"+{stats.MagicPower} MAG");
            if (stats.PhysDefense > 0) parts.Add($"+{stats.PhysDefense} DEF");
            if (stats.MagicDefense > 0) parts.Add($"+{stats.MagicDefense} MDEF");
            if (stats.Luck > 0) parts.Add($"+{stats.Luck} LCK");
            if (stats.MaxHealth > 0) parts.Add($"+{stats.MaxHealth} HP");
            if (stats.MaxMana > 0) parts.Add($"+{stats.MaxMana} MP");

            if (parts.Count == 0) return;

            var label = new Label(string.Join("  ", parts));
            label.style.color = StatGreen;
            label.style.fontSize = FontBase;
            label.style.marginBottom = 2;
            label.pickingMode = PickingMode.Ignore;
            parent.Add(label);
        }

        public static void AddDivider(VisualElement parent)
        {
            var line = new VisualElement();
            line.style.height = 1;
            line.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            line.style.marginTop = 3;
            line.style.marginBottom = 3;
            line.pickingMode = PickingMode.Ignore;
            parent.Add(line);
        }

        public static void AddAbilitiesSection(VisualElement parent, string[] abilityWords,
            IWordResolver resolver, IActionRegistry registry, IActionHandlerRegistry handlerRegistry = null)
        {
            if (abilityWords == null || abilityWords.Length == 0) return;

            AddDivider(parent);

            foreach (var word in abilityWords)
            {
                var actions = resolver.Resolve(word);
                if (actions.Count == 0) continue;
                AddActionRow(parent, word.ToUpperInvariant(), actions, registry, handlerRegistry);
            }
        }

        public static void AddAmmoSection(VisualElement parent, IReadOnlyList<string> ammoWords,
            IWordResolver resolver, IActionRegistry registry, IActionHandlerRegistry handlerRegistry = null)
        {
            if (ammoWords == null || ammoWords.Count == 0) return;

            AddDivider(parent);

            foreach (var word in ammoWords)
            {
                var actions = resolver.Resolve(word);
                if (actions.Count == 0) continue;
                AddActionRow(parent, word.ToUpperInvariant(), actions, registry, handlerRegistry);
            }
        }

        public static void AddPassivesSection(VisualElement parent, IReadOnlyList<PassiveEntry> passives)
        {
            if (passives == null || passives.Count == 0) return;

            AddDivider(parent);

            foreach (var entry in passives)
            {
                var pDef = PassiveDefinitions.Generate(entry);
                var label = new Label(pDef.Description);
                label.style.color = pDef.DisplayColor;
                label.style.fontSize = FontSmall;
                label.style.marginBottom = 1;
                label.pickingMode = PickingMode.Ignore;
                parent.Add(label);
            }
        }

        public static void AddStatusEffectsSection(VisualElement parent, IReadOnlyList<StatusEffectInstance> effects)
        {
            if (effects == null || effects.Count == 0) return;

            AddDivider(parent);

            foreach (var instance in effects)
            {
                var def = StatusEffectDefinitions.Get(instance.Type);

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 1;
                row.pickingMode = PickingMode.Ignore;

                var name = new Label(def.DisplayName);
                name.style.color = def.DisplayColor;
                name.style.fontSize = FontSmall;
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.marginRight = 6;
                name.pickingMode = PickingMode.Ignore;
                row.Add(name);

                var infoText = instance.IsPermanent
                    ? $"x{instance.StackCount}"
                    : $"{instance.RemainingDuration}t x{instance.StackCount}";
                var info = new Label(infoText);
                info.style.color = Dim;
                info.style.fontSize = FontSmall;
                info.pickingMode = PickingMode.Ignore;
                row.Add(info);

                parent.Add(row);
            }
        }

        public static void AddTagsSection(VisualElement parent, string[] tags)
        {
            if (tags == null || tags.Length == 0) return;

            AddDivider(parent);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginBottom = 2;
            row.pickingMode = PickingMode.Ignore;

            foreach (var tag in tags)
            {
                var chip = new Label(tag);
                chip.style.color = new Color(0.9f, 0.8f, 0.5f);
                chip.style.fontSize = FontSmall;
                chip.style.backgroundColor = new Color(0.3f, 0.25f, 0.1f);
                chip.style.paddingLeft = 6;
                chip.style.paddingRight = 6;
                chip.style.paddingTop = 2;
                chip.style.paddingBottom = 2;
                chip.style.borderTopLeftRadius = 4;
                chip.style.borderTopRightRadius = 4;
                chip.style.borderBottomLeftRadius = 4;
                chip.style.borderBottomRightRadius = 4;
                chip.style.marginRight = 4;
                chip.pickingMode = PickingMode.Ignore;
                row.Add(chip);
            }

            parent.Add(row);
        }

        private static void AddActionRow(VisualElement parent, string wordLabel,
            IReadOnlyList<WordActionMapping> actions, IActionRegistry registry, IActionHandlerRegistry handlerRegistry = null)
        {
            var realActions = FilterActions(actions, handlerRegistry);
            if (realActions.Count == 0) return;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2;
            row.pickingMode = PickingMode.Ignore;

            var firstColor = ActionDescriptions.GetColor(realActions[0].ActionId, registry);
            row.Add(CreateMiniWordBox(wordLabel, firstColor, 66, 45));

            var actionTexts = realActions.Select(ActionDescriptions.FormatNatural).ToList();
            var actionLabel = new Label(string.Join(", ", actionTexts));
            actionLabel.style.color = firstColor;
            actionLabel.style.fontSize = FontBase;
            actionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            actionLabel.style.marginLeft = 6;
            actionLabel.style.whiteSpace = WhiteSpace.Normal;
            actionLabel.style.flexShrink = 1;
            actionLabel.pickingMode = PickingMode.Ignore;
            row.Add(actionLabel);

            parent.Add(row);
        }
    }
}
