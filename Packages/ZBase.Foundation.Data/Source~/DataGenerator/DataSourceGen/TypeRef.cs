using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ZBase.Foundation.Data.DataSourceGen
{
    public class DataRef
    {
        public TypeDeclarationSyntax Syntax { get; set; }

        public ITypeSymbol Symbol { get; set; }

        public List<IFieldSymbol> Fields { get; set; }
    }
}
