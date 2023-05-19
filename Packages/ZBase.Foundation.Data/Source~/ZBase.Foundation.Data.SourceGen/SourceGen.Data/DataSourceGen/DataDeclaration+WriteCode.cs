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
                    if (field.PropertyIsImplemented)
                    {
                        continue;
                    }

                    var fieldName = field.Field.Name;
                    bool isCollection;
                    string typeName;

                    switch (field.CollectionKind)
                    {
                        case CollectionKind.Array:
                        {
                            isCollection = true;
                            typeName = $"global::System.ReadOnlyMemory<{field.CollectionElementType.ToFullName()}>";
                            break;
                        }

                        case CollectionKind.List:
                        {
                            isCollection = true;
                            typeName = $"global::System.Collections.Generic.IReadOnlyList<{field.CollectionElementType.ToFullName()}>";
                            break;
                        }

                        case CollectionKind.Dictionary:
                        {
                            isCollection = true;
                            typeName = $"global::System.Collections.Generic.IReadOnlyDictionary<{field.CollectionKeyType.ToFullName()}, {field.CollectionElementType.ToFullName()}>";
                            break;
                        }

                        default:
                        {
                            isCollection = false;
                            typeName = field.Type.ToFullName();
                            break;
                        }
                    }

                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"public {typeName} {field.PropertyName}");
                    p.OpenScope();
                    {
                        p.PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"get => this.{fieldName};");

                        if (IsMutable && !isCollection)
                        {
                            p.PrintEndLine();
                            p.PrintLine(AGGRESSIVE_INLINING);
                            p.PrintLine($"set => this.{fieldName} = value;");
                        }
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    if (IsMutable && isCollection)
                    {
                        p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                        p.PrintLine($"public void Set{field.PropertyName}({field.Type.ToFullName()} value)");
                        p.OpenScope();
                        {
                            p.PrintLine($"this.{fieldName} = value;");
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }
                }

                p.PrintEndLine().Print("#if UNITY_EDITOR").PrintEndLine();
                p.PrintEndLine();

                p.PrintLine("[global::System.Obsolete(\"This method is not intended to be used directly by user code.\")]");
                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine("internal void SetValues(");
                p = p.IncreasedIndent();
                {
                    for (var i = 0; i < Fields.Length; i++)
                    {
                        var field = Fields[i];
                        var comma = i == 0 ? " " : ",";
                        p.PrintLine($"{comma} {field.Type.ToFullName()} {field.Field.Name}");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    foreach (var field in Fields)
                    {
                        var fieldName = field.Field.Name;
                        p.PrintLine($"this.{fieldName} = {fieldName};");
                    }
                }
                p.CloseScope();
                p.PrintEndLine().Print("#endif").PrintEndLine();
            }
            p.CloseScope();

            p = p.DecreasedIndent();
            return p.Result;
        }
    }
}