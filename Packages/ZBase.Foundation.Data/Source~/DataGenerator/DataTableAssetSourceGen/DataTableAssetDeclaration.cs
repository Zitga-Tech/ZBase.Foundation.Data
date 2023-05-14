using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DataTableAssetSourceGen
{
    public partial class DataTableAssetDeclaration
    {
        public const string GENERATOR_NAME = nameof(DataTableAssetGenerator);
        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DataTableAssetGenerator\", \"1.0.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

        public TypeDeclarationSyntax Syntax { get; }

        public INamedTypeSymbol Symbol { get; }

        public DataTableAssetDeclaration(
              DataTableAssetRef candidate
            , SemanticModel semanticModel
        )
        {
            Syntax = candidate.Syntax;
            Symbol = semanticModel.GetDeclaredSymbol(candidate.Syntax);
        }
    }
}
