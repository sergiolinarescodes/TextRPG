using System;
using System.Collections.Generic;
using TextRPG.Core.WordAction;
using Unidad.Core.Grid;
using UnityEngine;

namespace TextRPG.Core.ActionExecution
{
    internal sealed class TargetingPreviewService : ITargetingPreviewService
    {
        private readonly IWordResolver _wordResolver;
        private readonly ICombatContext _combatContext;

        public TargetingPreviewService(IWordResolver wordResolver, ICombatContext combatContext)
        {
            _wordResolver = wordResolver;
            _combatContext = combatContext;
        }

        public TargetingPreview PreviewWord(string word)
        {
            if (!_wordResolver.HasWord(word))
            {
                Debug.Log($"[Preview:Service] HasWord(\"{word}\")=false (resolver has {_wordResolver.WordCount} words)");
                return new TargetingPreview(Array.Empty<ActionTargetPreview>());
            }

            var actions = _wordResolver.Resolve(word);
            var meta = _wordResolver.GetStats(word);
            Debug.Log($"[Preview:Service] \"{word}\" → actions={actions.Count} meta.Target={meta.Target} meta.Range={meta.Range} meta.Area={meta.Area}");
            var previews = new List<ActionTargetPreview>(actions.Count);
            var grid = _combatContext.CombatGrid;

            for (int i = 0; i < actions.Count; i++)
            {
                var mapping = actions[i];
                var actionTarget = mapping.Target ?? meta.Target;
                var actionRange = mapping.Range ?? meta.Range;
                var actionArea = mapping.Area ?? meta.Area;

                var targetType = TargetTypeClassifier.ParseTargetType(actionTarget);
                Debug.Log($"[Preview:Service]   [{i}] {mapping.ActionId}({mapping.Value}) target=\"{actionTarget}\"→{targetType} range={actionRange} area={actionArea}");
                var primaryTargets = _combatContext.GetTargets(targetType, 0);
                Debug.Log($"[Preview:Service]   [{i}] GetTargets → {primaryTargets.Count} entities");
                var targets = _combatContext.ExpandArea(primaryTargets, actionArea);
                Debug.Log($"[Preview:Service]   [{i}] ExpandArea({actionArea}) → {targets.Count} entities");

                var positions = new List<GridPosition>(targets.Count);
                if (mapping.ActionId == "Move" && grid != null)
                {
                    var sourcePos = grid.GetPosition(_combatContext.SourceEntity);
                    int moveRange = mapping.Value;
                    for (int dy = -moveRange; dy <= moveRange; dy++)
                    {
                        for (int dx = -moveRange; dx <= moveRange; dx++)
                        {
                            if (Math.Abs(dx) + Math.Abs(dy) > moveRange) continue;
                            var candidate = new GridPosition(sourcePos.X + dx, sourcePos.Y + dy);
                            if (!grid.Grid.IsInBounds(candidate)) continue;
                            if (grid.CanMoveTo(candidate) || candidate.Equals(sourcePos))
                                positions.Add(candidate);
                        }
                    }
                    Debug.Log($"[Preview:Service]   [{i}] Move range={moveRange} from ({sourcePos.X},{sourcePos.Y}) → {positions.Count} reachable cells");
                }
                else if (grid != null)
                {
                    for (int j = 0; j < targets.Count; j++)
                    {
                        try
                        {
                            var pos = grid.GetPosition(targets[j]);
                            positions.Add(pos);
                            Debug.Log($"[Preview:Service]   [{i}] entity={targets[j].Value} → ({pos.X},{pos.Y})");
                        }
                        catch (KeyNotFoundException)
                        {
                            Debug.Log($"[Preview:Service]   [{i}] entity={targets[j].Value} → NOT ON GRID (KeyNotFound)");
                        }
                    }
                }
                else
                {
                    Debug.Log($"[Preview:Service]   [{i}] grid is null — no positions");
                }

                previews.Add(new ActionTargetPreview(mapping.ActionId, positions));
            }

            return new TargetingPreview(previews);
        }
    }
}
