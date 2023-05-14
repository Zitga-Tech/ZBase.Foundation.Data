//using Microsoft.CodeAnalysis;
//using System;
//using System.Collections.Immutable;
//using ZBase.Foundation.SourceGen;

//using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

//namespace ZBase.Foundation.Data.DatabaseSourceGen
//{
//    partial class DatabaseDeclaration
//    {
//        public void GenerateCodeForContainer(
//              SourceProductionContext context
//            , Compilation compilation
//            , bool outputSourceGenFiles
//            , DiagnosticDescriptor errorDescriptor
//        )
//        {
//            var syntax = CompilationUnit().NormalizeWhitespace(eol: "\n");

//            try
//            {
//                var syntaxTree = syntax.SyntaxTree;
//                var source = WriteCodeContainer(compilation.Assembly.Name, DataTableRefs);
//                var sourceFilePath = syntaxTree.GetGeneratedSourceFilePath(compilation.Assembly.Name, GENERATOR_NAME);

//                var outputSource = TypeCreationHelpers.GenerateSourceTextForRootNodes(
//                      sourceFilePath
//                    , syntax
//                    , source
//                    , context.CancellationToken
//                );

//                var fileName = $"BakingSheetContainer_{compilation.Assembly.Name}";

//                context.AddSource(
//                      syntaxTree.GetGeneratedSourceFileName(GENERATOR_NAME, fileName, syntax)
//                    , outputSource
//                );

//                if (outputSourceGenFiles)
//                {
//                    SourceGenHelpers.OutputSourceToFile(
//                          context
//                        , syntax.GetLocation()
//                        , sourceFilePath
//                        , outputSource
//                    );
//                }
//            }
//            catch (Exception e)
//            {
//                context.ReportDiagnostic(Diagnostic.Create(
//                      errorDescriptor
//                    , syntax.GetLocation()
//                    , e.ToUnityPrintableString()
//                ));
//            }

//        }

//        private static string WriteCodeContainer(string assemblyName, ImmutableArray<DataTableAssetRef> dataTableRefs)
//        {
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
//                p.PrintLine("[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedSheetContainerAttribute]");
//                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
//                p.PrintBeginLine()
//                  .Print($"public sealed partial class SheetContainer : global::Cathei.BakingSheet.SheetContainerBase")
//                  .PrintEndLine();
//                p.OpenScope();
//                {
//                    p.PrintLine("public SheetContainer(global::Microsoft.Extensions.Logging.ILogger logger) : base(logger) { }");
//                    p.PrintEndLine();

//                    foreach (var dataTableRef in dataTableRefs)
//                    {
//                        var className = $"{dataTableRef.Syntax.Identifier.Text}Sheet";

//                        p.PrintLine($"[global::ZBase.Foundation.Data.Authoring.SourceGen.DataIdInfoAttribute(typeof({dataTableRef.IdType.ToFullName()}))]");
//                        p.PrintLine($"[global::ZBase.Foundation.Data.Authoring.SourceGen.DataInfoAttribute(typeof({dataTableRef.DataType.ToFullName()}))]");
//                        p.PrintLine($"[global::ZBase.Foundation.Data.Authoring.SourceGen.DataTableInfoAttribute(typeof({dataTableRef.Symbol.ToFullName()}))]");
//                        p.PrintLine($"[global::ZBase.Foundation.Data.Authoring.SourceGen.DataTableAssetInfoAttribute(typeof({dataTableRef.Syntax.Identifier.Text}Asset))]");
//                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
//                        p.PrintLine($"public {className} {className} {{ get; set; }}");
//                        p.PrintEndLine();
//                    }
//                }
//                p.CloseScope();
//            }
//            p.CloseScope();

//            p.PrintEndLine()
//                .Print("#endif")
//                .PrintEndLine();

//            return p.Result;
//        }
//    }
//}
