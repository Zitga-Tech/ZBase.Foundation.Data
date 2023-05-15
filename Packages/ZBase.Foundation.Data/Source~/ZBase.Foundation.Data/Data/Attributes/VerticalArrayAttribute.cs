using System;

namespace ZBase.Foundation.Data
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class VerticalArrayAttribute : Attribute { }
}
