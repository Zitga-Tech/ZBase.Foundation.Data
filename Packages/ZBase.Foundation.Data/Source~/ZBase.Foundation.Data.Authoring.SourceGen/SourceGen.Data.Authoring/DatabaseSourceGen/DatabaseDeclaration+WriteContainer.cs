﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    partial class DatabaseDeclaration
    {
        public string WriteContainer(
              Dictionary<string, DataTableAssetRef> dataTableAssetRefMap
            , Dictionary<string, DataDeclaration> dataMap
        )
        {
            var syntax = DatabaseRef.Syntax;
            var tables = DatabaseRef.Tables;
            var containsTables = tables.Length > 0 && dataTableAssetRefMap != null && dataMap != null;
            var databaseClassName = syntax.Identifier.Text;

            var scopePrinter = new SyntaxNodeScopePrinter(Printer.DefaultLarge, DatabaseRef.Syntax.Parent);
            var p = scopePrinter.printer;
            p = p.IncreasedIndent();

            p.PrintEndLine();
            p.Print("#pragma warning disable").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine()
                .Print($"partial class ").Print(databaseClassName)
                .Print($" : global::ZBase.Foundation.Data.Authoring.SourceGen.IContains<{databaseClassName}.SheetContainer>")
                .PrintEndLine();
            p.OpenScope();
            {
                p.PrintLine("[global::System.Serializable]");
                p.PrintLine("[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedSheetContainer]");
                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine($"public partial class SheetContainer : global::ZBase.Foundation.Data.Authoring.DataSheetContainerBase");
                p.OpenScope();
                {
                    if (containsTables)
                    {
                        foreach (var table in tables)
                        {
                            if (dataTableAssetRefMap.TryGetValue(table.Type.ToFullName(), out var dataTableAssetRef) == false)
                            {
                                continue;
                            }

                            var dataType = dataTableAssetRef.DataType;

                            if (dataMap.ContainsKey(dataType.ToFullName()) == false)
                            {
                                continue;
                            }

                            var typeName = GetSheetName(table, dataType);
                            var sheetName = typeName;

                            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                            p.PrintLine($"public {typeName} {sheetName} {{ get; private set; }}");
                            p.PrintEndLine();
                        }
                    }

                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"public SheetContainer(global::Microsoft.Extensions.Logging.ILogger logger) : base(logger)");
                    p.OpenScope();
                    {
                        if (containsTables)
                        {
                            foreach (var table in tables)
                            {
                                if (dataTableAssetRefMap.TryGetValue(table.Type.ToFullName(), out var dataTableAssetRef) == false)
                                {
                                    continue;
                                }

                                var dataType = dataTableAssetRef.DataType;

                                if (dataMap.ContainsKey(dataType.ToFullName()) == false)
                                {
                                    continue;
                                }

                                var typeName = GetSheetName(table, dataType);
                                var sheetName = typeName;

                                p.PrintLine($"this.{sheetName} = new {typeName}();");
                            }
                        }
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine("public void CopyFrom(global::ZBase.Foundation.Data.DatabaseAsset databaseAsset)");
                    p.OpenScope();
                    {
                        if (containsTables)
                        {
                            foreach (var table in tables)
                            {
                                if (dataTableAssetRefMap.TryGetValue(table.Type.ToFullName(), out var dataTableAssetRef) == false)
                                {
                                    continue;
                                }

                                var dataType = dataTableAssetRef.DataType;

                                if (dataMap.ContainsKey(dataType.ToFullName()) == false)
                                {
                                    continue;
                                }

                                var tableTypeName = dataTableAssetRef.Symbol.ToFullName();
                                var sheetName = GetSheetName(table, dataType);
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
                    }
                    p.CloseScope();
                }
                p.CloseScope();
            }
            p.CloseScope();

            p = p.DecreasedIndent();
            return p.Result;
        }

        private static string GetSheetName(DatabaseRef.Table table, ITypeSymbol dataType)
            => $"{table.Type.Name}_{dataType.Name}Sheet";
    }
}
