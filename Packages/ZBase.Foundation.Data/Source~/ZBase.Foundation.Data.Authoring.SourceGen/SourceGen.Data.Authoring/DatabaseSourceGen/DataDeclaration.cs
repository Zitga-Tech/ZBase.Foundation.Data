using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using ZBase.Foundation.SourceGen;
using static ZBase.Foundation.Data.DatabaseSourceGen.Helpers;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public partial class DataDeclaration
    {
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

                memberRef.Process();
                memberRef.GetConverterRef(context, member);
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

                memberRef.Process();
                memberRef.GetConverterRef(context, member);
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
