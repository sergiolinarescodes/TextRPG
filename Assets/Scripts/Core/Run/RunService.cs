using TextRPG.Core.Encounter;
using TextRPG.Core.EncounterManager;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Equipment;
using TextRPG.Core.EventEncounter;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Run
{
    internal sealed class RunService : SystemServiceBase, IRunService, IReservedWordHandler
    {
        private readonly ILootRewardService _lootRewardService;
        private RunDefinition _currentRun;
        private int _currentNodeIndex;
        private EntityId _player;
        private bool _awaitingLoot;
        private bool _awaitingAdvance;
        private bool _isInCombat;

        public RunDefinition CurrentRun => _currentRun;
        public int CurrentNodeIndex => _currentNodeIndex;
        public RunNode CurrentNode => _currentRun?.Nodes != null && _currentNodeIndex >= 0
            && _currentNodeIndex < _currentRun.Nodes.Length
            ? _currentRun.Nodes[_currentNodeIndex]
            : null;
        public bool IsRunActive => _currentRun != null && !IsRunComplete;
        public bool IsRunComplete => _currentRun != null && _currentNodeIndex >= _currentRun.Nodes.Length;
        public bool IsAwaitingAdvance => _awaitingAdvance;

        public RunService(IEventBus eventBus, ILootRewardService lootRewardService = null) : base(eventBus)
        {
            _lootRewardService = lootRewardService;
            Subscribe<EncounterEndedEvent>(OnEncounterEnded);
            Subscribe<LootRewardSelectedEvent>(OnLootSelected);
            Subscribe<EventEncounterEndedEvent>(OnEventEncounterEnded);
            Subscribe<CombatModeChangedEvent>(OnCombatModeChanged);
        }

        public void StartRun(RunDefinition run, EntityId player)
        {
            _currentRun = run;
            _currentNodeIndex = 0;
            _player = player;
            _awaitingLoot = false;
            _awaitingAdvance = false;
            _isInCombat = false;

            Publish(new RunStartedEvent(run.Id, run.Nodes.Length));

            var node = run.Nodes[0];
            var encounterId = node.CombatEncounter?.Id ?? node.EventEncounter?.Id ?? "unknown";
            Publish(new RunNodeStartedEvent(0, node.NodeType, encounterId));
        }

        public void AdvanceToNextNode()
        {
            _awaitingLoot = false;
            _awaitingAdvance = false;
            _currentNodeIndex++;

            if (_currentNodeIndex >= _currentRun.Nodes.Length)
            {
                Publish(new RunCompletedEvent(_currentRun.Id, true));
                return;
            }

            var node = _currentRun.Nodes[_currentNodeIndex];
            var encounterId = node.CombatEncounter?.Id ?? node.EventEncounter?.Id ?? "unknown";
            Publish(new RunNodeStartedEvent(_currentNodeIndex, node.NodeType, encounterId));
        }

        public bool TryEscape()
        {
            if (!_isInCombat)
                return false;

            // Stub: escape always fails in combat
            Publish(new EscapeAttemptedEvent(false));
            Debug.Log("[RunService] Escape failed!");
            return false;
        }

        public bool TryHandleReservedWord(string word)
        {
            if (_currentRun == null) return false;

            switch (word)
            {
                case "skip":
                case "continue":
                    if (_awaitingAdvance || !_isInCombat)
                    {
                        if (CurrentNode != null)
                            Publish(new RunNodeCompletedEvent(_currentNodeIndex, CurrentNode.NodeType, true));
                        AdvanceToNextNode();
                        return true;
                    }
                    return false;

                case "run":
                case "escape":
                    if (_isInCombat && !_awaitingAdvance)
                    {
                        TryEscape();
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            if (!IsRunActive) return;

            if (evt.Victory)
            {
                // Loot service will offer rewards → we wait for selection
                _awaitingLoot = true;
            }
            else
            {
                // Defeat
                Publish(new RunCompletedEvent(_currentRun.Id, false));
            }
        }

        private void OnLootSelected(LootRewardSelectedEvent evt)
        {
            if (!_awaitingLoot) return;

            // Check if more loot is pending (e.g. level-up rewards)
            if (_lootRewardService != null && _lootRewardService.IsAwaitingSelection)
                return;

            _awaitingLoot = false;

            if (CurrentNode != null)
                Publish(new RunNodeCompletedEvent(_currentNodeIndex, CurrentNode.NodeType, true));
            AdvanceToNextNode();
        }

        private void OnEventEncounterEnded(EventEncounterEndedEvent evt)
        {
            if (!IsRunActive) return;
            _awaitingAdvance = true;

            if (CurrentNode != null)
                Publish(new RunNodeCompletedEvent(_currentNodeIndex, CurrentNode.NodeType, true));
        }

        private void OnCombatModeChanged(CombatModeChangedEvent evt)
        {
            _isInCombat = evt.IsInCombat;
        }
    }
}
