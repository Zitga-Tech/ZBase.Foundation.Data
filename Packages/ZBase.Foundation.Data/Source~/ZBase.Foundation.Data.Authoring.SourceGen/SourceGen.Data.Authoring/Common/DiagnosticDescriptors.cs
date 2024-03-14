// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

#pragma warning disable IDE0090 // Use 'new DiagnosticDescriptor(...)'
#pragma warning disable RS2008 // Enable analyzer release tracking

namespace ZBase.Foundation.Data
{
    internal static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor MissingDefaultConstructor = new DiagnosticDescriptor(
              id: "DATA0010"
            , title: "Missing default constructor"
            , messageFormat: "The type \"{0}\" must contain a default (parameterless) constructor"
            , category: "DataGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type must contain a default (parameterless) constructor to be considered a valid converter."
        );

        public static readonly DiagnosticDescriptor ConvertMethodAmbiguity = new DiagnosticDescriptor(
              id: "DATA0011"
            , title: "Conversion method ambiguity"
            , messageFormat: "The type \"{0}\" contains multiple public methods named \"Convert\" thus it cannot be used as a converter"
            , category: "DataGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type must contain exactly 1 public method named \"Convert\" to be considered a valid converter."
        );

        public static readonly DiagnosticDescriptor MissingConvertMethod = new DiagnosticDescriptor(
              id: "DATA0012"
            , title: "Missing conversion method"
            , messageFormat: "The type \"{0}\" does not contain any public method named \"Convert\" that accepts a single parameter of any type and returns a value of type \"{1}\""
            , category: "DataGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type must contain exactly 1 public method named \"Convert\" to be considered a valid converter."
        );

        public static readonly DiagnosticDescriptor InvalidConvertMethod = new DiagnosticDescriptor(
              id: "DATA0013"
            , title: "Invalid conversion method"
            , messageFormat: "The public \"Convert\" method of type \"{0}\" must accept a single parameter of any type and must return a value of type \"{1}\""
            , category: "DataGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The public \"Convert\" method must conform to the valid format."
        );
    }
}
