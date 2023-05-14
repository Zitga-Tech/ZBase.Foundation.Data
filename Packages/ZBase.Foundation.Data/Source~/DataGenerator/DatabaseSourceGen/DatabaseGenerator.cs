//using System;
//using System.Collections.Immutable;
//using System.Linq;
//using System.Threading;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using ZBase.Foundation.SourceGen;

//namespace ZBase.Foundation.Data.DatabaseSourceGen
//{
//    [Generator]
//    public class DatabaseGenerator : IIncrementalGenerator
//    {
//        public const string IDATA = "global::ZBase.Foundation.Data.IData";
//        public const string IDATA_TABLE_T = "global::ZBase.Foundation.Data.IDataTable<";

//        public void Initialize(IncrementalGeneratorInitializationContext context)
//        {
//            var projectPathProvider = SourceGenHelpers.GetSourceGenConfigProvider(context);

//            var dataTableRefProvider = context.SyntaxProvider.CreateSyntaxProvider(
//                predicate: static (node, token) => GeneratorHelper.IsStructOrClassSyntaxMatch(node, token),
//                transform: static (syntaxContext, token) => GetDataTableRefSemanticMatch(syntaxContext, token)
//            ).Where(static t => t != null);

//            var dataRefProvider = context.SyntaxProvider.CreateSyntaxProvider(
//                predicate: static (node, token) => GeneratorHelper.IsStructOrClassSyntaxMatch(node, token),
//                transform: static (syntaxContext, token) => GetDataRefSemanticMatch(syntaxContext, token)
//            ).Where(static t => t != null);

//            var combined = dataTableRefProvider.Collect()
//                .Combine(dataRefProvider.Collect())
//                .Combine(context.CompilationProvider)
//                .Combine(projectPathProvider);

//            context.RegisterSourceOutput(combined, static (sourceProductionContext, source) => {
//                GenerateOutput(
//                    sourceProductionContext
//                    , source.Left.Right
//                    , source.Left.Left.Left
//                    , source.Left.Left.Right
//                    , source.Right.projectPath
//                    , source.Right.outputSourceGenFiles
//                );
//            });
//        }

//        public static DataTableRef GetDataTableRefSemanticMatch(
//              GeneratorSyntaxContext context
//            , CancellationToken token
//        )
//        {
//            if (context.SemanticModel.Compilation.IsValidCompilation() == false
//                || context.Node is not TypeDeclarationSyntax typeSyntax
//                || typeSyntax.Kind() is not (SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration)
//                || typeSyntax.BaseList == null
//            )
//            {
//                return null;
//            }

//            var semanticModel = context.SemanticModel;

//            foreach (var baseType in typeSyntax.BaseList.Types)
//            {
//                var typeInfo = semanticModel.GetTypeInfo(baseType.Type, token);

//                if (typeInfo.Type is INamedTypeSymbol typeSymbol)
//                {
//                    if (typeSymbol.IsGenericType
//                        && typeSymbol.TypeParameters.Length == 2
//                        && typeSymbol.ToFullName().StartsWith(IDATA_TABLE_T)
//                    )
//                    {
//                        return new DataTableRef {
//                            Syntax = typeSyntax,
//                            IdType = typeSymbol.TypeArguments[0],
//                            DataType = typeSymbol.TypeArguments[1],
//                        };
//                    }
//                }

//                if (TryGetMatchTypeArguments(typeInfo.Type.Interfaces, out var idType, out var dataType)
//                    || TryGetMatchTypeArguments(typeInfo.Type.AllInterfaces, out idType, out dataType)
//                )
//                {
//                    return new DataTableRef {
//                        Syntax = typeSyntax,
//                        IdType = idType,
//                        DataType = dataType,
//                    };
//                }
//            }

//            return null;

