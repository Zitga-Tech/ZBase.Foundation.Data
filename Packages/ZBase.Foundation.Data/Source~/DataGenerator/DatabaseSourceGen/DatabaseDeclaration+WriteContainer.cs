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
                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"protected SheetContainer(global::Microsoft.Extensions.Logging.ILogger logger) : base(logger) {{ }}");
                    p.PrintEndLine();

                    foreach (var table in tables)
                    {
                        if (dataTableAssetRefMap.TryGetValue(table.FullTypeName, out var dataTableAssetRef) == false)
                        {
                            continue;
                        }

                        var dataType = dataTableAssetRef.DataType;
                        var typeName = $"{dataType.Name}Sheet";
                        var name = $"{dataType.Name}Sheet";

                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                        p.PrintLine($" public {typeName} {name} {{ get; set; }}");
                        p.PrintEndLine();
                    }
                }
                p.CloseScope();
            }
            p.CloseScope();

            p = p.DecreasedIndent();
            return p.Result;
        }
    }
}
