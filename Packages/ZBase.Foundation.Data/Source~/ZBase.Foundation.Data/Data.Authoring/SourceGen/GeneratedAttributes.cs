using System;

namespace ZBase.Foundation.Data.Authoring.SourceGen
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratedSheetContainerAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratedSheetAttribute : Attribute
    {
        public Type IdType { get; }

        public Type DataTableType { get; }

        public GeneratedSheetAttribute(Type idType, Type dataTableType)
        {
            this.IdType = idType;
            this.DataTableType = dataTableType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratedSheetRow : Attribute
    {
        public Type IdType { get; }

        public Type DataType { get; }

        public GeneratedSheetRow(Type idType, Type dataType)
        {
            this.IdType = idType;
            this.DataType = dataType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratedSheetRowArray : Attribute
    {
        public Type IdType { get; }

        public Type DataType { get; }

        public Type ArrayElemType { get; }

        public GeneratedSheetRowArray(Type idType, Type dataType, Type arrayElemType)
        {
            this.IdType = idType;
            this.DataType = dataType;
            this.ArrayElemType = arrayElemType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratedSheetRowElem : Attribute
    {
        public Type DataType { get; }

        public GeneratedSheetRowElem(Type dataType)
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