﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Everything in this file was copied from Unity's source generators.
namespace ZBase.Foundation.SourceGen
{
    public static class SymbolExtensions
    {
        private static SymbolDisplayFormat QualifiedFormat { get; }
            = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            );

        private static SymbolDisplayFormat QualifiedFormatWithoutGlobalPrefix { get; }
            = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            );

        private static SymbolDisplayFormat QualifiedFormatWithoutSpecialTypeNames { get; }
            = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
            );

        public static bool Is(this ITypeSymbol symbol, string fullyQualifiedName, bool checkBaseType = true)
        {
            fullyQualifiedName = PrependGlobalIfMissing(fullyQualifiedName);

            if (symbol is null)
                return false;

            if (symbol.ToDisplayString(QualifiedFormat) == fullyQualifiedName)
                return true;

            return checkBaseType && symbol.BaseType.Is(fullyQualifiedName);
        }

        public static IEnumerable<string> GetAllFullyQualifiedInterfaceAndBaseTypeNames(this ITypeSymbol symbol)
        {
            if (symbol.BaseType != null)
            {
                var baseTypeName = symbol.BaseType.ToDisplayString(QualifiedFormat);
                if (baseTypeName != "global::System.ValueType")
                    yield return baseTypeName;
            }

            foreach (var @interface in symbol.Interfaces)
                yield return @interface.ToDisplayString(QualifiedFormat);
        }

        public static bool IsInt(this ITypeSymbol symbol)
            => symbol.SpecialType == SpecialType.System_Int32;

        public static bool IsDynamicBuffer(this ITypeSymbol symbol)
            => symbol.Name == "DynamicBuffer"
            && symbol.ContainingNamespace.ToDisplayString(QualifiedFormat) == "global::Unity.Entities";

        public static bool IsSharedComponent(this ITypeSymbol symbol)
            => symbol.InheritsFromInterface("Unity.Entities.ISharedComponentData");

        public static bool IsComponent(this ITypeSymbol symbol)
            => symbol.InheritsFromInterface("Unity.Entities.IComponentData");

        public static bool IsZeroSizedComponent(this ITypeSymbol symbol, HashSet<ITypeSymbol> seenSymbols = null)
        {
            // TODO: This was recently fixed (https://github.com/dotnet/roslyn-analyzers/issues/5804), remove pragmas after we update .net
#pragma warning disable RS1024
            if (seenSymbols == null)
                seenSymbols = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default) {
                    symbol
                };
