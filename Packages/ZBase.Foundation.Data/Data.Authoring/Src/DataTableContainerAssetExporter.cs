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
    public class DataTableContainerAssetExporter<TAsset, TDataTable, TData>
        : ISheetExporter, ISheetFormatter
        where TAsset : DataTableAsset<TDataTable, TData>
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

        public SheetContainerScriptableObject Result { get; private set; }

        public Task<bool> Export(SheetConvertingContext context)
        {
            var rowToSO = new Dictionary<ISheetRow, SheetRowScriptableObject>();

            var containerSO = GenerateAssets(_savePath, context, this, rowToSO);

            MapReferences(context, rowToSO);

            SaveAssets(containerSO);

            Result = containerSO;

            return Task.FromResult(true);
        }

        private static SheetContainerScriptableObject GenerateAssets(
              string savePath
            , SheetConvertingContext context
            , ISheetFormatter formatter
            , Dictionary<ISheetRow, SheetRowScriptableObject> rowToSO
        )
        {
            if (AssetDatabase.IsValidFolder(savePath) == false)
            {
                savePath = AssetDatabase.CreateFolder(
                      Path.GetDirectoryName(savePath)
                    , Path.GetFileName(savePath)
                );
            }

            var valueContext = new SheetValueConvertingContext(formatter, new SheetContractResolver());
            var containerSO = ScriptableObject.CreateInstance<SheetContainerScriptableObject>();
            var existingRowSO = new Dictionary<string, SheetRowScriptableObject>();

            foreach (var pair in context.Container.GetSheetProperties())
            {
                existingRowSO.Clear();

                using (context.Logger.BeginScope(pair.Key))
                {
                    if (pair.Value.GetValue(context.Container) is not ISheet sheet)
                    {
                        continue;
                    }

                    var sheetPath = Path.Combine(savePath, $"{sheet.Name}.asset");
                    var sheetSO = ScriptableObject.CreateInstance<SheetScriptableObject>();
                    sheetSO.name = sheet.Name;
                    sheetSO.SetTypeInfoEx(sheet.RowType.FullName);

                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
                    {
                        if (asset is not SheetRowScriptableObject rowSO)
                        {
                            continue;
                        }

                        existingRowSO.Add(rowSO.name, rowSO);
                    }

                    sheetSO.ClearEx();

                    foreach (var row in sheet)
                    {
                        string rowIdStr = valueContext.ValueToString(row.Id.GetType(), row.Id);

                        if (existingRowSO.TryGetValue(rowIdStr, out var rowSO) == false)
                        {
                            rowSO = ScriptableObject.CreateInstance<JsonSheetRowScriptableObject>();
                            AssetDatabase.AddObjectToAsset(rowSO, sheetSO);
                        }

                        rowSO.name = rowIdStr;
                        rowSO.SetRowEx(row);

                        sheetSO.AddEx(rowSO);
                        rowToSO.Add(row, rowSO);
                        existingRowSO.Remove(rowIdStr);
                    }

                    // clear removed scriptable objects
                    foreach (var rowSO in existingRowSO.Values)
                    {
                        AssetDatabase.RemoveObjectFromAsset(rowSO);
                    }

                    containerSO.AddEx(sheetSO);
                }
            }

            return containerSO;
        }

        private static void MapReferences(
              SheetConvertingContext context
            , Dictionary<ISheetRow, SheetRowScriptableObject> rowToSO
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
                            MapReferencesInSheet(context, sheet, node, indexes, SheetReferenceMapping, rowToSO);
                        }
                        else if (typeof(IUnitySheetDirectAssetPath).IsAssignableFrom(node.ValueType))
                        {
                            MapReferencesInSheet(context, sheet, node, indexes, AssetReferenceMapping, 0);
                        }
                    }
                }
            }
        }

        private static void MapReferencesInSheet<TState>(
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

        private static void SaveAssets(SheetContainerScriptableObject containerSO)
        {
            EditorUtility.SetDirty(containerSO);

            foreach (var sheetSO in containerSO.Sheets)
            {
                EditorUtility.SetDirty(sheetSO);

                foreach (var rowSO in sheetSO.Rows)
                    EditorUtility.SetDirty(rowSO);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
