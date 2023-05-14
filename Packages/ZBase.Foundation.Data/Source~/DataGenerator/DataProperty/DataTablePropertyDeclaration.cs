using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DataSourceGen
{
    public partial class DataTablePropertyDeclaration
    {
        public const string SERIALIZE_FIELD_ATTRIBUTE = "global::UnityEngine.SerializeField";
        public const string DATA_ID_ATTRIBUTE = "global::ZBase.Foundation.Data.DataIdAttribute";
        public const string VERTICAL_LIST_ATTRIBUTE = "global::ZBase.Foundation.Data.VerticalArrayAttribute";
        public const string LIST_TYPE = "global::System.Collections.Generic.List";
        public const string VERTICAL_LIST_TYPE = "global::Cathei.BakingSheet.VerticalList";
        public const string IDATA = "global::ZBase.Foundation.Data.IData";

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
                candidate.Fields = new List<IFieldSymbol>();
                candidate.ElementTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

                var members = candidate.TypeArgument.GetMembers();

                foreach (var member in members)
                {
                    if (member is IFieldSymbol field)
                    {
                        if (field.HasAttribute(SERIALIZE_FIELD_ATTRIBUTE) == false)
                        {
                            continue;
                        }
                        
                        if(field.HasAttribute(DATA_ID_ATTRIBUTE) && field.ToPropertyName() == "Id")
                        {
                            candidate.IdType = field.Type;
                            continue;
                        }

                        if (field.Type is IArrayTypeSymbol arrayType)
                        {
                            var isIData = arrayType.ElementType.GetAllFullyQualifiedInterfaceAndBaseTypeNames()
                                .Any(x => x == IDATA);
                            if (isIData)
                            {
                                candidate.ElementTypes.Add(arrayType.ElementType);
                            }
                        }

                        candidate.Fields.Add(field);
                    }
                }

                if (candidate.Fields.Count > 0)
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

        public ITypeSymbol IdType { get; set; }

        public List<IFieldSymbol> Fields { get; set; }

        public HashSet<ITypeSymbol> ElementTypes { get; set; }
    }

}

    



