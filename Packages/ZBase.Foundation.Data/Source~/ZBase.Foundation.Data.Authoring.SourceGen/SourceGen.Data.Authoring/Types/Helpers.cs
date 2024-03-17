using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public static class Helpers
    {
        public const string GENERATOR_NAME = nameof(DatabaseGenerator);

        public const string IDATA = "global::ZBase.Foundation.Data.IData";
        public const string DATA_TABLE_ASSET_T = "global::ZBase.Foundation.Data.DataTableAsset<";
        public const string DATABASE_ATTRIBUTE = "global::ZBase.Foundation.Data.Authoring.DatabaseAttribute";
        public const string TABLE_ATTRIBUTE = "global::ZBase.Foundation.Data.Authoring.TableAttribute";
        public const string VERTICAL_LIST_ATTRIBUTE = "global::ZBase.Foundation.Data.Authoring.VerticalListAttribute";

        public const string DATA_PROPERTY_ATTRIBUTE = "global::ZBase.Foundation.Data.DataPropertyAttribute";
        public const string DATA_CONVERTER_ATTRIBUTE = "global::ZBase.Foundation.Data.DataConverterAttribute";
        public const string SERIALIZE_FIELD_ATTRIBUTE = "global::UnityEngine.SerializeField";
        public const string JSON_INCLUDE_ATTRIBUTE = "global::System.Text.Json.Serialization.JsonIncludeAttribute";
        public const string JSON_PROPERTY_ATTRIBUTE = "global::Newtonsoft.Json.JsonPropertyAttribute";

        public const string LIST_TYPE_T = "global::System.Collections.Generic.List<";
        public const string DICTIONARY_TYPE_T = "global::System.Collections.Generic.Dictionary<";
        public const string HASH_SET_TYPE_T = "global::System.Collections.Generic.HashSet<";
        public const string QUEUE_TYPE_T = "global::System.Collections.Generic.Queue<";
        public const string STACK_TYPE_T = "global::System.Collections.Generic.Stack<";
        public const string VERTICAL_LIST_TYPE = "global::Cathei.BakingSheet.VerticalList<";

        public const string IREADONLY_LIST_TYPE_T = "global::System.Collections.Generic.IReadOnlyList<";
        public const string IREADONLY_DICTIONARY_TYPE_T = "global::System.Collections.Generic.IReadOnlyDictionary<";
        public const string READONLY_MEMORY_TYPE_T = "global::System.ReadOnlyMemory<";
        public const string READONLY_SPAN_TYPE_T = "global::System.ReadOnlySpan<";
        public const string MEMORY_TYPE_T = "global::System.Memory<";
        public const string SPAN_TYPE_T = "global::System.Span<";

        public const string GENERATED_PROPERTY_FROM_FIELD = "global::ZBase.Foundation.Data.SourceGen.GeneratedPropertyFromFieldAttribute";
        public const string GENERATED_FIELD_FROM_PROPERTY = "global::ZBase.Foundation.Data.SourceGen.GeneratedFieldFromPropertyAttribute";
        public const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        public const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DatabaseGenerator\", \"1.7.0\")]";
        public const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

        public static void Process(this MemberRef memberRef)
        {
            var typeRef = memberRef.TypeRef;
            GetCollectionTypeRef(typeRef);

            var typeMembers = typeRef.Type.GetMembers();
            bool? fieldTypeHasParameterlessConstructor = null;
            var fieldTypeParameterConstructorCount = 0;

            foreach (var typeMember in typeMembers)
            {
                if (fieldTypeHasParameterlessConstructor.HasValue == false
                    && typeMember is IMethodSymbol method
                    && method.MethodKind == MethodKind.Constructor
                )
                {
                    if (method.Parameters.Length == 0)
                    {
                        fieldTypeHasParameterlessConstructor = true;
                    }
                    else
                    {
                        fieldTypeParameterConstructorCount += 1;
                    }
                }
            }

            if (fieldTypeHasParameterlessConstructor.HasValue)
            {
                memberRef.TypeHasParameterlessConstructor = fieldTypeHasParameterlessConstructor.Value;
            }
            else
            {
                memberRef.TypeHasParameterlessConstructor = fieldTypeParameterConstructorCount == 0;
            }
        }

        public static void GetCollectionTypeRef(this TypeRef typeRef)
        {
            var collectionTypeRef = typeRef.CollectionTypeRef;

            if (typeRef.Type is IArrayTypeSymbol arrayType)
            {
                collectionTypeRef.CollectionKind = CollectionKind.Array;
                collectionTypeRef.CollectionElementType = arrayType.ElementType;
            }
            else if (typeRef.Type is INamedTypeSymbol namedType)
            {
                if (namedType.TryGetGenericType(LIST_TYPE_T, 1, out var listType))
                {
                    collectionTypeRef.CollectionKind = CollectionKind.List;
                    collectionTypeRef.CollectionElementType = listType.TypeArguments[0];
                }
                else if (namedType.TryGetGenericType(DICTIONARY_TYPE_T, 2, out var dictType))
                {
                    collectionTypeRef.CollectionKind = CollectionKind.Dictionary;
                    collectionTypeRef.CollectionKeyType = dictType.TypeArguments[0];
                    collectionTypeRef.CollectionElementType = dictType.TypeArguments[1];
                }
                else if (namedType.TryGetGenericType(HASH_SET_TYPE_T, 1, out var hashSetType))
                {
                    collectionTypeRef.CollectionKind = CollectionKind.HashSet;
                    collectionTypeRef.CollectionElementType = hashSetType.TypeArguments[0];
                }
                else if (namedType.TryGetGenericType(QUEUE_TYPE_T, 1, out var queueType))
                {
                    collectionTypeRef.CollectionKind = CollectionKind.Queue;
                    collectionTypeRef.CollectionElementType = queueType.TypeArguments[0];
                }
                else if (namedType.TryGetGenericType(STACK_TYPE_T, 1, out var stackType))
                {
                    collectionTypeRef.CollectionKind = CollectionKind.Stack;
                    collectionTypeRef.CollectionElementType = stackType.TypeArguments[0];
                }
                else if (namedType.TryGetGenericType(READONLY_MEMORY_TYPE_T, 1, out var readMemoryType))
                {
                    collectionTypeRef.CollectionKind = CollectionKind.Array;
                    collectionTypeRef.CollectionElementType = readMemoryType.TypeArguments[0];
                }
                else if (namedType.TryGetGenericType(MEMORY_TYPE_T, 1, out var memoryType))
                {
                    collectionTypeRef.CollectionKind = CollectionKind.Array;
                    collectionTypeRef.CollectionElementType = memoryType.TypeArguments[0];
                }
                else if (namedType.TryGetGenericType(READONLY_SPAN_TYPE_T, 1, out var readSpanType))
                {
                    collectionTypeRef.CollectionKind = CollectionKind.Array;
                    collectionTypeRef.CollectionElementType = readSpanType.TypeArguments[0];
                }
                else if (namedType.TryGetGenericType(SPAN_TYPE_T, 1, out var spanType))
                {
                    collectionTypeRef.CollectionKind = CollectionKind.Array;
                    collectionTypeRef.CollectionElementType = spanType.TypeArguments[0];
                }
                else if (namedType.TryGetGenericType(IREADONLY_LIST_TYPE_T, 1, out var readListType))
                {
                    collectionTypeRef.CollectionKind = CollectionKind.List;
                    collectionTypeRef.CollectionElementType = readListType.TypeArguments[0];
                }
                else if (namedType.TryGetGenericType(IREADONLY_DICTIONARY_TYPE_T, 2, out var readDictType))
                {
                    collectionTypeRef.CollectionKind = CollectionKind.Dictionary;
                    collectionTypeRef.CollectionKeyType = readDictType.TypeArguments[0];
                    collectionTypeRef.CollectionElementType = readDictType.TypeArguments[1];
                }
            }
        }

        public static void GetConverterRef(
              this MemberRef targetRef
            , SourceProductionContext context
            , ISymbol targetSymbol
        )
        {
            if (targetSymbol.GetAttribute(DATA_CONVERTER_ATTRIBUTE) is not AttributeData attrib
                || attrib.ConstructorArguments.Length != 1
            )
            {
                return;
            }

            if (attrib.ConstructorArguments[0].Value is not INamedTypeSymbol type)
            {
                context.ReportDiagnostic(
                      ConverterDiagnosticDescriptors.NotTypeOfExpression
                    , attrib.ApplicationSyntaxReference.GetSyntax()
                );
                return;
            }

            if (type.IsAbstract)
            {
                context.ReportDiagnostic(
                      ConverterDiagnosticDescriptors.AbstractTypeNotSupported
                    , attrib.ApplicationSyntaxReference.GetSyntax()
                    , type.Name
                );
                return;
            }

            if (type.IsUnboundGenericType)
            {
                context.ReportDiagnostic(
                      ConverterDiagnosticDescriptors.OpenGenericTypeNotSupported
                    , attrib.ApplicationSyntaxReference.GetSyntax()
                    , type.Name
                );
                return;
            }

            if (type.IsValueType == false)
            {
                var ctors = type.GetMembers(".ctor");
                IMethodSymbol ctorMethod = null;

                foreach (var ctor in ctors)
                {
                    if (ctor is not IMethodSymbol method
                        || method.DeclaredAccessibility != Accessibility.Public
                    )
                    {
                        continue;
                    }

                    if (method.Parameters.Length == 0)
                    {
                        ctorMethod = method;
                        break;
                    }
                }

                if (ctorMethod == null)
                {
                    context.ReportDiagnostic(
                          ConverterDiagnosticDescriptors.MissingDefaultConstructor
                        , attrib.ApplicationSyntaxReference.GetSyntax()
                        , type.Name
                    );
                    return;
                }
            }

            var members = type.GetMembers("Convert");
            IMethodSymbol convertMethod = null;
            var multipleConvertMethods = false;

            foreach (var member in members)
            {
                if (member is not IMethodSymbol method
                    || method.DeclaredAccessibility != Accessibility.Public
                )
                {
                    continue;
                }

                if (convertMethod != null)
                {
                    convertMethod = null;
                    multipleConvertMethods = true;
                    break;
                }

                convertMethod = method;
            }

            if (multipleConvertMethods)
            {
                context.ReportDiagnostic(
                      ConverterDiagnosticDescriptors.ConvertMethodAmbiguity
                    , attrib.ApplicationSyntaxReference.GetSyntax()
                    , type.Name
                );
                return;
            }

            var targetType = targetRef.TypeRef.Type;

            if (convertMethod == null)
            {
                context.ReportDiagnostic(
                      ConverterDiagnosticDescriptors.MissingConvertMethodReturnType
                    , attrib.ApplicationSyntaxReference.GetSyntax()
                    , type.Name
                    , targetType.Name
                );
                return;
            }

            if (convertMethod.Parameters.Length != 1
                || convertMethod.ReturnsVoid
                || SymbolEqualityComparer.Default.Equals(convertMethod.ReturnType, targetType) == false
            )
            {
                context.ReportDiagnostic(
                      ConverterDiagnosticDescriptors.InvalidConvertMethodReturnType
                    , attrib.ApplicationSyntaxReference.GetSyntax()
                    , type.Name
                    , targetType.Name
                );
                return;
            }

            var converterRef = targetRef.ConverterRef;
            converterRef.ConverterType = type;
            converterRef.TargetType = targetType;
            converterRef.Kind = convertMethod.IsStatic ? ConverterKind.Static : ConverterKind.Instance;

            var sourceTypeRef = converterRef.SourceTypeRef;
            sourceTypeRef.Type = convertMethod.Parameters[0].Type;

            GetCollectionTypeRef(sourceTypeRef);
        }

        public static bool TryGetConverterRef(
              this TypedConstant typedConstant
            , SourceProductionContext context
            , AttributeData attrib
            , int position
            , out ConverterRef result
        )
        {
            if (typedConstant.Value is not INamedTypeSymbol type)
            {
                context.ReportDiagnostic(
                      ConverterDiagnosticDescriptors.NotTypeOfExpressionAt
                    , attrib.ApplicationSyntaxReference.GetSyntax()
                    , position
                );

                result = default;
                return false;
            }

            if (type.IsAbstract)
            {
                context.ReportDiagnostic(
                      ConverterDiagnosticDescriptors.AbstractTypeNotSupported
                    , attrib.ApplicationSyntaxReference.GetSyntax()
                    , type.Name
                );

                result = default;
                return false;
            }

            if (type.IsUnboundGenericType)
            {
                context.ReportDiagnostic(
                      ConverterDiagnosticDescriptors.OpenGenericTypeNotSupported
                    , attrib.ApplicationSyntaxReference.GetSyntax()
                    , type.Name
                );

                result = default;
                return false;
            }

            if (type.IsValueType == false)
            {
                var ctors = type.GetMembers(".ctor");
                IMethodSymbol ctorMethod = null;

                foreach (var ctor in ctors)
                {
                    if (ctor is not IMethodSymbol method
                        || method.DeclaredAccessibility != Accessibility.Public
                    )
                    {
                        continue;
                    }

                    if (method.Parameters.Length == 0)
                    {
                        ctorMethod = method;
                        break;
                    }
                }

                if (ctorMethod == null)
                {
                    context.ReportDiagnostic(
                          ConverterDiagnosticDescriptors.MissingDefaultConstructor
                        , attrib.ApplicationSyntaxReference.GetSyntax()
                        , type.Name
                    );

                    result = default;
                    return false;
                }
            }

            var members = type.GetMembers("Convert");
            IMethodSymbol convertMethod = null;
            var multipleConvertMethods = false;

            foreach (var member in members)
            {
                if (member is not IMethodSymbol method
                    || method.DeclaredAccessibility != Accessibility.Public
                )
                {
                    continue;
                }

                if (convertMethod != null)
                {
                    convertMethod = null;
                    multipleConvertMethods = true;
                    break;
                }

                convertMethod = method;
            }

            if (multipleConvertMethods)
            {
                context.ReportDiagnostic(
                      ConverterDiagnosticDescriptors.ConvertMethodAmbiguity
                    , attrib.ApplicationSyntaxReference.GetSyntax()
                    , type.Name
                );

                result = default;
                return false;
            }

            if (convertMethod == null)
            {
                context.ReportDiagnostic(
                      ConverterDiagnosticDescriptors.MissingConvertMethod
                    , attrib.ApplicationSyntaxReference.GetSyntax()
                    , type.Name
                );

                result = default;
                return false;
            }

            if (convertMethod.Parameters.Length != 1
                || convertMethod.ReturnsVoid
            )
            {
                context.ReportDiagnostic(
                      ConverterDiagnosticDescriptors.InvalidConvertMethod
                    , attrib.ApplicationSyntaxReference.GetSyntax()
                    , type.Name
                );

                result = default;
                return false;
            }

            result = new ConverterRef {
                ConverterType = type,
                TargetType = convertMethod.ReturnType,
                Kind = convertMethod.IsStatic ? ConverterKind.Static : ConverterKind.Instance,
            };

            var sourceTypeRef = result.SourceTypeRef;
            sourceTypeRef.Type = convertMethod.Parameters[0].Type;

            GetCollectionTypeRef(sourceTypeRef);

            return true;
        }

        public static void GetConverterMapMap(
              this ImmutableArray<TypedConstant> values
            , SourceProductionContext context
            , AttributeData attrib
            , Dictionary<ITypeSymbol, Dictionary<ITypeSymbol, ConverterRef>> converterMapMap
            , int offset
        )
        {
            if (values.IsDefaultOrEmpty)
            {
                return;
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (values[i].TryGetConverterRef(context, attrib, i, out var converterRef) == false)
                {
                    continue;
                }

                if (converterMapMap.TryGetValue(converterRef.TargetType, out var converterMap) == false)
                {
                    converterMap = new(SymbolEqualityComparer.Default);
                    converterMapMap[converterRef.TargetType] = converterMap;
                }

                if (converterMap.TryGetValue(converterRef.SourceTypeRef.Type, out var anotherConverter) != false)
                {
                    context.ReportDiagnostic(
                          ConverterDiagnosticDescriptors.RedundantConverter
                        , attrib.ApplicationSyntaxReference.GetSyntax()
                        , converterRef.ConverterType.Name
                        , anotherConverter.ConverterType.Name
                        , anotherConverter.TargetType.Name
                        , anotherConverter.SourceTypeRef.Type.Name
                        , offset + i
                    );
                }
                else
                {
                    converterMap[converterRef.SourceTypeRef.Type] = converterRef;
                }
            }
        }
    }
}
