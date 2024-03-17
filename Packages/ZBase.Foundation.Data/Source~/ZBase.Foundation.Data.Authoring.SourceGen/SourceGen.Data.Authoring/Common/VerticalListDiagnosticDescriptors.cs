﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

#pragma warning disable IDE0090 // Use 'new DiagnosticDescriptor(...)'
#pragma warning disable RS2008 // Enable analyzer release tracking

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    internal static class VerticalListDiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor NotTypeOfExpression = new DiagnosticDescriptor(
              id: "DATABASE_VERTICAL_LIST_0001"
            , title: "Not a typeof expression"
            , messageFormat: "The first argument must be a 'typeof' expression"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The first argument must be a 'typeof' expression."
        );

        public static readonly DiagnosticDescriptor AbstractTypeNotSupported = new DiagnosticDescriptor(
              id: "DATABASE_VERTICAL_LIST_0003"
            , title: "Abstract type is not supported"
            , messageFormat: "The type \"{0}\" must not be abstract"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type must not be abstract to be considered a valid target type."
        );

        public static readonly DiagnosticDescriptor NotImplementIData = new DiagnosticDescriptor(
              id: "DATABASE_VERTICAL_LIST_0004"
            , title: "Target type does not implement IData"
            , messageFormat: "The type \"{0}\" must implement IData interface"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type must implement IData interface to be considered a valid target type."
        );

        public static readonly DiagnosticDescriptor InvalidPropertyName = new DiagnosticDescriptor(
              id: "DATABASE_VERTICAL_LIST_0005"
            , title: "Invalid property name"
            , messageFormat: "The property name must be a valid identifier"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The property name must be a valid identifier."
        );

        public static readonly DiagnosticDescriptor InvalidTableType = new DiagnosticDescriptor(
              id: "DATABASE_VERTICAL_LIST_0005"
            , title: "Invalid table type"
            , messageFormat: "The type \"{0}\" is not a valid table type, which must not be abstract nor generic, and must be derived from DataTableAsset<TId, TData>"
            , category: "DatabaseGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type \"{0}\" is not a valid table type, which must not be abstract nor generic, and must be derived from DataTableAsset<TId, TData>."
        );
    }
}