#pragma warning restore RS1024

            foreach (var field in symbol.GetMembers().OfType<IFieldSymbol>())
            {
                switch (symbol.SpecialType)
                {
                    case SpecialType.System_Void:
                        continue;

                    case SpecialType.None:
                        if (field.IsStatic || field.IsConst)
                            continue;

                        if (field.Type.TypeKind == TypeKind.Struct)
                        {
                            // Handle cycles in type (otherwise we will stack overflow)
                            if (!seenSymbols.Add(field.Type))
                                continue;

                            if (IsZeroSizedComponent(field.Type))
                                continue;
                        }
                        return false;

                    default:
                        return false;
                }
            }
            return true;
        }

        public static bool IsEnableableComponent(this ITypeSymbol symbol)
            => symbol.InheritsFromInterface("Unity.Entities.IEnableableComponent");

        public static string ToFullName(this ITypeSymbol symbol)
            => symbol.ToDisplayString(QualifiedFormat);

        public static string ToSimpleName(this ITypeSymbol symbol)
            => symbol.ToDisplayString(QualifiedFormatWithoutGlobalPrefix);

        public static string ToValidIdentifier(this ITypeSymbol symbol)
            => symbol.ToDisplayString(QualifiedFormatWithoutGlobalPrefix).ToValidIdentifier();

        public static bool ImplementsInterface(this ISymbol symbol, string interfaceName)
        {
            interfaceName = PrependGlobalIfMissing(interfaceName);

            return symbol is ITypeSymbol typeSymbol
                && typeSymbol.AllInterfaces.Any(i => i.ToFullName() == interfaceName || i.InheritsFromInterface(interfaceName));
        }

        public static bool Is(this ITypeSymbol symbol, string nameSpace, string typeName, bool checkBaseType = true)
        {
            if (symbol is null)
                return false;

            if (symbol.Name == typeName && symbol.ContainingNamespace?.Name == nameSpace)
                return true;

            return checkBaseType && symbol.BaseType.Is(nameSpace, typeName);
        }

        public static ITypeSymbol GetSymbolType(this ISymbol symbol)
        {
            return symbol switch
                   {
                       ILocalSymbol localSymbol => localSymbol.Type,
                       IParameterSymbol parameterSymbol => parameterSymbol.Type,
                       INamedTypeSymbol namedTypeSymbol => namedTypeSymbol,
                       IMethodSymbol methodSymbol => methodSymbol.ContainingType,
                       IPropertySymbol propertySymbol => propertySymbol.ContainingType,
                       _ => throw new InvalidOperationException($"Unknown typeSymbol type {symbol.GetType()}")
                   };
        }

        public static bool InheritsFromInterface(this ITypeSymbol symbol, string interfaceName, bool checkBaseType = true)
        {
            if (symbol is null)
                return false;

            interfaceName = PrependGlobalIfMissing(interfaceName);

            foreach (var @interface in symbol.Interfaces)
            {
                if (@interface.ToDisplayString(QualifiedFormat) == interfaceName)
                    return true;

                if (checkBaseType)
                {
                    foreach (var baseInterface in @interface.AllInterfaces)
                    {
                        if (baseInterface.ToDisplayString(QualifiedFormat) == interfaceName)
                            return true;

                        if (baseInterface.InheritsFromInterface(interfaceName))
                            return true;
                    }
                }
            }

            if (checkBaseType && symbol.BaseType != null)
            {
                if (symbol.BaseType.InheritsFromInterface(interfaceName))
                    return true;
            }

            return false;
        }

        public static bool InheritsFromType(this ITypeSymbol symbol, string typeName, bool checkBaseType = true)
        {
            typeName = PrependGlobalIfMissing(typeName);

            if (symbol is null)
                return false;

            if (symbol.ToDisplayString(QualifiedFormat) == typeName)
                return true;

            if (checkBaseType && symbol.BaseType != null)
            {
                if (symbol.BaseType.InheritsFromType(typeName))
                    return true;
            }

            return false;
        }

        public static bool HasAttribute(this ISymbol typeSymbol, string fullyQualifiedAttributeName)
        {
            fullyQualifiedAttributeName = PrependGlobalIfMissing(fullyQualifiedAttributeName);

            return typeSymbol.GetAttributes()
                .Any(attribute => attribute.AttributeClass.ToFullName() == fullyQualifiedAttributeName);
        }

        public static AttributeData GetAttribute(this ISymbol typeSymbol, string fullyQualifiedAttributeName)
        {
            fullyQualifiedAttributeName = PrependGlobalIfMissing(fullyQualifiedAttributeName);

            return typeSymbol.GetAttributes()
                .Where(attribute => attribute.AttributeClass.ToFullName() == fullyQualifiedAttributeName)
                .FirstOrDefault();
        }

        public static bool HasAttributeOrFieldWithAttribute(this ITypeSymbol typeSymbol, string fullyQualifiedAttributeName)
        {
            fullyQualifiedAttributeName = PrependGlobalIfMissing(fullyQualifiedAttributeName);

            return typeSymbol.HasAttribute(fullyQualifiedAttributeName)
                || typeSymbol.GetMembers().OfType<IFieldSymbol>()
                    .Any(f => !f.IsStatic && f.Type.HasAttributeOrFieldWithAttribute(fullyQualifiedAttributeName));
        }

        public static string GetMethodAndParamsAsString(this IMethodSymbol methodSymbol)
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append(methodSymbol.Name);

            for (var typeIndex = 0; typeIndex < methodSymbol.TypeParameters.Length; typeIndex++)
                strBuilder.Append($"_T{typeIndex}");

            foreach (var param in methodSymbol.Parameters)
            {
                if (param.RefKind != RefKind.None)
                    strBuilder.Append($"_{param.RefKind.ToString().ToLower()}");
                strBuilder.Append($"_{param.Type.ToDisplayString(QualifiedFormatWithoutSpecialTypeNames).Replace(" ", string.Empty)}");
            }

            return strBuilder.ToString();
        }

        public static bool IsAspect(this ITypeSymbol typeSymbol)
            => typeSymbol.InheritsFromInterface("Unity.Entities.IAspect");

        public static TypedConstantKind GetTypedConstantKind(this ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Char:
                case SpecialType.System_String:
                case SpecialType.System_Object:
                    return TypedConstantKind.Primitive;

                default:
                    switch (type.TypeKind)
                    {
                        case TypeKind.Array:
                            return TypedConstantKind.Array;
                        case TypeKind.Enum:
                            return TypedConstantKind.Enum;
                        case TypeKind.Error:
                            return TypedConstantKind.Error;
                    }
                    return TypedConstantKind.Type;
            }
        }

        private static string PrependGlobalIfMissing(this string typeOrNamespaceName)
            => typeOrNamespaceName.StartsWith("global::") == false
            ? $"global::{typeOrNamespaceName}"
            : typeOrNamespaceName;

        /// <summary>
        /// Checks whether or not a given symbol has an attribute with the specified fully qualified metadata name.
        /// </summary>
        /// <param name="symbol">The input <see cref="ISymbol"/> instance to check.</param>
        /// <param name="typeSymbol">The <see cref="ITypeSymbol"/> instance for the attribute type to look for.</param>
        /// <returns>Whether or not <paramref name="symbol"/> has an attribute with the specified type.</returns>
        public static bool HasAttributeWithType(this ISymbol symbol, ITypeSymbol typeSymbol)
        {
            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, typeSymbol))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gathers all forwarded attributes for the generated field and property.
        /// </summary>
        /// <param name="methodSymbol">The input <see cref="IMethodSymbol"/> instance to process.</param>
        /// <param name="semanticModel">The <see cref="SemanticModel"/> instance for the current run.</param>
        /// <param name="token">The cancellation token for the current operation.</param>
        /// <param name="diagnostics">The current collection of gathered diagnostics.</param>
        /// <param name="fieldAttributes">The resulting field attributes to forward.</param>
        /// <param name="propertyAttributes">The resulting property attributes to forward.</param>
        public static void GatherForwardedAttributes(
              this IMethodSymbol methodSymbol
            , SemanticModel semanticModel
            , CancellationToken token
            , in ImmutableArrayBuilder<DiagnosticInfo> diagnostics
            , out ImmutableArray<AttributeInfo> fieldAttributes
            , out ImmutableArray<AttributeInfo> propertyAttributes
            , DiagnosticDescriptor diagnostic
        )
        {
            using var fieldAttributesInfo = ImmutableArrayBuilder<AttributeInfo>.Rent();
            using var propertyAttributesInfo = ImmutableArrayBuilder<AttributeInfo>.Rent();

            // If the method is a partial definition, also gather attributes from the implementation part
            if (methodSymbol is { IsPartialDefinition: true } or { PartialDefinitionPart: not null })
            {
                IMethodSymbol partialDefinition = methodSymbol.PartialDefinitionPart ?? methodSymbol;
                IMethodSymbol partialImplementation = methodSymbol.PartialImplementationPart ?? methodSymbol;

                // We always give priority to the partial definition, to ensure a predictable and testable ordering
                GatherForwardedAttributes(
                      partialDefinition
                    , semanticModel
                    , token
                    , in diagnostics
                    , in fieldAttributesInfo
                    , in propertyAttributesInfo
                    , diagnostic
                );

                GatherForwardedAttributes(
                      partialImplementation
                    , semanticModel
                    , token
                    , in diagnostics
                    , in fieldAttributesInfo
                    , in propertyAttributesInfo
                    , diagnostic
                );
            }
            else
            {
                // If the method is not a partial definition/implementation, just gather attributes from the method with no modifications
                GatherForwardedAttributes(
                      methodSymbol
                    , semanticModel
                    , token
                    , in diagnostics
                    , in fieldAttributesInfo
                    , in propertyAttributesInfo
                    , diagnostic
                );
            }

            fieldAttributes = fieldAttributesInfo.ToImmutable();
            propertyAttributes = propertyAttributesInfo.ToImmutable();

            static void GatherForwardedAttributes(
                  IMethodSymbol symbol
                , SemanticModel semanticModel
                , CancellationToken token
                , in ImmutableArrayBuilder<DiagnosticInfo> diagnostics
                , in ImmutableArrayBuilder<AttributeInfo> fieldAttributesInfo
                , in ImmutableArrayBuilder<AttributeInfo> propertyAttributesInfo
                , DiagnosticDescriptor diagnostic
            )
            {
                // Get the single syntax reference for the input method symbol (there should be only one)
                if (symbol.DeclaringSyntaxReferences.Length != 1
                    || symbol.DeclaringSyntaxReferences[0] is not SyntaxReference syntaxReference
                )
                {
                    return;
                }

                // Try to get the target method declaration syntax node
                if (syntaxReference.GetSyntax(token) is not MethodDeclarationSyntax methodDeclaration)
                {
                    return;
                }

                // Gather explicit forwarded attributes info
                foreach (AttributeListSyntax attributeList in methodDeclaration.AttributeLists)
                {
                    // Same as in the [ObservableProperty] generator, except we're also looking for fields here
                    if (attributeList.Target == null
                        || attributeList.Target.Identifier.Kind() is not (SyntaxKind.PropertyKeyword or SyntaxKind.FieldKeyword)
                    )
                    {
                        continue;
                    }

                    foreach (AttributeSyntax attribute in attributeList.Attributes)
                    {
                        // Get the symbol info for the attribute (once again just like in the [ObservableProperty] generator)
                        if (!semanticModel.GetSymbolInfo(attribute, token).TryGetAttributeTypeSymbol(out INamedTypeSymbol attributeTypeSymbol))
                        {
                            diagnostics.Add(diagnostic, attribute, symbol, attribute.Name);
                            continue;
                        }

                        var attributeInfo = AttributeInfo.From(attributeTypeSymbol, semanticModel, attribute.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>(), token);

                        // Add the new attribute info to the right builder
                        if (attributeList.Target != null)
                        {
                            if (attributeList.Target.Identifier.IsKind(SyntaxKind.FieldKeyword))
                            {
                                fieldAttributesInfo.Add(attributeInfo);
                            }
                            else
                            {
                                propertyAttributesInfo.Add(attributeInfo);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gathers all forwarded attributes for the generated field and property.
        /// </summary>
        /// <param name="fieldSymbol">The input <see cref="IMethodSymbol"/> instance to process.</param>
        /// <param name="semanticModel">The <see cref="SemanticModel"/> instance for the current run.</param>
        /// <param name="token">The cancellation token for the current operation.</param>
        /// <param name="diagnostics">The current collection of gathered diagnostics.</param>
        /// <param name="fieldAttributes">The resulting field attributes to forward.</param>
        /// <param name="propertyAttributes">The resulting property attributes to forward.</param>
        public static void GatherForwardedAttributes(
              this IFieldSymbol fieldSymbol
            , SemanticModel semanticModel
            , CancellationToken token
            , in ImmutableArrayBuilder<DiagnosticInfo> diagnostics
            , out ImmutableArray<AttributeInfo> propertyAttributes
            , DiagnosticDescriptor diagnostic
        )
        {
            using var propertyAttributesInfo = ImmutableArrayBuilder<AttributeInfo>.Rent();

            GatherForwardedAttributes(
                  fieldSymbol
                , semanticModel
                , token
                , in diagnostics
                , in propertyAttributesInfo
                , diagnostic
            );

            propertyAttributes = propertyAttributesInfo.ToImmutable();

            static void GatherForwardedAttributes(
                  IFieldSymbol symbol
                , SemanticModel semanticModel
                , CancellationToken token
                , in ImmutableArrayBuilder<DiagnosticInfo> diagnostics
                , in ImmutableArrayBuilder<AttributeInfo> propertyAttributesInfo
                , DiagnosticDescriptor diagnostic
            )
            {
                // Get the single syntax reference for the input method symbol (there should be only one)
                if (symbol.DeclaringSyntaxReferences.Length != 1
                    || symbol.DeclaringSyntaxReferences[0] is not SyntaxReference syntaxReference
                )
                {
                    return;
                }

                var syntax = syntaxReference.GetSyntax(token);

                if (syntax.Parent?.Parent is not FieldDeclarationSyntax fieldDeclaration)
                {
                    return;
                }

                // Gather explicit forwarded attributes info
                foreach (AttributeListSyntax attributeList in fieldDeclaration.AttributeLists)
                {
                    // Same as in the [ObservableProperty] generator, except we're also looking for fields here
                    if (attributeList.Target == null
                        || attributeList.Target.Identifier.Kind() is not SyntaxKind.PropertyKeyword
                    )
                    {
                        continue;
                    }

                    foreach (AttributeSyntax attribute in attributeList.Attributes)
                    {
                        // Get the symbol info for the attribute (once again just like in the [ObservableProperty] generator)
                        if (!semanticModel.GetSymbolInfo(attribute, token).TryGetAttributeTypeSymbol(out INamedTypeSymbol attributeTypeSymbol))
                        {
                            diagnostics.Add(diagnostic, attribute, symbol, attribute.Name);
                            continue;
                        }

                        var attributeInfo = AttributeInfo.From(attributeTypeSymbol, semanticModel, attribute.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>(), token);

                        // Add the new attribute info to the right builder
                        if (attributeList.Target != null)
                        {
                            if (attributeList.Target.Identifier.IsKind(SyntaxKind.PropertyKeyword))
                            {
                                propertyAttributesInfo.Add(attributeInfo);
                            }
                        }
                    }
                }
            }
        }
    }
}

