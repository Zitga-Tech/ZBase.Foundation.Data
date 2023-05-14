using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using Cathei.BakingSheet;
using Cathei.BakingSheet.Internal;
using Cathei.BakingSheet.Unity;
using Cathei.BakingSheet.Unity.Exposed;
using UnityEngine;
using UnityEditor;
using Microsoft.Extensions.Logging;
using System.Reflection;
using ZBase.Foundation.Data.Authoring.SourceGen;

namespace ZBase.Foundation.Data.Authoring
{
    public class DatabaseAssetExporter<TDataAsset, TDataTable, TData>
        : ISheetExporter, ISheetFormatter
        where TDataAsset : DataTableAsset<TDataTable, TData>
        where TDataTable : IDataTable<TData>
        where TData : IData
    {
        private readonly string _savePath;

        public DatabaseAssetExporter(string savePath)
        {
            _savePath = savePath;
        }

        public TimeZoneInfo TimeZoneInfo => TimeZoneInfo.Utc;

        public IFormatProvider FormatProvider => CultureInfo.InvariantCulture;

        public DatabaseAsset Result { get; private set; }

        public Task<bool> Export(SheetConvertingContext context)
        {
            var sheetProperties = context.Container.GetSheetProperties();
            var bakingRowToAssetMap = new Dictionary<ISheetRow, SheetRowScriptableObject>();
            var bakingContainerAsset = GenerateBakingContainerAsset(context, sheetProperties, this, bakingRowToAssetMap);
            MapBakingReferences(context, sheetProperties, bakingRowToAssetMap);

            var containerAsset = GenerateDatabaseAsset(_savePath, bakingContainerAsset);
            SaveAsset(containerAsset);

            Result = containerAsset;
            return Task.FromResult(true);
        }

        private static void GetMeta(
              IReadOnlyDictionary<string, PropertyInfo> sheetProperties
            , out IReadOnlyDictionary<string, Type> dataTableTypes
            , out IReadOnlyDictionary<string, Type> dataTableAssetTypes
        )
        {
            var dataTableTypesBuilder = new Dictionary<string, Type>();
            var dataTableAssetTypesBuilder = new Dictionary<string, Type>();
            
            foreach (var (name, property) in sheetProperties)
            {
                if (TryGetAttribute<DataTableInfoAttribute>(property, out var dtInfoAttrib))
                {
                    dataTableTypesBuilder[name] = dtInfoAttrib.DataTableType;
                }

                if (TryGetAttribute<DataTableAssetInfoAttribute>(property, out var dtaInfoAttrib))
                {
                    dataTableAssetTypesBuilder[name] = dtaInfoAttrib.DataTableAssetType;
                }
            }

            dataTableTypes = dataTableTypesBuilder;
            dataTableAssetTypes = dataTableAssetTypesBuilder;
        }

        private static bool TryGetAttribute<T>(PropertyInfo property, out T attribute)
            where T : Attribute
        {
            attribute = property.GetCustomAttribute<T>();
            return attribute != null;
        }

        private static SheetContainerScriptableObject GenerateBakingContainerAsset(
              SheetConvertingContext context
            , IReadOnlyDictionary<string, PropertyInfo> sheetProperties
            , ISheetFormatter formatter
            , Dictionary<ISheetRow, SheetRowScriptableObject> assetMap
        )
        {
            var valueContext = new SheetValueConvertingContext(formatter, new SheetContractResolver());
            var containerSO = ScriptableObject.CreateInstance<SheetContainerScriptableObject>();

            foreach (var pair in sheetProperties)
            {
                using (context.Logger.BeginScope(pair.Key))
                {
                    if (pair.Value.GetValue(context.Container) is not ISheet sheet)
                    {
                        continue;
                    }

                    var sheetSO = ScriptableObject.CreateInstance<SheetScriptableObject>();
                    sheetSO.name = sheet.Name;
                    sheetSO.SetTypeInfoEx(sheet.RowType.FullName);

                    foreach (var row in sheet)
                    {
                        var rowIdStr = valueContext.ValueToString(row.Id.GetType(), row.Id);
                        var rowSO = ScriptableObject.CreateInstance<JsonSheetRowScriptableObject>();
                        rowSO.name = rowIdStr;
                        rowSO.SetRowEx(row);
                        sheetSO.AddEx(rowSO);
                        assetMap.Add(row, rowSO);
                    }

                    containerSO.AddEx(sheetSO);
                }
            }

            return containerSO;
        }

        private static void MapBakingReferences(
              SheetConvertingContext context
            , IReadOnlyDictionary<string, PropertyInfo> sheetProperties
            , Dictionary<ISheetRow, SheetRowScriptableObject> assetMap
        )
        {
            foreach (var pair in sheetProperties)
            {
                using (context.Logger.BeginScope(pair.Key))
                {
                    if (pair.Value.GetValue(context.Container) is not ISheet sheet)
                    {
                        continue;
                    }

                    var propertyMap = sheet.GetPropertyMap(context);

                    propertyMap.UpdateIndex(sheet);

                    foreach (var (node, indexes) in propertyMap.TraverseLeaf())
                    {
                        if (typeof(IUnitySheetReference).IsAssignableFrom(node.ValueType))
                        {
                            MapBakingReferencesInSheet(context, sheet, node, indexes, SheetReferenceMapping, assetMap);
                        }
                    }
                }
            }
        }

        private static void MapBakingReferencesInSheet<TState>(
              SheetConvertingContext context
            , ISheet sheet
            , PropertyNode node
            , IEnumerable<object> indexes
            , Action<SheetConvertingContext, object, TState> mapper
            , TState state
        )
        {
            foreach (var row in sheet)
            {
                int verticalCount = node.GetVerticalCount(row, indexes.GetEnumerator());

                using (context.Logger.BeginScope(row.Id))
                using (context.Logger.BeginScope(node.FullPath, indexes))
                {
                    for (int vindex = 0; vindex < verticalCount; ++vindex)
                    {
                        if (node.TryGetValue(row, vindex, indexes.GetEnumerator(), out var obj) == false)
                        {
                            continue;
                        }

                        mapper(context, obj, state);

                        // setting back for value type struct
                        if (node.ValueType.IsValueType)
                        {
                            node.SetValue(row, vindex, indexes.GetEnumerator(), obj);
                        }
                    }
                }
            }
        }

        private static void SheetReferenceMapping(
              SheetConvertingContext context
            , object obj
            , Dictionary<ISheetRow, SheetRowScriptableObject> rowToSO
        )
        {
            if (obj is not IUnitySheetReference refer)
            {
                return;
            }

            if (refer.IsValid() == false)
            {
                return;
            }

            if (rowToSO.TryGetValue(refer.Ref, out var asset) == false)
            {
                context.Logger.LogError($"Failed to find reference \"{refer.Id}\" from Asset");
                return;
            }

            refer.Asset = asset;
        }

        private static string MakeFolderPath(string savePath)
        {
            if (AssetDatabase.IsValidFolder(savePath) == false)
            {
                savePath = AssetDatabase.CreateFolder(
                      Path.GetDirectoryName(savePath)
                    , Path.GetFileName(savePath)
                );
            }

            return savePath;
        }

        private static DatabaseAsset GenerateDatabaseAsset(
              string savePath
            , SheetContainerScriptableObject bakingContainerAsset
        )
        {
            savePath = MakeFolderPath(savePath);

            var databaseAssetPath = Path.Combine(savePath, "_Database.asset");
            var databaseAsset = AssetDatabase.LoadAssetAtPath<DatabaseAsset>(databaseAssetPath);
            
            if (databaseAsset == false)
            {
                databaseAsset = ScriptableObject.CreateInstance<DatabaseAsset>();
                AssetDatabase.CreateAsset(databaseAsset, databaseAssetPath);
            }

            databaseAsset.Clear();

            foreach (var sheet in bakingContainerAsset.Sheets)
            {
            }

            return databaseAsset;
        }

        private static void SaveAsset(DatabaseAsset container)
        {
            EditorUtility.SetDirty(container);

            foreach (var table in container.TableAssets.Span)
            {
                EditorUtility.SetDirty(table);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
