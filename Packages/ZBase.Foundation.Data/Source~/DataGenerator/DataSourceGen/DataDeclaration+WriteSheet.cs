using Microsoft.CodeAnalysis;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DataSourceGen
{
    partial class DataDeclaration
    {
        public string WriteSheet()
        {
            var p = Printer.DefaultLarge;
            var nsName = Symbol.ContainingNamespace.ToDisplayString();

            p.PrintLine("#pragma warning disable");
            p.PrintEndLine();

            p.PrintEndLine()
                .Print("#if UNITY_EDITOR")
                .PrintEndLine();

            p.PrintLine($"namespace {nsName}.Authoring");
            p.OpenScope();
            {

            }
            p.CloseScope();

            p.PrintEndLine()
                .Print("#endif")
                .PrintEndLine();

            return p.Result;
        }
    }
}