using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    partial class DataDeclaration
    {
        /// <param name="verticalListMap">
        /// TargetTypeFullName -> ContainingTypeFullName -> PropertyName (s)
        /// </param>
        public void WriteCode(
              ref Printer p
            , Dictionary<string, DataDeclaration> dataMap
            , Dictionary<string, Dictionary<string, HashSet<string>>> verticalListMap
            , string containingTypeFullName
            , ITypeSymbol idType = null
        )
        {
            var typeName = Symbol.Name;
            var typeFullName = FullName;
            var idTypeName = idType?.ToFullName() ?? string.Empty;

            p.PrintLine("[global::System.Serializable]");

            if (string.IsNullOrWhiteSpace(idTypeName) == false)
            {
                p.PrintLine($"[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedSheetRow(typeof({idTypeName}), typeof({typeFullName}))]");
            }
            else
            {
                p.PrintLine($"[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedDataRowAttribute(typeof({typeFullName}))]");
            }

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintBeginLine()
                .Print($"public partial class __{typeName}");

            if (string.IsNullOrWhiteSpace(idTypeName) == false)
            {
                p.Print(" : global::Cathei.BakingSheet.SheetRow");

                if (dataMap.ContainsKey(idTypeName))
                {
                    p.Print($"<__{idType.Name}>");
                }
                else
                {
                    p.Print($"<{idTypeName}>");
                }
            }

            p.PrintEndLine();
            p.OpenScope();
            {
                p.PrintLine(GENERATED_CODE);
                p.PrintLine($"public static readonly __{typeName} Default = new __{typeName}();");
                p.PrintEndLine();

                WriteConstructor(ref p, dataMap, verticalListMap, typeName, typeFullName, containingTypeFullName);

                var baseTypeRefs = this.BaseTypeRefs;

                for (var i = baseTypeRefs.Length - 1; i >= 0; i--)
                {
                    var typeRef = baseTypeRefs[i];
                    WriteProperties(ref p, dataMap, verticalListMap, typeFullName, containingTypeFullName, idType, typeRef.PropRefs);
                    WriteProperties(ref p, dataMap, verticalListMap, typeFullName, containingTypeFullName, idType, typeRef.FieldRefs);
                }

                WriteProperties(ref p, dataMap, verticalListMap, typeFullName, containingTypeFullName, idType, PropRefs);
                WriteProperties(ref p, dataMap, verticalListMap, typeFullName, containingTypeFullName, idType, FieldRefs);

                WriteConvertMethod(ref p, dataMap);
                WriteCopyFromMethod(ref p, dataMap);

                for (var i = baseTypeRefs.Length - 1; i >= 0; i--)
                {
                    var typeRef = baseTypeRefs[i];
                    WriteToCollectionMethod(ref p, dataMap, typeRef.PropRefs);
                    WriteToCollectionMethod(ref p, dataMap, typeRef.FieldRefs);
                }

                WriteToCollectionMethod(ref p, dataMap, PropRefs);
                WriteToCollectionMethod(ref p, dataMap, FieldRefs);
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteConstructor(
              ref Printer p
            , Dictionary<string, DataDeclaration> dataMap
            , Dictionary<string, Dictionary<string, HashSet<string>>> verticalListMap
            , string typeName
            , string targetTypeFullName
            , string containingTypeFullName
        )
        {
            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"public __{typeName}()");
            p.OpenScope();
            {
                var baseTypeRefs = this.BaseTypeRefs;

                for (var i = baseTypeRefs.Length - 1; i >= 0; i--)
                {
                    var typeRef = baseTypeRefs[i];
                    Write(ref p, dataMap, verticalListMap, typeRef.PropRefs, targetTypeFullName, containingTypeFullName);
                    Write(ref p, dataMap, verticalListMap, typeRef.FieldRefs, targetTypeFullName, containingTypeFullName);
                }

                Write(ref p, dataMap, verticalListMap, PropRefs, targetTypeFullName, containingTypeFullName);
                Write(ref p, dataMap, verticalListMap, FieldRefs, targetTypeFullName, containingTypeFullName);
            }
            p.CloseScope();
            p.PrintEndLine();

            static void Write(
                  ref Printer p
                , Dictionary<string, DataDeclaration> dataMap
                , Dictionary<string, Dictionary<string, HashSet<string>>> verticalListMap
                , ImmutableArray<MemberRef> memberRefs
                , string targetTypeFullName
                , string containingTypeFullName
            )
            {
                foreach (var memberRef in memberRefs)
                {
                    string newExpression;

                    switch (memberRef.CollectionKind)
                    {
                        case CollectionKind.Array:
                        case CollectionKind.List:
                        case CollectionKind.HashSet:
                        case CollectionKind.Queue:
                        case CollectionKind.Stack:
                        {
                            var collectionTypeName = LIST_TYPE_T;

                            if (memberRef.CollectionKind == CollectionKind.Array
                                && verticalListMap.TryGetValue(targetTypeFullName, out var innerMap)
                            )
                            {
                                if (innerMap.TryGetValue(containingTypeFullName, out var propertyNames)
                                    || innerMap.TryGetValue(string.Empty, out propertyNames)
                                )
                                {
                                    if (propertyNames.Contains(memberRef.PropertyName))
                                    {
                                        collectionTypeName = VERTICAL_LIST_TYPE;
                                    }
                                }
                            }

                            var elemTypeFullName = memberRef.CollectionElementType.ToFullName();
                            var elemTypeName = dataMap.ContainsKey(elemTypeFullName)
                                ? $"__{memberRef.CollectionElementType.Name}" : elemTypeFullName;

                            newExpression = $"new {collectionTypeName}{elemTypeName}>()";
                            break;
                        }

                        case CollectionKind.Dictionary:
                        {
                            var keyTypeFullName = memberRef.CollectionKeyType.ToFullName();
                            var elemTypeFullName = memberRef.CollectionElementType.ToFullName();

                            var keyTypeName = dataMap.ContainsKey(keyTypeFullName)
                                ? $"__{memberRef.CollectionKeyType.Name}" : keyTypeFullName;

                            var elemTypeName = dataMap.ContainsKey(elemTypeFullName)
                                ? $"__{memberRef.CollectionElementType.Name}" : elemTypeFullName;

                            newExpression = $"new {DICTIONARY_TYPE_T}{keyTypeName}, {elemTypeName}>()";
                            break;
                        }

                        default:
                        {
                            var fieldTypeFullName = memberRef.Type.ToFullName();

                            if (dataMap.ContainsKey(fieldTypeFullName))
                            {
                                newExpression = $"new __{memberRef.Type.Name}()";
                            }
                            else if (memberRef.Type.IsValueType)
                            {
                                newExpression = "default";
                            }
                            else if (fieldTypeFullName == "string")
                            {
                                newExpression = "string.Empty";
                            }
                            else if (memberRef.TypeHasParameterlessConstructor)
                            {
                                newExpression = $"new {fieldTypeFullName}()";
                            }
                            else
                            {
                                newExpression = "default";
                            }

                            break;
                        }
                    }

                    p.PrintLine($"this.{memberRef.PropertyName} = {newExpression};");
                }
            }
        }

        private static void WriteProperties(
              ref Printer p
            , Dictionary<string, DataDeclaration> dataMap
            , Dictionary<string, Dictionary<string, HashSet<string>>> verticalListMap
            , string targetTypeFullName
            , string containingTypeFullName
            , ITypeSymbol idType
            , ImmutableArray<MemberRef> memberRefs
        )
        {
            foreach (var memberRef in memberRefs)
            {
                if (idType != null && memberRef.PropertyName == "Id")
                {
                    continue;
                }

                string propTypeName;

                switch (memberRef.CollectionKind)
                {
                    case CollectionKind.Array:
                    case CollectionKind.List:
                    case CollectionKind.HashSet:
                    case CollectionKind.Queue:
                    case CollectionKind.Stack:
                    {
                        var collectionTypeName = LIST_TYPE_T;

                        if (memberRef.CollectionKind == CollectionKind.Array
                            && verticalListMap.TryGetValue(targetTypeFullName, out var innerMap)
                        )
                        {
                            if (innerMap.TryGetValue(containingTypeFullName, out var propertyNames)
                                || innerMap.TryGetValue(string.Empty, out propertyNames)
                            )
                            {
                                if (propertyNames.Contains(memberRef.PropertyName))
                                {
                                    collectionTypeName = VERTICAL_LIST_TYPE;
                                }
                            }
                        }

                        var elemTypeFullName = memberRef.CollectionElementType.ToFullName();
                        var elemTypeName = dataMap.ContainsKey(elemTypeFullName)
                            ? $"__{memberRef.CollectionElementType.Name}" : elemTypeFullName;

                        propTypeName = $"{collectionTypeName}{elemTypeName}>";
                        break;
                    }

                    case CollectionKind.Dictionary:
                    {
                        var keyTypeFullName = memberRef.CollectionKeyType.ToFullName();
                        var elemTypeFullName = memberRef.CollectionElementType.ToFullName();

                        var keyTypeName = dataMap.ContainsKey(keyTypeFullName)
                            ? $"__{memberRef.CollectionKeyType.Name}" : keyTypeFullName;

                        var elemTypeName = dataMap.ContainsKey(elemTypeFullName)
                            ? $"__{memberRef.CollectionElementType.Name}" : elemTypeFullName;

                        propTypeName = $"{DICTIONARY_TYPE_T}{keyTypeName}, {elemTypeName}>";
                        break;
                    }

                    default:
                    {
                        var fieldTypeFullName = memberRef.Type.ToFullName();
                        propTypeName = dataMap.ContainsKey(fieldTypeFullName)
                            ? $"__{memberRef.Type.Name}" : fieldTypeFullName;

                        break;
                    }
                }

                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine($"public {propTypeName} {memberRef.PropertyName} {{ get; private set; }}");
                p.PrintEndLine();
            }
        }

        private void WriteConvertMethod(
              ref Printer p
            , Dictionary<string, DataDeclaration> dataMap
        )
        {
            var typeName = Symbol.Name;
            var typeFullName = Symbol.ToFullName();

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"public {typeFullName} To{typeName}()");
            p.OpenScope();
            {
                p.PrintLine($"var result = new {typeFullName}();");
                p.PrintEndLine();

                var baseTypeRefs = this.BaseTypeRefs;

                for (var i = baseTypeRefs.Length - 1; i >= 0; i--)
                {
                    var typeRef = baseTypeRefs[i];
                    WriteType(ref p, typeRef, dataMap);
                    p.PrintEndLine();
                }

                WriteType(ref p, this, dataMap);
                p.PrintEndLine();

                p.PrintLine("return result;");
            }
            p.CloseScope();
            p.PrintEndLine();

            static void WriteType(
                  ref Printer p
                , DataDeclaration typeRef
                , Dictionary<string, DataDeclaration> dataMap
            )
            {
                p.PrintLine($"result.SetValues_{typeRef.Symbol.ToValidIdentifier()}(");
                p = p.IncreasedIndent();
                {
                    var first = true;

                    foreach (var memberRef in typeRef.PropRefs)
                    {
                        var comma = first ? " " : ",";

                        first = false;

                        Write(ref p, dataMap, memberRef, comma);
                    }

                    foreach (var memberRef in typeRef.FieldRefs)
                    {
                        var comma = first ? " " : ",";

                        first = false;

                        Write(ref p, dataMap, memberRef, comma);
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(");");
            }

            static void Write(
                  ref Printer p
                , Dictionary<string, DataDeclaration> dataMap
                , MemberRef memberRef
                , string comma
            )
            {
                switch (memberRef.CollectionKind)
                {
                    case CollectionKind.Array:
                    {
                        var elemTypeFullName = memberRef.CollectionElementType.ToFullName();

                        if (dataMap.ContainsKey(elemTypeFullName))
                        {
                            var methodName = GetToCollectionMethodName(memberRef, "Array");
                            p.PrintLine($"{comma} this.{methodName}()");
                        }
                        else
                        {
                            p.PrintLine($"{comma} this.{memberRef.PropertyName}?.ToArray() ?? global::System.Array.Empty<{elemTypeFullName}>()");
                        }
                        break;
                    }

                    case CollectionKind.List:
                    {
                        var elemTypeFullName = memberRef.CollectionElementType.ToFullName();

                        if (dataMap.ContainsKey(elemTypeFullName))
                        {
                            var methodName = GetToCollectionMethodName(memberRef, "List");
                            p.PrintLine($"{comma} this.{methodName}()");
                        }
                        else
                        {
                            p.PrintLine($"{comma} this.{memberRef.PropertyName} ?? new {LIST_TYPE_T}{elemTypeFullName}>()");
                        }
                        break;
                    }

                    case CollectionKind.Dictionary:
                    {
                        var keyTypeFullName = memberRef.CollectionKeyType.ToFullName();
                        var elemTypeFullName = memberRef.CollectionElementType.ToFullName();

                        if (dataMap.ContainsKey(keyTypeFullName) == false
                            && dataMap.ContainsKey(elemTypeFullName) == false
                        )
                        {
                            p.PrintLine($"{comma} this.{memberRef.PropertyName} ?? new {DICTIONARY_TYPE_T}{keyTypeFullName}, {elemTypeFullName}>()");
                        }
                        else
                        {
                            var methodName = GetToCollectionMethodName(memberRef, "Dictionary");
                            p.PrintLine($"{comma} this.{methodName}()");
                        }
                        break;
                    }

                    case CollectionKind.HashSet:
                    {
                        var elemTypeFullName = memberRef.CollectionElementType.ToFullName();

                        if (dataMap.ContainsKey(elemTypeFullName))
                        {
                            var methodName = GetToCollectionMethodName(memberRef, "HashSet");
                            p.PrintLine($"{comma} this.{methodName}()");
                        }
                        else
                        {
                            p.PrintLine($"{comma} this.{memberRef.PropertyName} == null ? new {HASH_SET_TYPE_T}{elemTypeFullName}>() : new {HASH_SET_TYPE_T}{elemTypeFullName}>(this.{memberRef.PropertyName})");
                        }
                        break;
                    }

                    case CollectionKind.Queue:
                    {
                        var elemTypeFullName = memberRef.CollectionElementType.ToFullName();

                        if (dataMap.ContainsKey(elemTypeFullName))
                        {
                            var methodName = GetToCollectionMethodName(memberRef, "Queue");
                            p.PrintLine($"{comma} this.{methodName}()");
                        }
                        else
                        {
                            p.PrintLine($"{comma} this.{memberRef.PropertyName} == null ? new {QUEUE_TYPE_T}{elemTypeFullName}>() : new {QUEUE_TYPE_T}{elemTypeFullName}>(this.{memberRef.PropertyName})");
                        }
                        break;
                    }

                    case CollectionKind.Stack:
                    {
                        var elemTypeFullName = memberRef.CollectionElementType.ToFullName();

                        if (dataMap.ContainsKey(elemTypeFullName))
                        {
                            var methodName = GetToCollectionMethodName(memberRef, "Stack");
                            p.PrintLine($"{comma} this.{methodName}()");
                        }
                        else
                        {
                            p.PrintLine($"{comma} this.{memberRef.PropertyName} == null ? new {STACK_TYPE_T}{elemTypeFullName}>() : new {STACK_TYPE_T}{elemTypeFullName}>(this.{memberRef.PropertyName})");
                        }
                        break;
                    }

                    default:
                    {
                        var fieldTypeFullName = memberRef.Type.ToFullName();

                        if (dataMap.ContainsKey(fieldTypeFullName))
                        {
                            p.PrintLine($"{comma} (this.{memberRef.PropertyName} ?? __{memberRef.Type.Name}.Default).To{memberRef.Type.Name}()");
                        }
                        else
                        {
                            var newExpression = memberRef.Type.IsValueType
                                    ? "" : memberRef.TypeHasParameterlessConstructor
                                    ? $" ?? new {fieldTypeFullName}()" : " ?? default";

                            p.PrintLine($"{comma} this.{memberRef.PropertyName}{newExpression}");
                        }
                        break;
                    }
                }
            }
        }

        private void WriteCopyFromMethod(
              ref Printer p
            , Dictionary<string, DataDeclaration> dataMap
        )
        {
            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"public void CopyFrom({Symbol.ToFullName()} data)");
            p.OpenScope();
            {
                var baseTypeRefs = this.BaseTypeRefs;

                for (var i = baseTypeRefs.Length - 1; i >= 0; i--)
                {
                    var typeRef = baseTypeRefs[i];
                    WriteType(ref p, typeRef, dataMap);
                    p.PrintEndLine();
                }

                WriteType(ref p, this, dataMap);
            }
            p.CloseScope();
            p.PrintEndLine();

            static void WriteType(
                  ref Printer p
                , DataDeclaration typeRef
                , Dictionary<string, DataDeclaration> dataMap
            )
            {
                foreach (var memberRef in typeRef.PropRefs)
                {
                    Write(ref p, dataMap, memberRef);
                }

                foreach (var memberRef in typeRef.FieldRefs)
                {
                    Write(ref p, dataMap, memberRef);
                }
            }

            static void Write(
                  ref Printer p
                , Dictionary<string, DataDeclaration> dataMap
                , MemberRef memberRef
            )
            {
                switch (memberRef.CollectionKind)
                {
                    case CollectionKind.Array:
                    {
                        var elemTypeFullName = memberRef.CollectionElementType.ToFullName();

                        if (dataMap.ContainsKey(elemTypeFullName))
                        {
                            p.PrintEndLine();
                            p.PrintLine($"foreach (var item in data.{memberRef.PropertyName}.ToArray())");
                            p.OpenScope();
                            {
                                p.PrintLine($"var elem = new __{memberRef.CollectionElementType.Name}();");
                                p.PrintLine("elem.CopyFrom(item);");
                                p.PrintLine($"this.{memberRef.PropertyName}.Add(elem);");
                            }
                            p.CloseScope();
                        }
                        else
                        {
                            p.PrintLine($"this.{memberRef.PropertyName}.AddRange(data.{memberRef.PropertyName}.Span.ToArray());");
                        }
                        break;
                    }

                    case CollectionKind.List:
                    case CollectionKind.HashSet:
                    case CollectionKind.Queue:
                    case CollectionKind.Stack:
                    {
                        var elemTypeFullName = memberRef.CollectionElementType.ToFullName();

                        if (dataMap.ContainsKey(elemTypeFullName))
                        {
                            p.PrintEndLine();
                            p.PrintLine($"foreach (var item in data.{memberRef.PropertyName})");
                            p.OpenScope();
                            {
                                p.PrintLine($"var elem = new __{memberRef.CollectionElementType.Name}();");
                                p.PrintLine("elem.CopyFrom(item);");
                                p.PrintLine($"this.{memberRef.PropertyName}.Add(elem);");
                            }
                            p.CloseScope();
                        }
                        else
                        {
                            p.PrintLine($"this.{memberRef.PropertyName}.AddRange(data.{memberRef.PropertyName});");
                        }
                        break;
                    }

                    case CollectionKind.Dictionary:
                    {
                        var keyTypeFullName = memberRef.CollectionKeyType.ToFullName();
                        var elemTypeFullName = memberRef.CollectionElementType.ToFullName();
                        var keyIsData = dataMap.ContainsKey(keyTypeFullName);
                        var elemIsData = dataMap.ContainsKey(elemTypeFullName);

                        p.PrintEndLine();
                        p.PrintLine($"foreach (var kv in data.{memberRef.PropertyName})");
                        p.OpenScope();
                        {
                            if (keyIsData == false && elemIsData == false)
                            {
                                p.PrintLine($"this.{memberRef.PropertyName}[kv.Key] = kv.Value;");
                            }
                            else
                            {
                                if (keyIsData)
                                {
                                    p.PrintEndLine();
                                    p.PrintLine($"var key = new __{memberRef.CollectionKeyType.Name}();");
                                    p.PrintLine("key.CopyFrom(kv.Key);");
                                }
                                else
                                {
                                    p.PrintLine($"var key = kv.Key;");
                                }

                                if (elemIsData)
                                {
                                    p.PrintEndLine();
                                    p.PrintLine($"var elem = new __{memberRef.CollectionElementType.Name}();");
                                    p.PrintLine("elem.CopyFrom(kv.Value);");
                                }
                                else
                                {
                                    p.PrintLine($"var elem = kv.Value;");
                                }

                                p.PrintLine($"this.{memberRef.PropertyName}[key] = elem;");
                            }
                        }
                        p.CloseScope();
                        break;
                    }

                    default:
                    {
                        var fieldTypeFullName = memberRef.Type.ToFullName();

                        if (dataMap.ContainsKey(fieldTypeFullName))
                        {
                            p.PrintLine($"this.{memberRef.PropertyName}.CopyFrom(data.{memberRef.PropertyName});");
                        }
                        else
                        {
                            p.PrintLine($"this.{memberRef.PropertyName} = data.{memberRef.PropertyName};");
                        }
                        break;
                    }
                }
            }
        }

        private static void WriteToCollectionMethod(
              ref Printer p
            , Dictionary<string, DataDeclaration> dataMap
            , ImmutableArray<MemberRef> memberRefs
        )
        {
            foreach (var memberRef in memberRefs)
            {
                switch (memberRef.CollectionKind)
                {
                    case CollectionKind.Array:
                    {
                        WriteToArrayMethod(ref p, memberRef, dataMap);
                        break;
                    }

                    case CollectionKind.List:
                    {
                        WriteToListMethod(ref p, memberRef, dataMap);
                        break;
                    }

                    case CollectionKind.Dictionary:
                    {
                        WriteToDictionaryMethod(ref p, memberRef, dataMap);
                        break;
                    }

                    case CollectionKind.HashSet:
                    {
                        WriteToHashSetMethod(ref p, memberRef, dataMap);
                        break;
                    }

                    case CollectionKind.Queue:
                    {
                        WriteToQueueMethod(ref p, memberRef, dataMap);
                        break;
                    }

                    case CollectionKind.Stack:
                    {
                        WriteToStackMethod(ref p, memberRef, dataMap);
                        break;
                    }
                }
            }
        }

        private static void WriteToArrayMethod(ref Printer p, MemberRef field, Dictionary<string, DataDeclaration> dataMap)
        {
            var elemTypeFullName = field.CollectionElementType.ToFullName();

            if (dataMap.ContainsKey(elemTypeFullName) == false)
            {
                return;
            }

            var elemTypeName = field.CollectionElementType.Name;
            var methodName = GetToCollectionMethodName(field, "Array");

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"private {elemTypeFullName}[] {methodName}()");
            p.OpenScope();
            {
                p.PrintLine($"if (this.{field.PropertyName} == null || this.{field.PropertyName}.Count == 0)");
                p = p.IncreasedIndent();
                p.PrintLine($"return global::System.Array.Empty<{elemTypeFullName}>();");
                p = p.DecreasedIndent();
                p.PrintEndLine();

                p.PrintLine($"var rows = this.{field.PropertyName};");
                p.PrintLine("var count = rows.Count;");
                p.PrintLine($"var result = new {elemTypeFullName}[count];");
                p.PrintEndLine();

                p.PrintLine("for (var i = 0; i < count; i++)");
                p.OpenScope();
                {
                    p.PrintLine($"result[i] = (rows[i] ?? __{elemTypeName}.Default).To{elemTypeName}();");
                }
                p.CloseScope();

                p.PrintEndLine();
                p.PrintLine("return result;");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private static void WriteToListMethod(ref Printer p, MemberRef field, Dictionary<string, DataDeclaration> dataMap)
        {
            var elemTypeFullName = field.CollectionElementType.ToFullName();

            if (dataMap.ContainsKey(elemTypeFullName) == false)
            {
                return;
            }

            var elemTypeName = field.CollectionElementType.Name;
            var methodName = GetToCollectionMethodName(field, "List");

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"private {LIST_TYPE_T}{elemTypeFullName}> {methodName}()");
            p.OpenScope();
            {
                p.PrintLine($"if (this.{field.PropertyName} == null || this.{field.PropertyName}.Count == 0)");
                p = p.IncreasedIndent();
                p.PrintLine($"return new {LIST_TYPE_T}{elemTypeFullName}>();");
                p = p.DecreasedIndent();
                p.PrintEndLine();

                p.PrintLine($"var rows = this.{field.PropertyName};");
                p.PrintLine("var count = rows.Count;");
                p.PrintLine($"var result = new {LIST_TYPE_T}{elemTypeFullName}>(count);");
                p.PrintEndLine();

                p.PrintLine("for (var i = 0; i < count; i++)");
                p.OpenScope();
                {
                    p.PrintLine($"var item = (rows[i] ?? __{elemTypeName}.Default).To{elemTypeName}();");
                    p.PrintLine("result.Add(item);");
                }
                p.CloseScope();

                p.PrintEndLine();
                p.PrintLine("return result;");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private static void WriteToDictionaryMethod(ref Printer p, MemberRef field, Dictionary<string, DataDeclaration> dataMap)
        {
            var keyTypeFullName = field.CollectionKeyType.ToFullName();
            var elemTypeFullName = field.CollectionElementType.ToFullName();
            var keyIsData = dataMap.ContainsKey(keyTypeFullName);
            var elemIsData = dataMap.ContainsKey(elemTypeFullName);

            if (keyIsData == false && elemIsData == false)
            {
                return;
            }

            var keyTypeName = field.CollectionKeyType.Name;
            var elemTypeName = field.CollectionElementType.Name;
            var methodName = GetToCollectionMethodName(field, "Dictionary");

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"private {DICTIONARY_TYPE_T}{keyTypeFullName}, {elemTypeFullName}> {methodName}()");
            p.OpenScope();
            {
                p.PrintLine($"if (this.{field.PropertyName} == null || this.{field.PropertyName}.Count == 0)");
                p = p.IncreasedIndent();
                p.PrintLine($"return new {DICTIONARY_TYPE_T}{keyTypeFullName}, {elemTypeFullName}>();");
                p = p.DecreasedIndent();
                p.PrintEndLine();

                p.PrintLine($"var rows = this.{field.PropertyName};");
                p.PrintLine("var count = rows.Count;");
                p.PrintLine($"var result = new {DICTIONARY_TYPE_T}{keyTypeFullName}, {elemTypeFullName}>(count);");
                p.PrintEndLine();

                p.PrintLine("foreach (var kv in rows)");
                p.OpenScope();
                {
                    if (keyIsData)
                    {
                        p.PrintLine($"var key = (kv.Key ?? __{keyTypeName}.Default).To{keyTypeName}();");
                    }
                    else
                    {
                        p.PrintLine("var key = kv.Key;");
                    }

                    if (elemIsData)
                    {
                        p.PrintLine($"var value = (kv.Value ?? __{elemTypeName}.Default).To{elemTypeName}();");
                    }
                    else
                    {
                        p.PrintLine("var value = kv.Value;");
                    }

                    p.PrintEndLine();
                    p.PrintLine("result[key] = value;");
                }
                p.CloseScope();

                p.PrintEndLine();
                p.PrintLine("return result;");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private static void WriteToHashSetMethod(ref Printer p, MemberRef field, Dictionary<string, DataDeclaration> dataMap)
        {
            var elemTypeFullName = field.CollectionElementType.ToFullName();

            if (dataMap.ContainsKey(elemTypeFullName) == false)
            {
                return;
            }

            var elemTypeName = field.CollectionElementType.Name;
            var methodName = GetToCollectionMethodName(field, "HashSet");

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"private {HASH_SET_TYPE_T}{elemTypeFullName}> {methodName}()");
            p.OpenScope();
            {
                p.PrintLine($"if (this.{field.PropertyName} == null || this.{field.PropertyName}.Count == 0)");
                p = p.IncreasedIndent();
                p.PrintLine($"return new {HASH_SET_TYPE_T}{elemTypeFullName}>();");
                p = p.DecreasedIndent();
                p.PrintEndLine();

                p.PrintLine($"var rows = this.{field.PropertyName};");
                p.PrintLine("var count = rows.Count;");
                p.PrintLine($"var result = new {HASH_SET_TYPE_T}{elemTypeFullName}>(count);");
                p.PrintEndLine();

                p.PrintLine("for (var i = 0; i < count; i++)");
                p.OpenScope();
                {
                    p.PrintLine($"var item = (rows[i] ?? __{elemTypeName}.Default).To{elemTypeName}();");
                    p.PrintLine("result.Add(item);");
                }
                p.CloseScope();

                p.PrintEndLine();
                p.PrintLine("return result;");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private static void WriteToQueueMethod(ref Printer p, MemberRef field, Dictionary<string, DataDeclaration> dataMap)
        {
            var elemTypeFullName = field.CollectionElementType.ToFullName();

            if (dataMap.ContainsKey(elemTypeFullName) == false)
            {
                return;
            }

            var elemTypeName = field.CollectionElementType.Name;
            var methodName = GetToCollectionMethodName(field, "Queue");

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"private {QUEUE_TYPE_T}{elemTypeFullName}> {methodName}()");
            p.OpenScope();
            {
                p.PrintLine($"if (this.{field.PropertyName} == null || this.{field.PropertyName}.Count == 0)");
                p = p.IncreasedIndent();
                p.PrintLine($"return new {QUEUE_TYPE_T}{elemTypeFullName}>();");
                p = p.DecreasedIndent();
                p.PrintEndLine();

                p.PrintLine($"var rows = this.{field.PropertyName};");
                p.PrintLine("var count = rows.Count;");
                p.PrintLine($"var result = new {QUEUE_TYPE_T}{elemTypeFullName}>(count);");
                p.PrintEndLine();

                p.PrintLine("for (var i = 0; i < count; i++)");
                p.OpenScope();
                {
                    p.PrintLine($"var item = (rows[i] ?? __{elemTypeName}.Default).To{elemTypeName}();");
                    p.PrintLine("result.Enqueue(item);");
                }
                p.CloseScope();

                p.PrintEndLine();
                p.PrintLine("return result;");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private static void WriteToStackMethod(ref Printer p, MemberRef field, Dictionary<string, DataDeclaration> dataMap)
        {
            var elemTypeFullName = field.CollectionElementType.ToFullName();

            if (dataMap.ContainsKey(elemTypeFullName) == false)
            {
                return;
            }

            var elemTypeName = field.CollectionElementType.Name;
            var methodName = GetToCollectionMethodName(field, "Stack");

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"private {STACK_TYPE_T}{elemTypeFullName}> {methodName}()");
            p.OpenScope();
            {
                p.PrintLine($"if (this.{field.PropertyName} == null || this.{field.PropertyName}.Count == 0)");
                p = p.IncreasedIndent();
                p.PrintLine($"return new {STACK_TYPE_T}{elemTypeFullName}>();");
                p = p.DecreasedIndent();
                p.PrintEndLine();

                p.PrintLine($"var rows = this.{field.PropertyName};");
                p.PrintLine("var count = rows.Count;");
                p.PrintLine($"var result = new {STACK_TYPE_T}{elemTypeFullName}>(count);");
                p.PrintEndLine();

                p.PrintLine("for (var i = 0; i < count; i++)");
                p.OpenScope();
                {
                    p.PrintLine($"var item = (rows[i] ?? __{elemTypeName}.Default).To{elemTypeName}();");
                    p.PrintLine("result.Push(item);");
                }
                p.CloseScope();

                p.PrintEndLine();
                p.PrintLine("return result;");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private static string GetToCollectionMethodName(MemberRef field, string collectionName)
        {
            var elemTypeName = field.CollectionElementType.Name;
            return $"To{elemTypeName}{collectionName}For{field.PropertyName}";
        }
    }
}
