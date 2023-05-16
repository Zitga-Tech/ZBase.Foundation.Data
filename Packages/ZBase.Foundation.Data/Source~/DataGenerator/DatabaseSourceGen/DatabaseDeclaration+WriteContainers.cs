using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZBase.Foundation.SourceGen;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    partial class DatabaseDeclaration
    {
        public void GenerateContainers(
              SourceProductionContext context
            , Compilation compilation
            , bool outputSourceGenFiles
            , DiagnosticDescriptor errorDescriptor
        )
        {
            var syntax = CompilationUnit().NormalizeWhitespace(eol: "\n");

            foreach (var pair in DatabaseMap)
            {
                if (pair.Value.Count < 1)
                {
                    continue;
                }

                try
                {
                    var idx = pair.Key.LastIndexOf(':');
                    var namespaceName = pair.Key.Substring(0, idx);
                    var typeName = pair.Key.Substring(idx + 1);

                    var syntaxTree = syntax.SyntaxTree;
                    var source = GetSourceForContainer(namespaceName, typeName, pair.Value);

                    var filePath = $"{compilation.Assembly.Name.ToValidIdentifier()}_{namespaceName.ToValidIdentifier()}_{typeName.ToValidIdentifier()}";
                    var sourceFilePath = syntaxTree.GetGeneratedSourceFilePath(filePath, GENERATOR_NAME);

                    var outputSource = TypeCreationHelpers.GenerateSourceTextForRootNodes(
                          sourceFilePath
                        , syntax
                        , source
                        , context.CancellationToken
                    );

                    context.AddSource(
                          syntaxTree.GetGeneratedSourceFileName(GENERATOR_NAME, filePath, syntax)
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
                          errorDescriptor
                        , syntax.GetLocation()
                        , e.ToUnityPrintableString()
                    ));
                }
            }
        }

        private static string GetSourceForContainer(
              string namespaceName
            , string databaseTypeName
            , HashSet<DataTableAssetRef> dataTableAssetRefs
        )
        {
            var containerTypeName = $"{databaseTypeName}Container";

            var p = Printer.DefaultLarge;

            p.PrintEndLine();
            p.PrintLine("#pragma warning disable");
            p.PrintEndLine();

            p.PrintEndLine().Print("#if UNITY_EDITOR").PrintEndLine();
            p.PrintEndLine();

            p.PrintLine($"namespace {namespaceName}.Authoring");
            p.OpenScope();
            {
                p.PrintLine("[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedSheetContainer]");
                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine($"public partial class {containerTypeName}: global::Cathei.BakingSheet.SheetContainerBase");
                p.OpenScope();
                {
                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"protected {containerTypeName}(global::Microsoft.Extensions.Logging.ILogger logger) : base(logger) {{ }}");
                    p.PrintEndLine();

                    foreach (var dataTableAssetRef in dataTableAssetRefs)
                    {
                        var dataTableAssetType = dataTableAssetRef.Symbol;
                        var containingNamespace = dataTableAssetType.ContainingNamespace.ToDisplayString();
                        var dataType = dataTableAssetRef.DataType;
                        var typeName = $"global::{containingNamespace}.Authoring.{dataType.Name}Sheet";
                        var name = $"{dataType.Name}Sheet";

                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                        p.PrintLine($"public {typeName} {name} {{ get; set; }}");
                        p.PrintEndLine();
                    }
                }
                p.CloseScope();
            }
            p.CloseScope();

            p.PrintEndLine().Print("#endif").PrintEndLine();

            return p.Result;
        }
    }
}
