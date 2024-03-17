using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DataSourceGen
{
    public static class GeneratorHelper
    {
        private const string DISABLE_ATTRIBUTE = "global::ZBase.Foundation.Data.SkipGeneratorForAssemblyAttribute";

        public const string FIELD_PREFIX_UNDERSCORE = "_";
        public const string FIELD_PREFIX_M_UNDERSCORE = "m_";

        public static bool IsValidCompilation(this Compilation compilation)
            => compilation.Assembly.HasAttribute(DISABLE_ATTRIBUTE) == false;

        public static bool IsClassSyntaxMatch(SyntaxNode syntaxNode, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return syntaxNode is ClassDeclarationSyntax classSyntax
                && classSyntax.BaseList != null
                && classSyntax.BaseList.Types.Count > 0;
        }

        public static bool IsStructOrClassSyntaxMatch(SyntaxNode syntaxNode, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return syntaxNode is TypeDeclarationSyntax typeSyntax
                && typeSyntax.Kind() is (SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration)
                && typeSyntax.BaseList != null
                && typeSyntax.BaseList.Types.Count > 0;
        }

        public static string ToPropertyName(this IFieldSymbol field)
        {
            return ToPropertyName(field.Name);
        }

        public static string ToPropertyName(this string fieldName)
        {
            var nameSpan = fieldName.AsSpan();
            var prefix = FIELD_PREFIX_UNDERSCORE.AsSpan();

            if (nameSpan.StartsWith(prefix))
            {
                return ToTitleCase(nameSpan.Slice(1));
            }

            prefix = FIELD_PREFIX_M_UNDERSCORE.AsSpan();

            if (nameSpan.StartsWith(prefix))
            {
                return ToTitleCase(nameSpan.Slice(2));
            }

            return ToTitleCase(nameSpan);
        }

        public static string ToFieldName(this IPropertySymbol property)
        {
            return $"{FIELD_PREFIX_UNDERSCORE}{ToLowerCase(property.Name.AsSpan())}";
        }

        public static string ToFieldName(this string propertyName)
        {
            return $"{FIELD_PREFIX_UNDERSCORE}{ToLowerCase(propertyName.AsSpan())}";
        }

        public static string ToTitleCase(in ReadOnlySpan<char> value)
        {
            return $"{char.ToUpper(value[0])}{value.Slice(1).ToString()}";
        }

        public static string ToLowerCase(in ReadOnlySpan<char> value)
        {
            return $"{char.ToLower(value[0])}{value.Slice(1).ToString()}";
        }
    }
}
