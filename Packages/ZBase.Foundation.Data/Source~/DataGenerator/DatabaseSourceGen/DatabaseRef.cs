using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public class DatabaseRef
    {
        public ClassDeclarationSyntax Syntax { get; set; }

        public ITypeSymbol Symbol { get; set; }

        public ImmutableArray<Table> Tables { get; set; }

        /// <summary>
        /// TargetTypeFullName -> ContainingTypeFullName -> PropertyName (s)
        /// <br/>
        /// ContainingTypeFullName can be empty if it is not defined.
        /// </summary>
        public Dictionary<string, Dictionary<string, HashSet<string>>> VerticalListMap { get; set; }

        public class Table
        {
            public string TypeFullName { get; set; }

            public string SheetName { get; set; }

            public NamingStrategy NamingStrategy { get; set; }
        }
    }
}
