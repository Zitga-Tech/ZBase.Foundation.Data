﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using ZBase.Foundation.SourceGen;
using static ZBase.Foundation.Data.DataSourceGen.Helpers;

namespace ZBase.Foundation.Data.DataSourceGen
{
    public partial class DataDeclaration
    {
        public TypeDeclarationSyntax Syntax { get; }

        public INamedTypeSymbol Symbol { get; }

        public string ClassName { get; }

        public bool IsMutable { get; }

        public bool IsSealed { get; }

        public bool HasBaseType => string.IsNullOrEmpty(BaseTypeName) == false;

        public string BaseTypeName { get; }

        public bool ReferenceUnityEngine { get; }

        public ImmutableArray<FieldRef> FieldRefs { get; }

        public ImmutableArray<PropertyRef> PropRefs { get; }

        public ImmutableArray<string> OverrideEquals { get; }

        public ImmutableArray<DiagnosticInfo> Diagnostics { get; }

        public bool HasGetHashCodeMethod { get; }

        public bool HasEqualsMethod { get; }

        public bool HasIEquatableMethod { get; }

        public DataDeclaration(
              TypeDeclarationSyntax candidate
            , SemanticModel semanticModel
            , CancellationToken token
        )
        {
            Syntax = candidate;
            Symbol = semanticModel.GetDeclaredSymbol(candidate, token);
            IsMutable = Symbol.HasAttribute(DATA_MUTABLE_ATTRIBUTE);
            IsSealed = Symbol.IsSealed || Symbol.IsValueType;

            if (Symbol.BaseType is INamedTypeSymbol baseNamedTypeSymbol
                && baseNamedTypeSymbol.TypeKind == TypeKind.Class
                && baseNamedTypeSymbol.ImplementsInterface(IDATA)
            )
            {
                BaseTypeName = baseNamedTypeSymbol.ToFullName();
            }
            else
            {
                BaseTypeName = string.Empty;
            }

            {
                var classNameSb = new StringBuilder(Syntax.Identifier.Text);

                if (candidate.TypeParameterList is TypeParameterListSyntax typeParamList
                    && typeParamList.Parameters.Count > 0
                )
                {
                    classNameSb.Append("<");

                    var typeParams = typeParamList.Parameters;
                    var last = typeParams.Count - 1;

                    for (var i = 0; i <= last; i++)
                    {
                        classNameSb.Append(typeParams[i].Identifier.Text);

                        if (i < last)
                        {
                            classNameSb.Append(", ");
                        }
                    }

                    classNameSb.Append(">");
                }

                ClassName = classNameSb.ToString();
            }

            foreach (var assembly in Symbol.ContainingModule.ReferencedAssemblySymbols)
            {
                if (assembly.ToDisplayString().StartsWith("UnityEngine,"))
                {
                    ReferenceUnityEngine = true;
                    break;
                }
            }

            var existingFields = new HashSet<string>();
            var existingProperties = new HashSet<string>();
            var existingOverrideEquals = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

            using var fieldArrayBuilder = ImmutableArrayBuilder<FieldRef>.Rent();
            using var propArrayBuilder = ImmutableArrayBuilder<PropertyRef>.Rent();
            using var overrideEqualsArrayBuilder = ImmutableArrayBuilder<string>.Rent();
            using var diagnosticBuilder = ImmutableArrayBuilder<DiagnosticInfo>.Rent();

            var equalityComparer = SymbolEqualityComparer.Default;
            var members = Symbol.GetMembers();

            foreach (var member in members)
            {
                if (member is IFieldSymbol field)
                {
                    existingFields.Add(field.Name);
                    continue;
                }

                if (member is IPropertySymbol property)
                {
                    existingProperties.Add(property.Name);
                    continue;
                }
                
                if (member is IMethodSymbol method)
                {
                    if (method.Name == "GetHashCode" && method.Parameters.Length == 0)
                    {
                        HasGetHashCodeMethod = true;
                        continue;
                    }

                    if (method.Name == "Equals" && method.Parameters.Length == 1
                        && method.ReturnType.SpecialType == SpecialType.System_Boolean
                    )
                    {
                        var param = method.Parameters[0];

                        if (param.Type.SpecialType == SpecialType.System_Object)
                        {
                            HasEqualsMethod = true;
                            continue;
                        }

                        if (equalityComparer.Equals(Symbol, param.Type))
                        {
                            HasIEquatableMethod = true;
                            continue;
                        }

                        existingOverrideEquals.Add(param.Type);
                    }
                }
            }

            if (HasBaseType)
            {
                var baseType = Symbol.BaseType;

                while (baseType != null)
                {
                    if (baseType.ImplementsInterface(IDATA) == false)
                    {
                        break;
                    }

                    if (existingOverrideEquals.Contains(baseType) == false)
                    {
                        overrideEqualsArrayBuilder.Add(baseType.ToFullName());
                    }

                    baseType = baseType.BaseType;
                }
            }

            foreach (var member in members)
            {
                if (member is IFieldSymbol field)
                {
                    if (field.HasAttribute(SERIALIZE_FIELD_ATTRIBUTE) == false
                        && field.HasAttribute(JSON_INCLUDE_ATTRIBUTE) == false
                        && field.HasAttribute(JSON_PROPERTY_ATTRIBUTE) == false
                    )
                    {
                        continue;
                    }

                    field.GatherForwardedAttributes(
                          semanticModel
                        , token
                        , diagnosticBuilder
                        , out var propertyAttributes
                        , DiagnosticDescriptors.InvalidPropertyTargetedAttribute
                    );

                    var propertyName = field.ToPropertyName();

                    var fieldRef = new FieldRef {
                        Field = field,
                        Type = field.Type,
                        PropertyName = propertyName,
                        PropertyIsImplemented = existingProperties.Contains(propertyName),
                        ForwardedPropertyAttributes = propertyAttributes,
                    };

                    if (field.Type is IArrayTypeSymbol arrayType)
                    {
                        fieldRef.CollectionKind = CollectionKind.Array;
                        fieldRef.CollectionElementType = arrayType.ElementType;
                    }
                    else if (field.Type is INamedTypeSymbol namedType)
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
                        else if (namedType.TryGetGenericType(HASH_SET_TYPE_T, 1, out var hashSetType))
                        {
                            fieldRef.CollectionKind = CollectionKind.HashSet;
                            fieldRef.CollectionElementType = hashSetType.TypeArguments[0];
                        }
                        else if (namedType.TryGetGenericType(QUEUE_TYPE_T, 1, out var queueType))
                        {
                            fieldRef.CollectionKind = CollectionKind.Queue;
                            fieldRef.CollectionElementType = queueType.TypeArguments[0];
                        }
                        else if (namedType.TryGetGenericType(STACK_TYPE_T, 1, out var stackType))
                        {
                            fieldRef.CollectionKind = CollectionKind.Stack;
                            fieldRef.CollectionElementType = stackType.TypeArguments[0];
                        }
                    }

                    fieldArrayBuilder.Add(fieldRef);
                    continue;
                }

                if (member is IPropertySymbol property)
                {
                    var attribute = property.GetAttribute(DATA_PROPERTY_ATTRIBUTE);

                    if (attribute == null)
                    {
                        continue;
                    }

                    var checkAutoCollectionType = false;

                    if (attribute.ConstructorArguments.Length < 1
                        || attribute.ConstructorArguments[0].Value is not ITypeSymbol fieldType
                    )
                    {
                        fieldType = property.Type;
                        checkAutoCollectionType = true;
                    }

                    property.GatherForwardedAttributes(
                          semanticModel
                        , token
                        , diagnosticBuilder
                        , out var fieldAttributes
                        , DiagnosticDescriptors.InvalidFieldTargetedAttribute
                    );

                    var fieldName = property.ToFieldName();

                    var propRef = new PropertyRef {
                        Property = property,
                        Type = fieldType,
                        FieldName = fieldName,
                        FieldIsImplemented = existingFields.Contains(fieldName),
                        ForwardedFieldAttributes = fieldAttributes,
                    };

                    if (checkAutoCollectionType && property.Type is INamedTypeSymbol namedType)
                    {
                        if (namedType.TryGetGenericType(READONLY_MEMORY_TYPE_T, 1, out var readMemoryType))
                        {
                            propRef.CollectionKind = CollectionKind.ReadOnlyMemory;
                            propRef.CollectionElementType = readMemoryType.TypeArguments[0];
                        }
                        else if (namedType.TryGetGenericType(MEMORY_TYPE_T, 1, out var memoryType))
                        {
                            propRef.CollectionKind = CollectionKind.Memory;
                            propRef.CollectionElementType = memoryType.TypeArguments[0];
                        }
                        else if (namedType.TryGetGenericType(READONLY_SPAN_TYPE_T, 1, out var readSpanType))
                        {
                            propRef.CollectionKind = CollectionKind.ReadOnlySpan;
                            propRef.CollectionElementType = readSpanType.TypeArguments[0];
                        }
                        else if (namedType.TryGetGenericType(SPAN_TYPE_T, 1, out var spanType))
                        {
                            propRef.CollectionKind = CollectionKind.Span;
                            propRef.CollectionElementType = spanType.TypeArguments[0];
                        }
                        else if (namedType.TryGetGenericType(IREADONLY_LIST_TYPE_T, 1, out var readListType))
                        {
                            propRef.CollectionKind = CollectionKind.ReadOnlyList;
                            propRef.CollectionElementType = readListType.TypeArguments[0];
                        }
                        else if (namedType.TryGetGenericType(IREADONLY_DICTIONARY_TYPE_T, 2, out var readDictType))
                        {
                            propRef.CollectionKind = CollectionKind.ReadOnlyDictionary;
                            propRef.CollectionKeyType = readDictType.TypeArguments[0];
                            propRef.CollectionElementType = readDictType.TypeArguments[1];
                        }
                    }

                    propArrayBuilder.Add(propRef);
                    continue;
                }
            }

