using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public partial class DatabaseDeclaration
    {
        public const string GENERATOR_NAME = nameof(DatabaseGenerator);
        public const string TABLE_ATTRIBUTE = "global::ZBase.Foundation.Data.Authoring.TableAttribute";

        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DatabaseGenerator\", \"1.0.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

        public DatabaseRef DatabaseRef { get; }

        public DatabaseDeclaration(DatabaseRef databaseRef)
        {
            this.DatabaseRef = databaseRef;

            var uniqueTypeNames = new HashSet<string>();
            var tables = new List<DatabaseRef.Table>();
            var attributes = DatabaseRef.Symbol.GetAttributes(TABLE_ATTRIBUTE);

            foreach (var attrib in attributes)
            {
                var args = attrib.ConstructorArguments;

                if (args.Length < 1 || args[0].Value is not INamedTypeSymbol type)
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
                    FullTypeName = fullTypeName
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

            using var arrayBuilder = ImmutableArrayBuilder<DatabaseRef.Table>.Rent();
            arrayBuilder.AddRange(tables);
            DatabaseRef.Tables = arrayBuilder.ToImmutable();
        }
    }
}
