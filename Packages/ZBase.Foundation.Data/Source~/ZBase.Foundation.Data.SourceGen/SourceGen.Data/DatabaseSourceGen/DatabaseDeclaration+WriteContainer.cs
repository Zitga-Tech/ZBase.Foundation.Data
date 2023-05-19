using System.Collections.Generic;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    partial class DatabaseDeclaration
    {
        public string WriteContainer(
            Dictionary<string, DataTableAssetRef> dataTableAssetRefMap
        )
        {
            var syntax = DatabaseRef.Syntax;
            var tables = DatabaseRef.Tables;

            var scopePrinter = new SyntaxNodeScopePrinter(Printer.DefaultLarge, DatabaseRef.Syntax.Parent);
            var p = scopePrinter.printer;
            p = p.IncreasedIndent();

            p.PrintEndLine();
            p.PrintLine("#pragma warning disable");
            p.PrintEndLine();

            p.PrintBeginLine()
                .Print($"partial class ").Print(syntax.Identifier.Text)
                .PrintEndLine();
            p.OpenScope();
            {
                p.PrintLine("[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedSheetContainer]");
                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine($"public partial class SheetContainer : global::Cathei.BakingSheet.SheetContainerBase");
                p.OpenScope();
                {
                    foreach (var table in tables)
                    {
                        if (dataTableAssetRefMap.TryGetValue(table.Type.ToFullName(), out var dataTableAssetRef) == false)
                        {
                            continue;
                        }

                        var dataType = dataTableAssetRef.DataType;
                        var typeName = $"{dataType.Name}Sheet";
                        var name = $"{dataType.Name}Sheet";

                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                        p.PrintLine($"public {typeName} {name} {{ get; private set; }}");
                        p.PrintEndLine();
                    }

                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"public SheetContainer(global::Microsoft.Extensions.Logging.ILogger logger) : base(logger)");
                    p.OpenScope();
                    {
                        foreach (var table in tables)
                        {
                            if (dataTableAssetRefMap.TryGetValue(table.Type.ToFullName(), out var dataTableAssetRef) == false)
                            {
                                continue;
                            }

                            var dataType = dataTableAssetRef.DataType;
                            var typeName = $"{dataType.Name}Sheet";
                            var sheetName = $"{dataType.Name}Sheet";

                            p.PrintLine($"this.{sheetName} = new {typeName}();");
                        }
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine("public void CopyFrom(global::ZBase.Foundation.Data.DatabaseAsset databaseAsset)");
                    p.OpenScope();
                    {
                        foreach (var table in tables)
                        {
                            if (dataTableAssetRefMap.TryGetValue(table.Type.ToFullName(), out var dataTableAssetRef) == false)
                            {
                                continue;
                            }

                            var dataType = dataTableAssetRef.DataType;
                            var tableTypeName = dataTableAssetRef.Symbol.ToFullName();
                            var sheetName = $"{dataType.Name}Sheet";
                            var variableName = $"m{dataTableAssetRef.Symbol.Name}";

                            p.PrintLine($"if (databaseAsset.TryGetDataTableAsset<{tableTypeName}>(out var {variableName}))");
                            p.OpenScope();
                            {
                                p.PrintLine($"this.{sheetName}.CopyFrom({variableName});");
                            }
                            p.CloseScope();
                            p.PrintEndLine();
                        }
                    }
                    p.CloseScope();
                }
                p.CloseScope();
            }
            p.CloseScope();

            p = p.DecreasedIndent();
            return p.Result;
        }
    }
}
