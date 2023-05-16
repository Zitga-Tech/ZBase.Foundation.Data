﻿using Microsoft.CodeAnalysis;
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
        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DataGenerator\", \"1.0.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

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
                if (member is IFieldSymbol field && field.HasAttribute(SERIALIZE_FIELD_ATTRIBUTE))
                {
                    var propertyName = field.ToPropertyName();
                    var fieldRef = new FieldRef {
                        Field = field,
                        Type = field.Type,
                        PropertyName = propertyName,
                        PropertyIsImplemented = existingProperties.Contains(propertyName),
                    };

                    if (field.Type is IArrayTypeSymbol arrayType)
                    {
                        fieldRef.IsArray = true;
                        fieldRef.ArrayElementType = arrayType.ElementType;
                    }

                    memberArrayBuilder.Add(fieldRef);
                }
            }

            Fields = memberArrayBuilder.ToImmutable();
        }

        public class FieldRef
        {
            public IFieldSymbol Field { get; set; }

            public ITypeSymbol Type { get; set; }

            public string PropertyName { get; set; }

            public bool PropertyIsImplemented { get; set; }

            public bool IsArray { get; set; }

            public ITypeSymbol ArrayElementType { get; set; }
        }
    }
}