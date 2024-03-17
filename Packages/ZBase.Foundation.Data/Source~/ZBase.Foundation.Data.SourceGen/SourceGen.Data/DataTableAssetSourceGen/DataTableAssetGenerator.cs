using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZBase.Foundation.SourceGen;
using static ZBase.Foundation.Data.DataSourceGen.Helpers;

namespace ZBase.Foundation.Data.DataSourceGen
{
    [Generator]
    public class DataTableAssetGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var projectPathProvider = SourceGenHelpers.GetSourceGenConfigProvider(context);

            var dataRefProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, token) => GeneratorHelper.IsClassSyntaxMatch(node, token),
                transform: static (syntaxContext, token) => GetSemanticMatch(syntaxContext, token)
            ).Where(static t => t is { });

            var combined = dataRefProvider
                .Combine(context.CompilationProvider)
                .Combine(projectPathProvider);

            context.RegisterSourceOutput(combined, static (sourceProductionContext, source) => {
                GenerateOutput(
                    sourceProductionContext
                    , source.Left.Right
                    , source.Left.Left
                    , source.Right.projectPath
                    , source.Right.outputSourceGenFiles
                );
            });
        }

        public static DataTableAssetRef GetSemanticMatch(
              GeneratorSyntaxContext context
            , CancellationToken token
        )
        {
            if (context.SemanticModel.Compilation.IsValidCompilation() == false
                || context.Node is not ClassDeclarationSyntax classSyntax
                || classSyntax.BaseList == null
            )
            {
                return null;
            }

            if (classSyntax.TypeParameterList is TypeParameterListSyntax typeParamList
                && typeParamList.Parameters.Count > 0
            )
            {
                return null;
            }

            var semanticModel = context.SemanticModel;
            var symbol = semanticModel.GetDeclaredSymbol(classSyntax, token);

            if (symbol.IsAbstract)
            {
                return null;
            }

            var baseType = symbol.BaseType;

            while (baseType != null)
            {
                if (baseType.ToFullName().StartsWith(DATA_TABLE_ASSET_T) && baseType.TypeArguments.Length == 2)
                {
                    return new DataTableAssetRef {
                        Syntax = classSyntax,
                        Symbol = symbol,
                        IdType = baseType.TypeArguments[0],
                        DataType = baseType.TypeArguments[1],
                    };
                }

                baseType = baseType.BaseType;
            }

            return null;
        }

        private static void GenerateOutput(
              SourceProductionContext context
            , Compilation compilation
            , DataTableAssetRef candidate
            , string projectPath
            , bool outputSourceGenFiles
        )
        {
            if (candidate == null)
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            var syntax = candidate.Syntax;

            try
            {
                SourceGenHelpers.ProjectPath = projectPath;

                var syntaxTree = syntax.SyntaxTree;
                var declaration = new DataTableAssetDeclaration(candidate);

                if (declaration.GetIdMethodIsImplemented)
                {
                    return;
                }

                var source = declaration.WriteCode();
                var sourceFilePath = syntaxTree.GetGeneratedSourceFilePath(compilation.Assembly.Name, DATA_TABLE_ASSET_GENERATOR_NAME);
                var outputSource = TypeCreationHelpers.GenerateSourceTextForRootNodes(
                      sourceFilePath
                    , syntax
                    , source
                    , context.CancellationToken
                );

                context.AddSource(
                      syntaxTree.GetGeneratedSourceFileName(DATA_TABLE_ASSET_GENERATOR_NAME, syntax, declaration.TypeRef.Symbol.ToValidIdentifier())
                    , outputSource
                );

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
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                      s_errorDescriptor
                    , syntax.GetLocation()
                    , e.ToUnityPrintableString()
                ));
            }
        }

        private static readonly DiagnosticDescriptor s_errorDescriptor
            = new("SG_DATA_TABLE_ASSET_01"
                , "Data Table Asset Generator Error"
                , "This error indicates a bug in the Data Table Asset source generators. Error message: '{0}'."
                , "ZBase.Foundation.Data.DataTableAsset<TId, TData>"
                , DiagnosticSeverity.Error
                , isEnabledByDefault: true
                , description: ""
            );

    }
}
