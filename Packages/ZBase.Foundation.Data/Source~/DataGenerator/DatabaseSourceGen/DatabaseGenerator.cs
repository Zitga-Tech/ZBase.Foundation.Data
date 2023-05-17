using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
        public const string GENERATOR_NAME = nameof(DatabaseGenerator);
        public const string IDATA = "global::ZBase.Foundation.Data.IData";
        public const string DATA_TABLE_ASSET_T = "global::ZBase.Foundation.Data.DataTableAsset<";
        public const string DATABASE_ATTRIBUTE = "global::ZBase.Foundation.Data.Authoring.DatabaseAttribute";
        public const string TABLE_ATTRIBUTE = "global::ZBase.Foundation.Data.Authoring.TableAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var projectPathProvider = SourceGenHelpers.GetSourceGenConfigProvider(context);

            var databaseRefProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: IsValidDatabaseSyntax,
                transform: GetDatabaseRefSemanticMatch
            ).Where(static t => t is { });

            var dataTableAssetRefProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: GeneratorHelper.IsClassSyntaxMatch,
                transform: GetDataTableAssetRefSemanticMatch
            ).Where(static t => t is { });

            var dataRefProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: GeneratorHelper.IsStructOrClassSyntaxMatch,
                transform: GetDataRefSemanticMatch
            ).Where(static t => t is { });

            var combined = databaseRefProvider
                .Combine(dataTableAssetRefProvider.Collect())
                .Combine(dataRefProvider.Collect())
                .Combine(context.CompilationProvider)
                .Combine(projectPathProvider);

            context.RegisterSourceOutput(combined, static (sourceProductionContext, source) => {
                GenerateOutput(
                    sourceProductionContext
                    , source.Left.Right
                    , source.Left.Left.Left.Left
                    , source.Left.Left.Left.Right
                    , source.Left.Left.Right
                    , source.Right.projectPath
                    , source.Right.outputSourceGenFiles
                );
            });
        }

        private static bool IsValidDatabaseSyntax(SyntaxNode node, CancellationToken token)
        {
            return node is ClassDeclarationSyntax classSyntax
                && classSyntax.AttributeLists.Count > 0;
        }

        public static DatabaseRef GetDatabaseRefSemanticMatch(
              GeneratorSyntaxContext context
            , CancellationToken token
        )
        {
            if (context.SemanticModel.Compilation.IsValidCompilation() == false
                || context.Node is not ClassDeclarationSyntax classSyntax
                || classSyntax.AttributeLists.Count < 1
            )
            {
                return null;
            }

            var semanticModel = context.SemanticModel;
            var symbol = semanticModel.GetDeclaredSymbol(classSyntax);
            
            if (symbol.HasAttribute(DATABASE_ATTRIBUTE) && symbol.HasAttribute(TABLE_ATTRIBUTE))
            {
                return new DatabaseRef {
                    Syntax = classSyntax,
                    Symbol = symbol,
                };
            }

            return null;
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
                || typeSyntax.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)
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
            , DatabaseRef candidate
            , ImmutableArray<DataTableAssetRef> dataTableAssetRefs
            , ImmutableArray<TypeDeclarationSyntax> dataDeclarations
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

                var declaration = new DatabaseDeclaration(candidate);

                if (declaration.DatabaseRef.Tables.Length < 1)
                {
                    return;
                }

                var token = context.CancellationToken;
                var dataMap = BuildDataMap(compilation, dataDeclarations, token);
                var dataTableAssetRefMap = BuildDataTableAssetRefMap(dataTableAssetRefs, dataMap);

                var syntaxTree = candidate.Syntax.SyntaxTree;
                var assemblyName = compilation.Assembly.Name;
                var databaseIdentifier = candidate.Symbol.ToValidIdentifier();

                var databaseHintName = syntaxTree.GetGeneratedSourceFileName(
                      GENERATOR_NAME
                    , candidate.Syntax
                    , databaseIdentifier
                );

                var databaseSourceFilePath = syntaxTree.GetGeneratedSourceFilePath(
                      assemblyName
                    , GENERATOR_NAME
                );

                OutputSource(
                      context
                    , outputSourceGenFiles
                    , declaration.DatabaseRef.Syntax
                    , declaration.WriteContainer(dataTableAssetRefMap)
                    , databaseHintName
                    , databaseSourceFilePath
                );

                var tables = declaration.DatabaseRef.Tables;

                foreach (var table in tables)
                {
                    if (dataTableAssetRefMap.TryGetValue(table.FullTypeName, out var dataTableAssetRef) == false)
                    {
                        continue;
                    }

                    var sheetHintName = GetHintName(
                          syntaxTree
                        , GENERATOR_NAME
                        , candidate.Syntax
                        , $"{databaseIdentifier}_{dataTableAssetRef.DataType.Name}Sheet"
                    );

                    var sheetSourceFilePath = GetSourceFilePath(
                          sheetHintName
                        , assemblyName
                    );

                    OutputSource(
                          context
                        , outputSourceGenFiles
                        , declaration.DatabaseRef.Syntax
                        , declaration.WriteSheet(table, dataTableAssetRef, dataMap)
                        , sheetHintName
                        , sheetSourceFilePath
                    );
                }
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                      s_errorDescriptor
                    , candidate.Syntax.GetLocation()
                    , e.ToUnityPrintableString()
                ));
            }

            static string GetHintName(
                  SyntaxTree syntaxTree
                , string generatorName
                , SyntaxNode node
                , string typeName
            )
            {
                var (isSuccess, fileName) = syntaxTree.TryGetFileNameWithoutExtension();
                var stableHashCode = SourceGenHelpers.GetStableHashCode(syntaxTree.FilePath) & 0x7fffffff;

                var postfix = generatorName.Length > 0 ? $"__{generatorName}" : string.Empty;

                if (string.IsNullOrWhiteSpace(typeName) == false)
                {
                    postfix = $"__{typeName}{postfix}";
                }

                fileName = $"{fileName}_";

                if (isSuccess)
                {
                    var salting = node.GetLocation().GetLineSpan().StartLinePosition.Line;
                    fileName = $"{fileName}{postfix}_{stableHashCode}{salting}.g.cs";
                }
                else
                {
                    fileName = Path.Combine($"{Path.GetRandomFileName()}{postfix}", ".g.cs");
                }

                return fileName;
            }

            static string GetSourceFilePath(string fileName, string assemblyName)
            {
                if (SourceGenHelpers.CanWriteToProjectPath)
                {
                    var saveToDirectory = $"{SourceGenHelpers.ProjectPath}/Temp/GeneratedCode/{assemblyName}/";
                    Directory.CreateDirectory(saveToDirectory);
                    return saveToDirectory + fileName;
                }

                return $"Temp/GeneratedCode/{assemblyName}";
            }
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

        private static Dictionary<string, DataDeclaration> BuildDataMap(
              Compilation compilation
            , ImmutableArray<TypeDeclarationSyntax> dataDeclarations
            , CancellationToken token
        )
        {
            var map = new Dictionary<string, DataDeclaration>();

            foreach (var declaration in dataDeclarations)
            {
                var syntaxTree = declaration.SyntaxTree;
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var symbol = semanticModel.GetDeclaredSymbol(declaration, token);
                var name = symbol.ToFullName();

                if (map.ContainsKey(name) == false)
                {
                    map[name] = new DataDeclaration(declaration, symbol);
                }
            }

            return map;
        }

        private static Dictionary<string, DataTableAssetRef> BuildDataTableAssetRefMap(
              ImmutableArray<DataTableAssetRef> dataTableAssetRefs
            , Dictionary<string, DataDeclaration> dataMap
        )
        {
            var map = new Dictionary<string, DataTableAssetRef>();
            var uniqueTypeNames = new HashSet<string>();
            var typeQueue = new Queue<DataDeclaration>();

            foreach (var dataTableAssetRef in dataTableAssetRefs)
            {
                var typeName = dataTableAssetRef.Symbol.ToFullName();

                if (map.ContainsKey(typeName) == false)
                {
                    InitializeDataTableAssetRef(dataTableAssetRef, dataMap, uniqueTypeNames, typeQueue);
                    map[typeName] = dataTableAssetRef;
                }

                uniqueTypeNames.Clear();
                typeQueue.Clear();
            }

            return map;
        }

        private static void InitializeDataTableAssetRef(
              DataTableAssetRef dataTableAssetRef
            , Dictionary<string, DataDeclaration> dataMap
            , HashSet<string> uniqueTypeNames
            , Queue<DataDeclaration> typeQueue
        )
        {
            var idTypeFullName = dataTableAssetRef.IdType.ToFullName();
            var dataTypeFullName = dataTableAssetRef.DataType.ToFullName();

            if (dataMap.TryGetValue(idTypeFullName, out var idDeclaration))
            {
                typeQueue.Enqueue(idDeclaration);
                uniqueTypeNames.Add(idTypeFullName);
            }

            if (dataMap.TryGetValue(dataTypeFullName, out var dataDeclaration))
            {
                typeQueue.Enqueue(dataDeclaration);
                uniqueTypeNames.Add(dataTypeFullName);
            }

            while (typeQueue.Count > 0)
            {
                var declaration = typeQueue.Dequeue();

                foreach (var field in declaration.Fields)
                {
                    var fieldTypeFullName = field.IsArray
                            ? field.ArrayElementType.ToFullName()
                            : field.Type.ToFullName();

                    if (uniqueTypeNames.Contains(fieldTypeFullName))
                    {
                        continue;
                    }

                    if (dataMap.TryGetValue(fieldTypeFullName, out var fieldTypeDeclaration))
                    {
                        typeQueue.Enqueue(fieldTypeDeclaration);
                        uniqueTypeNames.Add(fieldTypeFullName);
                    }
                }
            }

            uniqueTypeNames.Remove(idTypeFullName);
            uniqueTypeNames.Remove(dataTypeFullName);

            using var arrayBuilder = ImmutableArrayBuilder<string>.Rent();
            arrayBuilder.AddRange(uniqueTypeNames);
            dataTableAssetRef.NestedDataTypeFullNames = arrayBuilder.ToImmutable();
        }

        private static readonly DiagnosticDescriptor s_errorDescriptor
            = new("SG_DATABASE_01"
                , "Database Generator Error"
                , "This error indicates a bug in the Database source generators. Error message: '{0}'."
                , "ZBase.Foundation.Data.Authoring.DatabaseAttribute"
                , DiagnosticSeverity.Error
                , isEnabledByDefault: true
                , description: ""
            );
    }
}
