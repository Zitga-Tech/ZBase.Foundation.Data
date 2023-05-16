using System;

namespace ZBase.Foundation.Data
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DatabaseAttribute : Attribute
    {
        public string Namespace { get; }

        public string TypeName { get; }

        public DatabaseAttribute(string @namespace, string typeName)
        {
            this.Namespace = @namespace;
            this.TypeName = typeName;
        }
    }
}