            if (fieldArrayBuilder.Count > 0)
            {
                FieldRefs = fieldArrayBuilder.ToImmutable();
            }
            else
            {
                FieldRefs = ImmutableArray<FieldRef>.Empty;
            }

            if (propArrayBuilder.Count > 0)
            {
                PropRefs = propArrayBuilder.ToImmutable();
            }
            else
            {
                PropRefs = ImmutableArray<PropertyRef>.Empty;
            }

            if (diagnosticBuilder.Count > 0)
            {
                Diagnostics = diagnosticBuilder.ToImmutable();
            }
            else
            {
                Diagnostics = ImmutableArray<DiagnosticInfo>.Empty;
            }

            if (overrideEqualsArrayBuilder.Count > 0)
            {
                OverrideEquals = overrideEqualsArrayBuilder.ToImmutable();
            }
            else
            {
                OverrideEquals = ImmutableArray<string>.Empty;
            }
        }

        public abstract class MemberRef
        {
            public ITypeSymbol Type { get; set; }

            public CollectionKind CollectionKind { get; set; }

            public ITypeSymbol CollectionKeyType { get; set; }

            public ITypeSymbol CollectionElementType { get; set; }
        }

        public class FieldRef : MemberRef
        {
            public IFieldSymbol Field { get; set; }

            public string PropertyName { get; set; }

            public bool PropertyIsImplemented { get; set; }

            public ImmutableArray<AttributeInfo> ForwardedPropertyAttributes { get; set; }
        }

        public class PropertyRef : MemberRef
        {
            public IPropertySymbol Property { get; set; }

            public string FieldName { get; set; }

            public bool FieldIsImplemented { get; set; }
            
            public ImmutableArray<(string, AttributeInfo)> ForwardedFieldAttributes { get; set; }
        }
    }
}
