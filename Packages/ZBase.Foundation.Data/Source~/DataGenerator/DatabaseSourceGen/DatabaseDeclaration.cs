using System.Collections.Generic;
using System.Collections.Immutable;
using ZBase.Foundation.SourceGen;

namespace ZBase.Foundation.Data.DatabaseSourceGen
{
    public partial class DatabaseDeclaration
    {
        public const string GENERATOR_NAME = nameof(DatabaseGenerator);

        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"ZBase.Foundation.Data.DatabaseGenerator\", \"1.0.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

        public ImmutableArray<DataTableAssetRef> DataTableAssetRefs { get; }

        public Dictionary<string, DataDeclaration> DataMap { get; }

        public Dictionary<string, HashSet<DataTableAssetRef>> DatabaseMap { get; }

        public DatabaseDeclaration(
              ImmutableArray<DataTableAssetRef> candidates
            , Dictionary<string, DataDeclaration> dataMap
            , Dictionary<string, HashSet<DataTableAssetRef>> databaseMap
        )
        {
            this.DataTableAssetRefs = candidates;
            this.DataMap = dataMap;
            this.DatabaseMap = databaseMap;

            var uniqueTypeNames = new HashSet<string>();
            var typeQueue = new Queue<DataDeclaration>();

            foreach (var candidate in candidates)
            {
                var idTypeFullName = candidate.IdType.ToFullName();
                var dataTypeFullName = candidate.DataType.ToFullName();

                if (dataMap.TryGetValue(idTypeFullName, out var idDeclaration))
                {
                    typeQueue.Enqueue(idDeclaration);
                    uniqueTypeNames.Add(idTypeFullName);
                }

                if (dataMap.TryGetValue(dataTypeFullName, out var dataDeclaration))
                {
                    typeQueue.Enqueue(dataDeclaration);
                    uniqueTypeNames.Add(dataTypeFullName);
                }

                while (typeQueue.Count > 0)
                {
                    var declaration = typeQueue.Dequeue();
                    
                    foreach (var field in declaration.Fields)
                    {
                        var fieldTypeFullName = field.IsArray
                            ? field.ArrayElementType.ToFullName()
                            : field.Type.ToFullName();

                        if (uniqueTypeNames.Contains(fieldTypeFullName))
                        {
                            continue;
                        }

                        if (dataMap.TryGetValue(fieldTypeFullName, out var fieldTypeDeclaration))
                        {
                            typeQueue.Enqueue(fieldTypeDeclaration);
                            uniqueTypeNames.Add(fieldTypeFullName);
                        }
                    }
                }

                uniqueTypeNames.Remove(idTypeFullName);
                uniqueTypeNames.Remove(dataTypeFullName);

                using var arrayBuilder = ImmutableArrayBuilder<string>.Rent();
                arrayBuilder.AddRange(uniqueTypeNames);
                candidate.NestedDataTypeFullNames = arrayBuilder.ToImmutable();

                uniqueTypeNames.Clear();
                typeQueue.Clear();
            }
        }
    }
}
