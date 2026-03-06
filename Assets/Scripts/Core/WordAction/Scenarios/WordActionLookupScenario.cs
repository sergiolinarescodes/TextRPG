using System;
using System.Collections.Generic;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.WordAction.Scenarios
{
    internal sealed class WordActionLookupScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter WordParam = new(
            "word", "Word to Resolve", typeof(string), "tsunami");

        private IWordResolver _resolver;
        private IActionRegistry _actionRegistry;
        private VisualElement _resultsPanel;
        private IReadOnlyList<WordActionMapping> _lastResult;
        private string _resolvedWord;

        public WordActionLookupScenario() : base(new TestScenarioDefinition(
            "word-action-lookup",
            "Word Action Lookup",
            "Resolves a word to its action mappings and displays results. " +
            "Uses in-memory test data (tsunami, ember, inferno, etc.).",
            new[] { WordParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var word = ResolveParam<string>(overrides, "word")?.Trim().ToLowerInvariant() ?? "";

            var data = WordActionTestFactory.CreateTestData();
            _resolver = data.Resolver;
            _actionRegistry = data.ActionRegistry;

            _lastResult = _resolver.Resolve(word);
            _resolvedWord = word;
            var meta = _resolver.GetStats(word);

            Debug.Log($"[WordActionLookup] Resolving \"{word}\" — {_lastResult.Count} action(s), target={meta.Target}, cost={meta.Cost}");
            foreach (var mapping in _lastResult)
            {
                Debug.Log($"[WordActionLookup]   {mapping.ActionId}({mapping.Value})");
            }

            BuildUI(word, _lastResult, meta);
        }

        private void BuildUI(string word, IReadOnlyList<WordActionMapping> actions, WordMeta meta)
        {
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            root.style.paddingTop = 20;
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;

            // Title
            var title = new Label("Word Action Lookup");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            // Word label
            var wordLabel = new Label($"Word: \"{word}\"");
            wordLabel.style.fontSize = 18;
            wordLabel.style.color = new Color(0.6f, 0.8f, 1f);
            wordLabel.style.marginBottom = 12;
            root.Add(wordLabel);

            // Results panel
            _resultsPanel = new VisualElement();
            _resultsPanel.style.flexDirection = FlexDirection.Column;
            _resultsPanel.style.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
            _resultsPanel.style.borderTopLeftRadius = 8;
            _resultsPanel.style.borderTopRightRadius = 8;
            _resultsPanel.style.borderBottomLeftRadius = 8;
            _resultsPanel.style.borderBottomRightRadius = 8;
            _resultsPanel.style.paddingTop = 12;
            _resultsPanel.style.paddingBottom = 12;
            _resultsPanel.style.paddingLeft = 16;
            _resultsPanel.style.paddingRight = 16;
            root.Add(_resultsPanel);

            // Target and Cost display
            var metaRow = new VisualElement();
            metaRow.style.flexDirection = FlexDirection.Row;
            metaRow.style.marginBottom = 12;

            var targetLabel = new Label($"Target: {(string.IsNullOrEmpty(meta.Target) ? "—" : meta.Target)}");
            targetLabel.style.fontSize = 16;
            targetLabel.style.color = new Color(0.6f, 0.8f, 1f);
            targetLabel.style.marginRight = 24;
            metaRow.Add(targetLabel);

            var costLabel = new Label($"Cost: {meta.Cost}");
            costLabel.style.fontSize = 16;
            costLabel.style.color = new Color(1f, 0.85f, 0.3f);
            metaRow.Add(costLabel);

            root.Add(metaRow);

            if (actions.Count == 0)
            {
                var noResult = new Label("No actions found");
                noResult.style.fontSize = 16;
                noResult.style.color = new Color(0.6f, 0.6f, 0.6f);
                noResult.style.unityFontStyleAndWeight = FontStyle.Italic;
                _resultsPanel.Add(noResult);
            }
            else
            {
                foreach (var mapping in actions)
                {
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 6;
                    row.style.alignItems = Align.Center;

                    var actionLabel = new Label(mapping.ActionId);
                    actionLabel.style.fontSize = 16;
                    actionLabel.style.color = GetActionColor(mapping.ActionId);
                    actionLabel.style.width = 100;
                    row.Add(actionLabel);

                    // Value bar
                    var barContainer = new VisualElement();
                    barContainer.style.flexGrow = 1;
                    barContainer.style.height = 20;
                    barContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
                    barContainer.style.borderTopLeftRadius = 4;
                    barContainer.style.borderTopRightRadius = 4;
                    barContainer.style.borderBottomLeftRadius = 4;
                    barContainer.style.borderBottomRightRadius = 4;
                    row.Add(barContainer);

                    var bar = new VisualElement();
                    bar.style.width = Length.Percent(mapping.Value * 10f);
                    bar.style.height = Length.Percent(100);
                    bar.style.backgroundColor = GetActionColor(mapping.ActionId);
                    bar.style.borderTopLeftRadius = 4;
                    bar.style.borderTopRightRadius = 4;
                    bar.style.borderBottomLeftRadius = 4;
                    bar.style.borderBottomRightRadius = 4;
                    barContainer.Add(bar);

                    var valueLabel = new Label($"  {mapping.Value}");
                    valueLabel.style.fontSize = 16;
                    valueLabel.style.color = Color.white;
                    valueLabel.style.width = 30;
                    row.Add(valueLabel);

                    _resultsPanel.Add(row);
                }
            }

            // Word count info
            var infoLabel = new Label($"Database: {_resolver.WordCount} words, {_actionRegistry.Count} actions");
            infoLabel.style.fontSize = 12;
            infoLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            infoLabel.style.marginTop = 16;
            root.Add(infoLabel);
        }

        private static Color GetActionColor(string actionId)
        {
            return actionId switch
            {
                "Water"     => new Color(0.2f, 0.5f, 1f),
                "Fire"      => new Color(1f, 0.4f, 0.1f),
                "Earth"     => new Color(0.6f, 0.4f, 0.2f),
                "Wind"      => new Color(0.7f, 0.9f, 0.9f),
                "Push"      => new Color(0.8f, 0.8f, 0.2f),
                "Slow"      => new Color(0.4f, 0.4f, 0.7f),
                "Burn"      => new Color(1f, 0.5f, 0.2f),
                "Freeze"    => new Color(0.5f, 0.8f, 1f),
                "Curse"     => new Color(0.6f, 0.1f, 0.4f),
                "Buff"      => new Color(0.2f, 0.9f, 0.3f),
                "Heavy"     => new Color(0.5f, 0.5f, 0.4f),
                "Shock"     => new Color(1f, 1f, 0.3f),
                "Light"     => new Color(1f, 1f, 0.7f),
                "Dark"      => new Color(0.5f, 0.2f, 0.8f),
                "Poison"    => new Color(0.3f, 0.8f, 0.2f),
                "Shield"    => new Color(0.6f, 0.7f, 0.8f),
                "Damage"    => new Color(1f, 0.2f, 0.2f),
                "Heal"      => new Color(0.2f, 0.9f, 0.3f),
                "Summon"    => new Color(0.8f, 0.5f, 1f),
                "Time"      => new Color(0.9f, 0.8f, 0.5f),
                _           => Color.gray,
            };
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var word = ResolveParam<string>(overrides, "word")?.Trim().ToLowerInvariant() ?? "";
            var expectedHasWord = _resolver.HasWord(word);

            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("Resolver initialized", _resolver != null,
                    _resolver != null ? null : "Resolver is null"),
                new("Action registry initialized", _actionRegistry != null,
                    _actionRegistry != null ? null : "Action registry is null"),
                new("Results panel created", _resultsPanel != null,
                    _resultsPanel != null ? null : "Results panel is null"),
                new($"Word \"{_resolvedWord}\" resolution consistent",
                    (_lastResult.Count > 0) == expectedHasWord,
                    (_lastResult.Count > 0) == expectedHasWord
                        ? null : $"HasWord={expectedHasWord} but got {_lastResult.Count} actions"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            _resolver = null;
            _actionRegistry = null;
            _resultsPanel = null;
            _lastResult = null;
            _resolvedWord = null;
        }
    }
}
