using System;

namespace ZBase.Foundation.Data
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class DataPropertyAttribute : Attribute
    {
        public Type FieldType { get; }

        public DataPropertyAttribute()
        {
        }

        public DataPropertyAttribute(Type fieldType)
        {
            this.FieldType = fieldType;
        }
    }
}
