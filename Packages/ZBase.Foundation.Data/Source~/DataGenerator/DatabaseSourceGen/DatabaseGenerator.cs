using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    [Generator]
    public class DatabaseGenerator : IIncrementalGenerator
    {
        public const string IDATA = "global::ZBase.Foundation.Data.IData";
        public const string DATA_TABLE_ASSET_T = "global::ZBase.Foundation.Data.DataTableAsset<";
        public const string DATA_SHEET_NAMING_ATTRIBUTE = "global::ZBase.Foundation.Data.DataSheetNamingAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var projectPathProvider = SourceGenHelpers.GetSourceGenConfigProvider(context);

            var dataTableAssetRefProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, token) => GeneratorHelper.IsClassSyntaxMatch(node, token),
                transform: static (syntaxContext, token) => GetDataTableAssetRefSemanticMatch(syntaxContext, token)
            ).Where(static t => t != null);

            var dataRefProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, token) => GeneratorHelper.IsStructOrClassSyntaxMatch(node, token),
                transform: static (syntaxContext, token) => GetDataRefSemanticMatch(syntaxContext, token)
            ).Where(static t => t != null);

            var combined = dataTableAssetRefProvider.Collect()
                .Combine(dataRefProvider.Collect())
                .Combine(context.CompilationProvider)
                .Combine(projectPathProvider);

            context.RegisterSourceOutput(combined, static (sourceProductionContext, source) => {
                GenerateOutput(
                    sourceProductionContext
                    , source.Left.Right
                    , source.Left.Left.Left
                    , source.Left.Left.Right
                    , source.Right.projectPath
                    , source.Right.outputSourceGenFiles
                );
            });
        }

        public static DataTableAssetRef GetDataTableAssetRefSemanticMatch(
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

            var semanticModel = context.SemanticModel;
            var symbol = semanticModel.GetDeclaredSymbol(classSyntax);

            if (symbol.IsAbstract)
            {
                return null;
            }

            foreach (var baseType in classSyntax.BaseList.Types)
            {
                var typeInfo = semanticModel.GetTypeInfo(baseType.Type, token);

                if (typeInfo.Type is INamedTypeSymbol typeSymbol)
                {
                    if (typeSymbol.IsGenericType
                        && typeSymbol.TypeArguments.Length == 2
                        && typeSymbol.ToFullName().StartsWith(DATA_TABLE_ASSET_T))
                    {
                        return new DataTableAssetRef {
                            Syntax = classSyntax,
                            Symbol = symbol,
                            IdType = typeSymbol.TypeArguments[0],
                            DataType = typeSymbol.TypeArguments[1],
                            NamingAttribute = symbol.GetAttribute(DATA_SHEET_NAMING_ATTRIBUTE),
                        };
                    }
                }
            }

            return null;
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
            , ImmutableArray<DataTableAssetRef> candidates
            , ImmutableArray<TypeDeclarationSyntax> dataDeclarations
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

                var token = context.CancellationToken;
                var dataMap = BuildDataMap(compilation, dataDeclarations, token);
                var databaseDeclaration = new DatabaseDeclaration(candidates, dataMap);
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

        private static Dictionary<string, DataDeclaration> BuildDataMap(
              Compilation compilation
            , ImmutableArray<TypeDeclarationSyntax> dataDeclaration
            , CancellationToken token
        )
        {
            var map = new Dictionary<string, DataDeclaration>();

            foreach (var dataRef in dataDeclaration)
            {
                var syntaxTree = dataRef.SyntaxTree;
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var symbol = semanticModel.GetDeclaredSymbol(dataRef, token);
                var name = symbol.ToFullName();

                if (map.ContainsKey(name) == false)
                {
                    map[name] = new DataDeclaration(dataRef, symbol);
                }
            }

            return map;
        }

        private static readonly DiagnosticDescriptor s_errorDescriptor
            = new("SG_DATABASE_01"
                , "Database Generator Error"
                , "This error indicates a bug in the Database source generators. Error message: '{0}'."
                , "ZBase.Foundation.Data.DatabaseAsset"
                , DiagnosticSeverity.Error
                , isEnabledByDefault: true
                , description: ""
            );

    }
}
