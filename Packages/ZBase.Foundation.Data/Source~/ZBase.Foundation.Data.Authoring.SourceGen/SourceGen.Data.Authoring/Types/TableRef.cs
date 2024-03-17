using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public class TableRef
    {
        public ITypeSymbol Type { get; set; }

        public INamedTypeSymbol BaseType { get; set; }

        public string SheetName { get; set; }

        public NamingStrategy NamingStrategy { get; set; }

        /// <summary>
        /// Target Type --map-to--> Source Type --map-to--> Converter Ref
        /// </summary>
        public Dictionary<ITypeSymbol, Dictionary<ITypeSymbol, ConverterRef>> ConverterMapMap { get; } = new(SymbolEqualityComparer.Default);
    }
}
