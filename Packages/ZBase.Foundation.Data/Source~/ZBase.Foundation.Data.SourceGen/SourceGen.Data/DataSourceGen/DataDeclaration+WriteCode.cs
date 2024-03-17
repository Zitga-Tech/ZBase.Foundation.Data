﻿using ZBase.Foundation.SourceGen;
using static ZBase.Foundation.Data.DataSourceGen.Helpers;

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
            p.Print("#pragma warning disable").PrintEndLine();
            p.PrintEndLine();

            p.PrintLine("[global::System.Serializable]");
            p.PrintBeginLine()
                .Print($"partial {keyword} ").Print(ClassName)
                .Print(" : ")
                .PrintIf(HasBaseType, $"{BaseTypeName}, ")
                .Print($"global::System.IEquatable<{ClassName}>")
                .PrintEndLine();
            p.OpenScope();
            {
                WriteFields(ref p);
                WriteProperties(ref p);
                WriteGetHashCodeMethod(ref p);
                WriteGetHashCodeInternalMethod(ref p);
                WriteEqualsMethod(ref p);
                WriteIEquatableMethod(ref p);
                
                if (Symbol.IsValueType == false)
                {
                    WriteEqualsInternalMethod(ref p);
                    WriteOverrideIEquatableMethod(ref p);
                }

                WriteSetValues_TypeMethod(ref p);
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

                var typeName = GetPropertyTypeName(prop);

                p.PrintLine($"private {typeName} {prop.FieldName};");
                p.PrintEndLine();

                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine($"private {typeName} Get_{prop.Property.Name}()");
                p.OpenScope();
                {
                    p.PrintLine($"return this.{fieldName};");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine($"private void Set_{prop.Property.Name}({typeName} value)");
                p.OpenScope();
                {
                    p.PrintLine($"this.{fieldName} = value;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }
        }

        private static string GetPropertyTypeName(PropertyRef prop)
        {
            switch (prop.CollectionKind)
            {
                case CollectionKind.ReadOnlyMemory:
                case CollectionKind.Memory:
                case CollectionKind.ReadOnlySpan:
                case CollectionKind.Span:
                {
                    return $"{prop.CollectionElementType.ToFullName()}[]";
                }

                case CollectionKind.ReadOnlyList:
                {
                    return $"{LIST_TYPE_T}{prop.CollectionElementType.ToFullName()}>";
                }

                case CollectionKind.ReadOnlyDictionary:
                {
                    var keyType = prop.CollectionKeyType.ToFullName();
                    var valueType = prop.CollectionElementType.ToFullName();
                    return $"{DICTIONARY_TYPE_T}{keyType}, {valueType}>";
                }

                default:
                {
                    return prop.Type.ToFullName();
                }
            }
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
                string typeName;

                switch (field.CollectionKind)
                {
                    case CollectionKind.Array:
                    {
                        typeName = IsMutable
                            ? $"{field.CollectionElementType.ToFullName()}[]"
                            : $"global::System.ReadOnlyMemory<{field.CollectionElementType.ToFullName()}>";
                        break;
                    }

                    case CollectionKind.List:
                    {
                        typeName = IsMutable
                            ? $"global::System.Collections.Generic.List<{field.CollectionElementType.ToFullName()}>"
                            : $"global::System.Collections.Generic.IReadOnlyList<{field.CollectionElementType.ToFullName()}>";
                        break;
                    }

                    case CollectionKind.Dictionary:
                    {
                        typeName = IsMutable
                            ? $"global::System.Collections.Generic.Dictionary<{field.CollectionKeyType.ToFullName()}, {field.CollectionElementType.ToFullName()}>"
                            : $"global::System.Collections.Generic.IReadOnlyDictionary<{field.CollectionKeyType.ToFullName()}, {field.CollectionElementType.ToFullName()}>";
                        break;
                    }

                    case CollectionKind.HashSet:
                    {
                        typeName = IsMutable
                            ? $"global::System.Collections.Generic.HashSet<{field.CollectionElementType.ToFullName()}>"
                            : $"global::System.Collections.Generic.IReadOnlyCollection<{field.CollectionElementType.ToFullName()}>";
                        break;
                    }

                    case CollectionKind.Queue:
                    {
                        typeName = IsMutable
                            ? $"global::System.Collections.Generic.Queue<{field.CollectionElementType.ToFullName()}>"
                            : $"global::System.Collections.Generic.IReadOnlyCollection<{field.CollectionElementType.ToFullName()}>";
                        break;
                    }

                    case CollectionKind.Stack:
                    {
                        typeName = IsMutable
                            ? $"global::System.Collections.Generic.Stack<{field.CollectionElementType.ToFullName()}>"
                            : $"global::System.Collections.Generic.IReadOnlyCollection<{field.CollectionElementType.ToFullName()}>";
                        break;
                    }

                    default:
                    {
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

                    if (IsMutable)
                    {
                        p.PrintEndLine();
                        p.PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"set => this.{fieldName} = value;");
                    }
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            p.PrintEndLine();
        }

        private void WriteGetHashCodeMethod(ref Printer p)
        {
            if (HasGetHashCodeMethod)
            {
                return;
            }

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine("public override int GetHashCode()");
            p.OpenScope();
            {
                p.PrintLine("var hash = GetHashCodeInternal();");
                p.PrintLine("return hash.ToHashCode();");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteGetHashCodeInternalMethod(ref Printer p)
        {
            var fromBase = false;

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);

            if (HasBaseType == false && IsSealed == false)
            {
                p.PrintBeginLine("protected virtual ");
            }
            else if (HasBaseType == true)
            {
                fromBase = true;
                p.PrintBeginLine("protected override ");
            }
            else
            {
                p.PrintBeginLine("private ");
            }

            p.PrintEndLine("global::System.HashCode GetHashCodeInternal()");
            p.OpenScope();
            {
                p.PrintBeginLine("var hash = ")
                    .PrintIf(fromBase, "base.GetHashCodeInternal()", "new global::System.HashCode()")
                    .PrintEndLine(";");

                foreach (var field in FieldRefs)
                {
                    p.PrintLine($"hash.Add({field.Field.Name});");
                }

                foreach (var prop in PropRefs)
                {
                    p.PrintLine($"hash.Add({prop.FieldName});");
                }

                p.PrintLine("return hash;");
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

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine("public override bool Equals(object obj)");
            p.OpenScope();
            {
                p.PrintLine($"return obj is {ClassName} other && Equals(other);");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteOverrideIEquatableMethod(ref Printer p)
        {
            foreach (var typeName in OverrideEquals)
            {
                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine($"public override bool Equals({typeName} other)");
                p.OpenScope();
                {
                    p.PrintLine($"if (other is not {ClassName} otherDerived) return false;");
                    p.PrintLine("if (ReferenceEquals(this, otherDerived)) return true;");
                    p.PrintEndLine();

                    p.PrintLine("return this.EqualsInternal(otherDerived);");
                }
                p.CloseScope();
                p.PrintEndLine();
            }
        }

        private void WriteIEquatableMethod(ref Printer p)
        {
            if (HasIEquatableMethod)
            {
                return;
            }

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintBeginLineIf(IsSealed, "public ", "public virtual ");
            p.PrintEndLine($"bool Equals({ClassName} other)");
            p.OpenScope();
            {
                if (Symbol.IsValueType)
                {
                    p.PrintLine("return");
                    WriteEqualComparerLines(ref p, false);
                }
                else
                {
                    p.PrintLine("if (ReferenceEquals(other, null)) return false;");
                    p.PrintLine("if (ReferenceEquals(this, other)) return true;");
                    p.PrintEndLine();
                    p.PrintLine("return EqualsInternal(other);");
                }
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteEqualsInternalMethod(ref Printer p)
        {
            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintBeginLineIf(IsSealed, "private ", "protected ")
                .PrintEndLine($"bool EqualsInternal({ClassName} other)");
            p.OpenScope();
            {
                p.PrintBeginLine("return")
                    .PrintEndLineIf(HasBaseType, " base.EqualsInternal(other)", "");

                WriteEqualComparerLines(ref p, HasBaseType);
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteEqualComparerLines(ref Printer p, bool previous)
        {
            p = p.IncreasedIndent();
            {
                for (var i = 0; i < FieldRefs.Length; i++)
                {
                    var fieldRef = FieldRefs[i];
                    var fieldName = fieldRef.Field.Name;
                    var fieldType = fieldRef.Type.ToFullName();
                    var and = i == 0 && previous == false ? "  " : "&&";
                    previous = true;

                    p.PrintLine($"{and} global::System.Collections.Generic.EqualityComparer<{fieldType}>.Default.Equals(this.{fieldName}, other.{fieldName})");
                }

                for (var i = 0; i < PropRefs.Length; i++)
                {
                    var propRef = PropRefs[i];
                    var fieldName = propRef.FieldName;
                    var fieldType = GetPropertyTypeName(propRef);
                    var and = i == 0 && previous == false ? "  " : "&&";
                    previous = true;

                    p.PrintLine($"{and} global::System.Collections.Generic.EqualityComparer<{fieldType}>.Default.Equals(this.{fieldName}, other.{fieldName})");
                }
            }
            p = p.DecreasedIndent();
            p.PrintLine(";");
        }

        private void WriteSetValues_TypeMethod(ref Printer p)
        {
            p.PrintLine("[global::System.Obsolete(\"This method is not intended to be used directly by user code.\")]");
            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"internal void SetValues_{Symbol.ToValidIdentifier()}(");
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
                    p.PrintLine($"{comma} {GetPropertyTypeName(propRef)} {propRef.FieldName}");
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