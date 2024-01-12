using System;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Cathei.BakingSheet.Unity;
using UnityEditor;
using System.Threading.Tasks;

#if USE_UNITY_EDITORCOROUTINES
using Unity.EditorCoroutines.Editor;
#endif

#if USE_CYSHARP_UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace ZBase.Foundation.Data.Authoring.Configs.GoogleSheets
{
    partial class DatabaseGoogleSheetConfigBase
    {
        public virtual void ExportCsvFiles(Action<bool> resultCallback)
        {
            if (ServiceAccountFileExist == false)
            {
                Debug.LogError($"Credential file does not exists");
                resultCallback?.Invoke(false);
                return;
            }

            if (SpreadsheetIdFilePathExist == false)
            {
                Debug.LogError($"Spreadsheet ID file does not exists");
                resultCallback?.Invoke(false);
                return;
            }

            if (CsvOutputFolderExist == false)
            {
                Debug.LogError($"Output folder does not exists");
                resultCallback?.Invoke(false);
                return;
            }

            var args = new ExportCsvFilesArgs {
                CredentialFilePath = ServiceAccountFilePath,
                SpreadsheetIdFilePath = SpreadsheetIdFilePath,
                ListOfSpreadsheets = ListOfSpreadsheets,
                OutputFolderPath = CsvOutputFolderPath,
                FolderPerSpreadsheet = CsvFolderPerSpreadsheet,
                CleanOutputFolder = CleanCsvOutputFolder,
                CleanOutputSubFolders = CleanCsvOutputSubFolders,
                CommentOutFileNameIfPossible = CommentOutFileNameIfPossible,
                ShowProgress = false,
                ResultCallback = resultCallback,
            };

            EditorCoroutineUtility.StartCoroutine(Export(args), this);
        }

        public virtual void ExportCsvFiles()
        {
#if USE_CYSHARP_UNITASK && USE_UNITY_EDITORCOROUTINES

            if (ServiceAccountFileExist == false)
            {
                Debug.LogError($"Credential file does not exists");
                return;
            }

            if (SpreadsheetIdFilePathExist == false)
            {
                Debug.LogError($"Spreadsheet ID file does not exists");
                return;
            }

            if (CsvOutputFolderExist == false)
            {
                Debug.LogError($"Output folder does not exists");
                return;
            }

            var args = new ExportCsvFilesArgs {
                CredentialFilePath = ServiceAccountFilePath,
                SpreadsheetIdFilePath = SpreadsheetIdFilePath,
                ListOfSpreadsheets = ListOfSpreadsheets,
                OutputFolderPath = CsvOutputFolderPath,
                FolderPerSpreadsheet = CsvFolderPerSpreadsheet,
                CleanOutputFolder = CleanCsvOutputFolder,
                CleanOutputSubFolders = CleanCsvOutputSubFolders,
                CommentOutFileNameIfPossible = CommentOutFileNameIfPossible,
                ShowProgress = true,
            };

            EditorCoroutineUtility.StartCoroutine(Export(args), this);

#else
            UnityEditor.EditorUtility
                .DisplayDialog("Missing packages", "Requires \"UniTask\" and \"Editor Coroutines\" packages", "OK");
#endif
        }

        protected virtual IEnumerator Export(ExportCsvFilesArgs args)
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

            var exporters = new List<DatabaseGoogleSheetExporter>();
            var fileSystem = new DatabaseFileSystem();

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
                        if (row.Type != "application/vnd.google-apps.spreadsheet")
                        {
                            continue;
                        }

                        ShowProgress(args, $"Add file `{row.FileName}` with id `{row.FileId}`");

                        exporters.Add(new DatabaseGoogleSheetExporter(
                              row.FileId
                            , credentialJson
                            , fileSystem
                        ));
                    }
                }
            }

            if (exporters.Count < 1)
            {
                exporters.Add(new DatabaseGoogleSheetExporter(
                      spreadSheetId
                    , credentialJson
                    , fileSystem
                ));
            }

            if (args.CleanOutputFolder && fileSystem.DirectoryExists(args.OutputFolderPath))
            {
                fileSystem.DeleteDirectory(args.OutputFolderPath, true);
            }

            fileSystem.CreateDirectory(args.OutputFolderPath);

            ShowProgress(args, "Exporting all Spreadsheets...");

            var exportTask = Export(args, exporters).AsUniTask();

            while (exportTask.Status == UniTaskStatus.Pending)
            {
                ShowProgress(args, "Exporting all Spreadsheets...");
                yield return null;
            }

            StopReport(args);
            AssetDatabase.Refresh();

            args.ResultCallback?.Invoke(true);

            static async Task Export(
                  ExportCsvFilesArgs args
                , IEnumerable<DatabaseGoogleSheetExporter> exporters
            )
            {
                DatabaseGoogleSheetExporter.ITransformFileName transformer = null;

                if (args.CommentOutFileNameIfPossible)
                {
                    transformer = new CommentOutFileNameTransformer();
                }

                foreach (var exporter in exporters)
                {
                    await exporter.Export(
                          args.OutputFolderPath
                        , args.FolderPerSpreadsheet
                        , args.CleanOutputSubFolders
                        , transformer
                    );
                }
            }

#else
            UnityEditor.EditorUtility
                .DisplayDialog("Missing packages", "Requires \"UniTask\" and \"Editor Coroutines\" packages", "OK");
            yield break;
#endif
        }

        protected class ExportCsvFilesArgs : ExportArgs
        {
            public bool FolderPerSpreadsheet { get; set; }

            public bool CleanOutputFolder { get; set; }

            public bool CleanOutputSubFolders { get; set; }

            public bool CommentOutFileNameIfPossible { get; set; }
        }

        protected class CommentOutFileNameTransformer : DatabaseGoogleSheetExporter.ITransformFileName
        {
            public string Transform(DatabaseGoogleSheetExporter.ITransformFileName.Args args)
            {
                if (SheetUtility.ValidateSheetName(args.sheetName) == false)
                    return args.sheetName;

                return SheetUtility.ValidateSheetName(args.spreadsheetName)
                    ? args.sheetName
                    : $"${args.sheetName}"
                    ;
            }
        }
    }
}
