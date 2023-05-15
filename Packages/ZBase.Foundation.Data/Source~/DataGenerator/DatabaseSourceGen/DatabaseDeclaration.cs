using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public partial class DatabaseDeclaration
    {
        public const string GENERATOR_NAME = nameof(DatabaseGenerator);
        public const string SERIALIZE_FIELD_ATTRIBUTE = "global::UnityEngine.SerializeField";
        public const string VERTICAL_ARRAY_ATTRIBUTE = "global::ZBase.Foundation.Data.VerticalArrayAttribute";
        public const string LIST_TYPE = "global::System.Collections.Generic.List";
        public const string VERTICAL_LIST_TYPE = "global::Cathei.BakingSheet.VerticalList";

        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DatabaseGenerator\", \"1.0.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

        public ImmutableArray<DataTableAssetRef> DataTableRefs { get; }

        public ImmutableArray<DataRef> DataRefs { get; }

        public DatabaseDeclaration(
              ImmutableArray<DataTableAssetRef> dataTableAssetRefs
            , ImmutableArray<TypeDeclarationSyntax> dataRefs
            , Compilation compilation
            , CancellationToken token
        )
        {
            //var typeList = new List<DataTableRef>();

            //foreach (var candidate in dataTableRefs)
            //{
            //    var syntaxTree = candidate.Syntax.SyntaxTree;
            //    var semanticModel = compilation.GetSemanticModel(syntaxTree);

            //    candidate.Symbol = semanticModel.GetDeclaredSymbol(candidate.Syntax, token);
            //    candidate.Fields = new List<IFieldSymbol>();
            //    candidate.ElementTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

            //    var members = candidate.DataType.GetMembers();

            //    foreach (var member in members)
            //    {
            //        if (member is IFieldSymbol field)
            //        {
            //            if (field.HasAttribute(SERIALIZE_FIELD_ATTRIBUTE) == false)
            //            {
            //                continue;
            //            }

            //            if (field.ToPropertyName() == "Id"
            //                && SymbolEqualityComparer.Default.Equals(field.Type, candidate.IdType)
            //            )
            //            {
            //                continue;
            //            }

            //            candidate.Fields.Add(field);

            //            if (field.Type is IArrayTypeSymbol arrayType)
            //            {
            //                var isIData = arrayType.ElementType
            //                    .GetAllFullyQualifiedInterfaceAndBaseTypeNames()
            //                    .Any(x => x == IDATA);

            //                if (isIData)
            //                {
            //                    candidate.ElementTypes.Add(arrayType.ElementType);
            //                }
            //            }
            //        }
            //    }

            //    if (candidate.Fields.Count > 0)
            //    {
            //        typeList.Add(candidate);
            //    }
            //}

            //using var typeRefArrayBuilder = ImmutableArrayBuilder<DataTableRef>.Rent();
            //typeRefArrayBuilder.AddRange(typeList);
            //DataTableRefs = typeRefArrayBuilder.ToImmutable();
        }
    }
}
