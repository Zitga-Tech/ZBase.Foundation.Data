using System;

namespace ZBase.Foundation.Data.Authoring
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DatabaseAttribute : Attribute
    {
        public Type[] Converters { get; }

        public DatabaseAttribute(params Type[] converters)
        {
            Converters = converters ?? Array.Empty<Type>();
        }
    }
}
