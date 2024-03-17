// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

#pragma warning disable IDE0090 // Use 'new DiagnosticDescriptor(...)'
#pragma warning disable RS2008 // Enable analyzer release tracking

namespace ZBase.Foundation.Data.DataSourceGen
{
    /// <summary>
    /// A container for all <see cref="DiagnosticDescriptor"/> instances for errors reported by analyzers in this project.
    /// </summary>
    internal static class DiagnosticDescriptors
    {
        /// <summary>
        /// Gets a <see cref="DiagnosticDescriptor"/> indicating when a field with <c>[ObservableProperty]</c> is using an invalid attribute targeting the property.
        /// <para>
        /// Format: <c>"The field {0} annotated with [ObservableProperty] is using attribute "{1}" which was not recognized as a valid type (are you missing a using directive?)"</c>.
        /// </para>
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidPropertyTargetedAttribute = new DiagnosticDescriptor(
              id: "DATA_0001"
            , title: "Invalid property targeted attribute type"
            , messageFormat: "The field {0} is using attribute \"{1}\" which was not recognized as a valid type (are you missing a using directive?)"
            , category: "DataGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "All attributes targeting the generated property for a field must correctly be resolved to valid types."
        );

        /// <summary>
        /// Gets a <see cref="DiagnosticDescriptor"/> indicating when a property with <c>[ObservableProperty]</c> is using an invalid attribute targeting the property.
        /// <para>
        /// Format: <c>"The property {0} annotated with [ObservableProperty] is using attribute "{1}" which was not recognized as a valid type (are you missing a using directive?)"</c>.
        /// </para>
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidFieldTargetedAttribute = new DiagnosticDescriptor(
              id: "DATA_0002"
            , title: "Invalid field targeted attribute type"
            , messageFormat: "The property is using attribute \"{1}\" which was not recognized as a valid type (are you missing a using directive?)"
            , category: "DataGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "All attributes targeting the generated field for a property must correctly be resolved to valid types."
        );
    }
}
