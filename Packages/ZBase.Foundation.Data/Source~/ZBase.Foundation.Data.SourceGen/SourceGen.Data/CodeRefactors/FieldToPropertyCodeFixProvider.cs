using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.CodeRefactors
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FieldToPropertyCodeFixProvider)), Shared]
    internal class FieldToPropertyCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(DataDiagnosticAnalyzer.DIAGNOSTIC_FIELD);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md
            // for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent
                .AncestorsAndSelf()
                .OfType<FieldDeclarationSyntax>()
                .First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                      title: "Replace field by property"
                    , createChangedSolution: c => MakePropertyAsync(context.Document, declaration, c)
                    , equivalenceKey: "Replace field by property"
                )
                , diagnostic
            );
        }

        private async Task<Solution> MakePropertyAsync(
              Document document
            , FieldDeclarationSyntax fieldDecl
            , CancellationToken cancellationToken
        )
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var propAttribListList = new List<List<AttributeSyntax>>();
            var propAttribCheck = new HashSet<string>();
            var fieldAttribListList = new List<List<AttributeSyntax>>();

            foreach (var fieldAttribList in fieldDecl.AttributeLists)
            {
                var attributes = fieldAttribList.Attributes;

                if (attributes.Count < 1)
                {
                    continue;
                }

                var propList = new List<AttributeSyntax>();
                var fieldList = new List<AttributeSyntax>();
                var targetKind = fieldAttribList.Target?.Identifier.Kind();

                if (targetKind is SyntaxKind.PropertyKeyword)
                {
                    foreach (var attrib in attributes)
                    {
                        var (name, _) = GetAttributeInfo(semanticModel, attrib);

                        if (propAttribCheck.Contains(name) == false)
                        {
                            propAttribCheck.Add(name);
                            propList.Add(attrib);
                        }
                    }

                    propAttribListList.Add(propList);
                    continue;
                }

                if (targetKind is SyntaxKind.FieldKeyword)
                {
                    foreach (var attrib in attributes)
                    {
                        var (name, _) = GetAttributeInfo(semanticModel, attrib);

                        if (propAttribCheck.Contains(name) == false)
                        {
                            fieldList.AddRange(attributes);
                        }
                    }

                    fieldAttribListList.Add(fieldList);
                    continue;
                }

                foreach (var attrib in attributes)
                {
                    var (name, target) = GetAttributeInfo(semanticModel, attrib);

                    if (target.HasFlag(AttributeTargets.Property))
                    {
                        if (propAttribCheck.Contains(name) == false)
                        {
                            propAttribCheck.Add(name);
                            propList.Add(attrib);
                        }
                    }
                    
                    if (target.HasFlag(AttributeTargets.Field))
                    {
                        if (propAttribCheck.Contains(name) == false)
                        {
                            fieldList.Add(attrib);
                        }
                    }
                }

                if (propList.Count > 0)
                {
                    propAttribListList.Add(propList);
                }

                if (fieldList.Count > 0)
                {
                    fieldAttribListList.Add(fieldList);
                }
            }

            if (propAttribCheck.Contains("DataPropertyAttribute") == false)
            {
                propAttribListList.Add(new List<AttributeSyntax> {
                    SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("DataProperty"))
                });
            }

            var variableDelc = fieldDecl.Declaration.Variables.First();
            var propName = variableDelc.Identifier.Text.ToPropertyName();

            var propDecl = SyntaxFactory.PropertyDeclaration(fieldDecl.Declaration.Type, propName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithAdditionalAnnotations(Formatter.Annotation)
                ;

            var arrowExpression = SyntaxFactory.ArrowExpressionClause(
                  arrowToken: SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken)
                , SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName($"GetValue_{propName}"))
            );

            propDecl = propDecl.WithExpressionBody(arrowExpression)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            //propDecl = propDecl.AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
            //      kind: SyntaxKind.GetAccessorDeclaration
            //    , attributeLists: new SyntaxList<AttributeListSyntax>()
            //    , modifiers: SyntaxFactory.TokenList()
            //    , keyword: SyntaxFactory.Token(SyntaxKind.GetKeyword)
            //    , semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken)
            //    , expressionBody: SyntaxFactory.ArrowExpressionClause(
            //        SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName($"GetValue_{propName}"))
            //    )
            //));

            //propDecl = propDecl.AddAccessorListAccessors(SyntaxFactory.AccessorDeclaration(
            //      kind: SyntaxKind.SetAccessorDeclaration
            //    , attributeLists: new SyntaxList<AttributeListSyntax>()
            //    , modifiers: SyntaxFactory.TokenList()
            //    , keyword: SyntaxFactory.Token(SyntaxKind.SetKeyword)
            //    , semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken)
            //    , expressionBody: SyntaxFactory.ArrowExpressionClause(
            //        SyntaxFactory.InvocationExpression(
            //                SyntaxFactory.IdentifierName($"SetValue_{propName}")
            //            , SyntaxFactory.ArgumentList(
            //                SyntaxFactory.SingletonSeparatedList(
            //                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("value"))
            //                )
            //            )
            //        )
            //    )
            //));

            foreach (var list in propAttribListList)
            {
                var propAttribList = SyntaxFactory.AttributeList(
                      openBracketToken: SyntaxFactory.Token(SyntaxKind.OpenBracketToken)
                    , target: null
                    , attributes: SyntaxFactory.SeparatedList(list)
                    , closeBracketToken: SyntaxFactory.Token(SyntaxKind.CloseBracketToken)
                );

                propDecl = propDecl.AddAttributeLists(propAttribList);
            }

            foreach (var list in fieldAttribListList)
            {
                var propAttribList = SyntaxFactory.AttributeList(
                      openBracketToken: SyntaxFactory.Token(SyntaxKind.OpenBracketToken)
                    , target: SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.FieldKeyword))
                    , attributes: SyntaxFactory.SeparatedList(list)
                    , closeBracketToken: SyntaxFactory.Token(SyntaxKind.CloseBracketToken)
                );

                propDecl = propDecl.AddAttributeLists(propAttribList);
            }

            var newRoot = root.ReplaceNode(fieldDecl, propDecl);
            return document.WithSyntaxRoot(newRoot).Project.Solution;
        }

        private static (string, AttributeTargets) GetAttributeInfo(
              SemanticModel semanticModel
            , AttributeSyntax attribSyntax
        )
        {
            var attribSymbol = semanticModel.GetSymbolInfo(attribSyntax);

            if (attribSymbol.TryGetAttributeTypeSymbol(out INamedTypeSymbol attribTypeSymbol) == false)
            {
                return (string.Empty, 0);
            }

            var attributeUsageAttribute = attribTypeSymbol.GetAttribute("global::System.AttributeUsageAttribute");

            if (attributeUsageAttribute == null
                || attributeUsageAttribute.ConstructorArguments.Length < 1
            )
            {
                if (attribTypeSymbol.Name == "SerializeField")
                {
                    return (attribTypeSymbol.Name, AttributeTargets.Field);
                }

                return (string.Empty, 0);
            }

            return (attribTypeSymbol.Name, (AttributeTargets)attributeUsageAttribute.ConstructorArguments[0].Value);
        }
    }
}
