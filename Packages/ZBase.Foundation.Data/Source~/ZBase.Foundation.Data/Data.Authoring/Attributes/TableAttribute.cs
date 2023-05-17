using System;

namespace ZBase.Foundation.Data.Authoring
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class TableAttribute : Attribute
    {
        public Type TableType { get; }

        public string SheetName { get; }

        public NamingStrategy NamingStrategy { get; }

        public TableAttribute(Type tableType) : this(tableType, tableType.Name)
        { }

        public TableAttribute(Type tableType, string sheetName) : this(tableType, sheetName, NamingStrategy.PascalCase)
        { }

        public TableAttribute(Type tableType, string sheetName, NamingStrategy namingStrategy)
        {
            if (typeof(DataTableAsset).IsAssignableFrom(tableType) == false)
            {
                throw new InvalidCastException($"{tableType} is not derived from {typeof(DataTableAsset)}");
            }

            this.SheetName = sheetName;
            this.NamingStrategy = namingStrategy;
        }
    }
}
