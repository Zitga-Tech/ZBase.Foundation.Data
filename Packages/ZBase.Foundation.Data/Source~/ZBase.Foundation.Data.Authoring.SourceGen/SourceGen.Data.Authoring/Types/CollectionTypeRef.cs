using Microsoft.CodeAnalysis;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public class CollectionTypeRef
    {
        public CollectionKind Kind { get; set; }

        public ITypeSymbol ElementType { get; set; }

        public ITypeSymbol KeyType { get; set; }
    }
}
