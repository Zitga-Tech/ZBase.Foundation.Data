using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DataSourceGen
{
    public partial class DataDeclaration
    {
        public const string DATA_PROPERTY_ATTRIBUTE = "global::ZBase.Foundation.Data.DataPropertyAttribute";
        public const string SERIALIZE_FIELD_ATTRIBUTE = "global::UnityEngine.SerializeField";
        public const string JSON_INCLUDE_ATTRIBUTE = "global::System.Text.Json.Serialization.JsonIncludeAttribute";
        public const string JSON_PROPERTY_ATTRIBUTE = "global::Newtonsoft.Json.JsonPropertyAttribute";
        public const string DATA_MUTABLE_ATTRIBUTE = "global::ZBase.Foundation.Data.DataMutableAttribute";
        public const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        public const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DataGenerator\", \"1.3.0\")]";
        public const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";
        public const string LIST_TYPE_T = "global::System.Collections.Generic.List<";
        public const string DICTIONARY_TYPE_T = "global::System.Collections.Generic.Dictionary<";
        public const string HASH_SET_TYPE_T = "global::System.Collections.Generic.HashSet<";
        public const string QUEUE_TYPE_T = "global::System.Collections.Generic.Queue<";
        public const string STACK_TYPE_T = "global::System.Collections.Generic.Stack<";

        public TypeDeclarationSyntax Syntax { get; }

        public INamedTypeSymbol Symbol { get; }

        public string ClassName { get; }

        public bool IsMutable { get; }

        public bool ReferenceUnityEngine { get; }

        public ImmutableArray<FieldRef> FieldRefs { get; }

        public ImmutableArray<PropertyRef> PropRefs { get; }

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

            using var fieldArrayBuilder = ImmutableArrayBuilder<FieldRef>.Rent();
            using var propArrayBuilder = ImmutableArrayBuilder<PropertyRef>.Rent();
            using var diagnosticBuilder = ImmutableArrayBuilder<DiagnosticInfo>.Rent();

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

                        if (param.Type.Name == Symbol.Name)
                        {
                            HasIEquatableMethod = true;
                            continue;
                        }
                    }
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

                    if (attribute.ConstructorArguments.Length < 1
                        || attribute.ConstructorArguments[0].Value is not ITypeSymbol fieldType
                    )
                    {
                        fieldType = property.Type;
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
        }

        public abstract class MemberRef
        {
            public ITypeSymbol Type { get; set; }

            public CollectionKind CollectionKind { get; set; }
        }

        public class FieldRef : MemberRef
        {
            public IFieldSymbol Field { get; set; }

            public string PropertyName { get; set; }

            public bool PropertyIsImplemented { get; set; }

            public ITypeSymbol CollectionElementType { get; set; }

            public ITypeSymbol CollectionKeyType { get; set; }

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
