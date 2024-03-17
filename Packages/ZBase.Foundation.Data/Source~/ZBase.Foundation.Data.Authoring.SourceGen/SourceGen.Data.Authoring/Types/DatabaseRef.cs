using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public class DatabaseRef
    {
        public ClassDeclarationSyntax Syntax { get; }

        public ITypeSymbol Symbol { get; }

        public AttributeData Attribute { get; }

        public ImmutableArray<TableRef> Tables { get; private set; }

        /// <summary>
        /// Target Type --map-to--> Source Type --map-to--> Converter Ref
        /// </summary>
        public Dictionary<ITypeSymbol, Dictionary<ITypeSymbol, ConverterRef>> ConverterMapMap { get; }

        /// <summary>
        /// TargetTypeFullName --map-to--> ContainingTypeFullName --map-to--> PropertyName(s)
        /// <br/>
        /// ContainingTypeFullName can be empty if it is not defined.
        /// </summary>
        public Dictionary<string, Dictionary<string, HashSet<string>>> VerticalListMap { get; }

        public DatabaseRef(ClassDeclarationSyntax syntax, ITypeSymbol symbol, AttributeData attribute)
        {
            Syntax = syntax;
            Symbol = symbol;
            Attribute = attribute;
            Tables = ImmutableArray<TableRef>.Empty;
            ConverterMapMap = new(SymbolEqualityComparer.Default);
            VerticalListMap = new();
        }

        public void SetTables(ImmutableArray<TableRef> tables)
        {
            if (tables.IsDefault)
            {
                return;
            }

            Tables = tables;
        }
    }
}
