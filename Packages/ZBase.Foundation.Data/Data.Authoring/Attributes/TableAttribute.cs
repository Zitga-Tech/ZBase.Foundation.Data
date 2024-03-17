using System;

namespace ZBase.Foundation.Data.Authoring
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class TableAttribute : Attribute
    {
        /// <summary>
        /// Type derived from <see cref="DataTableAsset{TId, TData}"/>
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

        public Type[] Converters { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataTableAssetType">Type derived from <see cref="DataTableAsset{TId, TData}"/></param>
        public TableAttribute(Type dataTableAssetType, params Type[] converters)
            : this(dataTableAssetType, dataTableAssetType.Name, converters)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataTableAssetType">Type derived from <see cref="DataTableAsset{TId, TData}"/></param>
        /// <param name="sheetName">Alternative name of <see cref="DataTableAssetType"/></param>
        public TableAttribute(Type dataTableAssetType, string sheetName, params Type[] converters)
            : this(dataTableAssetType, sheetName, NamingStrategy.PascalCase, converters)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataTableAssetType">Type derived from <see cref="DataTableAsset{TId, TData}"/></param>
        /// <param name="sheetName">Alternative name of <see cref="DataTableAssetType"/></param>
        /// <param name="namingStrategy">How the names of the sheet and its properties are serialized.</param>
        public TableAttribute(Type dataTableAssetType, string sheetName, NamingStrategy namingStrategy, params Type[] converters)
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
            this.Converters = converters ?? Array.Empty<Type>();
        }
    }
}
