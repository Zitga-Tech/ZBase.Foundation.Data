using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DataSourceGen
{
    public partial class DataDeclaration
    {
        public const string SERIALIZE_FIELD_ATTRIBUTE = "global::UnityEngine.SerializeField";
        public const string DATA_MUTABLE_ATTRIBUTE = "global::ZBase.Foundation.Data.DataMutableAttribute";
        public const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        public const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DataGenerator\", \"1.0.0\")]";
        public const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";
        public const string LIST_TYPE_T = "global::System.Collections.Generic.List<";
        public const string DICTIONARY_TYPE_T = "global::System.Collections.Generic.Dictionary<";
        public const string HASH_SET_TYPE_T = "global::System.Collections.Generic.HashSet<";
        public const string QUEUE_TYPE_T = "global::System.Collections.Generic.Queue<";
        public const string STACK_TYPE_T = "global::System.Collections.Generic.Stack<";

        public TypeDeclarationSyntax Syntax { get; }

        public INamedTypeSymbol Symbol { get; }

        public bool IsMutable { get; }

        public ImmutableArray<FieldRef> Fields { get; }

        public DataDeclaration(
              TypeDeclarationSyntax candidate
            , SemanticModel semanticModel
        )
        {
            Syntax = candidate;
            Symbol = semanticModel.GetDeclaredSymbol(candidate);
            IsMutable = Symbol.HasAttribute(DATA_MUTABLE_ATTRIBUTE);

            var existingProperties = new HashSet<string>();

            using var memberArrayBuilder = ImmutableArrayBuilder<FieldRef>.Rent();
            var members = Symbol.GetMembers();

            foreach (var member in members)
            {
                if (member is IPropertySymbol property)
                {
                    existingProperties.Add(property.Name);
                }
            }
            
            foreach (var member in members)
            {
                if (member is not IFieldSymbol field
                    || field.HasAttribute(SERIALIZE_FIELD_ATTRIBUTE) == false
                )
                {
                    continue;
                }

                var propertyName = field.ToPropertyName();
                var fieldRef = new FieldRef {
                    Field = field,
                    Type = field.Type,
                    PropertyName = propertyName,
                    PropertyIsImplemented = existingProperties.Contains(propertyName),
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

                memberArrayBuilder.Add(fieldRef);
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
            public IFieldSymbol Field { get; set; }

            public ITypeSymbol Type { get; set; }

            public string PropertyName { get; set; }

            public bool PropertyIsImplemented { get; set; }

            public CollectionKind CollectionKind { get; set; }

            public ITypeSymbol CollectionElementType { get; set; }

            public ITypeSymbol CollectionKeyType { get; set; }
        }
    }
}
