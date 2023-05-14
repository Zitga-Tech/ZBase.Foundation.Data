//using Microsoft.CodeAnalysis;
//using System;
//using ZBase.Foundation.SourceGen;

//using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

//namespace ZBase.Foundation.Data.DatabaseSourceGen
//{
//    partial class DatabaseDeclaration
//    {
//        public void GenerateCodeForDataTableAssets(
//              SourceProductionContext context
//            , Compilation compilation
//            , bool outputSourceGenFiles
//            , DiagnosticDescriptor errorDescriptor
//        )
//        {
//            var syntax = CompilationUnit().NormalizeWhitespace(eol: "\n");

//            foreach (var dataTableRef in DataTableRefs)
//            {
//                try
//                {
//                    var dataTableSyntax = dataTableRef.Syntax;
//                    var source = WriteDataTableAsset(compilation.Assembly.Name, dataTableRef);
//                    var sourceFilePath = syntax.SyntaxTree.GetGeneratedSourceFilePath(compilation.Assembly.Name, GENERATOR_NAME);

//                    var outputSource = TypeCreationHelpers.GenerateSourceTextForRootNodes(
//                          sourceFilePath
//                        , syntax
//                        , source
//                        , context.CancellationToken
//                    );

//                    context.AddSource(
//                          dataTableSyntax.SyntaxTree.GetGeneratedSourceFileName(GENERATOR_NAME, dataTableSyntax, dataTableRef.Symbol.ToValidIdentifier())
//                        , outputSource
//                    );

//                    if (outputSourceGenFiles)
//                    {
//                        SourceGenHelpers.OutputSourceToFile(
//                              context
//                            , dataTableSyntax.GetLocation()
//                            , sourceFilePath
//                            , outputSource
//                        );
//                    }
//                }
//                catch (Exception e)
//                {
//                    context.ReportDiagnostic(Diagnostic.Create(
//                          errorDescriptor
//                        , dataTableRef.Syntax.GetLocation()
//                        , e.ToUnityPrintableString()
//                    ));
//                }
//            }
//        }

//        private static string WriteDataTableAsset(string assemblyName, DataTableRef dataTableRef)
//        {
//            var dataTableAssetTypeName = $"{dataTableRef.Syntax.Identifier.Text}Asset";
//            var dataTableTypeName = dataTableRef.Symbol.ToFullName();
//            var idTypeName = dataTableRef.IdType.ToFullName();
//            var dataTypeName = dataTableRef.DataType.ToFullName();

//            var p = Printer.DefaultLarge;

//            p.PrintEndLine();
//            p.PrintLine("#pragma warning disable");
//            p.PrintEndLine();

//            p.PrintEndLine()
//                .Print("#if UNITY_EDITOR")
//                .PrintEndLine();

//            p.PrintLine($"namespace ZBase.Foundation.Data.Authoring.__Internals.{assemblyName.ToValidIdentifier()}");
//            p.OpenScope();
//            {
//                p.PrintLine($"[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedDataTableAssetAttribute(typeof({dataTableTypeName}))]");
//                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
//                p.PrintBeginLine()
//                    .Print($"public sealed partial class {dataTableAssetTypeName}")
//                    .Print($" : global::ZBase.Foundation.Data.DataTableAsset<{idTypeName}, {dataTypeName}>")
//                    .PrintEndLine();
//                p.OpenScope();
//                {
                    
//                }
//                p.CloseScope();

//                p.PrintEndLine()
//                    .Print("#endif")
//                    .PrintEndLine();

//                return p.Result;
//            }
//        }
//    }
//}
