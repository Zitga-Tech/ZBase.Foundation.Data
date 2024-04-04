using System;

namespace ZBase.Foundation.Data.Authoring
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DatabaseAttribute : Attribute
    {
        public NamingStrategy NamingStrategy { get; }

        public Type[] Converters { get; }

        public DatabaseAttribute(params Type[] converters)
        {
            Converters = converters ?? Array.Empty<Type>();
        }

        public DatabaseAttribute(NamingStrategy namingStrategy, params Type[] converters)
        {
            NamingStrategy = namingStrategy;
            Converters = converters ?? Array.Empty<Type>();
        }
    }
}
