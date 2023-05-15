using Microsoft.CodeAnalysis;

namespace ZBase.Foundation.Data.DataTableAssetSourceGen
{
    public partial class DataTableAssetDeclaration
    {
        public const string GENERATOR_NAME = nameof(DataTableAssetGenerator);
        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DataTableAssetGenerator\", \"1.0.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

        public DataTableAssetRef TypeRef { get; }

        public bool GetIdMethodIsImplemented { get; }

        public DataTableAssetDeclaration(DataTableAssetRef candidate)
        {
            TypeRef = candidate;

            var members = candidate.Symbol.GetMembers();

            foreach (var member in members)
            {
                if (member is not IMethodSymbol method)
                {
                    continue;
                }

                if (method.Name == "GetId"
                    && method.Parameters.Length == 1
                    && method.Parameters[0].RefKind == RefKind.In
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, TypeRef.DataType)
                )
                {
                    GetIdMethodIsImplemented = true;
                    break;
                }
            }
        }
    }
}
