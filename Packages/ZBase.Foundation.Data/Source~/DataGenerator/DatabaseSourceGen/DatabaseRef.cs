using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public class DatabaseRef
    {
        public ClassDeclarationSyntax Syntax { get; set; }

        public ITypeSymbol Symbol { get; set; }

        public ImmutableArray<string> DataTableAssetTypeNames { get; set; }
    }
}
