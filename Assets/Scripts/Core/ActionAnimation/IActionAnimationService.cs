using System;
using TextRPG.Core.EntityStats;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionAnimation
{
    public interface IActionAnimationService
    {
        bool IsAnimating { get; }
        void Initialize(Func<EntityId, Vector3> positionProvider, VisualElement projectileLayer);
        void SetEnabled(bool enabled);
    }
}
