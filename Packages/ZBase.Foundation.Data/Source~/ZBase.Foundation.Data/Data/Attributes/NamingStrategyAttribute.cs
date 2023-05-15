using System;

namespace ZBase.Foundation.Data
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class NamingStrategyAttribute : Attribute
    {
        public NamingStrategy NamingStrategy { get; }

        public NamingStrategyAttribute(NamingStrategy namingStrategy)
        {
            this.NamingStrategy = namingStrategy;
        }
    }

    public enum NamingStrategy
    {
        CamelCase,
        SnakeCase,
    }
}
