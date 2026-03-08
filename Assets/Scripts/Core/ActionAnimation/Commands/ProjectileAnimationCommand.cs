using System;
using System.Collections.Generic;
using PrimeTween;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Patterns.CommandQueue;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionAnimation.Commands
{
    internal sealed class ProjectileAnimationCommand : ICommand
    {
        private readonly string _actionId;
        private readonly EntityId _source;
        private readonly IReadOnlyList<EntityId> _targets;
        private readonly int _value;
        private readonly string _word;
        private readonly Func<EntityId, Vector3> _positionProvider;
        private readonly ProjectilePool _pool;
        private readonly float _duration;
        private readonly bool _isInstant;
        private readonly IActionHandler _handler;
        private readonly IEventBus _eventBus;

        private readonly List<ProjectileEntry> _activeProjectiles = new();
        private int _arrivedCount;
        private int _totalCount;
        private bool _started;
        private bool _handlerExecuted;

        // Arc scale factor: how much the projectile "pops up" (in pixels) to simulate 3D travel
        private const float ArcHeight = 60f;

        public string Id => $"Projectile_{_actionId}_{_word}";

        public ProjectileAnimationCommand(
            string actionId,
            EntityId source,
            IReadOnlyList<EntityId> targets,
            int value,
            string word,
            Func<EntityId, Vector3> positionProvider,
            ProjectilePool pool,
            float duration,
            bool isInstant,
            IActionHandler handler = null,
            IEventBus eventBus = null)
        {
            _actionId = actionId;
            _source = source;
            _targets = targets;
            _value = value;
            _word = word;
            _positionProvider = positionProvider;
            _pool = pool;
            _duration = duration;
            _isInstant = isInstant;
            _handler = handler;
            _eventBus = eventBus;
        }

        public CommandStatus Execute(ICommandContext context, float deltaTime)
        {
            if (!_started)
            {
                _started = true;
                SpawnProjectiles();

                if (_isInstant || _totalCount == 0)
                {
                    ExecuteHandler();
                    return CommandStatus.Completed;
                }
            }

            return _arrivedCount >= _totalCount ? CommandStatus.Completed : CommandStatus.Running;
        }

        public void Cancel()
        {
            foreach (var entry in _activeProjectiles)
            {
                if (entry.CurrentTween.isAlive)
                    entry.CurrentTween.Stop();
                _pool.Return(entry);
            }
            _activeProjectiles.Clear();
        }

        private void SpawnProjectiles()
        {
            if (_targets == null || _targets.Count == 0)
            {
                _totalCount = 0;
                return;
            }

            var sourcePos = _positionProvider(_source);
            var color = ProjectileStyle.GetColor(_actionId);

            _totalCount = _targets.Count;
            _arrivedCount = 0;

            for (int i = 0; i < _targets.Count; i++)
            {
                var target = _targets[i];
                var targetPos = _positionProvider(target);
                var entry = _pool.Acquire(_actionId, color, sourcePos);
                _activeProjectiles.Add(entry);

                if (_isInstant)
                {
                    OnProjectileArrived(entry);
                    continue;
                }

                bool isSelfTarget = _source.Equals(target);
                float stagger = i * 0.03f;

                if (isSelfTarget)
                    AnimateSelfTarget(entry, sourcePos, stagger);
                else
                    AnimateToTarget(entry, sourcePos, targetPos, stagger);
            }
        }

        private void AnimateToTarget(ProjectileEntry entry, Vector3 from, Vector3 to, float delay)
        {
            if (delay > 0f)
            {
                entry.CurrentTween = Tween.Delay(delay).OnComplete(this, cmd =>
                {
                    cmd.AnimateToTargetCore(entry, from, to);
                });
            }
            else
            {
                AnimateToTargetCore(entry, from, to);
            }
        }

        private void AnimateToTargetCore(ProjectileEntry entry, Vector3 from, Vector3 to)
        {
            entry.CurrentTween = Tween.Custom(entry, 0f, 1f, _duration,
                onValueChange: (e, progress) =>
                {
                    if (e.Root == null) return;
                    // Lerp X/Y linearly, add parabolic arc on Y (negative = upward in screen space)
                    var pos = Vector3.Lerp(from, to, progress);
                    float arc = 4f * ArcHeight * progress * (1f - progress);
                    pos.y -= arc;
                    e.Root.style.left = pos.x;
                    e.Root.style.top = pos.y;
                }, Ease.InOutQuad).OnComplete(this, cmd => cmd.OnProjectileArrived(entry));
        }

        private void AnimateSelfTarget(ProjectileEntry entry, Vector3 sourcePos, float delay)
        {
            // Self-target: arc upward (negative Y in screen space) and return
            var midPoint = sourcePos + new Vector3(0, -ArcHeight, 0);
            var halfDuration = _duration * 0.5f;

            if (delay > 0f)
            {
                entry.CurrentTween = Tween.Delay(delay).OnComplete(this, cmd =>
                {
                    cmd.AnimateSelfTargetCore(entry, sourcePos, midPoint, halfDuration);
                });
            }
            else
            {
                AnimateSelfTargetCore(entry, sourcePos, midPoint, halfDuration);
            }
        }

        private void AnimateSelfTargetCore(ProjectileEntry entry, Vector3 sourcePos, Vector3 midPoint, float halfDuration)
        {
            entry.CurrentTween = Tween.Custom(entry, 0f, 1f, halfDuration,
                onValueChange: (e, progress) =>
                {
                    if (e.Root == null) return;
                    var p = Vector3.Lerp(sourcePos, midPoint, progress);
                    e.Root.style.left = p.x;
                    e.Root.style.top = p.y;
                }, Ease.OutQuad).OnComplete(this, cmd =>
            {
                entry.CurrentTween = Tween.Custom(entry, 0f, 1f, halfDuration,
                    onValueChange: (e, progress) =>
                    {
                        if (e.Root == null) return;
                        var p2 = Vector3.Lerp(midPoint, sourcePos, progress);
                        e.Root.style.left = p2.x;
                        e.Root.style.top = p2.y;
                    }, Ease.InQuad).OnComplete(cmd, cmd2 => cmd2.OnProjectileArrived(entry));
            });
        }

        private void OnProjectileArrived(ProjectileEntry entry)
        {
            _activeProjectiles.Remove(entry);
            _pool.Return(entry);
            _arrivedCount++;

            if (_arrivedCount >= _totalCount)
                ExecuteHandler();
        }

        private void ExecuteHandler()
        {
            if (_handlerExecuted) return;
            _handlerExecuted = true;

            if (_handler != null)
            {
                var context = new ActionContext(_source, _targets, _value, _word);
                _handler.Execute(context);
                _eventBus?.Publish(new ActionHandlerExecutedEvent(_actionId, _value, _source, _targets));
            }
        }
    }
}
