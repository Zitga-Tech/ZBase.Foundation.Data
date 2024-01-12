using System;
using System.Collections;
using UnityEditor;

#if USE_CYSHARP_UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace ZBase.Foundation.Data.Authoring.Configs.CsvSheets
{
    public abstract partial class DatabaseCsvSheetConfig<TSheetContainer> : DatabaseCsvSheetConfig<DatabaseAsset, TSheetContainer>
        where TSheetContainer : DataSheetContainerBase
    {
    }

    public abstract partial class DatabaseCsvSheetConfig<TDatabaseAsset, TSheetContainer> : DatabaseCsvSheetConfigBase
        where TDatabaseAsset : DatabaseAsset
        where TSheetContainer : DataSheetContainerBase
    {
        protected abstract TSheetContainer CreateSheetContainer();

        protected virtual IEnumerator Export(ExportArgs args)
        {
#if USE_CYSHARP_UNITASK && USE_UNITY_EDITORCOROUTINES

            var converter = new DatabaseCsvSheetConverter(
                  args.CsvFolderPath
                , TimeZoneInfo.Utc
                , includeSubFolders: args.IncludeSubFolders
            );

            ShowProgress(args, "Importing all CSV files...");

            var sheetContainer = args.SheetContainer;
            var sheetBakeTask = sheetContainer.Bake(converter).AsUniTask();

            while (sheetBakeTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Importing all CSV files...");
                yield return null;
            }

            ShowProgress(args, "Converting to data table asset files...");

            var exporter = new DatabaseAssetExporter<TDatabaseAsset>(
                  args.AssetOutputFolderPath
                , args.DatabaseAssetName
            );

            var sheetStoreTask = sheetContainer.Store(exporter).AsUniTask();

            while (sheetStoreTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Converting to data table asset files...");
                yield return null;
            }

            StopReport(args);
            AssetDatabase.Refresh();

            args.ResultCallback?.Invoke(true);
#else
            UnityEditor.EditorUtility
                .DisplayDialog("Missing packages", "Requires \"UniTask\" and \"Editor Coroutines\" packages", "OK");
            yield break;
#endif
        }

        protected static void ShowProgress(ExportArgs args, string message)
        {
            if (args.ShowProgress)
            {
                EditorUtility.DisplayProgressBar("Master Database Exporter", message, 0);
            }
        }

        protected static void StopReport(ExportArgs args)
        {
            if (args.ShowProgress)
            {
                EditorUtility.ClearProgressBar();
            }
        }

        protected class ExportArgs
        {
            public TSheetContainer SheetContainer { get; set; }

            public string DatabaseAssetName { get; set; }

            public string CsvFolderPath { get; set; }

            public bool IncludeSubFolders { get; set; }

            public string AssetOutputFolderPath { get; set; }

            public bool ShowProgress { get; set; }

            public Action<bool> ResultCallback { get; set; }
        }
    }
}
