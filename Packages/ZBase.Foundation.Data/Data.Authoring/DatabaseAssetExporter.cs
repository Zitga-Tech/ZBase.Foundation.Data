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
    public class DatabaseAssetExporter<TDataAsset, TDataTable, TId, TData>
        : ISheetExporter, ISheetFormatter
        where TDataAsset : DataTableAsset<TDataTable, TId, TData>
        where TDataTable : IDataTable<TId, TData>
        where TData : IData
    {
        private readonly string _savePath;
        private readonly string _databaseName;

        public DatabaseAssetExporter(string savePath, string databaseName = "_Database")
        {
            _savePath = savePath;
            _databaseName = databaseName;
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

            var databaseAsset = GenerateDatabaseAsset(context, _savePath, _databaseName, bakingContainerAsset, sheetProperties);
            SaveAsset(databaseAsset);

            Result = databaseAsset;
            return Task.FromResult(true);
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
                context.Logger.LogError("Failed to find reference \"{ReferenceId}\" from Asset", refer.Id);
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

        private static DatabaseAsset GenerateDatabaseAsset(
              SheetConvertingContext context
            , string savePath
            , string databaseName
            , SheetContainerScriptableObject bakingContainerAsset
            , IReadOnlyDictionary<string, PropertyInfo> sheetProperties
        )
        {
            savePath = MakeFolderPath(savePath);

            GetMeta(
                  sheetProperties
                , out var dataTableTypes
                , out var dataTableAssetTypes
            );

            var databaseAssetPath = Path.Combine(savePath, $"{databaseName}.asset");
            var databaseAsset = AssetDatabase.LoadAssetAtPath<DatabaseAsset>(databaseAssetPath);
            
            if (databaseAsset == false)
            {
                databaseAsset = ScriptableObject.CreateInstance<DatabaseAsset>();
                AssetDatabase.CreateAsset(databaseAsset, databaseAssetPath);
            }

            databaseAsset.Clear();

            var dataTableAssetList = new List<DataTableAsset>();

            foreach (var sheet in bakingContainerAsset.Sheets)
            {
                using (context.Logger.BeginScope(sheet.name))
                {
                    if (TryGetDataTableAssetType(context, sheet.name, dataTableAssetTypes, out var dataTableAssetType) == false
                        || TryGetDataTableType(context, sheet.name, dataTableTypes, out var dataTableType) == false
                        || TryGetToDataTableMethod(context, sheet, dataTableType, out var toDataTableMethod) == false
                    )
                    {
                        continue;
                    }

                    var dataTableAssetPath = Path.Combine(savePath, $"{dataTableAssetType.Name}.asset");
                    var dataTableAsset = AssetDatabase.LoadAssetAtPath<DataTableAsset>(dataTableAssetPath);

                    if (dataTableAsset == null)
                    {
                        dataTableAsset = ScriptableObject.CreateInstance(dataTableAssetType) as DataTableAsset;
                        AssetDatabase.CreateAsset(dataTableAsset, dataTableAssetPath);
                    }

                    dataTableAsset.name = dataTableAssetType.Name;

                    var dataTable = toDataTableMethod.Invoke(sheet, null);
                    dataTableAsset.SetDataTable(dataTable);

                    dataTableAssetList.Add(dataTableAsset);
                }
            }

            databaseAsset.AddRange(dataTableAssetList);
            return databaseAsset;
        }

        private static bool TryGetDataTableAssetType(
              SheetConvertingContext context
            , string sheetName
            , IReadOnlyDictionary<string, Type> dataTableAssetTypes
            , out Type dataTableAssetType
        )
        {
            if (dataTableAssetTypes.TryGetValue(sheetName, out dataTableAssetType) == false)
            {
                context.Logger.LogError("Cannot find corresponding DataTableAsset type for {SheetName}", sheetName);
                return false;
            }

            if (typeof(DataTableAsset).IsAssignableFrom(dataTableAssetType) == false)
            {
                context.Logger.LogError("Cannot create an asset from {Type} because it is not derived from {DataTableAssetType}", dataTableAssetType, typeof(DataTableAsset));
                return false;
            }

            if (dataTableAssetType.IsGenericType || dataTableAssetType.IsAbstract)
            {
                context.Logger.LogError("Cannot create an asset from {DataTableAssetType} because it is either open generic or abstract", dataTableAssetType.FullName);
                return false;
            }

            return true;
        }

        private static bool TryGetDataTableType(
              SheetConvertingContext context
            , string sheetName
            , IReadOnlyDictionary<string, Type> dataTableTypes
            , out Type dataTableType
        )
        {
            if (dataTableTypes.TryGetValue(sheetName, out dataTableType) == false)
            {
                context.Logger.LogError("Cannot find corresponding DataTable type for {SheetName}", sheetName);
                return false;
            }

            if (dataTableType.IsGenericType || dataTableType.IsAbstract)
            {
                context.Logger.LogError("Cannot create an instance of {DataTableType} because it is either open generic or abstract", dataTableType.FullName);
                return false;
            }

            if (dataTableType.GetConstructor(Type.EmptyTypes) == null)
            {
                context.Logger.LogError("Cannot create an instance of {DataTableType} because it does not have a parameterless constructor", dataTableType.FullName);
                return false;
            }

            return true;
        }

        private static bool TryGetToDataTableMethod(
              SheetConvertingContext context
            , SheetScriptableObject sheet
            , Type dataTableType
            , out MethodInfo toDataTableMethod
        )
        {
            var methodName = $"To{dataTableType.Name}";
            var sheetType = sheet.GetType();
            toDataTableMethod = sheetType.GetMethod(methodName, Type.EmptyTypes);

            if (toDataTableMethod == null)
            {
                context.Logger.LogError("Cannot find {MethodName} method in {SheetType}", methodName, sheetType.Name);
                return false;
            }

            return true;
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
