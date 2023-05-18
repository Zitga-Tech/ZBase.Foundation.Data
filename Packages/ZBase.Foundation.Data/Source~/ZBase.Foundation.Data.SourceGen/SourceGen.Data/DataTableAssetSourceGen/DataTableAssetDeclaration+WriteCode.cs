using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DataTableAssetSourceGen
{
    partial class DataTableAssetDeclaration
    {
        public string WriteCode()
        {
            var syntax = TypeRef.Syntax;
            var idTypeName = TypeRef.IdType.ToFullName();
            var dataTypeName = TypeRef.DataType.ToFullName();

            var scopePrinter = new SyntaxNodeScopePrinter(Printer.DefaultLarge, syntax.Parent);
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
                p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine($"protected override {idTypeName} GetId(in {dataTypeName} row)");
                p.OpenScope();
                {
                    p.PrintLine($"return row.Id;");
                }
                p.CloseScope();
            }
            p.CloseScope();

            p = p.DecreasedIndent();
            return p.Result;
        }
    }
}