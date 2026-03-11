using UnityEngine;

namespace TextRPG.Core.UnitRendering
{
    public interface IGameMessageService
    {
        void Spawn(Vector2 sourcePos, string message, Color color);
    }
}
