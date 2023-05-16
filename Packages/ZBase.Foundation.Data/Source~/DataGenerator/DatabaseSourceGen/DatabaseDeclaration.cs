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

        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DatabaseGenerator\", \"1.0.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

        public ImmutableArray<DataTableAssetRef> DataTableAssetRefs { get; }

        public Dictionary<string, DataDeclaration> DataMap { get; }

        public DatabaseDeclaration(
              ImmutableArray<DataTableAssetRef> candidates
            , Dictionary<string, DataDeclaration> dataMap
        )
        {
            this.DataTableAssetRefs = candidates;
            this.DataMap = dataMap;
        }

        private void WriteCodeSample()
        {
            //var newCompilation = CompilationUnit().NormalizeWhitespace(eol: "\n");
            //var namespaceFileName = declaration.Symbol.ContainingNamespace.ToDisplayString().ToValidIdentifier();
            //var fileName = $"BakingSheets_{namespaceFileName}_{declaration.Symbol.Name}Sheet";

            //OutputSource(
            //      context
            //    , outputSourceGenFiles
            //    , newCompilation
            //    , declaration.WriteSheet()
            //    , newCompilation.SyntaxTree.GetGeneratedSourceFileName(GENERATOR_NAME, fileName, newCompilation)
            //    , newCompilation.SyntaxTree.GetGeneratedSourceFilePath(assemblyName, GENERATOR_NAME)
            //);
        }

        private static void OutputSource(
              SourceProductionContext context
            , bool outputSourceGenFiles
            , SyntaxNode syntax
            , string source
            , string hintName
            , string sourceFilePath
        )
        {
            var outputSource = TypeCreationHelpers.GenerateSourceTextForRootNodes(
                  sourceFilePath
                , syntax
                , source
                , context.CancellationToken
            );

            context.AddSource(hintName, outputSource);

            if (outputSourceGenFiles)
            {
                SourceGenHelpers.OutputSourceToFile(
                      context
                    , syntax.GetLocation()
                    , sourceFilePath
                    , outputSource
                );
            }
        }
    }
}
