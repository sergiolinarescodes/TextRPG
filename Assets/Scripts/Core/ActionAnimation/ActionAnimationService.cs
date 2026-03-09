using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Patterns.CommandQueue;
using Unidad.Core.Systems;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionAnimation
{
    internal sealed class ActionAnimationService : SystemServiceBase, IActionAnimationService, ITickable
    {
        private readonly IAnimationResolver _animationResolver;
        private readonly IActionHandlerRegistry _handlerRegistry;
        private readonly IEntityStatsService _entityStats;
        private readonly CommandQueue _commandQueue = new();
        private readonly ActionAnimationCommandContext _commandContext = new();
        private readonly ProjectilePool _projectilePool = new();

        private Func<EntityId, Vector3> _positionProvider;
        private bool _enabled = true;
        private bool _initialized;
        private string _currentWord;
        private bool _deferredInProgress;
        private bool _executionCompletedPublished;

        public bool IsAnimating => !_commandQueue.IsEmpty || _deferredInProgress;

        public ActionAnimationService(IEventBus eventBus, IAnimationResolver animationResolver,
            IActionHandlerRegistry handlerRegistry = null, IEntityStatsService entityStats = null)
            : base(eventBus)
        {
            _animationResolver = animationResolver;
            _handlerRegistry = handlerRegistry;
            _entityStats = entityStats;

            Subscribe<ActionResolvedEvent>(OnActionResolved);
            Subscribe<ActionExecutionCompletedEvent>(OnExecutionCompleted);

            _commandQueue.OnQueueEmpty += OnQueueEmpty;
        }

        public void Initialize(Func<EntityId, Vector3> positionProvider, VisualElement projectileLayer)
        {
            _positionProvider = positionProvider;
            _projectilePool.Initialize(projectileLayer);
            _initialized = true;
        }

        public void SetEnabled(bool enabled) => _enabled = enabled;

        public void Tick(float deltaTime)
        {
            if (!_initialized || _commandQueue.IsEmpty) return;
            _commandQueue.Tick(_commandContext, deltaTime);
        }

        private void OnActionResolved(ActionResolvedEvent e)
        {
            if (e.IsInstant) return;

            _currentWord = e.Word;

            if (!_enabled || !_initialized || _handlerRegistry == null)
            {
                ExecuteFallback(e);
                return;
            }

            _deferredInProgress = true;
            _executionCompletedPublished = false;

            if (e.Actions.Count == 0)
            {
                TryPublishCompletion();
                return;
            }

            Publish(new ActionAnimationStartedEvent(e.Word, e.Actions.Count));

            foreach (var action in e.Actions)
            {
                _handlerRegistry.TryGet(action.ActionId, out var handler);

                var targets = FilterAliveTargets(action.Targets);
                if (targets.Count == 0) continue;

                var command = new Commands.ProjectileAnimationCommand(
                    action.ActionId,
                    action.Source,
                    targets,
                    action.Value,
                    e.Word,
                    _positionProvider,
                    _projectilePool,
                    0.9f,
                    false,
                    handler,
                    EventBus);
                _commandQueue.Enqueue(command);
            }
        }

        private void ExecuteFallback(ActionResolvedEvent e)
        {
            // Suppress OnExecutionCompleted from re-publishing AnimationCompleted
            _deferredInProgress = true;
            _executionCompletedPublished = false;

            if (_handlerRegistry != null)
            {
                foreach (var action in e.Actions)
                {
                    if (_handlerRegistry.TryGet(action.ActionId, out var handler))
                    {
                        var context = new ActionContext(action.Source, action.Targets, action.Value, action.Word);
                        handler.Execute(context);
                        Publish(new ActionHandlerExecutedEvent(action.ActionId, action.Value, action.Source, action.Targets));
                    }
                }
            }

            TryPublishCompletion();
        }

        private void OnExecutionCompleted(ActionExecutionCompletedEvent e)
        {
            // Instant mode only: forward as animation completed
            // Skip if deferred mode is active (OnQueueEmpty or ExecuteFallback handles it)
            if (!_deferredInProgress)
                Publish(new ActionAnimationCompletedEvent(e.Word));
        }

        private void OnQueueEmpty()
        {
            TryPublishCompletion();
        }

        private void TryPublishCompletion()
        {
            if (!_executionCompletedPublished)
            {
                _executionCompletedPublished = true;
                Publish(new ActionExecutionCompletedEvent(_currentWord));
            }

            // A passive may have inserted commands during the event above
            if (!_commandQueue.IsEmpty)
                return;

            _deferredInProgress = false;
            Publish(new ActionAnimationCompletedEvent(_currentWord));
        }

        public void EnqueuePassiveAnimation(EntityId owner, string effectId, int value,
            IReadOnlyList<EntityId> targets, Action onArrival)
        {
            var aliveTargets = FilterAliveTargets(targets);
            if (!_initialized || !_enabled || aliveTargets == null || aliveTargets.Count == 0)
            {
                Debug.Log($"[AnimService] EnqueuePassive FALLBACK effectId={effectId} init={_initialized} enabled={_enabled} targets={aliveTargets?.Count}");
                onArrival?.Invoke();
                return;
            }

            Debug.Log($"[AnimService] EnqueuePassive INSERT effectId={effectId} owner={owner} targets={aliveTargets.Count} queueCount={_commandQueue.Count}");
            var command = new Commands.ProjectileAnimationCommand(
                effectId, owner, aliveTargets, value, effectId,
                _positionProvider, _projectilePool, 0.6f, false,
                handler: null, eventBus: null, onArrival: onArrival);
            _commandQueue.InsertFront(command);
        }

        private IReadOnlyList<EntityId> FilterAliveTargets(IReadOnlyList<EntityId> targets)
        {
            if (_entityStats == null || targets == null) return targets;
            var alive = new List<EntityId>(targets.Count);
            foreach (var t in targets)
            {
                if (_entityStats.HasEntity(t) && _entityStats.GetCurrentHealth(t) > 0)
                    alive.Add(t);
            }
            return alive;
        }

        public override void Dispose()
        {
            _commandQueue.Clear();
            _commandQueue.OnQueueEmpty -= OnQueueEmpty;
            _projectilePool.Dispose();
            base.Dispose();
        }
    }
}
