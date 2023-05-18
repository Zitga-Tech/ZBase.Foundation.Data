using System;

namespace ZBase.Foundation.Data.Authoring
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class TableAttribute : Attribute
    {
        /// <summary>
        /// Type derived from <see cref="DataTableAsset{TKey, TData}"/>
        /// </summary>
        public Type DataTableAssetType { get; }

        /// <summary>
        /// Alternative name of <see cref="DataTableAssetType"/>
        /// </summary>
        public string SheetName { get; }

        /// <summary>
        /// How the names of the sheet and its properties are serialized.
        /// </summary>
        public NamingStrategy NamingStrategy { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataTableAssetType">Type derived from <see cref="DataTableAsset{TKey, TData}"/></param>
        public TableAttribute(Type dataTableAssetType)
            : this(dataTableAssetType, dataTableAssetType.Name)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataTableAssetType">Type derived from <see cref="DataTableAsset{TKey, TData}"/></param>
        /// <param name="sheetName">Alternative name of <see cref="DataTableAssetType"/></param>
        public TableAttribute(Type dataTableAssetType, string sheetName)
            : this(dataTableAssetType, sheetName, NamingStrategy.PascalCase)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataTableAssetType">Type derived from <see cref="DataTableAsset{TKey, TData}"/></param>
        /// <param name="sheetName">Alternative name of <see cref="DataTableAssetType"/></param>
        /// <param name="namingStrategy">How the names of the sheet and its properties are serialized.</param>
        public TableAttribute(Type dataTableAssetType, string sheetName, NamingStrategy namingStrategy)
        {
            if (typeof(DataTableAsset).IsAssignableFrom(dataTableAssetType) == false)
            {
                throw new InvalidCastException($"{dataTableAssetType} is not derived from {typeof(DataTableAsset)}<TId, TData>");
            }

            if (dataTableAssetType.IsAbstract)
            {
                throw new InvalidOperationException($"{dataTableAssetType} cannot be abstract");
            }

            if (dataTableAssetType.IsGenericType)
            {
                throw new InvalidOperationException($"{dataTableAssetType} cannot be open generic");
            }

            this.SheetName = sheetName;
            this.NamingStrategy = namingStrategy;
        }
    }
}