//            static bool TryGetMatchTypeArguments(
//                  ImmutableArray<INamedTypeSymbol> interfaces
//                , out ITypeSymbol idType
//                , out ITypeSymbol dataType
//            )
//            {
//                foreach (var interfaceSymbol in interfaces)
//                {
//                    if (interfaceSymbol.IsGenericType
//                        && interfaceSymbol.TypeParameters.Length == 2
//                        && interfaceSymbol.ToFullName().StartsWith(IDATA_TABLE_T)
//                    )
//                    {
//                        idType = interfaceSymbol.TypeArguments[0];
//                        dataType = interfaceSymbol.TypeArguments[1];
//                        return true;
//                    }
//                }

//                idType = dataType = default;
//                return false;
//            }
//        }

//        public static DataRef GetDataRefSemanticMatch(
//              GeneratorSyntaxContext context
//            , CancellationToken token
//        )
//        {
//            if (context.SemanticModel.Compilation.IsValidCompilation() == false
//                || context.Node is not TypeDeclarationSyntax typeSyntax
//                || typeSyntax.Kind() is not (SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration)
//                || typeSyntax.BaseList == null
//            )
//            {
//                return null;
//            }

//            var isValid = false;

//            foreach (var member in typeSyntax.Members)
//            {
//                if (member is FieldDeclarationSyntax fieldSyntax
//                    && fieldSyntax.HasAttributeCandidate("UnityEngine", "SerializeField")
//                )
//                {
//                    isValid = true;
//                    break;
//                }
//            }

//            if (isValid == false)
//            {
//                return null;
//            }

//            var semanticModel = context.SemanticModel;

//            foreach (var baseType in typeSyntax.BaseList.Types)
//            {
//                var typeInfo = semanticModel.GetTypeInfo(baseType.Type, token);

//                if (typeInfo.Type is INamedTypeSymbol typeSymbol
//                    && typeSymbol.ToFullName() == IDATA
//                )
//                {
//                    return new DataRef {
//                        Syntax = typeSyntax,
//                    };
//                }

//                if (DoesMatchInterface(typeInfo.Type.Interfaces)
//                    || DoesMatchInterface(typeInfo.Type.AllInterfaces)
//                )
//                {
//                    return new DataRef {
//                        Syntax = typeSyntax,
//                    };
//                }
//            }

//            return null;

//            static bool DoesMatchInterface(ImmutableArray<INamedTypeSymbol> interfaces)
//            {
//                foreach (var interfaceSymbol in interfaces)
//                {
//                    if (interfaceSymbol.ToFullName() == IDATA)
//                    {
//                        return true;
//                    }
//                }

//                return false;
//            }
//        }

//        private static void GenerateOutput(
//              SourceProductionContext context
//            , Compilation compilation
//            , ImmutableArray<DataTableRef> dataTableRefs
//            , ImmutableArray<DataRef> dataRefs
//            , string projectPath
//            , bool outputSourceGenFiles
//        )
//        {
//            if (dataTableRefs.Length < 1)
//            {
//                return;
//            }

//            context.CancellationToken.ThrowIfCancellationRequested();

//            try
//            {
//                SourceGenHelpers.ProjectPath = projectPath;

//                var declaration = new DatabaseDeclaration(dataTableRefs, dataRefs, compilation, context.CancellationToken);

//                declaration.GenerateCodeForSheets(
//                      context
//                    , compilation
//                    , outputSourceGenFiles
//                    , s_errorDescriptor
//                );

//                declaration.GenerateCodeForContainer(
//                      context
//                    , compilation
//                    , outputSourceGenFiles
//                    , s_errorDescriptor
//                );


//            }
//            catch (Exception e)
//            {
//                context.ReportDiagnostic(Diagnostic.Create(
//                      s_errorDescriptor
//                    , null
//                    , e.ToUnityPrintableString()
//                ));
//            }
//        }

//        private static readonly DiagnosticDescriptor s_errorDescriptor
//            = new("SG_DATABASE_01"
//                , "Database Generator Error"
//                , "This error indicates a bug in the Database source generators. Error message: '{0}'."
//                , "ZBase.Foundation.Data.IDataTable<TId, TData>"
//                , DiagnosticSeverity.Error
//                , isEnabledByDefault: true
//                , description: ""
//            );

//    }
//}
