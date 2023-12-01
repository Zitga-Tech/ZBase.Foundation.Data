using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public partial class DataDeclaration
    {
        public const string GENERATOR_NAME = nameof(DatabaseGenerator);
        public const string DATA_PROPERTY_ATTRIBUTE = "global::ZBase.Foundation.Data.DataPropertyAttribute";
        public const string SERIALIZE_FIELD_ATTRIBUTE = "global::UnityEngine.SerializeField";
        public const string JSON_INCLUDE_ATTRIBUTE = "global::System.Text.Json.Serialization.JsonIncludeAttribute";
        public const string JSON_PROPERTY_ATTRIBUTE = "global::Newtonsoft.Json.JsonPropertyAttribute";
        public const string LIST_TYPE_T = "global::System.Collections.Generic.List<";
        public const string DICTIONARY_TYPE_T = "global::System.Collections.Generic.Dictionary<";
        public const string HASH_SET_TYPE_T = "global::System.Collections.Generic.HashSet<";
        public const string QUEUE_TYPE_T = "global::System.Collections.Generic.Queue<";
        public const string STACK_TYPE_T = "global::System.Collections.Generic.Stack<";
        public const string VERTICAL_LIST_TYPE = "global::Cathei.BakingSheet.VerticalList<";

        private const string GENERATED_PROPERTY_FROM_FIELD = "global::ZBase.Foundation.Data.SourceGen.GeneratedPropertyFromFieldAttribute";
        private const string GENERATED_FIELD_FROM_PROPERTY = "global::ZBase.Foundation.Data.SourceGen.GeneratedFieldFromPropertyAttribute";
        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DatabaseGenerator\", \"1.3.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

        public ITypeSymbol Symbol { get; }

        public string FullName { get; }

        public ImmutableArray<MemberRef> PropRefs { get; }

        public ImmutableArray<MemberRef> FieldRefs { get; }

        public DataDeclaration(ITypeSymbol symbol)
        {
            Symbol = symbol;
            FullName = Symbol.ToFullName();

            var properties = new List<(string propertyName, string fieldName, ITypeSymbol fieldType)>();
            var fields = new List<(string propertyName, string fieldName, ITypeSymbol fieldType)>();

            using var propArrayBuilder = ImmutableArrayBuilder<MemberRef>.Rent();
            using var fieldArrayBuilder = ImmutableArrayBuilder<MemberRef>.Rent();

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
                            properties.Add((property.Name, fieldName, fieldType));
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
                            fields.Add((property.Name, property.ToFieldName(), fieldType));
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
                            fields.Add((propertyName, field.Name, field.Type));
                            continue;
                        }
                    }

                    // For types in the same assembly
                    if (field.HasAttribute(SERIALIZE_FIELD_ATTRIBUTE)
                        || field.HasAttribute(JSON_INCLUDE_ATTRIBUTE)
                        || field.HasAttribute(JSON_PROPERTY_ATTRIBUTE)
                    )
                    {
                        properties.Add((field.ToPropertyName(), field.Name, field.Type));
                        continue;
                    }

                    continue;
                }
            }

            var uniqueFieldNames = new HashSet<string>();

            foreach (var (propertyName, fieldName, fieldType) in properties)
            {
                if (uniqueFieldNames.Contains(fieldName))
                {
                    continue;
                }

                uniqueFieldNames.Add(fieldName);

                var memberRef = new MemberRef {
                    Type = fieldType,
                    PropertyName = propertyName,
                    TypeHasParameterlessConstructor = false,
                };

                Process(memberRef);
                propArrayBuilder.Add(memberRef);
            }

            foreach (var (propertyName, fieldName, fieldType) in fields)
            {
                if (uniqueFieldNames.Contains(fieldName))
                {
                    continue;
                }

                uniqueFieldNames.Add(fieldName);

                var memberRef = new MemberRef {
                    Type = fieldType,
                    PropertyName = propertyName,
                    TypeHasParameterlessConstructor = false,
                };

                Process(memberRef);
                fieldArrayBuilder.Add(memberRef);
            }

            if (propArrayBuilder.Count > 0)
            {
                PropRefs = propArrayBuilder.ToImmutable();
            }
            else
            {
                PropRefs = ImmutableArray<MemberRef>.Empty;
            }

            if (fieldArrayBuilder.Count > 0)
            {
                FieldRefs = fieldArrayBuilder.ToImmutable();
            }
            else
            {
                FieldRefs = ImmutableArray<MemberRef>.Empty;
            }
        }

        private static void Process(MemberRef memberRef)
        {
            if (memberRef.Type is IArrayTypeSymbol arrayType)
            {
                memberRef.CollectionKind = CollectionKind.Array;
                memberRef.CollectionElementType = arrayType.ElementType;
            }
            else if (memberRef.Type is INamedTypeSymbol namedType)
            {
                if (namedType.TryGetGenericType(LIST_TYPE_T, 1, out var listType))
                {
                    memberRef.CollectionKind = CollectionKind.List;
                    memberRef.CollectionElementType = listType.TypeArguments[0];
                }
                else if (namedType.TryGetGenericType(DICTIONARY_TYPE_T, 2, out var dictType))
                {
                    memberRef.CollectionKind = CollectionKind.Dictionary;
                    memberRef.CollectionKeyType = dictType.TypeArguments[0];
                    memberRef.CollectionElementType = dictType.TypeArguments[1];
                }
                else if (namedType.TryGetGenericType(HASH_SET_TYPE_T, 1, out var hashSetType))
                {
                    memberRef.CollectionKind = CollectionKind.HashSet;
                    memberRef.CollectionElementType = hashSetType.TypeArguments[0];
                }
                else if (namedType.TryGetGenericType(QUEUE_TYPE_T, 1, out var queueType))
                {
                    memberRef.CollectionKind = CollectionKind.Queue;
                    memberRef.CollectionElementType = queueType.TypeArguments[0];
                }
                else if (namedType.TryGetGenericType(STACK_TYPE_T, 1, out var stackType))
                {
                    memberRef.CollectionKind = CollectionKind.Stack;
                    memberRef.CollectionElementType = stackType.TypeArguments[0];
                }
            }

            var typeMembers = memberRef.Type.GetMembers();
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

        public class MemberRef
        {
            public ITypeSymbol Type { get; set; }

            public bool TypeHasParameterlessConstructor { get; set; }

            public string PropertyName { get; set; }

            public CollectionKind CollectionKind { get; set; }

            public ITypeSymbol CollectionElementType { get; set; }

            public ITypeSymbol CollectionKeyType { get; set; }
        }
    }
}
