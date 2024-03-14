using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public partial class DataDeclaration
    {
        public const string GENERATOR_NAME = nameof(DatabaseGenerator);
        public const string DATA_PROPERTY_ATTRIBUTE = "global::ZBase.Foundation.Data.DataPropertyAttribute";
        public const string DATA_CONVERTER_ATTRIBUTE = "global::ZBase.Foundation.Data.DataConverterAttribute";
        public const string SERIALIZE_FIELD_ATTRIBUTE = "global::UnityEngine.SerializeField";
        public const string JSON_INCLUDE_ATTRIBUTE = "global::System.Text.Json.Serialization.JsonIncludeAttribute";
        public const string JSON_PROPERTY_ATTRIBUTE = "global::Newtonsoft.Json.JsonPropertyAttribute";
        public const string IDATA = "global::ZBase.Foundation.Data.IData";

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

        private const string GENERATED_PROPERTY_FROM_FIELD = "global::ZBase.Foundation.Data.SourceGen.GeneratedPropertyFromFieldAttribute";
        private const string GENERATED_FIELD_FROM_PROPERTY = "global::ZBase.Foundation.Data.SourceGen.GeneratedFieldFromPropertyAttribute";
        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DatabaseGenerator\", \"1.3.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

        public ITypeSymbol Symbol { get; }

        public string FullName { get; }

        public ImmutableArray<MemberRef> PropRefs { get; }

        public ImmutableArray<MemberRef> FieldRefs { get; }

        public ImmutableArray<DataDeclaration> BaseTypeRefs { get; }

        public DataDeclaration(SourceProductionContext context, ITypeSymbol symbol, bool buildBaseTypeRefs)
        {
            Symbol = symbol;
            FullName = Symbol.ToFullName();

            var properties = new List<MemberCandidate>();
            var fields = new List<MemberCandidate>();

            using var propArrayBuilder = ImmutableArrayBuilder<MemberRef>.Rent();
            using var fieldArrayBuilder = ImmutableArrayBuilder<MemberRef>.Rent();
            using var baseArrayBuilder = ImmutableArrayBuilder<DataDeclaration>.Rent();

            var members = Symbol.GetMembers();

            foreach (var member in members)
            {
                if (member is IPropertySymbol property)
                {
                    // For types in another assembly
                    {
                        var attribute = property.GetAttribute(GENERATED_PROPERTY_FROM_FIELD);

                        if (attribute != null
                            && attribute.ConstructorArguments.Length > 1
                            && attribute.ConstructorArguments[0].Value is string fieldName
                            && attribute.ConstructorArguments[1].Value is ITypeSymbol fieldType
                        )
                        {
                            properties.Add((property.Name, fieldName, property, fieldType));
                            continue;
                        }
                    }

                    // For types in the same assembly
                    {
                        var attribute = property.GetAttribute(DATA_PROPERTY_ATTRIBUTE);

                        if (attribute != null)
                        {
                            if (attribute.ConstructorArguments.Length < 1
                                || attribute.ConstructorArguments[0].Value is not ITypeSymbol fieldType
                            )
                            {
                                fieldType = property.Type;
                            }

                            // Generated fields
                            fields.Add((property.Name, property.ToFieldName(), property, fieldType));
                            continue;
                        }
                    }

                    continue;
                }

                if (member is IFieldSymbol field)
                {
                    // For types in another assembly
                    {
                        var attribute = field.GetAttribute(GENERATED_FIELD_FROM_PROPERTY);

                        if (attribute != null
                            && attribute.ConstructorArguments.Length > 0
                            && attribute.ConstructorArguments[0].Value is string propertyName
                        )
                        {
                            fields.Add((propertyName, field.Name, field, field.Type));
                            continue;
                        }
                    }

                    // For types in the same assembly
                    if (field.HasAttribute(SERIALIZE_FIELD_ATTRIBUTE)
                        || field.HasAttribute(JSON_INCLUDE_ATTRIBUTE)
                        || field.HasAttribute(JSON_PROPERTY_ATTRIBUTE)
                    )
                    {
                        properties.Add((field.ToPropertyName(), field.Name, field, field.Type));
                        continue;
                    }

                    continue;
                }
            }

            var uniqueFieldNames = new HashSet<string>();

            foreach (var (propertyName, fieldName, member, fieldType) in properties)
            {
                if (uniqueFieldNames.Contains(fieldName))
                {
                    continue;
                }

                uniqueFieldNames.Add(fieldName);

                var memberRef = new MemberRef {
                    PropertyName = propertyName,
                    TypeHasParameterlessConstructor = false,
                };

                memberRef.TypeRef.Type = fieldType;

                Process(memberRef);
                GetConverterRef(context, member, memberRef);
                propArrayBuilder.Add(memberRef);
            }

            foreach (var (propertyName, fieldName, member, fieldType) in fields)
            {
                if (uniqueFieldNames.Contains(fieldName))
                {
                    continue;
                }

                uniqueFieldNames.Add(fieldName);

                var memberRef = new MemberRef {
                    PropertyName = propertyName,
                    TypeHasParameterlessConstructor = false,
                };

                memberRef.TypeRef.Type = fieldType;

                Process(memberRef);
                GetConverterRef(context, member, memberRef);
                fieldArrayBuilder.Add(memberRef);
            }

            if (buildBaseTypeRefs)
            {
                var baseSymbol = symbol.BaseType;

                while (baseSymbol != null)
                {
                    if (baseSymbol.TypeKind != TypeKind.Class
                        || baseSymbol.ImplementsInterface(IDATA) == false
                    )
                    {
                        break;
                    }

                    baseArrayBuilder.Add(new DataDeclaration(context, baseSymbol, false));
                    baseSymbol = baseSymbol.BaseType;
                }
            }

            PropRefs = propArrayBuilder.ToImmutable();
            FieldRefs = fieldArrayBuilder.ToImmutable();
            BaseTypeRefs = baseArrayBuilder.ToImmutable();
        }

        private static void Process(MemberRef memberRef)
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

        private static void GetCollectionTypeRef(TypeRef typeRef)
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

        private static void GetConverterRef(
              SourceProductionContext context
            , ISymbol targetSymbol
            , MemberRef targetRef
        )
        {
            var converterRef = targetRef.ConverterRef;
            var targetTypeRef = targetRef.TypeRef;
            var sourceTypeRef = converterRef.SourceTypeRef;

            if (targetSymbol.GetAttribute(DATA_CONVERTER_ATTRIBUTE) is not AttributeData converterAttrib
                || converterAttrib.ConstructorArguments.Length != 1
                || converterAttrib.ConstructorArguments[0].Value is not ITypeSymbol converterType
            )
            {
                return;
            }

            if (converterType.IsValueType == false)
            {
                var ctors = converterType.GetMembers(".ctor");
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
                          DiagnosticDescriptors.MissingDefaultConstructor
                        , converterAttrib.ApplicationSyntaxReference.GetSyntax()
                        , converterType.Name
                    );
                    return;
                }
            }

            var members = converterType.GetMembers("Convert");
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
                      DiagnosticDescriptors.ConvertMethodAmbiguity
                    , converterAttrib.ApplicationSyntaxReference.GetSyntax()
                    , converterType.Name
                );
                return;
            }

            if (convertMethod == null)
            {
                context.ReportDiagnostic(
                      DiagnosticDescriptors.MissingConvertMethod
                    , converterAttrib.ApplicationSyntaxReference.GetSyntax()
                    , converterType.Name
                    , targetTypeRef.Type.Name
                );
                return;
            }

            if (convertMethod.Parameters.Length != 1
                || SymbolEqualityComparer.Default.Equals(convertMethod.ReturnType, targetTypeRef.Type) == false
            )
            {
                context.ReportDiagnostic(
                      DiagnosticDescriptors.InvalidConvertMethod
                    , converterAttrib.ApplicationSyntaxReference.GetSyntax()
                    , converterType.Name
                    , targetTypeRef.Type.Name
                );
                return;
            }

            converterRef.ConverterType = converterType;
            sourceTypeRef.Type = convertMethod.Parameters[0].Type;
            converterRef.Kind = convertMethod.IsStatic ? ConverterKind.Static : ConverterKind.Instance;

            GetCollectionTypeRef(sourceTypeRef);
        }

        public enum ConverterKind
        {
            None = 0,
            Static,
            Instance,
        }

        public class CollectionTypeRef
        {
            public CollectionKind CollectionKind { get; set; }

            public ITypeSymbol CollectionElementType { get; set; }

            public ITypeSymbol CollectionKeyType { get; set; }
        }

        public class TypeRef
        {
            public ITypeSymbol Type { get; set; }

            public CollectionTypeRef CollectionTypeRef { get; } = new();
        }

        public class ConverterRef
        {
            public ConverterKind Kind { get; set; }

            public ITypeSymbol ConverterType { get; set; }

            public TypeRef SourceTypeRef { get; } = new();

            public string Convert(string expression)
            {
                if (ConverterType == null)
                {
                    return expression;
                }

                if (Kind == ConverterKind.Instance)
                {
                    return $"new {ConverterType.ToFullName()}().Convert({expression})";
                }

                if (Kind == ConverterKind.Static)
                {
                    return $"{ConverterType.ToFullName()}.Convert({expression})";
                }

                return expression;
            }
        }

        public class MemberRef
        {
            public TypeRef TypeRef { get; } = new();

            public bool TypeHasParameterlessConstructor { get; set; }

            public string PropertyName { get; set; }

            public ConverterRef ConverterRef { get; } = new();

            public TypeRef SelectTypeRef()
            {
                if (ConverterRef.Kind == ConverterKind.None)
                {
                    return TypeRef;
                }

                return ConverterRef.SourceTypeRef;
            }
        }

        private struct MemberCandidate
        {
            public string propertyName;
            public string fieldName;
            public ISymbol member;
            public ITypeSymbol fieldType;

            public MemberCandidate(
                  string propertyName
                , string fieldName
                , ISymbol member
                , ITypeSymbol fieldType
            )
            {
                this.propertyName = propertyName;
                this.fieldName = fieldName;
                this.member = member;
                this.fieldType = fieldType;
            }

            public void Deconstruct(
                  out string propertyName
                , out string fieldName
                , out ISymbol member
                , out ITypeSymbol fieldType
            )
            {
                propertyName = this.propertyName;
                fieldName = this.fieldName;
                member = this.member;
                fieldType = this.fieldType;
            }

            public static implicit operator (
                  string propertyName
                , string fieldName
                , ISymbol member
                , ITypeSymbol fieldType
            )(MemberCandidate value)
            {
                return (
                      value.propertyName
                    , value.fieldName
                    , value.member
                    , value.fieldType
                );
            }

            public static implicit operator MemberCandidate((
                  string propertyName
                , string fieldName
                , ISymbol member
                , ITypeSymbol fieldType
            ) value)
            {
                return new(
                      value.propertyName
                    , value.fieldName
                    , value.member
                    , value.fieldType
                );
            }
        }
    }
}
