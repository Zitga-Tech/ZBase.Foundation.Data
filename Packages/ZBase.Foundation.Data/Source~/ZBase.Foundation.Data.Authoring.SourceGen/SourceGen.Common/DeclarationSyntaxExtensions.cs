using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZBase.Foundation.SourceGen
{
    public static class DeclarationSyntaxExtensions
    {
        public static ImmutableArray<AttributeInfo> GatherForwardedAttributes(
              this PropertyDeclarationSyntax propertyDeclaration
            , SemanticModel semanticModel
            , CancellationToken token
        )
        {
            using var fieldAttributesInfo = ImmutableArrayBuilder<AttributeInfo>.Rent();

            GatherForwardedAttributes(
                  propertyDeclaration
                , semanticModel
                , token
                , in fieldAttributesInfo
            );

            return fieldAttributesInfo.ToImmutable();

            static void GatherForwardedAttributes(
                  PropertyDeclarationSyntax propertyDeclaration
                , SemanticModel semanticModel
                , CancellationToken token
                , in ImmutableArrayBuilder<AttributeInfo> fieldAttributesInfo
            )
            {
                if (propertyDeclaration == null)
                {
                    return;
                }

                foreach (AttributeListSyntax attributeList in propertyDeclaration.AttributeLists)
                {
                    if (attributeList.Target == null
                        || attributeList.Target.Identifier.Kind() is not SyntaxKind.FieldKeyword
                    )
                    {
                        continue;
                    }

                    foreach (AttributeSyntax attribute in attributeList.Attributes)
                    {
                        var tryGetAttrib = semanticModel.GetSymbolInfo(attribute, token)
                            .TryGetAttributeTypeSymbol(out INamedTypeSymbol attributeTypeSymbol);

                        if (tryGetAttrib == false)
                        {
                            continue;
                        }

                        var attributeInfo = AttributeInfo.From(
                              attributeTypeSymbol
                            , semanticModel
                            , attribute.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>()
                            , token
                        );

                        if (attributeList.Target != null)
                        {
                            if (attributeList.Target.Identifier.IsKind(SyntaxKind.FieldKeyword))
                            {
                                fieldAttributesInfo.Add(attributeInfo);
                            }
                        }
                    }
                }
            }
        }
    }
}
