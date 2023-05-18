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
using Newtonsoft.Json;

namespace ZBase.Foundation.Data.Authoring
{
    public class DatabaseAssetExporter : ISheetExporter, ISheetFormatter
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
            var bakingContainerAsset = GenerateBakingContainerAsset(context, sheetProperties, bakingRowToAssetMap);
            MapBakingReferences(context, sheetProperties, bakingRowToAssetMap);

            var databaseAsset = GenerateDatabaseAsset(context, _savePath, _databaseName, bakingContainerAsset);
            SaveAsset(databaseAsset);

            Result = databaseAsset;
            return Task.FromResult(true);
        }

        private static SheetContainerScriptableObject GenerateBakingContainerAsset(
              SheetConvertingContext context
            , IReadOnlyDictionary<string, PropertyInfo> sheetProperties
            , Dictionary<ISheetRow, SheetRowScriptableObject> assetMap
        )
        {
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
                    sheetSO.SetTypeInfoEx(sheet.GetType().AssemblyQualifiedName);

                    foreach (var row in sheet)
                    {
                        try
                        {
                            var rowSO = ScriptableObject.CreateInstance<JsonSheetRowScriptableObject>();
                            rowSO.SetRowEx(row);
                            sheetSO.AddEx(rowSO);
                            assetMap.Add(row, rowSO);
                        }
                        catch (Exception ex)
                        {
                            context.Logger.LogError(ex, JsonConvert.SerializeObject(row.Id));
                        }
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

        private static DatabaseAsset GenerateDatabaseAsset(
              SheetConvertingContext context
            , string savePath
            , string databaseName
            , SheetContainerScriptableObject bakingContainerAsset
        )
        {
            savePath = MakeFolderPath(savePath);

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
                    if (TryGetGeneratedSheetAttribute(context, sheet, out var sheetAttrib) == false
                        || TryGetToDataRowsMethod(context, sheet, sheetAttrib.DataType, out var toDataRowsMethod) == false
                    )
                    {
                        continue;
                    }

                    var dataTableAssetType = sheetAttrib.DataTableAssetType;
                    var dataTableAssetPath = Path.Combine(savePath, $"{dataTableAssetType.Name}.asset");
                    var dataTableAsset = AssetDatabase.LoadAssetAtPath<DataTableAsset>(dataTableAssetPath);

                    if (dataTableAsset == null)
                    {
                        dataTableAsset = ScriptableObject.CreateInstance(dataTableAssetType) as DataTableAsset;
                        AssetDatabase.CreateAsset(dataTableAsset, dataTableAssetPath);
                    }

                    dataTableAsset.name = dataTableAssetType.Name;

                    var dataRows = toDataRowsMethod.Invoke(sheet, null);
                    dataTableAsset.SetRows(dataRows);
                    dataTableAssetList.Add(dataTableAsset);
                }
            }

            databaseAsset.AddRange(dataTableAssetList);
            return databaseAsset;
        }

        private static bool TryGetGeneratedSheetAttribute(
              SheetConvertingContext context
            , SheetScriptableObject sheet
            , out GeneratedSheetAttribute attribute
        )
        {
            var sheetTypeName = sheet.GetTypeInfoEx();
            var sheetType = Type.GetType(sheetTypeName, true);
            attribute = sheetType.GetCustomAttribute<GeneratedSheetAttribute>();

            if (attribute == null)
            {
                context.Logger.LogError("Cannot find {Attribute} on {Sheet}", typeof(GeneratedSheetAttribute), sheetType);
                return false;
            }

            return true;
        }

        private static bool TryGetToDataRowsMethod(
              SheetConvertingContext context
            , SheetScriptableObject sheet
            , Type dataType
            , out MethodInfo toDataRowsMethod
        )
        {
            var methodName = $"To{dataType.Name}Rows";
            var sheetTypeName = sheet.GetTypeInfoEx();
            var sheetType = Type.GetType(sheetTypeName, true);
            toDataRowsMethod = sheetType.GetMethod(methodName, Type.EmptyTypes);

            if (toDataRowsMethod == null)
            {
                context.Logger.LogError("Cannot find {MethodName} method in {SheetType}", methodName, sheetType);
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
