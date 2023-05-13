using System;

namespace ZBase.Foundation.Data.Authoring.SourceGen
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DataInfoAttribute : Attribute
    {
        public Type Type { get; }

        public DataInfoAttribute(Type type)
        {
            this.Type = type;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DataTableInfoAttribute : Attribute
    {
        public Type Type { get; }

        public DataTableInfoAttribute(Type type)
        {
            this.Type = type;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DataTableAssetInfoAttribute : Attribute
    {
        public Type Type { get; }

        public DataTableAssetInfoAttribute(Type type)
        {
            this.Type = type;
        }
    }
}