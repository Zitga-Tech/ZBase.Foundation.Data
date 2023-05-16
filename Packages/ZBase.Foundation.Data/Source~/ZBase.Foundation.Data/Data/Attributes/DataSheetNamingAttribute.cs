using System;

namespace ZBase.Foundation.Data
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DataSheetNamingAttribute : Attribute
    {
        public string SheetName { get; }

        public NamingStrategy NamingStrategy { get; }

        public DataSheetNamingAttribute(string sheetName)
        {
            this.SheetName = sheetName;
        }

        public DataSheetNamingAttribute(string sheetName, NamingStrategy namingStrategy)
        {
            this.SheetName = sheetName;
            this.NamingStrategy = namingStrategy;
        }
    }

    /// <summary>
    /// The supported formats of sheet and column names.
    /// </summary>
    public enum NamingStrategy
    {
        /// <summary>
        /// To support column names in the format of "ColumnName".
        /// </summary>
        PascalCase = 0,

        /// <summary>
        /// To support column names in the format of "columnName".
        /// </summary>
        CamelCase = 1,

        /// <summary>
        /// To support column names in the format of "column_name".
        /// </summary>
        SnakeCase = 2,

        /// <summary>
        /// To support column names in the format of "column-name".
        /// </summary>
        KebabCase = 3,
    }
}
