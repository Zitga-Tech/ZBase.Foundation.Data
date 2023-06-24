using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public partial class DatabaseDeclaration
    {
        public const string GENERATOR_NAME = nameof(DatabaseGenerator);
        public const string IDATA = "global::ZBase.Foundation.Data.IData";
        public const string DATA_TABLE_ASSET_T = "global::ZBase.Foundation.Data.DataTableAsset<";
        public const string TABLE_ATTRIBUTE = "global::ZBase.Foundation.Data.Authoring.TableAttribute";
        public const string VERTICAL_LIST_ATTRIBUTE = "global::ZBase.Foundation.Data.Authoring.VerticalListAttribute";

        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DatabaseGenerator\", \"1.0.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

        public DatabaseRef DatabaseRef { get; }

        public DatabaseDeclaration(DatabaseRef databaseRef)
        {
            this.DatabaseRef = databaseRef;
            InitializeTables();

            if (DatabaseRef.Tables.Length > 0)
            {
                InitializeVerticalLists();
            }
        }

        private void InitializeTables()
        {
            var uniqueTypeNames = new HashSet<string>();
            var tables = new List<DatabaseRef.Table>();
            var attributes = DatabaseRef.Symbol.GetAttributes(TABLE_ATTRIBUTE);

            foreach (var attrib in attributes)
            {
                var args = attrib.ConstructorArguments;

                if (args.Length < 1
                    || args[0].Value is not INamedTypeSymbol type
                    || type.IsAbstract
                    || type.IsGenericType
                    || type.BaseType == null
                )
                {
                    continue;
                }

                if (type.TryGetGenericType(DATA_TABLE_ASSET_T, 2, out var baseType) == false)
                {
                    continue;
                }

                var fullTypeName = type.ToFullName();

                if (uniqueTypeNames.Contains(fullTypeName))
                {
                    continue;
                }

                uniqueTypeNames.Add(fullTypeName);

                var table = new DatabaseRef.Table {
                    Type = type,
                    BaseType = baseType,
                };

                if (args.Length > 1)
                {
                    table.SheetName = args[1].Value.ToString();
                }
                else
                {
                    table.SheetName = type.Name;
                }

                if (args.Length > 2)
                {
                    table.NamingStrategy = args[2].Value.ToNamingStrategy();
                }

                tables.Add(table);
            }

            if (tables.Count > 0)
            {
                using var arrayBuilder = ImmutableArrayBuilder<DatabaseRef.Table>.Rent();
                arrayBuilder.AddRange(tables);
                DatabaseRef.Tables = arrayBuilder.ToImmutable();
            }
            else
            {
                DatabaseRef.Tables = ImmutableArray<DatabaseRef.Table>.Empty;
            }
        }

        private void InitializeVerticalLists()
        {
            var verticalListMap = DatabaseRef.VerticalListMap = new();
            var attributes = DatabaseRef.Symbol.GetAttributes(VERTICAL_LIST_ATTRIBUTE);

            foreach (var attrib in attributes)
            {
                var args = attrib.ConstructorArguments;

                if (args.Length < 2
                    || args[0].Value is not INamedTypeSymbol targetType
                    || targetType.IsAbstract
                    || targetType.InheritsFromInterface(IDATA, true) == false
                    || args[1].Value is not string propertyName
                    || string.IsNullOrWhiteSpace(propertyName)
                )
                {
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
