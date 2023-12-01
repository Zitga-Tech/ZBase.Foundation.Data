using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DataSourceGen
{
    partial class DataDeclaration
    {
        private const string GENERATED_PROPERTY_FROM_FIELD_ATTRIBUTE = "[global::ZBase.Foundation.Data.SourceGen.GeneratedPropertyFromField(nameof({0}), typeof({1}))]";
        private const string GENERATED_FIELD_FROM_PROPERTY_ATTRIBUTE = "[global::ZBase.Foundation.Data.SourceGen.GeneratedFieldFromProperty(nameof({0}))]";

        public string WriteCode()
        {
            var keyword = Symbol.IsValueType ? "struct" : "class";

            var scopePrinter = new SyntaxNodeScopePrinter(Printer.DefaultLarge, Syntax.Parent);
            var p = scopePrinter.printer;
            p = p.IncreasedIndent();

            p.PrintEndLine();
            p.Print("#pragma warning disable").PrintEndLine();
            p.PrintEndLine();

            p.PrintLine("[global::System.Serializable]");
            p.PrintBeginLine()
                .Print($"partial {keyword} ").Print(Syntax.Identifier.Text)
                .Print($" : global::System.IEquatable<{Syntax.Identifier.Text}>")
                .PrintEndLine();
            p.OpenScope();
            {
                WriteFields(ref p);
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

        private void WriteFields(ref Printer p)
        {
            foreach (var prop in PropRefs)
            {
                if (prop.FieldIsImplemented)
                {
                    continue;
                }

                var fieldName = prop.FieldName;
                var typeName = prop.Type.ToFullName();

                p.PrintLine(string.Format(GENERATED_FIELD_FROM_PROPERTY_ATTRIBUTE, prop.Property.Name));
                p.PrintLine(GENERATED_CODE);

                var withSerializeField = false;

                foreach (var (fullTypeName, attribute) in prop.ForwardedFieldAttributes)
                {
                    if (fullTypeName == SERIALIZE_FIELD_ATTRIBUTE)
                    {
                        withSerializeField = true;
                    }

                    p.PrintLine($"[{attribute.GetSyntax().ToFullString()}]");
                }

                if (withSerializeField == false && ReferenceUnityEngine)
                {
                    p.PrintLine($"[{SERIALIZE_FIELD_ATTRIBUTE}]");
                }

                p.PrintLine($"private {typeName} {prop.FieldName};");
                p.PrintEndLine();

                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine($"private {typeName} GetValue_{prop.Property.Name}()");
                p.OpenScope();
                {
                    p.PrintLine($"return this.{fieldName};");
                }
                p.CloseScope();
                p.PrintEndLine();

                if (IsMutable)
                {
                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine($"private void SetValue_{prop.Property.Name}({typeName} value)");
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

        private void WriteProperties(ref Printer p)
        {
            foreach (var field in FieldRefs)
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

                foreach (var attribute in field.ForwardedPropertyAttributes)
                {
                    p.PrintLine($"[{attribute.GetSyntax().ToFullString()}]");
                }

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
                
                foreach (var field in FieldRefs)
                {
                    p.PrintLine($"hash.Add({field.Field.Name});");
                }

                foreach (var prop in PropRefs)
                {
                    p.PrintLine($"hash.Add({prop.FieldName});");
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
                    var previous = false;

                    for (var i = 0; i < FieldRefs.Length; i++)
                    {
                        previous = true;
                        var fieldRef = FieldRefs[i];
                        var fieldName = fieldRef.Field.Name;
                        var fieldType = fieldRef.Type.ToFullName();
                        var and = i == 0 ? "  " : "&&";

                        p.PrintLine($"{and} global::System.Collections.Generic.EqualityComparer<{fieldType}>.Default.Equals(this.{fieldName}, other.{fieldName})");
                    }

                    for (var i = 0; i < PropRefs.Length; i++)
                    {
                        var propRef = PropRefs[i];
                        var fieldName = propRef.FieldName;
                        var fieldType = propRef.Type.ToFullName();
                        var and = i == 0 && previous == false ? "  " : "&&";

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
                var previous = false;

                for (var i = 0; i < FieldRefs.Length; i++)
                {
                    previous = true;
                    var fieldRef = FieldRefs[i];
                    var comma = i == 0 ? " " : ",";
                    p.PrintLine($"{comma} {fieldRef.Type.ToFullName()} {fieldRef.Field.Name}");
                }

                for (var i = 0; i < PropRefs.Length; i++)
                {
                    var propRef = PropRefs[i];
                    var comma = i == 0 && previous == false ? " " : ",";
                    p.PrintLine($"{comma} {propRef.Type.ToFullName()} {propRef.FieldName}");
                }
            }
            p = p.DecreasedIndent();
            p.PrintLine(")");
            p.OpenScope();
            {
                foreach (var field in FieldRefs)
                {
                    var fieldName = field.Field.Name;
                    p.PrintLine($"this.{fieldName} = {fieldName};");
                }

                foreach (var prop in PropRefs)
                {
                    var fieldName = prop.FieldName;
                    p.PrintLine($"this.{fieldName} = {fieldName};");
                }
            }
            p.CloseScope();
            p.PrintEndLine();
        }
    }
}