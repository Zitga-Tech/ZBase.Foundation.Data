using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZBase.Foundation.SourceGen;
using static ZBase.Foundation.Data.DatabaseSourceGen.Helpers;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public partial class DatabaseDeclaration
    {
        public DatabaseRef DatabaseRef { get; }

        public DatabaseDeclaration(SourceProductionContext context, DatabaseRef databaseRef)
        {
            DatabaseRef = databaseRef;
            InitializeConverters(context);
            InitializeTables(context);

            if (DatabaseRef.Tables.Length > 0)
            {
                InitializeVerticalLists(context);
            }
        }

        private void InitializeConverters(SourceProductionContext context)
        {
            var attrib = DatabaseRef.Attribute;
            var args = attrib.ConstructorArguments;

            if (args.Length < 1)
            {
                return;
            }

            var arg = args[0];

            if (arg.Kind == TypedConstantKind.Array)
            {
                arg.Values.GetConverterMapMap(context, attrib, DatabaseRef.ConverterMapMap, 0);
            }
        }

        private void InitializeTables(SourceProductionContext context)
        {
            var uniqueTypeNames = new HashSet<string>();
            var tables = new List<TableRef>();
            var attributes = DatabaseRef.Symbol.GetAttributes(TABLE_ATTRIBUTE);

            foreach (var attrib in attributes)
            {
                var args = attrib.ConstructorArguments;

                if (args.Length < 1)
                {
                    continue;
                }

                if (args[0].Value is not INamedTypeSymbol type)
                {
                    context.ReportDiagnostic(
                          TableDiagnosticDescriptors.NotTypeOfExpression
                        , attrib.ApplicationSyntaxReference.GetSyntax()
                    );
                    continue;
                }

                if (type.IsAbstract)
                {
                    context.ReportDiagnostic(
                          TableDiagnosticDescriptors.AbstractTypeNotSupported
                        , attrib.ApplicationSyntaxReference.GetSyntax()
                        , type.Name
                    );
                    continue;
                }

                if (type.IsGenericType)
                {
                    context.ReportDiagnostic(
                          TableDiagnosticDescriptors.GenericTypeNotSupported
                        , attrib.ApplicationSyntaxReference.GetSyntax()
                        , type.Name
                    );
                    continue;
                }

                if (type.BaseType == null
                    || type.TryGetGenericType(DATA_TABLE_ASSET_T, 2, out var baseType) == false
                )
                {
                    context.ReportDiagnostic(
                          TableDiagnosticDescriptors.NotDerivedFromDataTableAsset
                        , attrib.ApplicationSyntaxReference.GetSyntax()
                        , type.Name
                    );
                    continue;
                }

                var fullTypeName = type.ToFullName();

                if (uniqueTypeNames.Contains(fullTypeName))
                {
                    continue;
                }

                uniqueTypeNames.Add(fullTypeName);

                var table = new TableRef {
                    Type = type,
                    BaseType = baseType,
                };

                if (args.Length > 1)
                {
                    var arg = args[1];

                    if (arg.Kind != TypedConstantKind.Array && arg.Value is string sheetName)
                    {
                        table.SheetName = sheetName;
                    }
                    else if (arg.Kind == TypedConstantKind.Array)
                    {
                        arg.Values.GetConverterMapMap(context, attrib, table.ConverterMapMap, 1);
                    }
                }

                if (args.Length > 2)
                {
                    var arg = args[2];

                    if (arg.Kind != TypedConstantKind.Array && arg.Value != null)
                    {
                        table.NamingStrategy = arg.Value.ToNamingStrategy();
                    }
                    else if (arg.Kind == TypedConstantKind.Array)
                    {
                        arg.Values.GetConverterMapMap(context, attrib, table.ConverterMapMap, 2);
                    }
                }

                if (args.Length > 3)
                {
                    var arg = args[3];

                    if (arg.Kind == TypedConstantKind.Array)
                    {
                        arg.Values.GetConverterMapMap(context, attrib, table.ConverterMapMap, 3);
                    }
                }

                if (string.IsNullOrWhiteSpace(table.SheetName))
                {
                    table.SheetName = type.Name;
                }

                tables.Add(table);
            }

            if (tables.Count > 0)
            {
                using var arrayBuilder = ImmutableArrayBuilder<TableRef>.Rent();
                arrayBuilder.AddRange(tables);
                DatabaseRef.SetTables(arrayBuilder.ToImmutable());
            }
        }

        private void InitializeVerticalLists(SourceProductionContext context)
        {
            var verticalListMap = DatabaseRef.VerticalListMap;
            var attributes = DatabaseRef.Symbol.GetAttributes(VERTICAL_LIST_ATTRIBUTE);

            foreach (var attrib in attributes)
            {
                var args = attrib.ConstructorArguments;

                if (args.Length < 2)
                {
                    continue;
                }

                if (args[0].Value is not INamedTypeSymbol targetType)
                {
                    context.ReportDiagnostic(
                          VerticalListDiagnosticDescriptors.NotTypeOfExpression
                        , attrib.ApplicationSyntaxReference.GetSyntax()
                    );
                    continue;
                }

                if (targetType.IsAbstract)
                {
                    context.ReportDiagnostic(
                          VerticalListDiagnosticDescriptors.AbstractTypeNotSupported
                        , attrib.ApplicationSyntaxReference.GetSyntax()
                        , targetType.Name
                    );
                    continue;
                }

                if (targetType.InheritsFromInterface(IDATA, true) == false)
                {
                    context.ReportDiagnostic(
                          VerticalListDiagnosticDescriptors.NotImplementIData
                        , attrib.ApplicationSyntaxReference.GetSyntax()
                        , targetType.Name
                    );
                    continue;
                }

                if (args[1].Value is not string propertyName || string.IsNullOrWhiteSpace(propertyName))
                {
                    context.ReportDiagnostic(
                          VerticalListDiagnosticDescriptors.InvalidPropertyName
                        , attrib.ApplicationSyntaxReference.GetSyntax()
                    );
                    continue;
                }

                var targetTypeFullName = targetType.ToFullName();
                string dataTableAssetTypeFullName;

                if (args.Length > 2)
                {
                    if (args[2].Value is INamedTypeSymbol containingType
                        && containingType.IsAbstract == false
                        && containingType.IsGenericType == false
                        && containingType.TryGetGenericType(DATA_TABLE_ASSET_T, 2, out _)
                    )
                    {
                        dataTableAssetTypeFullName = containingType.ToFullName();
                    }
                    else
                    {
                        context.ReportDiagnostic(
                              VerticalListDiagnosticDescriptors.InvalidTableType
                            , attrib.ApplicationSyntaxReference.GetSyntax()
                            , args[2].Value?.ToString() ?? string.Empty
                        );
                        continue;
                    }
                }
                else
                {
                    dataTableAssetTypeFullName = string.Empty;
                }

                if (verticalListMap.TryGetValue(targetTypeFullName, out var innerMap) == false)
                {
                    verticalListMap[targetTypeFullName] = innerMap = new();
                }

                if (innerMap.TryGetValue(dataTableAssetTypeFullName, out var propertNames) == false)
                {
                    innerMap[dataTableAssetTypeFullName] = propertNames = new();
                }

                propertNames.Add(propertyName);
            }
        }
    }
}
