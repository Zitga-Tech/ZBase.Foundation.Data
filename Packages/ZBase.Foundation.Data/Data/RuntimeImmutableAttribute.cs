using System;

namespace ZBase.Foundation.Data
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class RuntimeImmutableAttribute : Attribute { }
}
