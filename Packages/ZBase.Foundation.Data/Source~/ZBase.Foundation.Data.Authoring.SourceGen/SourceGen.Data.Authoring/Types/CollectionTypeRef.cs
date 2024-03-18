﻿using Microsoft.CodeAnalysis;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public class CollectionTypeRef
    {
        public CollectionKind Kind { get; set; }

        public ITypeSymbol ElementType { get; set; }

        public ITypeSymbol KeyType { get; set; }

        public void CopyFrom(CollectionTypeRef source)
        {
            if (source == null) return;

            Kind = source.Kind;
            ElementType = source.ElementType;
            KeyType = source.KeyType;
        }
    }
}
