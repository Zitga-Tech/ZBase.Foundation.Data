using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
        public const string SERIALIZE_FIELD_ATTRIBUTE = "global::UnityEngine.SerializeField";
        public const string DATABASE_ATTRIBUTE = "global::ZBase.Foundation.Data.Authoring.DatabaseAttribute";
        public const string LIST_TYPE_T = "global::System.Collections.Generic.List<";
        public const string DICTIONARY_TYPE_T = "global::System.Collections.Generic.Dictionary<";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var projectPathProvider = SourceGenHelpers.GetSourceGenConfigProvider(context);

            var databaseRefProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: IsValidDatabaseSyntax,
                transform: GetDatabaseRefSemanticMatch
            ).Where(static t => t is { });

            var combined = databaseRefProvider.Collect()
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

        private static bool IsValidDatabaseSyntax(SyntaxNode node, CancellationToken token)
        {
            return node is ClassDeclarationSyntax classSyntax
                && classSyntax.AttributeLists.Count > 0
                && classSyntax.HasAttributeCandidate("ZBase.Foundation.Data.Authoring", "Database");
        }

        public static DatabaseRef GetDatabaseRefSemanticMatch(
              GeneratorSyntaxContext context
            , CancellationToken token
        )
        {
            if (context.SemanticModel.Compilation.IsValidCompilation() == false
                || context.Node is not ClassDeclarationSyntax classSyntax
            )
            {
                return null;
            }

            var semanticModel = context.SemanticModel;
            var symbol = semanticModel.GetDeclaredSymbol(classSyntax, token);
            
            if (symbol.HasAttribute(DATABASE_ATTRIBUTE))
            {
                return new DatabaseRef {
                    Syntax = classSyntax,
                    Symbol = symbol,
                };
            }

            return null;
        }

        private static void GenerateOutput(
              SourceProductionContext context
            , Compilation compilation
            , ImmutableArray<DatabaseRef> candidates
            , string projectPath
            , bool outputSourceGenFiles
        )
        {
            if (candidates.Length < 1)
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            foreach (var candidate in candidates)
            {
                try
                {
                    SourceGenHelpers.ProjectPath = projectPath;

                    var declaration = new DatabaseDeclaration(candidate);

                    if (declaration.DatabaseRef.Tables.Length < 1)
                    {
                        return;
                    }

                    var assemblyName = compilation.Assembly.Name;
                    var dataMap = BuildDataMap(declaration);
                    var dataTableAssetRefMap = BuildDataTableAssetRefMap(declaration, dataMap);
                    var syntaxTree = candidate.Syntax.SyntaxTree;
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
                        , declaration.WriteContainer(dataTableAssetRefMap, dataMap)
                        , databaseHintName
                        , databaseSourceFilePath
                    );

                    var tables = declaration.DatabaseRef.Tables;

                    foreach (var table in tables)
                    {
                        if (dataTableAssetRefMap.TryGetValue(table.Type.ToFullName(), out var dataTableAssetRef) == false)
                        {
                            continue;
                        }

                        if (dataMap.TryGetValue(dataTableAssetRef.DataType.ToFullName(), out var dataTypeDeclaration) == false)
                        {
                            continue;
                        }

                        var sheetHintName = GetHintName(
                              syntaxTree
                            , GENERATOR_NAME
                            , candidate.Syntax
                            , $"{databaseIdentifier}_{dataTableAssetRef.Symbol.Name}_{dataTableAssetRef.DataType.Name}Sheet"
                        );

                        var sheetSourceFilePath = GetSourceFilePath(
                              sheetHintName
                            , assemblyName
                        );

                        OutputSource(
                              context
                            , outputSourceGenFiles
                            , declaration.DatabaseRef.Syntax
                            , declaration.WriteSheet(table, dataTableAssetRef, dataTypeDeclaration, dataMap)
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
            }
        }

        private static string GetHintName(
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

        private static string GetSourceFilePath(string fileName, string assemblyName)
        {
            if (SourceGenHelpers.CanWriteToProjectPath)
            {
                var saveToDirectory = $"{SourceGenHelpers.ProjectPath}/Temp/GeneratedCode/{assemblyName}/";
                Directory.CreateDirectory(saveToDirectory);
                return saveToDirectory + fileName;
            }

            return $"Temp/GeneratedCode/{assemblyName}";
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

        private static Dictionary<string, DataDeclaration> BuildDataMap(DatabaseDeclaration declaration)
        {
            var tables = declaration.DatabaseRef.Tables;
            var map = new Dictionary<string, DataDeclaration>();
            var queue = new Queue<ITypeSymbol>();

            foreach (var table in tables)
            {
                var args = table.BaseType.TypeArguments;
                queue.Enqueue(args[0]);
                queue.Enqueue(args[1]);

                while (queue.Count > 0)
                {
                    var type = queue.Dequeue();
                    
                    if (type.InheritsFromInterface(IDATA) == false)
                    {
                        continue;
                    }

                    var typeName = type.ToFullName();
                    
                    if (map.ContainsKey(typeName))
                    {
                        continue;
                    }

                    var dataDeclaration = new DataDeclaration(type);

                    if (dataDeclaration.Fields.Length < 1)
                    {
                        continue;
                    }

                    map[typeName] = dataDeclaration;

                    foreach (var fieldRef in dataDeclaration.Fields)
                    {
                        if (fieldRef.Type is IArrayTypeSymbol arrayType)
                        {
                            queue.Enqueue(arrayType.ElementType);
                        }
                        else if (fieldRef.Type is INamedTypeSymbol namedType)
                        {
                            var typeFullName = fieldRef.Type.ToFullName();

                            if (typeFullName.StartsWith(LIST_TYPE_T)
                                || typeFullName.StartsWith(DICTIONARY_TYPE_T)
                            )
                            {
                                foreach (var typeArg in namedType.TypeArguments)
                                {
                                    queue.Enqueue(typeArg);
                                }
                            }
                            else
                            {
                                queue.Enqueue(namedType);
                            }
                        }
                        else
                        {
                            queue.Enqueue(fieldRef.Type);
                        }
                    }
                }
            }

            return map;
        }

        private static Dictionary<string, DataTableAssetRef> BuildDataTableAssetRefMap(
              DatabaseDeclaration declaration
            , Dictionary<string, DataDeclaration> dataMap
        )
        {
            var tables = declaration.DatabaseRef.Tables;
            var map = new Dictionary<string, DataTableAssetRef>();
            var uniqueTypeNames = new HashSet<string>();
            var typeQueue = new Queue<DataDeclaration>();

            foreach (var table in tables)
            {
                var typeName = table.Type.ToFullName();

                if (map.ContainsKey(typeName) == false)
                {
                    var dataTableAssetRef = new DataTableAssetRef {
                        Symbol = table.Type,
                        IdType = table.BaseType.TypeArguments[0],
                        DataType = table.BaseType.TypeArguments[1],
                    };

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
                    switch (field.CollectionKind)
                    {
                        case CollectionKind.Array:
                        case CollectionKind.List:
                        case CollectionKind.HashSet:
                        case CollectionKind.Queue:
                        case CollectionKind.Stack:
                        {
                            TryAdd(field.CollectionElementType, dataMap, uniqueTypeNames, typeQueue);
                            break;
                        }

                        case CollectionKind.Dictionary:
                        {
                            TryAdd(field.CollectionKeyType, dataMap, uniqueTypeNames, typeQueue);
                            TryAdd(field.CollectionElementType, dataMap, uniqueTypeNames, typeQueue);
                            break;
                        }

                        default:
                        {
                            TryAdd(field.Type, dataMap, uniqueTypeNames, typeQueue);
                            break;
                        }
                    }
                }
            }

            uniqueTypeNames.Remove(idTypeFullName);
            uniqueTypeNames.Remove(dataTypeFullName);

            if (uniqueTypeNames.Count > 0)
            {
                using var arrayBuilder = ImmutableArrayBuilder<string>.Rent();
                arrayBuilder.AddRange(uniqueTypeNames);
                dataTableAssetRef.NestedDataTypeFullNames = arrayBuilder.ToImmutable();
            }
            else
            {
                dataTableAssetRef.NestedDataTypeFullNames = ImmutableArray<string>.Empty;
            }

            static void TryAdd(
                  ITypeSymbol typeSymbol
                , Dictionary<string, DataDeclaration> dataMap
                , HashSet<string> uniqueTypeNames
                , Queue<DataDeclaration> typeQueue
            )
            {
                if (typeSymbol == null)
                {
                    return;
                }

                var typeFullName = typeSymbol.ToFullName();

                if (uniqueTypeNames.Contains(typeFullName))
                {
                    return;
                }

                if (dataMap.TryGetValue(typeFullName, out var typeDeclaration))
                {
                    typeQueue.Enqueue(typeDeclaration);
                    uniqueTypeNames.Add(typeFullName);
                }
            }
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
