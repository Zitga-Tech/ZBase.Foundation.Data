using System;

namespace ZBase.Foundation.Data.Authoring
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class VerticalListAttribute : Attribute
    {
        public Type TargetType { get; }

        public string PropertyName { get; }

        public Type ContainingType { get; }

        public VerticalListAttribute(Type targetType, string propertyName)
            : this(targetType, propertyName, null)
        { }

        public VerticalListAttribute(Type targetType, string propertyName, Type containingType)
        {
            if (typeof(IData).IsAssignableFrom(targetType) == false)
            {
                throw new InvalidCastException($"{targetType} does not implement {typeof(IData)}");
            }

            if (containingType != null && typeof(IData).IsAssignableFrom(containingType) == false)
            {
                throw new InvalidCastException($"{containingType} does not implement {typeof(IData)}");
            }

            this.TargetType = targetType;
            this.PropertyName = propertyName;
            this.ContainingType = containingType;
        }
    }
}
