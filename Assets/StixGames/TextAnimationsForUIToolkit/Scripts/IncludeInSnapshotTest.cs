using System;

namespace TextAnimationsForUIToolkit
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IncludeInSnapshotTestAttribute : Attribute { }
}
