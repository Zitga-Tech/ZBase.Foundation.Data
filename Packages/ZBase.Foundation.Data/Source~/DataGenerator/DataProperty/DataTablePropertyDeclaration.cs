using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DataSourceGen
{
    public partial class DataTablePropertyDeclaration
    {
        public ImmutableArray<TypeRef> TypeRefs { get; }

        public DataTablePropertyDeclaration(
               ImmutableArray<TypeRef> candidates
             , Compilation compilation
             , CancellationToken token
         )
        {
            var typeList = new List<TypeRef>();

            foreach (var candidate in candidates)
            {
                var syntaxTree = candidate.Syntax.SyntaxTree;
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                candidate.Symbol = semanticModel.GetDeclaredSymbol(candidate.Syntax, token);
                candidate.Properties = new List<IPropertySymbol>();

                var members = candidate.Symbol.GetMembers();

                foreach (var member in members)
                {
                    if (member is IPropertySymbol property)
                    {
                        candidate.Properties.Add(property);
                    }
                }

                if (candidate.Properties.Count > 0)
                {
                }
                    typeList.Add(candidate);
            }

            using var typeRefArrayBuilder = ImmutableArrayBuilder<TypeRef>.Rent();
            typeRefArrayBuilder.AddRange(typeList);
            TypeRefs = typeRefArrayBuilder.ToImmutable();
        }
    }

    public class TypeRef
    {
        public TypeDeclarationSyntax Syntax { get; set; }

        public ITypeSymbol Symbol { get; set; }

        public ITypeSymbol TypeArgument { get; set; }

        public List<IPropertySymbol> Properties { get; set; }
    }

}

    



