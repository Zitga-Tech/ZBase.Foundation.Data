using System;

namespace ZBase.Foundation.Data.Authoring.SourceGen
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratedSheetContainerAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratedSheetAttribute : Attribute
    {
        public Type DataTableType { get; }

        public Type IdType { get; }

        public Type DataType { get; }

        public GeneratedSheetAttribute(Type dataTableType, Type idType, Type dataType)
        {
            this.DataTableType = dataTableType;
            this.IdType = idType;
            this.DataType = dataType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratedSheetRowAttribute : Attribute
    {
        public Type IdType { get; }

        public Type DataType { get; }

        public GeneratedSheetRowAttribute(Type idType, Type dataType)
        {
            this.IdType = idType;
            this.DataType = dataType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratedListElementAttribute : Attribute
    {
        public Type DataType { get; }

        public GeneratedListElementAttribute(Type dataType)
        {
            this.DataType = dataType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratedDataTableAssetAttribute : Attribute
    {
        public Type DataTableType { get; }

        public GeneratedDataTableAssetAttribute(Type dataTableType)
        {
            this.DataTableType = dataTableType;
        }
    }
}