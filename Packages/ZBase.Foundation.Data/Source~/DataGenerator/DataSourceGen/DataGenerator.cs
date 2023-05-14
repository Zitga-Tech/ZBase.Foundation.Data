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
    public class DataGenerator : IIncrementalGenerator
    {
        public const string GENERATOR_NAME = nameof(DataGenerator);
        public const string IDATA = "global::ZBase.Foundation.Data.IData";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var projectPathProvider = SourceGenHelpers.GetSourceGenConfigProvider(context);

            var dataRefProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, token) => GeneratorHelper.IsStructOrClassSyntaxMatch(node, token),
                transform: static (syntaxContext, token) => GetDataRefSemanticMatch(syntaxContext, token)
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

        public static TypeDeclarationSyntax GetDataRefSemanticMatch(
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

            //var isValid = false;

            //foreach (var member in typeSyntax.Members)
            //{
            //    if (member is FieldDeclarationSyntax fieldSyntax
            //        && fieldSyntax.HasAttributeCandidate("UnityEngine", "SerializeField")
            //    )
            //    {
            //        isValid = true;
            //        break;
            //    }

            //    if (member is PropertyDeclarationSyntax propertySyntax)
            //    {
            //        var attributes = propertySyntax.GatherForwardedAttributes(context.SemanticModel, token);

            //        foreach (var attrib in attributes)
            //        {
            //            if (attrib.GetSyntax().IsTypeNameCandidate("UnityEngine", "SerializeField"))
            //            {
            //                isValid = true;
            //                break;
            //            }
            //        }

            //        if (isValid)
            //        {
            //            break;
            //        }
            //    }
            //}

            //if (isValid == false)
            //{
            //    return null;
            //}

            var semanticModel = context.SemanticModel;

            foreach (var baseType in typeSyntax.BaseList.Types)
            {
                var typeInfo = semanticModel.GetTypeInfo(baseType.Type, token);

                if (typeInfo.Type is INamedTypeSymbol typeSymbol
                    && typeSymbol.ToFullName() == IDATA
                )
                {
                    return typeSyntax;
                }

                if (DoesMatchInterface(typeInfo.Type.Interfaces)
                    || DoesMatchInterface(typeInfo.Type.AllInterfaces)
                )
                {
                    return typeSyntax;
                }
            }

            return null;

            static bool DoesMatchInterface(ImmutableArray<INamedTypeSymbol> interfaces)
            {
                foreach (var interfaceSymbol in interfaces)
                {
                    if (interfaceSymbol.ToFullName() == IDATA)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static void GenerateOutput(
              SourceProductionContext context
            , Compilation compilation
            , TypeDeclarationSyntax candidate
            , string projectPath
            , bool outputSourceGenFiles
        )
        {
            if (candidate == null)
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            try
            {
                SourceGenHelpers.ProjectPath = projectPath;

                var syntaxTree = candidate.SyntaxTree;
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var declaration = new DataDeclaration(candidate, semanticModel);

                if (declaration.Fields.Length < 0)
                {
                    return;
                }

                var source = declaration.WriteCode();
                var sourceFilePath = syntaxTree.GetGeneratedSourceFilePath(compilation.Assembly.Name, GENERATOR_NAME);
                var outputSource = TypeCreationHelpers.GenerateSourceTextForRootNodes(
                      sourceFilePath
                    , candidate
                    , source
                    , context.CancellationToken
                );

                context.AddSource(
                      syntaxTree.GetGeneratedSourceFileName(GENERATOR_NAME, candidate, declaration.Symbol.ToValidIdentifier())
                    , outputSource
                );

                if (outputSourceGenFiles)
                {
                    SourceGenHelpers.OutputSourceToFile(
                          context
                        , candidate.GetLocation()
                        , sourceFilePath
                        , outputSource
                    );
                }
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
            = new("SG_DATA_01"
                , "Data Generator Error"
                , "This error indicates a bug in the Data source generators. Error message: '{0}'."
                , "ZBase.Foundation.Data.IData"
                , DiagnosticSeverity.Error
                , isEnabledByDefault: true
                , description: ""
            );

    }
}
