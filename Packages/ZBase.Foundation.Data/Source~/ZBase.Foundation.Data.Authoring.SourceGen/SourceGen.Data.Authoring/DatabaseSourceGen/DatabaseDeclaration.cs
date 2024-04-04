using System.Collections.Generic;
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
            InitializeNamingStrategy();
            InitializeConverters(context);
            InitializeTables(context);
        }

        private void InitializeNamingStrategy()
        {
            var attrib = DatabaseRef.Attribute;
            var args = attrib.ConstructorArguments;

            foreach (var arg in args)
            {
                if (arg.Kind != TypedConstantKind.Array && arg.Value != null)
                {
                    DatabaseRef.NamingStrategy = arg.Value.ToNamingStrategy();
                    break;
                }
            }
        }

        private void InitializeConverters(SourceProductionContext context)
        {
            var attrib = DatabaseRef.Attribute;
            var args = attrib.ConstructorArguments;

            foreach (var arg in args)
            {
                if (arg.Kind == TypedConstantKind.Array)
                {
                    arg.Values.MakeConverterMap(context, DatabaseRef.Syntax, attrib, DatabaseRef.ConverterMap, 0);
                    break;
                }
            }
        }

        private void InitializeTables(SourceProductionContext context)
        {
            var tables = new List<TableRef>();
            var databaseRef = DatabaseRef;
            var members = databaseRef.Symbol.GetMembers();
            var outerNode = databaseRef.Syntax;
            var namingStrategy = databaseRef.NamingStrategy;
            
            foreach (var member in members)
            {
                INamedTypeSymbol type;

                if (member is IFieldSymbol field)
                {
                    type = field.Type as INamedTypeSymbol;
                }
                else if (member is IPropertySymbol property)
                {
                    type = property.Type as INamedTypeSymbol;
                }
                else
                {
                    continue;
                }

                if (type == null)
                {
                    continue;
                }

                var attrib = member.GetAttribute(TABLE_ATTRIBUTE);

                if (attrib == null)
                {
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

                var table = new TableRef {
                    Type = type,
                    BaseType = baseType,
                    SheetName = member.Name,
                    NamingStrategy = namingStrategy,
                };

                foreach (var arg in attrib.ConstructorArguments)
                {
                    if (arg.Kind != TypedConstantKind.Array && arg.Value != null)
                    {
                        table.NamingStrategy = arg.Value.ToNamingStrategy();
                    }
                    else if (arg.Kind == TypedConstantKind.Array)
                    {
                        arg.Values.MakeConverterMap(context, outerNode, attrib, table.ConverterMap, 2);
                    }
                }

                tables.Add(table);

                GetVerticalLists(context, databaseRef, member, table);
            }

            if (tables.Count > 0)
            {
                using var arrayBuilder = ImmutableArrayBuilder<TableRef>.Rent();
                arrayBuilder.AddRange(tables);
                databaseRef.SetTables(arrayBuilder.ToImmutable());
            }
        }

        private static void GetVerticalLists(
              SourceProductionContext context
            , DatabaseRef databaseRef
            , ISymbol member
            , TableRef tableRef
        )
        {
            var verticalListMap = databaseRef.VerticalListMap;
            var attributes = member.GetAttributes(VERTICAL_LIST_ATTRIBUTE);

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

                var tableType = tableRef.Type;

                if (verticalListMap.TryGetValue(targetType, out var innerMap) == false)
                {
                    verticalListMap[targetType] = innerMap = new(SymbolEqualityComparer.Default);
                }

                if (innerMap.TryGetValue(tableType, out var propertNames) == false)
                {
                    innerMap[tableType] = propertNames = new();
                }

                propertNames.Add(propertyName);
            }
        }
    }
}
