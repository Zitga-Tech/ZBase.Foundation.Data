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
        public const string GENERATOR_NAME = nameof(DataGenerator);
        public const string SERIALIZE_FIELD_ATTRIBUTE = "global::UnityEngine.SerializeField";
        public const string RUNTIME_IMMUTABLE_ATTRIBUTE = "global::ZBase.Foundation.Data.RuntimeImmutableAttribute";
        public const string VERTICAL_ARRAY_ATTRIBUTE = "global::ZBase.Foundation.Data.VerticalArrayAttribute";
        public const string LIST_TYPE = "global::System.Collections.Generic.List";
        public const string VERTICAL_LIST_TYPE = "global::Cathei.BakingSheet.VerticalList";

        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DataGenerator\", \"1.0.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

        public TypeDeclarationSyntax Syntax { get; }

        public INamedTypeSymbol Symbol { get; }

        public bool IsRuntimeImmutable { get; }

        public ImmutableArray<IFieldSymbol> Fields { get; }

        public DataDeclaration(
              TypeDeclarationSyntax candidate
            , SemanticModel semanticModel
        )
        {
            Syntax = candidate;
            Symbol = semanticModel.GetDeclaredSymbol(candidate);
            IsRuntimeImmutable = Symbol.HasAttribute(RUNTIME_IMMUTABLE_ATTRIBUTE);

            var existingProperties = new HashSet<string>();

            using var memberArrayBuilder = ImmutableArrayBuilder<IFieldSymbol>.Rent();
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

                    if (existingProperties.Contains(propertyName) == false)
                    {
                        memberArrayBuilder.Add(field);
                    }
                }
            }

            Fields = memberArrayBuilder.ToImmutable();
        }
    }
}
