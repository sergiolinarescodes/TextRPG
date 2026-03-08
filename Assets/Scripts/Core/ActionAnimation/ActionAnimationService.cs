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
        private readonly CommandQueue _commandQueue = new();
        private readonly ActionAnimationCommandContext _commandContext = new();
        private readonly ProjectilePool _projectilePool = new();

        private Func<EntityId, Vector3> _positionProvider;
        private bool _enabled = true;
        private bool _initialized;
        private string _currentWord;
        private bool _deferredInProgress;

        public bool IsAnimating => !_commandQueue.IsEmpty;

        public ActionAnimationService(IEventBus eventBus, IAnimationResolver animationResolver,
            IActionHandlerRegistry handlerRegistry = null)
            : base(eventBus)
        {
            _animationResolver = animationResolver;
            _handlerRegistry = handlerRegistry;

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

            if (!_enabled || !_initialized || _handlerRegistry == null)
            {
                ExecuteFallback(e);
                return;
            }

            _currentWord = e.Word;
            _deferredInProgress = true;

            if (e.Actions.Count == 0)
            {
                Publish(new ActionExecutionCompletedEvent(e.Word));
                _deferredInProgress = false;
                Publish(new ActionAnimationCompletedEvent(e.Word));
                return;
            }

            Publish(new ActionAnimationStartedEvent(e.Word, e.Actions.Count));

            foreach (var action in e.Actions)
            {
                _handlerRegistry.TryGet(action.ActionId, out var handler);

                var command = new Commands.ProjectileAnimationCommand(
                    action.ActionId,
                    action.Source,
                    action.Targets,
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
            Publish(new ActionExecutionCompletedEvent(e.Word));
            _deferredInProgress = false;
            Publish(new ActionAnimationCompletedEvent(e.Word));
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
            Publish(new ActionExecutionCompletedEvent(_currentWord));
            _deferredInProgress = false;
            Publish(new ActionAnimationCompletedEvent(_currentWord));
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
