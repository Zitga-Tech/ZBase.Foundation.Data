using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public class DataTableAssetRef
    {
        public ITypeSymbol Symbol { get; set; }

        public ITypeSymbol IdType { get; set; }

        public ITypeSymbol DataType { get; set; }

        public ImmutableArray<ITypeSymbol> NestedDataTypes { get; set; }
    }
}
