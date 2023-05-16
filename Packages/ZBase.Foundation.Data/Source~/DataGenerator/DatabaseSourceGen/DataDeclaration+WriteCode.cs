using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    partial class DataDeclaration
    {
        public void WriteCode(ref Printer p, Dictionary<string, DataDeclaration> dataMap, ITypeSymbol idType = null)
        {
            var typeName = Syntax.Identifier.Text;
            var typeFullName = Symbol.ToFullName();

            if (idType != null)
            {
                p.PrintLine($"[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedSheetRow(typeof({idType.ToFullName()}), typeof({typeFullName}))]");
            }
            else
            {
                p.PrintLine($"[global::ZBase.Foundation.Data.Authoring.SourceGen.GeneratedDataRowAttribute(typeof({typeFullName}))]");
            }

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintBeginLine()
                .Print($"public partial class __{typeName}");

            if (idType != null)
            {
                var idTypeName = idType.ToFullName();

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
                WriteProperties(ref p, dataMap, idType);
                WriteConvertMethod(ref p, dataMap);
                WriteConvertArrayMethod(ref p, dataMap);
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteProperties(ref Printer p, Dictionary<string, DataDeclaration> dataMap, ITypeSymbol idType)
        {
            foreach (var field in Fields)
            {
                if (idType != null && field.PropertyName == "Id")
                {
                    continue;
                }

                string propTypeName;

                if (field.IsArray == false)
                {
                    var fieldTypeFullName = field.Type.ToFullName();
                    propTypeName = dataMap.ContainsKey(fieldTypeFullName)
                        ? $"__{field.Type.Name}" : fieldTypeFullName;
                }
                else
                {
                    var arrayTypeName = field.IsVerticalArray ? VERTICAL_LIST_TYPE : LIST_TYPE;
                    var elemTypeFullName = field.ArrayElementType.ToFullName();
                    var elemTypeName = dataMap.ContainsKey(elemTypeFullName)
                        ? $"__{field.ArrayElementType.Name}" : elemTypeFullName;

                    propTypeName = $"{arrayTypeName}<{elemTypeName}>";
                }

                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine($"public {propTypeName} {field.PropertyName} {{ get; set; }}");
                p.PrintEndLine();
            }
        }

        private void WriteConvertMethod(ref Printer p, Dictionary<string, DataDeclaration> dataMap)
        {
            var typeName = Syntax.Identifier.Text;
            var typeFullName = Symbol.ToFullName();

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"public {typeFullName} To{typeName}()");
            p.OpenScope();
            {
                p.PrintLine($"var result = new {typeFullName}();");
                p.PrintEndLine();

                p.PrintLine("result.SetValues(");
                p = p.IncreasedIndent();
                {
                    var first = true;

                    foreach (var field in Fields)
                    {
                        var comma = first ? " " : ",";
                        first = false;

                        if (field.IsArray)
                        {
                            var elemTypeFullName = field.ArrayElementType.ToFullName();

                            if (dataMap.ContainsKey(elemTypeFullName))
                            {
                                p.PrintLine($"{comma} this.To{field.ArrayElementType.Name}Array()");
                            }
                            else
                            {
                                p.PrintLine($"{comma} this.{field.PropertyName}.ToArray()");
                            }
                        }
                        else
                        {
                            var fieldTypeFullName = field.Type.ToFullName();

                            if (dataMap.ContainsKey(fieldTypeFullName))
                            {
                                p.PrintLine($"{comma} this.{field.PropertyName}.To{field.Type.Name}()");
                            }
                            else
                            {
                                p.PrintLine($"{comma} this.{field.PropertyName}");
                            }
                        }
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(");");

                p.PrintEndLine();
                p.PrintLine("return result;");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteConvertArrayMethod(ref Printer p, Dictionary<string, DataDeclaration> dataMap)
        {
            foreach (var field in Fields)
            {
                if (field.IsArray == false)
                {
                    continue;
                }

                var elemTypeFullName = field.ArrayElementType.ToFullName();

                if (dataMap.ContainsKey(elemTypeFullName) == false)
                {
                    continue;
                }

                var elemTypeName = field.ArrayElementType.Name;

                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine($"private {elemTypeFullName}[] To{elemTypeName}Array()");
                p.OpenScope();
                {
                    p.PrintLine($"var rows = this.{field.PropertyName};");
                    p.PrintLine("var count = rows.Count;");
                    p.PrintLine($"var result = new {elemTypeFullName}[count];");
                    p.PrintEndLine();

                    p.PrintLine("for (var i = 0; i < count; i++)");
                    p.OpenScope();
                    {
                        p.PrintLine($"result[i] = rows[i].To{elemTypeName}();");
                    }
                    p.CloseScope();

                    p.PrintEndLine();
                    p.PrintLine("return result;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }
        }
    }
}
