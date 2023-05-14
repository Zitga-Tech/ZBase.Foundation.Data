using Microsoft.CodeAnalysis;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DataSourceGen
{
    partial class DataDeclaration
    {
        public string WriteCode()
        {
            var keyword = Symbol.IsValueType ? "struct" : "class";

            var scopePrinter = new SyntaxNodeScopePrinter(Printer.DefaultLarge, Syntax.Parent);
            var p = scopePrinter.printer;
            p = p.IncreasedIndent();

            p.PrintEndLine();
            p.PrintLine("#pragma warning disable");
            p.PrintEndLine();

            p.PrintLine("[global::System.Serializable]");
            p.PrintBeginLine()
                .Print($"partial {keyword} ").Print(Syntax.Identifier.Text)
                .PrintEndLine();
            p.OpenScope();
            {
                foreach (var field in Fields)
                {
                    bool isArray;
                    string typeName;
                    var propertyName = field.ToPropertyName();

                    if (field.Type is IArrayTypeSymbol arrayType)
                    {
                        isArray = true;
                        typeName = $"global::System.ReadOnlyMemory<{arrayType.ElementType.ToFullName()}>";
                    }
                    else
                    {
                        isArray = false;
                        typeName = field.Type.ToFullName();
                    }

                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"public {typeName} {propertyName}");
                    p.OpenScope();
                    {
                        p.PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"get => this.{field.Name};");

                        if (!IsRuntimeImmutable && !isArray)
                        {
                            p.PrintEndLine();
                            p.PrintLine(AGGRESSIVE_INLINING);
                            p.PrintLine($"set => this.{field.Name} = value;");
                        }
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    if (IsRuntimeImmutable == false && isArray)
                    {
                        p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                        p.PrintLine($"public void Set{propertyName}({field.Type.ToFullName()} value)");
                        p.OpenScope();
                        {
                            p.PrintLine($"this.{field.Name} = value;");
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }
                }
            }
            p.CloseScope();

            p = p.DecreasedIndent();
            return p.Result;
        }
    }
}