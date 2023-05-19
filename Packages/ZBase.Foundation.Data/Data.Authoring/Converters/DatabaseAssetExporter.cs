using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using Cathei.BakingSheet;
using UnityEngine;
using UnityEditor;
using Microsoft.Extensions.Logging;
using System.Reflection;
using ZBase.Foundation.Data.Authoring.SourceGen;

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
            var savePath = MakeFolderPath(_savePath);

            GenerateDatabaseAsset(
                  context
                , savePath
                , _databaseName
                , out var databaseAsset
                , out var dataTableAssets
                , out var redundantAssets
            );

            RemoveAssets(redundantAssets);
            SaveAsset(databaseAsset, dataTableAssets);

            Result = databaseAsset;
            return Task.FromResult(true);
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

        private static void GenerateDatabaseAsset(
              SheetConvertingContext context
            , string savePath
            , string databaseName
            , out DatabaseAsset databaseAsset
            , out List<DataTableAsset> dataTableAssetList
            , out HashSet<DataTableAsset> redundantAssets
        )
        {
            var databaseAssetPath = Path.Combine(savePath, $"{databaseName}.asset");
            
            databaseAsset = AssetDatabase.LoadAssetAtPath<DatabaseAsset>(databaseAssetPath);

            if (databaseAsset == false)
            {
                databaseAsset = ScriptableObject.CreateInstance<DatabaseAsset>();
                AssetDatabase.CreateAsset(databaseAsset, databaseAssetPath);
            }

            redundantAssets = new HashSet<DataTableAsset>();
            
            foreach (var asset in databaseAsset._assets)
            {
                redundantAssets.Add(asset.reference.asset);
            }

            databaseAsset.Clear();

            var sheetProperties = context.Container.GetSheetProperties();
            dataTableAssetList = new List<DataTableAsset>();

            foreach (var pair in sheetProperties)
            {
                using (context.Logger.BeginScope(pair.Key))
                {
                    if (pair.Value.GetValue(context.Container) is not ISheet sheet)
                    {
                        continue;
                    }

                    if (TryGetGeneratedSheetAttribute(context, sheet, out var sheetAttrib) == false
                        || TryGetToDataArrayMethod(context, sheet, sheetAttrib.DataType, out var toDataArrayMethod) == false
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

                    redundantAssets.Remove(dataTableAsset);
                    dataTableAsset.name = dataTableAssetType.Name;

                    var dataArray = toDataArrayMethod.Invoke(sheet, null);
                    dataTableAsset.SetRows(dataArray);
                    dataTableAssetList.Add(dataTableAsset);
                }
            }

            databaseAsset.AddRange(dataTableAssetList);
        }

        private static bool TryGetGeneratedSheetAttribute(
              SheetConvertingContext context
            , ISheet sheet
            , out GeneratedSheetAttribute attribute
        )
        {
            var sheetType = sheet.GetType();
            attribute = sheetType.GetCustomAttribute<GeneratedSheetAttribute>();

            if (attribute == null)
            {
                context.Logger.LogError("Cannot find {Attribute} on {Sheet}", typeof(GeneratedSheetAttribute), sheetType);
                return false;
            }

            return true;
        }

        private static bool TryGetToDataArrayMethod(
              SheetConvertingContext context
            , ISheet sheet
            , Type dataType
            , out MethodInfo toDataArrayMethod
        )
        {
            var sheetType = sheet.GetType();
            var methodName = $"To{dataType.Name}Array";

            toDataArrayMethod = sheetType.GetMethod(methodName, Type.EmptyTypes);

            if (toDataArrayMethod == null)
            {
                context.Logger.LogError("Cannot find {MethodName} method in {SheetType}", methodName, sheetType);
                return false;
            }

            return true;
        }

        private static void RemoveAssets(IEnumerable<DataTableAsset> assets)
        {
            foreach (var asset in assets)
            {
                var path = AssetDatabase.GetAssetPath(asset);
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void SaveAsset(DatabaseAsset databaseAsset, List<DataTableAsset> dataTableAssets)
        {
            EditorUtility.SetDirty(databaseAsset);

            foreach (var asset in dataTableAssets)
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
