using UnityEngine;

#if USE_UNITY_EDITORCOROUTINES
using Unity.EditorCoroutines.Editor;
#endif

namespace ZBase.Foundation.Data.Authoring.Configs.GoogleSheets
{
    partial class DatabaseGoogleSheetConfig<TDatabaseAsset, TSheetContainer>
    {
        public override void ExportAllAssets()
        {
#if USE_CYSHARP_UNITASK && USE_UNITY_EDITORCOROUTINES

            if (ServiceAccountFileExist == false)
            {
                Debug.LogError($"Credential file does not exists");
                return;
            }

            if (SpreadSheetIdFilePathExist == false)
            {
                Debug.LogError($"Spreadsheet ID file does not exists");
                return;
            }

            if (OutputFolderExist == false)
            {
                Debug.LogError($"Output folder does not exists");
                return;
            }

            var databaseAssetName = GetDatabaseAssetName();

            if (string.IsNullOrWhiteSpace(databaseAssetName))
            {
                Debug.LogError("The name of Database Asset must not be null or contain only white spaces.");
                return;
            }

            var args = new ExportArgs {
                SheetContainer = CreateSheetContainer(),
                DatabaseAssetName = databaseAssetName,
                CredentialFilePath = ServiceAccountFilePath,
                SpreadSheetIdFilePath = SpreadSheetIdFilePath,
                AssetOutputFolderPath = AssetOutputFolderPath,
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
