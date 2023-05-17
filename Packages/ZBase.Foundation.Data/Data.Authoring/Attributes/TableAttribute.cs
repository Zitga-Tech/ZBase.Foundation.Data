using System;

namespace ZBase.Foundation.Data.Authoring
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class TableAttribute : Attribute
    {
        public Type TableType { get; }

        public TableAttribute(Type tableType)
        {
            if (typeof(DataTableAsset).IsAssignableFrom(tableType) == false)
            {
                throw new InvalidCastException($"{tableType} is not derived from {typeof(DataTableAsset)}");
            }

            this.TableType = tableType;
        }
    }
}
