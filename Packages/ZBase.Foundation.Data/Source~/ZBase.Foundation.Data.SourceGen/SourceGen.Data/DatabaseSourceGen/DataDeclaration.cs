using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public partial class DataDeclaration
    {
        public const string GENERATOR_NAME = nameof(DatabaseGenerator);
        public const string SERIALIZE_FIELD_ATTRIBUTE = "global::UnityEngine.SerializeField";
        public const string LIST_TYPE_T = "global::System.Collections.Generic.List<";
        public const string DICTIONARY_TYPE_T = "global::System.Collections.Generic.Dictionary<";
        public const string VERTICAL_LIST_TYPE = "global::Cathei.BakingSheet.VerticalList<";

        private const string GENERATED_PROPERTY_FROM_FIELD = "global::ZBase.Foundation.Data.SourceGen.GeneratedPropertyFromFieldAttribute";
        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DatabaseGenerator\", \"1.0.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

        public ITypeSymbol Symbol { get; }

        public string FullName { get; }

        public ImmutableArray<FieldRef> Fields { get; }

        public DataDeclaration(ITypeSymbol symbol)
        {
            Symbol = symbol;
            FullName = Symbol.ToFullName();

            var fields = new List<IFieldSymbol>();
            var properties = new List<(string propertyName, string fieldName, ITypeSymbol fieldType)>();

            using var memberArrayBuilder = ImmutableArrayBuilder<FieldRef>.Rent();
            var members = Symbol.GetMembers();

            foreach (var member in members)
            {
                if (member is IPropertySymbol property)
                {
                    var attribute = property.GetAttribute(GENERATED_PROPERTY_FROM_FIELD);
                    
                    if (attribute != null
                        && attribute.ConstructorArguments.Length > 1
                        && attribute.ConstructorArguments[0].Value is string fieldName
                        && attribute.ConstructorArguments[1].Value is ITypeSymbol fieldType
                    )
                    {
                        properties.Add((property.Name, fieldName, fieldType));
                    }
                }
                else if (member is IFieldSymbol field && field.HasAttribute(SERIALIZE_FIELD_ATTRIBUTE))
                {
                    fields.Add(field);
                }
            }

            var uniqueFieldNames = new HashSet<string>();

            foreach (var field in fields)
            {
                if (uniqueFieldNames.Contains(field.Name))
                {
                    continue;
                }

                uniqueFieldNames.Add(field.Name);

                var fieldRef = new FieldRef {
                    Type = field.Type,
                    PropertyName = field.ToPropertyName(),
                    TypeHasParameterlessConstructor = false,
                };

                memberArrayBuilder.Add(fieldRef);
            }

            foreach (var property in properties)
            {
                if (uniqueFieldNames.Contains(property.fieldName))
                {
                    continue;
                }

                uniqueFieldNames.Add(property.fieldName);

                var fieldRef = new FieldRef {
                    Type = property.fieldType,
                    PropertyName = property.propertyName,
                    TypeHasParameterlessConstructor = false,
                };

                memberArrayBuilder.Add(fieldRef);
            }

            foreach (var fieldRef in memberArrayBuilder.WrittenSpan)
            {
                if (fieldRef.Type is IArrayTypeSymbol arrayType)
                {
                    fieldRef.CollectionKind = CollectionKind.Array;
                    fieldRef.CollectionElementType = arrayType.ElementType;
                }
                else if (fieldRef.Type is INamedTypeSymbol namedType)
                {
                    if (namedType.TryGetGenericType(LIST_TYPE_T, 1, out var listType))
                    {
                        fieldRef.CollectionKind = CollectionKind.List;
                        fieldRef.CollectionElementType = listType.TypeArguments[0];
                    }
                    else if (namedType.TryGetGenericType(DICTIONARY_TYPE_T, 2, out var dictType))
                    {
                        fieldRef.CollectionKind = CollectionKind.Dictionary;
                        fieldRef.CollectionKeyType = dictType.TypeArguments[0];
                        fieldRef.CollectionElementType = dictType.TypeArguments[1];
                    }
                }

                var fieldTypeMembers = fieldRef.Type.GetMembers();
                bool? fieldTypeHasParameterlessConstructor = null;
                var fieldTypeParameterConstructorCount = 0;

                foreach (var member in fieldTypeMembers)
                {
                    if (fieldTypeHasParameterlessConstructor.HasValue == false
                        && member is IMethodSymbol method
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
                    fieldRef.TypeHasParameterlessConstructor = fieldTypeHasParameterlessConstructor.Value;
                }
                else
                {
                    fieldRef.TypeHasParameterlessConstructor = fieldTypeParameterConstructorCount == 0;
                }
            }

            if (memberArrayBuilder.Count > 0)
            {
                Fields = memberArrayBuilder.ToImmutable();
            }
            else
            {
                Fields = ImmutableArray<FieldRef>.Empty;
            }
        }

        public class FieldRef
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
