using System;

namespace ZBase.Foundation.Data.SourceGen
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class GeneratedPropertyFromFieldAttribute : Attribute
    {
        public string FieldName { get; }

        public Type FieldType { get; }

        public GeneratedPropertyFromFieldAttribute(string fieldName, Type fieldType)
        {
            this.FieldName = fieldName;
            this.FieldType = fieldType;
        }
    }
}
