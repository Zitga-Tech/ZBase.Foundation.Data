using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cathei.BakingSheet.Unity;
using UnityEditor;
using UnityEngine;

#if USE_CYSHARP_UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace ZBase.Foundation.Data.Authoring.Configs.GoogleSheets
{
    [Obsolete("DatabaseGoogleSheetConfig is deprecated. Use DatabaseConfig instead.", false)]
    public abstract partial class DatabaseGoogleSheetConfig<TSheetContainer>
        : DatabaseGoogleSheetConfig<DatabaseAsset, TSheetContainer>
        where TSheetContainer : DataSheetContainerBase
    {
    }

    [Obsolete("DatabaseGoogleSheetConfig is deprecated. Use DatabaseConfig instead.", false)]
    public abstract partial class DatabaseGoogleSheetConfig<TDatabaseAsset, TSheetContainer>
        : DatabaseGoogleSheetConfigBase
        where TDatabaseAsset : DatabaseAsset
        where TSheetContainer : DataSheetContainerBase
    {
        protected abstract TSheetContainer CreateSheetContainer();

        protected virtual IEnumerator Export(ExportAllAssetsArgs args)
        {
#if USE_CYSHARP_UNITASK && USE_UNITY_EDITORCOROUTINES

            ShowProgress(args, "Retrieving service account credential...");

            var credentialJsonTask = File.ReadAllTextAsync(args.CredentialFilePath).AsUniTask();

            while (credentialJsonTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Retrieving service account credential...");
                yield return null;
            }

            var credentialJson = credentialJsonTask.AsTask().Result;

            if (string.IsNullOrWhiteSpace(credentialJson))
            {
                Debug.LogError($"Service account credential is undefined.");
                StopReport(args);
                args.ResultCallback?.Invoke(false);
                yield break;
            }

            ShowProgress(args, "Retrieving id of the spreadsheet which contains a table of file meta...");

            var spreadSheetIdTask = File.ReadAllTextAsync(args.SpreadsheetIdFilePath).AsUniTask();

            while (spreadSheetIdTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Retrieving id of the spreadsheet which contains a table of file meta...");
                yield return null;
            }

            var spreadSheetId = spreadSheetIdTask.AsTask().Result;

            if (string.IsNullOrWhiteSpace(spreadSheetId))
            {
                Debug.LogError($"Spreadsheet Id is undefined.");
                StopReport(args);
                args.ResultCallback?.Invoke(false);
                yield break;
            }

            var converters = new List<DatabaseGoogleSheetConverter>();

            if (args.ListOfSpreadsheets)
            {
                ShowProgress(args, "Reading the list of Spreadsheets...");

                var fileSheetContainer = new FileDatabaseDefinition.SheetContainer(UnityLogger.Default);
                var fileSheetConverter = new DatabaseGoogleSheetConverter(
                      spreadSheetId
                    , credentialJson
                    , TimeZoneInfo.Utc
                );

                var fileSheetBakeTask = fileSheetContainer.Bake(fileSheetConverter).AsUniTask();

                while (fileSheetBakeTask.Status == UniTaskStatus.Pending)
                {
                    ShowProgress(args, "Reading the table of file meta...");
                    yield return null;
                }

                var fileDataTableAsset = fileSheetContainer.FileDataTableAsset_FileDataSheet;

                if (fileDataTableAsset != null && fileDataTableAsset.Count >= 1)
                {
                    foreach (var row in fileDataTableAsset)
                    {
                        if (row.Type != "application/vnd.google-apps.spreadsheet"
                            || row.FileName.StartsWith('$')
                            || row.FileName.StartsWith('<')
                            || row.FileName.EndsWith('>')
                        )
                        {
                            continue;
                        }

                        ShowProgress(args, $"Add file `{row.FileName}` with id `{row.FileId}`");

                        converters.Add(new DatabaseGoogleSheetConverter(
                              row.FileId
                            , credentialJson
                            , TimeZoneInfo.Utc
                        ));
                    }
                }
            }
            
            if (converters.Count < 1)
            {
                converters.Add(new DatabaseGoogleSheetConverter(
                      spreadSheetId
                    , credentialJson
                    , TimeZoneInfo.Utc
                ));
            }

            ShowProgress(args, "Importing all Spreadsheets...");

            var sheetContainer = args.SheetContainer;
            var sheetBakeTask = sheetContainer.Bake(converters.ToArray()).AsUniTask();

            while (sheetBakeTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Importing all Spreadsheets...");
                yield return null;
            }

            ShowProgress(args, "Converting to data table assets...");

            var exporter = new DatabaseAssetExporter<TDatabaseAsset>(
                  args.OutputFolderPath
                , args.DatabaseAssetName
            );

            var sheetStoreTask = sheetContainer.Store(exporter).AsUniTask();

            while (sheetStoreTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Converting to data table assets...");
                yield return null;
            }

            StopReport(args);
            AssetDatabase.Refresh();

            args.ResultCallback?.Invoke(true);

#else
            UnityEditor.EditorUtility
                .DisplayDialog("Missing packages", "Requires \"UniTask\" and \"Editor Coroutines\" packages", "OK");

            args.ResultCallback?.Invoke(false);

            yield break;
#endif
        }

        protected class ExportAllAssetsArgs : ExportArgs
        {
            public TSheetContainer SheetContainer { get; set; }

            public string DatabaseAssetName { get; set; }
        }
    }
}
