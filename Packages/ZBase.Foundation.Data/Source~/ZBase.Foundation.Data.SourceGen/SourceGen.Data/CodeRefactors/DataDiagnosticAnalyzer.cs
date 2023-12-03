using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.CodeRefactors
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class DataDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_FIELD = "DATA0010";
        public const string DIAGNOSTIC_PROPERTY = "DATA0011";

        public const string INTERFACE = "ZBase.Foundation.Data.IData";
        public const string DATA_PROPERTY_ATTRIBUTE = "global::ZBase.Foundation.Data.DataPropertyAttribute";
        public const string SERIALIZE_FIELD_ATTRIBUTE = "global::UnityEngine.SerializeField";
        public const string JSON_INCLUDE_ATTRIBUTE = "global::System.Text.Json.Serialization.JsonIncludeAttribute";
        public const string JSON_PROPERTY_ATTRIBUTE = "global::Newtonsoft.Json.JsonPropertyAttribute";

        private static readonly DiagnosticDescriptor s_diagnosticField = new(
              id: DIAGNOSTIC_FIELD
            , title: "Field can be replaced by a property"
            , messageFormat: "Field name '{0}' can be replaced by a property"
            , category: nameof(DataDiagnosticAnalyzer)
            , DiagnosticSeverity.Hidden
            , isEnabledByDefault: true
            , description: "Field can be replaced by a property."
        );

        private static readonly DiagnosticDescriptor s_diagnosticProperty = new(
              id: DIAGNOSTIC_PROPERTY
            , title: "Property can be replaced by a field"
            , messageFormat: "Property name '{0}' can be replaced by a field"
            , category: nameof(DataDiagnosticAnalyzer)
            , DiagnosticSeverity.Hidden
            , isEnabledByDefault: true
            , description: "Property can be replaced by a field."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(s_diagnosticField, s_diagnosticProperty);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        }

        private void AnalyzeField(SymbolAnalysisContext context)
        {
            if (context.Symbol is not IFieldSymbol fieldSymbol
                || fieldSymbol.ContainingType is not INamedTypeSymbol typeSymbol
                || typeSymbol.InheritsFromInterface(INTERFACE) == false
                || (fieldSymbol.HasAttribute(SERIALIZE_FIELD_ATTRIBUTE) == false
                    && fieldSymbol.HasAttribute(JSON_INCLUDE_ATTRIBUTE) == false
                    && fieldSymbol.HasAttribute(JSON_PROPERTY_ATTRIBUTE) == false
                )
            )
            {
                return;
            }

            var diagnostic = Diagnostic.Create(s_diagnosticField, fieldSymbol.Locations[0], fieldSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        private void AnalyzeProperty(SymbolAnalysisContext context)
        {
            if (context.Symbol is not IPropertySymbol propSymbol
                || propSymbol.ContainingType is not INamedTypeSymbol typeSymbol
                || typeSymbol.InheritsFromInterface(INTERFACE) == false
                || propSymbol.HasAttribute(DATA_PROPERTY_ATTRIBUTE) == false
            )
            {
                return;
            }

            var diagnostic = Diagnostic.Create(s_diagnosticProperty, propSymbol.Locations[0], propSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
