using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DataSourceGen
{
    [Generator]
    public class DataTableGenerator: IIncrementalGenerator
    {
        public const string IDATA_TABLE_T = "global::ZBase.Foundation.Data.IDataTable<";
       

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var projectPathProvider = SourceGenHelpers.GetSourceGenConfigProvider(context);

            var candidateProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, token) => GeneratorHelper.IsStructOrClassSyntaxMatch(node, token),
                transform: static (syntaxContext, token) => GettypeSemanticMatch(syntaxContext, token)
                ).Where(static t => t != null);

            var combined = candidateProvider.Collect()
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

        public static TypeRef GettypeSemanticMatch(
             GeneratorSyntaxContext context
           , CancellationToken token
       )
        {
            if (context.SemanticModel.Compilation.IsValidCompilation() == false
                || context.Node is not TypeDeclarationSyntax typeSyntax
                || typeSyntax.Kind() is not (SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration)
                || typeSyntax.BaseList == null
            )
            {
                return null;
            }

            var semanticModel = context.SemanticModel;

            foreach (var baseType in typeSyntax.BaseList.Types)
            {
                var typeInfo = semanticModel.GetTypeInfo(baseType.Type, token);

                if (typeInfo.Type is INamedTypeSymbol typeSymbol)
                {
                    if (typeSymbol.IsGenericType
                       && typeSymbol.TypeParameters.Length == 1
                       && typeSymbol.ToFullName().StartsWith(IDATA_TABLE_T)
                    )
                    {
                        return new TypeRef {
                            Syntax = typeSyntax,
                            TypeArgument = typeSymbol.TypeArguments[0],
                        };
                    }
                }

                if (TryGetMatchTypeArgument(typeInfo.Type.Interfaces, out var type)
                    || TryGetMatchTypeArgument(typeInfo.Type.AllInterfaces, out type)
                )
                {
                    return new TypeRef {
                        Syntax = typeSyntax,
                        TypeArgument = type,
                    };
                }
            }

            return null;

            static bool TryGetMatchTypeArgument(
                  ImmutableArray<INamedTypeSymbol> interfaces
                , out ITypeSymbol result
            )
            {
                foreach (var interfaceSymbol in interfaces)
                {
                    if (interfaceSymbol.IsGenericType
                        && interfaceSymbol.TypeParameters.Length == 1
                        && interfaceSymbol.ToFullName().StartsWith(IDATA_TABLE_T)
                    )
                    {
                        result = interfaceSymbol.TypeArguments[0];
                        return true;
                    }
                }

                result = default;
                return false;
            }
        }

        private static void GenerateOutput(
             SourceProductionContext context
           , Compilation compilation
           , ImmutableArray<TypeRef> candidates
           , string projectPath
           , bool outputSourceGenFiles
       )
        {
            if (candidates.Length < 1)
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            try
            {
                SourceGenHelpers.ProjectPath = projectPath;

                var declaration = new DataTablePropertyDeclaration(candidates, compilation, context.CancellationToken);

                declaration.GenerateCodeForSheets(
                      context
                    , compilation
                    , outputSourceGenFiles
                    , s_errorDescriptor
                );

                declaration.GenerateCodeForContainer(
                      context
                    , compilation
                    , outputSourceGenFiles
                    , s_errorDescriptor
                );

               
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                      s_errorDescriptor
                    , null
                    , e.ToUnityPrintableString()
                ));
            }
        }

        private static readonly DiagnosticDescriptor s_errorDescriptor
            = new("SG_GENERIC_UNIONS_01"
                , "Generic Union Generator Error"
                , "This error indicates a bug in the Generic Union source generators. Error message: '{0}'."
                , "ZBase.Foundation.Mvvm.Unions.IUnion<T>"
                , DiagnosticSeverity.Error
                , isEnabledByDefault: true
                , description: ""
            );

    }
}
