using System;
using System.Collections.Generic;
using System.Linq;
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

namespace ZBase.Foundation.Data.Authoring
{
    public class DataTableContainerAssetExporter<TDataAsset, TDataTable, TData>
        : ISheetExporter, ISheetFormatter
        where TDataAsset : DataTableAsset<TDataTable, TData>
        where TDataTable : IDataTable<TData>
        where TData : IData
    {
        private readonly string _savePath;

        public DataTableContainerAssetExporter(string savePath)
        {
            _savePath = savePath;
        }

        public TimeZoneInfo TimeZoneInfo => TimeZoneInfo.Utc;

        public IFormatProvider FormatProvider => CultureInfo.InvariantCulture;

        public DataTableContainerAsset Result { get; private set; }

        public Task<bool> Export(SheetConvertingContext context)
        {
            var bakingRowToAssetMap = new Dictionary<ISheetRow, SheetRowScriptableObject>();
            var bakingContainerAsset = GenerateBakingContainerAsset(context, this, bakingRowToAssetMap);
            MapBakingReferences(context, bakingRowToAssetMap);

            var containerAsset = GenerateDataTableContainerAsset(_savePath, bakingContainerAsset);
            SaveAssets(containerAsset);

            Result = containerAsset;
            return Task.FromResult(true);
        }

        private static SheetContainerScriptableObject GenerateBakingContainerAsset(
              SheetConvertingContext context
            , ISheetFormatter formatter
            , Dictionary<ISheetRow, SheetRowScriptableObject> assetMap
        )
        {
            var valueContext = new SheetValueConvertingContext(formatter, new SheetContractResolver());
            var containerSO = ScriptableObject.CreateInstance<SheetContainerScriptableObject>();

            foreach (var pair in context.Container.GetSheetProperties())
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
            , Dictionary<ISheetRow, SheetRowScriptableObject> assetMap
        )
        {
            foreach (var pair in context.Container.GetSheetProperties())
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
                        else if (typeof(IUnitySheetDirectAssetPath).IsAssignableFrom(node.ValueType))
                        {
                            MapBakingReferencesInSheet(context, sheet, node, indexes, AssetReferenceMapping, 0);
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

        private static void AssetReferenceMapping(SheetConvertingContext context, object obj, int _)
        {
            if (obj is not IUnitySheetDirectAssetPath path)
            {
                return;
            }

            if (path.IsValid() == false)
            {
                return;
            }

            var fullPath = path.FullPath;

            UnityEngine.Object asset;

            if (string.IsNullOrEmpty(path.SubAssetName))
            {
                asset = AssetDatabase.LoadMainAssetAtPath(fullPath);
            }
            else
            {
                var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(fullPath);
                asset = assets.FirstOrDefault(x => x.name == path.SubAssetName);
            }

            if (asset == null)
            {
                context.Logger.LogError("Failed to find asset \"{AssetPath}\" from Asset", fullPath);
                return;
            }

            path.Asset = asset;
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

        private static DataTableContainerAsset GenerateDataTableContainerAsset(
              string savePath
            , SheetContainerScriptableObject bakingContainerAsset
        )
        {
            savePath = MakeFolderPath(savePath);

            var containerAssetPath = Path.Combine(savePath, "_Container.asset");
            var containerAsset = AssetDatabase.LoadAssetAtPath<DataTableContainerAsset>(containerAssetPath);
            
            if (containerAsset == false)
            {
                containerAsset = ScriptableObject.CreateInstance<DataTableContainerAsset>();
                AssetDatabase.CreateAsset(containerAsset, containerAssetPath);
            }

            return containerAsset;
        }

        private static void SaveAssets(DataTableContainerAsset container)
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
