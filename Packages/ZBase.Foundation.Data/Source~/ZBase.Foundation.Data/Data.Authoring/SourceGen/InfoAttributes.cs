using System;

namespace ZBase.Foundation.Data.Authoring.SourceGen
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DataInfoAttribute : Attribute
    {
        public Type DataType { get; }

        public DataInfoAttribute(Type dataType)
        {
            this.DataType = dataType;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DataTableInfoAttribute : Attribute
    {
        public Type DataTableType { get; }

        public DataTableInfoAttribute(Type dataTableType)
        {
            this.DataTableType = dataTableType;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DataTableAssetInfoAttribute : Attribute
    {
        public Type DataTableAssetType { get; }

        public DataTableAssetInfoAttribute(Type dataTableAssetType)
        {
            this.DataTableAssetType = dataTableAssetType;
        }
    }
}