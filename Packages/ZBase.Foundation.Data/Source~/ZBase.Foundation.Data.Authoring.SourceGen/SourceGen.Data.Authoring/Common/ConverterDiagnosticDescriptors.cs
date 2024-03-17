// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

#pragma warning disable IDE0090 // Use 'new DiagnosticDescriptor(...)'
#pragma warning disable RS2008 // Enable analyzer release tracking

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    internal static class ConverterDiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor MissingDefaultConstructor = new DiagnosticDescriptor(
              id: "DATABASE_CONVERTER_0001"
            , title: "Missing default constructor"
            , messageFormat: "The type \"{0}\" must contain a default (parameterless) constructor"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type must contain a default (parameterless) constructor to be considered a valid converter."
        );

        public static readonly DiagnosticDescriptor ConvertMethodAmbiguity = new DiagnosticDescriptor(
              id: "DATABASE_CONVERTER_0002"
            , title: "Conversion method ambiguity"
            , messageFormat: "The type \"{0}\" contains multiple public methods named \"Convert\" thus it cannot be used as a converter"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type must contain exactly 1 public method named \"Convert\" to be considered a valid converter."
        );

        public static readonly DiagnosticDescriptor MissingConvertMethod = new DiagnosticDescriptor(
              id: "DATABASE_CONVERTER_0003"
            , title: "Missing conversion method"
            , messageFormat: "The type \"{0}\" does not contain any public method named \"Convert\" that accepts a single parameter of any non-void type and returns a value of type \"{1}\""
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type must contain exactly 1 public method named \"Convert\" to be considered a valid converter."
        );

        public static readonly DiagnosticDescriptor MissingConvertMethodReturnType = new DiagnosticDescriptor(
              id: "DATABASE_CONVERTER_0004"
            , title: "Missing conversion method"
            , messageFormat: "The type \"{0}\" does not contain any public method named \"Convert\" that accepts a single parameter of any non-void type and returns a value of any non-void type"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type must contain exactly 1 public method named \"Convert\" to be considered a valid converter."
        );

        public static readonly DiagnosticDescriptor InvalidConvertMethodReturnType = new DiagnosticDescriptor(
              id: "DATABASE_CONVERTER_0005"
            , title: "Invalid conversion method"
            , messageFormat: "The public \"Convert\" method of type \"{0}\" must accept a single parameter of any non-void type and must return a value of type \"{1}\""
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The public \"Convert\" method must conform to the valid format."
        );

        public static readonly DiagnosticDescriptor InvalidConvertMethod = new DiagnosticDescriptor(
              id: "DATABASE_CONVERTER_0006"
            , title: "Invalid conversion method"
            , messageFormat: "The public \"Convert\" method of type \"{0}\" must accept a single parameter of any non-void type and must return a value of any non-void type"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The public \"Convert\" method must conform to the valid format."
        );

        public static readonly DiagnosticDescriptor NotTypeOfExpression = new DiagnosticDescriptor(
              id: "DATABASE_CONVERTER_0007"
            , title: "Not a typeof expression"
            , messageFormat: "The first argument must be a 'typeof' expression"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The first argument must be a 'typeof' expression."
        );

        public static readonly DiagnosticDescriptor NotTypeOfExpressionAt = new DiagnosticDescriptor(
              id: "DATABASE_CONVERTER_0008"
            , title: "Not a typeof expression"
            , messageFormat: "The argument at position {0} must be a 'typeof' expression"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The argument must be a 'typeof' expression."
        );

        public static readonly DiagnosticDescriptor AbstractTypeNotSupported = new DiagnosticDescriptor(
              id: "DATABASE_CONVERTER_0009"
            , title: "Abstract type is not supported"
            , messageFormat: "The type \"{0}\" must not be abstract"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type must not be abstract to be considered a valid converter."
        );

        public static readonly DiagnosticDescriptor OpenGenericTypeNotSupported = new DiagnosticDescriptor(
              id: "DATABASE_CONVERTER_0010"
            , title: "Open generic type is not supported"
            , messageFormat: "The type \"{0}\" must not be open generic"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type must not be open generic to be considered a valid converter."
        );

        public static readonly DiagnosticDescriptor RedundantConverter = new DiagnosticDescriptor(
              id: "DATABASE_CONVERTER_0011"
            , title: "Redundant converter"
            , messageFormat: "The type \"{0}\" at position {4} is redundant because a method of the same signature \"{2} Convert({3})\" has been already defined by {1}"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Warning
            , isEnabledByDefault: true
            , description: "A \"Convert\" method of the same signature has already been defined by another type."
        );
    }
}
