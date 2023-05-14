using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data
{
    public static class GeneratorHelper
    {
        private const string DISABLE_ATTRIBUTE = "global::ZBase.Foundation.Data.SkipGeneratorForAssemblyAttribute";

        public static bool IsValidCompilation(this Compilation compilation)
            => compilation.Assembly.HasAttribute(DISABLE_ATTRIBUTE) == false;

        public static bool IsStructOrClassSyntaxMatch(SyntaxNode syntaxNode, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return syntaxNode is TypeDeclarationSyntax typeSyntax
                && typeSyntax.Kind() is (SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration)
                && typeSyntax.BaseList != null
                && typeSyntax.BaseList.Types.Count > 0;
        }
    }
}
