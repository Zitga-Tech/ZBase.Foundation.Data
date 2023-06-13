using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DataSourceGen
{
    partial class DataDeclaration
    {
        private const string GENERATED_PROPERTY_FROM_FIELD_ATTRIBUTE = "[global::ZBase.Foundation.Data.SourceGen.GeneratedPropertyFromField(nameof({0}), typeof({1}))]";

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
                .Print($" : global::System.IEquatable<{Syntax.Identifier.Text}>")
                .PrintEndLine();
            p.OpenScope();
            {
                WriteProperties(ref p);
                WriteGetHashCodeMethod(ref p);
                WriteEqualsMethod(ref p);
                WriteIEquatableMethod(ref p);
                WriteSetValuesMethod(ref p);
            }
            p.CloseScope();

            p = p.DecreasedIndent();
            return p.Result;
        }

        private void WriteProperties(ref Printer p)
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

                    case CollectionKind.HashSet:
                    case CollectionKind.Queue:
                    case CollectionKind.Stack:
                    {
                        isCollection = true;
                        typeName = $"global::System.Collections.Generic.IReadOnlyCollection<{field.CollectionElementType.ToFullName()}>";
                        break;
                    }

                    default:
                    {
                        isCollection = false;
                        typeName = field.Type.ToFullName();
                        break;
                    }
                }

                p.PrintLine(string.Format(GENERATED_PROPERTY_FROM_FIELD_ATTRIBUTE, fieldName, field.Type.ToFullName()));
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

            p.PrintEndLine();
        }

        private void WriteGetHashCodeMethod(ref Printer p)
        {
            if (HasGetHashCodeMethod)
            {
                return;
            }

            p.PrintLine("public override int GetHashCode()");
            p.OpenScope();
            {
                p.PrintLine("var hash = new global::System.HashCode();");
                
                foreach (var field in Fields)
                {
                    p.PrintLine($"hash.Add({field.Field.Name});");
                }

                p.PrintLine("return hash.ToHashCode();");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteEqualsMethod(ref Printer p)
        {
            if (HasEqualsMethod)
            {
                return;
            }

            p.PrintLine("public override bool Equals(object obj)");
            p.OpenScope();
            {
                p.PrintLine($"return obj is {Syntax.Identifier.Text} other && Equals(other);");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteIEquatableMethod(ref Printer p)
        {
            if (HasIEquatableMethod)
            {
                return;
            }

            p.PrintLine($"public bool Equals({Syntax.Identifier.Text} other)");
            p.OpenScope();
            {
                if (Symbol.IsValueType == false)
                {
                    p.PrintLine("if (ReferenceEquals(other, null)) return false;");
                    p.PrintLine("if (ReferenceEquals(this, other)) return true;");
                    p.PrintEndLine();
                }

                p.PrintLine("return");
                p = p.IncreasedIndent();
                {
                    for (var i = 0; i < Fields.Length; i++)
                    {
                        var fieldName = Fields[i].Field.Name;
                        var fieldType = Fields[i].Type.ToFullName();
                        var and = i == 0 ? "  " : "&&";

                        p.PrintLine($"{and} global::System.Collections.Generic.EqualityComparer<{fieldType}>.Default.Equals(this.{fieldName}, other.{fieldName})");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(";");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteSetValuesMethod(ref Printer p)
        {
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
            p.PrintEndLine();
        }
    }
}