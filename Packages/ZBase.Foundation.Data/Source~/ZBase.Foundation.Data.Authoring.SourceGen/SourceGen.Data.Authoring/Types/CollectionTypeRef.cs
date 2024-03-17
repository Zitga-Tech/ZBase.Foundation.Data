using Microsoft.CodeAnalysis;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public class CollectionTypeRef
    {
        public CollectionKind CollectionKind { get; set; }

        public ITypeSymbol CollectionElementType { get; set; }

        public ITypeSymbol CollectionKeyType { get; set; }
    }
}
