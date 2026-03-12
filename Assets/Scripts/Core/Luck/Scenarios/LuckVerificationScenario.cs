using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Luck.Scenarios
{
    internal sealed class LuckVerificationScenario : DataDrivenScenario
    {
        private IEventBus _eventBus;
        private IEntityStatsService _entityStats;
        private ILuckService _luckService;

        private readonly EntityId _entityA = new("entity_a");
        private readonly EntityId _entityB = new("entity_b");

        private readonly List<ScenarioVerificationResult.CheckResult> _checks = new();

        public LuckVerificationScenario() : base(new TestScenarioDefinition(
            "luck-verification",
            "Luck Verification",
            "Verifies correctness of luck stat reading, crit damage multiplier, and chance adjustment.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _checks.Clear();
            Setup();

            RunGetLuckGroup();
            RunCritDamageMultiplierGroup();
            RunAdjustChanceGroup();
            RunCritChanceCapGroup();

            BuildUI();
        }

        private void Setup()
        {
            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            _luckService = new LuckService(_entityStats);

            // entityA: luck=5, criticalDamage=80
            _entityStats.RegisterEntity(_entityA, maxHealth: 50, strength: 5, magicPower: 5,
                physicalDefense: 3, magicDefense: 3, luck: 5, criticalDamage: 80);
            // entityB: luck=0, criticalDamage=50 (default)
            _entityStats.RegisterEntity(_entityB, maxHealth: 50, strength: 5, magicPower: 5,
                physicalDefense: 3, magicDefense: 3, luck: 0);
        }

        // ── GetLuck ─────────────────────────────────────────────────

        private void RunGetLuckGroup()
        {
            int luckA = _luckService.GetLuck(_entityA);
            Check("GetLuck: returns correct stat value (luck=5)",
                luckA == 5, $"Got {luckA}(exp 5)");

            int luckB = _luckService.GetLuck(_entityB);
            Check("GetLuck: returns zero luck",
                luckB == 0, $"Got {luckB}(exp 0)");
        }

        // ── CritDamageMultiplier ────────────────────────────────────

        private void RunCritDamageMultiplierGroup()
        {
            // entityA: criticalDamage=80 → 1.0 + 80/100 = 1.8
            float multA = _luckService.GetCritDamageMultiplier(_entityA);
            Check("CritDamageMultiplier: 80 critDmg = 1.8x",
                Math.Abs(multA - 1.8f) < 0.001f, $"Got {multA}(exp 1.8)");

            // entityB: criticalDamage=50 (default) → 1.0 + 50/100 = 1.5
            float multB = _luckService.GetCritDamageMultiplier(_entityB);
            Check("CritDamageMultiplier: 50 critDmg (default) = 1.5x",
                Math.Abs(multB - 1.5f) < 0.001f, $"Got {multB}(exp 1.5)");
        }

        // ── AdjustChance ────────────────────────────────────────────

        private void RunAdjustChanceGroup()
        {
            // entityA: luck=5 → bonus=0.05
            // Positive: 0.50 + 0.05 = 0.55
            float posAdj = _luckService.AdjustChance(0.50f, _entityA, true);
            Check("AdjustChance: positive increases chance",
                Math.Abs(posAdj - 0.55f) < 0.001f, $"Got {posAdj}(exp 0.55)");

            // Negative: 0.50 - 0.05 = 0.45
            float negAdj = _luckService.AdjustChance(0.50f, _entityA, false);
            Check("AdjustChance: negative decreases chance",
                Math.Abs(negAdj - 0.45f) < 0.001f, $"Got {negAdj}(exp 0.45)");

            // Positive cap: 0.98 + 0.05 = capped at 1.0
            float capPos = _luckService.AdjustChance(0.98f, _entityA, true);
            Check("AdjustChance: positive capped at 1.0",
                Math.Abs(capPos - 1.0f) < 0.001f, $"Got {capPos}(exp 1.0)");

            // Negative cap: 0.02 - 0.05 = capped at 0.0
            float capNeg = _luckService.AdjustChance(0.02f, _entityA, false);
            Check("AdjustChance: negative capped at 0.0",
                Math.Abs(capNeg - 0.0f) < 0.001f, $"Got {capNeg}(exp 0.0)");

            // Zero luck: no adjustment
            float zeroAdj = _luckService.AdjustChance(0.50f, _entityB, true);
            Check("AdjustChance: zero luck = no adjustment",
                Math.Abs(zeroAdj - 0.50f) < 0.001f, $"Got {zeroAdj}(exp 0.50)");
        }

        // ── CritChance cap ──────────────────────────────────────────

        private void RunCritChanceCapGroup()
        {
            // entityA: luck=5 → chance = Min(95, 5*3) = 15%
            // entityB: luck=0 → chance = 0% → never crits
            // We can't test Random.Range deterministically, but we can verify
            // the formula by checking luck=0 produces no crits over many rolls
            int crits = 0;
            for (int i = 0; i < 100; i++)
            {
                if (_luckService.RollCritical(_entityB))
                    crits++;
            }
            Check("RollCritical: luck=0 never crits",
                crits == 0, $"Got {crits} crits(exp 0)");
        }

        // ── Helpers ─────────────────────────────────────────────────

        private void Check(string name, bool passed, string detail = null)
        {
            _checks.Add(new ScenarioVerificationResult.CheckResult(name, passed, passed ? null : detail));
            Debug.Log($"[LuckVerification] {(passed ? "PASS" : "FAIL")}: {name}{(passed ? "" : $" - {detail}")}");
        }

        // ── UI ──────────────────────────────────────────────────────

        private void BuildUI()
        {
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            root.style.paddingTop = 20;
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;

            var title = new Label("Luck Verification");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            int passed = 0, failed = 0;
            foreach (var check in _checks)
            {
                var line = new Label($"{(check.Passed ? "PASS" : "FAIL")}: {check.Name}");
                line.style.fontSize = 14;
                line.style.color = check.Passed ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);
                root.Add(line);
                if (check.Passed) passed++; else failed++;

                if (!check.Passed && check.Message != null)
                {
                    var detail = new Label($"    {check.Message}");
                    detail.style.fontSize = 12;
                    detail.style.color = new Color(1f, 0.6f, 0.6f);
                    root.Add(detail);
                }
            }

            var summary = new Label($"\nTotal: {_checks.Count} | Passed: {passed} | Failed: {failed}");
            summary.style.fontSize = 16;
            summary.style.color = failed == 0 ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.5f, 0.2f);
            summary.style.unityFontStyleAndWeight = FontStyle.Bold;
            summary.style.marginTop = 12;
            root.Add(summary);
        }

        // ── Verify & Cleanup ────────────────────────────────────────

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            return new ScenarioVerificationResult(_checks);
        }

        protected override void OnCleanup()
        {
            _eventBus?.ClearAllSubscriptions();
            _luckService = null;
            _entityStats = null;
            _eventBus = null;
        }
    }
}
