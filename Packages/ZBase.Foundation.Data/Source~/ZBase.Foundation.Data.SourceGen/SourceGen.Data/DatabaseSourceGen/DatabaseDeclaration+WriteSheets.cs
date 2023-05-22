using System.Collections.Generic;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    partial class DatabaseDeclaration
    {
        public const string GENERATED_SHEET_ATTRIBUTE = "[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedSheet(typeof({0}), typeof({1}), typeof({2}))]";

        public string WriteSheet(
              DatabaseRef.Table table
            , DataTableAssetRef dataTableAssetRef
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
            var nestedDataTypeFullNames = dataTableAssetRef.NestedDataTypeFullNames;
            var verticalListMap = DatabaseRef.VerticalListMap;

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

            var scopePrinter = new SyntaxNodeScopePrinter(Printer.DefaultLarge, DatabaseRef.Syntax.Parent);
            var p = scopePrinter.printer;
            p = p.IncreasedIndent();

            p.PrintEndLine();
            p.PrintLine("#pragma warning disable");
            p.PrintEndLine();

            p.PrintBeginLine()
                .Print($"partial class ").Print(DatabaseRef.Syntax.Identifier.Text)
                .PrintEndLine();
            p.OpenScope();
            {
                p.PrintBeginLine()
                    .Print($"[global::ZBase.Foundation.Data.Authoring.SourceGen.SheetNamingAttribute(")
                    .Print($"\"{table.SheetName}\"")
                    .Print($", global::ZBase.Foundation.Data.Authoring.NamingStrategy.{table.NamingStrategy}")
                    .Print(")]")
                    .PrintEndLine();

                p.PrintLine("[global::System.Serializable]");
                p.PrintLine(string.Format(GENERATED_SHEET_ATTRIBUTE, idTypeFullName, dataTypeFullName, dataTableAssetTypeName));
                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintBeginLine()
                    .Print($"public partial class {sheetName}")
                    .Print($" : global::Cathei.BakingSheet.Sheet<{sheetIdTypeName}, {sheetDataTypeName}>")
                    .PrintEndLine();
                p.OpenScope();
                {
                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"public void CopyFrom({dataTableAssetTypeName} dataTableAsset)");
                    p.OpenScope();
                    {
                        p.PrintLine("if (dataTableAsset == false) return;");
                        p.PrintEndLine();

                        p.PrintLine("foreach (var row in dataTableAsset.Rows.Span)");
                        p.OpenScope();
                        {
                            p.PrintLine($"var item = new __{dataType.Name}();");
                            p.PrintLine("item.CopyFrom(row);");
                            p.PrintLine("Add(item);");
                        }
                        p.CloseScope();
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    if (dataTypeDeclaration != null)
                    {
                        var typeFullName = dataTypeDeclaration.FullName;
                        var typeName = dataTypeDeclaration.Symbol.Name;

                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                        p.PrintLine($"public {typeFullName}[] To{typeName}Array()");
                        p.OpenScope();
                        {
                            p.PrintLine($"if (this.Items == null || this.Count == 0)");
                            p = p.IncreasedIndent();
                            p.PrintLine($"return global::System.Array.Empty<{typeFullName}>();");
                            p = p.DecreasedIndent();
                            p.PrintEndLine();

                            p.PrintLine("var rows = this.Items;");
                            p.PrintLine("var count = this.Count;");
                            p.PrintLine($"var result = new {typeFullName}[count];");
                            p.PrintEndLine();

                            p.PrintLine("for (var i = 0; i < count; i++)");
                            p.OpenScope();
                            {
                                p.PrintLine($"result[i] = (rows[i] ?? __{typeName}.Default).To{typeName}();");
                            }
                            p.CloseScope();

                            p.PrintEndLine();
                            p.PrintLine("return result;");
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        dataTypeDeclaration.WriteCode(
                              ref p
                            , dataMap
                            , verticalListMap
                            , dataTableAssetTypeName
                            , inheritSheetRow: true
                            , idTypeDeclaration?.Symbol
                        );
                    }

                    idTypeDeclaration?.WriteCode(
                          ref p
                        , dataMap
                        , verticalListMap
                        , dataTableAssetTypeName
                        , inheritSheetRow: false
                        , idType: null
                    );

                    foreach (var nestedFullName in nestedDataTypeFullNames)
                    {
                        if (dataMap.TryGetValue(nestedFullName, out var nestedDataDeclaration))
                        {
                            nestedDataDeclaration?.WriteCode(
                                  ref p
                                , dataMap
                                , verticalListMap
                                , dataTableAssetTypeName
                                , inheritSheetRow: false
                                , idType: null
                            );
                        }
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
