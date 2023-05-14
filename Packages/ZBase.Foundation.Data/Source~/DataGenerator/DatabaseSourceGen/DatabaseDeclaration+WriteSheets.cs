//using Microsoft.CodeAnalysis;
//using System;
//using ZBase.Foundation.SourceGen;

//using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

//namespace ZBase.Foundation.Data.DatabaseSourceGen
//{
//    partial class DatabaseDeclaration
//    {
//        public void GenerateCodeForSheets(
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
//                    var source = WriteSheet(compilation.Assembly.Name, dataTableRef);
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

//        private static string WriteSheet(string assemblyName, DataTableAssetRef dataTableRef)
//        {
//            var sheetTypeName = $"{dataTableRef.Syntax.Identifier.Text}Sheet";
//            var rowTypeName = dataTableRef.DataType.Name;
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
//                p.PrintLine($"[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedSheetAttribute(typeof({dataTableTypeName}), typeof({idTypeName}), typeof({dataTypeName}))]");
//                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
//                p.PrintBeginLine()
//                    .Print($"public sealed partial class {sheetTypeName}")
//                    .Print($" : global::Cathei.BakingSheet.Sheet<{idTypeName}, {sheetTypeName}.{rowTypeName}>")
//                    .PrintEndLine();
//                p.OpenScope();
//                {
//                    p.PrintLine($"[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedSheetRowAttribute(typeof({idTypeName}), typeof({dataTypeName}))]");
//                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
//                    p.PrintBeginLine()
//                       .Print($"public sealed partial class {rowTypeName}")
//                       .Print($" : global::Cathei.BakingSheet.SheetRow<{idTypeName}>")
//                       .PrintEndLine();
//                    p.OpenScope();
//                    {
//                        foreach (var field in dataTableRef.Fields)
//                        {
//                            var propertyName = field.ToPropertyName();

//                            if (field.Type is not IArrayTypeSymbol arrayType)
//                            {
//                                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
//                                p.PrintLine($"public {field.Type} {propertyName} {{ get; set; }}");
//                            }
//                            else
//                            {
//                                var elemTypeName = dataTableRef.ElementTypes.Contains(arrayType.ElementType)
//                                    ? arrayType.ElementType.Name
//                                    : arrayType.ElementType.ToFullName();

//                                var listTypeName = field.HasAttribute(VERTICAL_ARRAY_ATTRIBUTE)
//                                    ? VERTICAL_LIST_TYPE
//                                    : LIST_TYPE;

//                                p.PrintLine($"public {listTypeName}<{elemTypeName}> {propertyName} {{ get; set; }}");
//                            }
//                        }
//                    }
//                    p.CloseScope();

//                    foreach (var elementType in dataTableRef.ElementTypes)
//                    {
//                        var elemMembers = elementType.GetMembers();

//                        p.PrintLine($"public sealed partial class {elementType.Name}");
//                        p.OpenScope();
//                        {
//                            foreach (var member in elemMembers)
//                            {
//                                if (member is IFieldSymbol memberField)
//                                {
//                                    p.PrintLine($"public {memberField.Type} {memberField.ToPropertyName()} {{ get; set; }}");
//                                }
//                            }
//                        }
//                        p.CloseScope();
//                    }
//                    p.CloseScope();
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
