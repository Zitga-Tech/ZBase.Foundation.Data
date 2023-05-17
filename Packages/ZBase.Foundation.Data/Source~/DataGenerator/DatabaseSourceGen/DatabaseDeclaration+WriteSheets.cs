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

                p.PrintLine(string.Format(GENERATED_SHEET_ATTRIBUTE, idTypeFullName, dataTypeFullName, dataTableAssetTypeName));
                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintBeginLine()
                    .Print($"public partial class {sheetName}")
                    .Print($" : global::Cathei.BakingSheet.Sheet<{sheetIdTypeName}, {sheetDataTypeName}>")
                    .PrintEndLine();
                p.OpenScope();
                {
                    dataTypeDeclaration?.WriteCode(
                          ref p
                        , dataMap
                        , verticalListMap
                        , dataTypeFullName
                        , idTypeDeclaration?.Symbol
                    );

                    idTypeDeclaration?.WriteCode(
                          ref p
                        , dataMap
                        , verticalListMap
                        , dataTypeFullName
                    );

                    foreach (var nestedFullName in nestedDataTypeFullNames)
                    {
                        if (dataMap.TryGetValue(nestedFullName, out var nestedDataDeclaration))
                        {
                            nestedDataDeclaration?.WriteCode(
                                  ref p
                                , dataMap
                                , verticalListMap
                                , dataTypeFullName
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
