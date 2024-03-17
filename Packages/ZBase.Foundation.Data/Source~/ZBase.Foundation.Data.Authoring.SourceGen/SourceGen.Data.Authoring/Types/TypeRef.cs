using Microsoft.CodeAnalysis;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public class TypeRef
    {
        public ITypeSymbol Type { get; set; }

        public CollectionTypeRef CollectionTypeRef { get; } = new();
    }
}
