using System;
using JetBrains.Annotations;

namespace TextRPG.Core.Services
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AutoScanAttribute : Attribute { }
}
