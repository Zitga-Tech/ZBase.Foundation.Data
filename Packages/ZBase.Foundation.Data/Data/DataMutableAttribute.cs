using System;

namespace ZBase.Foundation.Data
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class DataMutableAttribute : Attribute { }
}
