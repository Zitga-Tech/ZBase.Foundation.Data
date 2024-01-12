using System;
using UnityEngine;

#if USE_UNITY_EDITORCOROUTINES
using Unity.EditorCoroutines.Editor;
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
    }
}