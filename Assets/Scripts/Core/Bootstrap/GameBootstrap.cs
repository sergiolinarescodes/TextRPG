using System.Collections.Generic;
using Unidad.Core.Bootstrap;

namespace TextRPG.Core.Bootstrap
{
    /// <summary>
    /// Game-specific bootstrap. The single MonoBehaviour in the scene.
    /// Registers all game system installers in dependency order.
    /// </summary>
    public sealed class GameBootstrap : UnidadBootstrap
    {
        protected override void RegisterInstallers(List<ISystemInstaller> installers)
        {
            // Register game systems here in dependency order.
        }
    }
}
