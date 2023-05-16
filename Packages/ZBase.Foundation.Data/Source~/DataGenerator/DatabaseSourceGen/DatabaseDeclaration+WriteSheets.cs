using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ZBase.Foundation.SourceGen;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    partial class DatabaseDeclaration
    {
        public const string GENERATED_SHEET_ATTRIBUTE = "[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedSheet(typeof({0}), typeof({1}), typeof({2}))]";

        public void GenerateSheets(
              SourceProductionContext context
            , Compilation compilation
            , bool outputSourceGenFiles
            , DiagnosticDescriptor errorDescriptor
        )
        {
            var compilationUnitSyntax = CompilationUnit().NormalizeWhitespace(eol: "\n");

            foreach (var dataTableAssetRef in DataTableAssetRefs)
            {
                try
                {
                    var syntax = dataTableAssetRef.Syntax;
                    var syntaxTree = syntax.SyntaxTree;
                    var source = GetSourceForSheet(dataTableAssetRef, DataMap);
                    var sourceFilePath = syntaxTree.GetGeneratedSourceFilePath(compilation.Assembly.Name, GENERATOR_NAME);

                    var outputSource = TypeCreationHelpers.GenerateSourceTextForRootNodes(
                          sourceFilePath
                        , compilationUnitSyntax
                        , source
                        , context.CancellationToken
                    );

                    context.AddSource(
                          syntaxTree.GetGeneratedSourceFileName(GENERATOR_NAME, syntax, $"{dataTableAssetRef.DataType.Name}Sheet")
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
                        , dataTableAssetRef.Syntax.GetLocation()
                        , e.ToUnityPrintableString()
                    ));
                }
            }
        }

        private static string GetSourceForSheet(
              DataTableAssetRef dataTableAssetRef
            , Dictionary<string, DataDeclaration> dataMap
        )
        {
            var idType = dataTableAssetRef.IdType;
            var dataType = dataTableAssetRef.DataType;
            var dataTableAssetType = dataTableAssetRef.Symbol;
            var idTypeFullName = idType.ToFullName();
            var dataTypeFullName = dataType.ToFullName();
            var dataTableAssetTypeName = dataTableAssetType.ToFullName();
            var sheetName = $"{dataType.Name}Sheet";
            var containingNamespace = dataTableAssetType.ContainingNamespace.ToDisplayString();
            var nestedDataTypeFullNames = dataTableAssetRef.NestedDataTypeFullNames;

            string sheetIdTypeName;
            string sheetDataTypeName;

            if (dataMap.TryGetValue(idTypeFullName, out var idTypeDeclaration))
            {
                sheetIdTypeName = $"{sheetName}.__{idType.Name}";
            }
            else
            {
                sheetIdTypeName = idTypeFullName;
            }

            if (dataMap.TryGetValue(dataTypeFullName, out var dataTypeDeclaration))
            {
                sheetDataTypeName = $"{sheetName}.__{dataType.Name}";
            }
            else
            {
                sheetDataTypeName = dataTypeFullName;
            }

            var p = Printer.DefaultLarge;

            p.PrintEndLine();
            p.PrintLine("#pragma warning disable");
            p.PrintEndLine();

            p.PrintEndLine().Print("#if UNITY_EDITOR").PrintEndLine();
            p.PrintEndLine();

            p.PrintLine($"namespace {containingNamespace}.Authoring");
            p.OpenScope();
            {
                if (dataTableAssetRef.NamingAttribute != null)
                {
                    var attribute = dataTableAssetRef.NamingAttribute;
                    var attribArgs = attribute.ConstructorArguments;

                    p.PrintBeginLine()
                        .Print($"[global::ZBase.Foundation.Data.DataSheetNamingAttribute(");

                    if (attribArgs.Length > 0)
                    {
                        p.Print($"\"{attribArgs[0].Value}\"");
                    }

                    if (attribArgs.Length > 1)
                    {
                        p.Print($", global::ZBase.Foundation.Data.NamingStrategy.{attribArgs[1].Value.ToNamingStrategy()}");
                    }

                    p.Print(")]").PrintEndLine();
                }

                p.PrintLine(string.Format(GENERATED_SHEET_ATTRIBUTE, idTypeFullName, dataTypeFullName, dataTableAssetTypeName));
                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintBeginLine()
                    .Print($"public partial class {sheetName}")
                    .Print($" : global::Cathei.BakingSheet.Sheet<{sheetIdTypeName}, {sheetDataTypeName}>")
                    .PrintEndLine();
                p.OpenScope();
                {
                    dataTypeDeclaration?.WriteCode(ref p, dataMap, idTypeDeclaration?.Symbol);
                    idTypeDeclaration?.WriteCode(ref p, dataMap);

                    foreach (var nestedFullName in nestedDataTypeFullNames)
                    {
                        if (dataMap.TryGetValue(nestedFullName, out var nestedDataDeclaration))
                        {
                            nestedDataDeclaration?.WriteCode(ref p, dataMap);
                        }
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
