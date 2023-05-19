using System;

namespace ZBase.Foundation.Data.Authoring
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class VerticalListAttribute : Attribute
    {
        /// <summary>
        /// Type implements <see cref="IData"/>
        /// </summary>
        public Type TargetType { get; }

        /// <summary>
        /// Name of a property of <see cref="TargetType"/>
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Type derived from <see cref="DataTableAsset{TId, TData}"/>
        /// </summary>
        public Type DataTableAssetType { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetType">Type implements <see cref="IData"/></param>
        /// <param name="propertyName">Name of a property of <paramref name="targetType"/></param>
        public VerticalListAttribute(Type targetType, string propertyName)
            : this(targetType, propertyName, null)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetType">Type implements <see cref="IData"/></param>
        /// <param name="propertyName">Name of a property of <paramref name="targetType"/></param>
        /// <param name="dataTableAssetType">Type derived from <see cref="DataTableAsset{TId, TData}"/></param>
        public VerticalListAttribute(Type targetType, string propertyName, Type dataTableAssetType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (typeof(IData).IsAssignableFrom(targetType) == false)
            {
                throw new InvalidCastException($"{targetType} does not implement {typeof(IData)}");
            }

            if (targetType.IsAbstract)
            {
                throw new InvalidOperationException($"{targetType} cannot be abstract");
            }

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentException($"Property name `{propertyName}` is invalid", nameof(propertyName));
            }

            if (targetType.GetProperty(propertyName) == null)
            {
                throw new ArgumentException($"Target type {targetType} does not contain any property named `{propertyName}`", nameof(propertyName));
            }

            this.TargetType = targetType;
            this.PropertyName = propertyName;

            if (dataTableAssetType == null)
            {
                return;
            }

            if (typeof(DataTableAsset).IsAssignableFrom(dataTableAssetType) == false)
            {
                throw new InvalidCastException($"{dataTableAssetType} does not implement {typeof(DataTableAsset)}<TId, TData>");
            }

            if (dataTableAssetType.IsAbstract)
            {
                throw new InvalidOperationException($"{dataTableAssetType} cannot be abstract");
            }

            if (dataTableAssetType.IsGenericType)
            {
                throw new InvalidOperationException($"{dataTableAssetType} cannot be open generic");
            }

            this.DataTableAssetType = dataTableAssetType;
        }
    }
}
