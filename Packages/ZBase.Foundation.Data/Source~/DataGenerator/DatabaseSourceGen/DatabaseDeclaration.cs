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
            var attributes = DatabaseRef.Symbol.GetAttributes(TABLE_ATTRIBUTE);

            foreach (var attrib in attributes)
            {
                if (attrib.ConstructorArguments.Length != 1
                    || attrib.ConstructorArguments[0].Value is not INamedTypeSymbol arg
                )
                {
                    continue;
                }

                uniqueTypeNames.Add(arg.ToFullName());
            }

            using var arrayBuilder = ImmutableArrayBuilder<string>.Rent();
            arrayBuilder.AddRange(uniqueTypeNames);
            DatabaseRef.DataTableAssetTypeNames = arrayBuilder.ToImmutable();
        }
    }
}
