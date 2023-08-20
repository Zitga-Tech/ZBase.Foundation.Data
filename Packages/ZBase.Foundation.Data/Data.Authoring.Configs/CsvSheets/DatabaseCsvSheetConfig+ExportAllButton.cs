using UnityEngine;

#if USE_UNITY_EDITORCOROUTINES
using Unity.EditorCoroutines.Editor;
#endif

namespace ZBase.Foundation.Data.Authoring.Configs.CsvSheets
{
    partial class DatabaseCsvSheetConfig<TDatabaseAsset, TSheetContainer>
    {
        public override void ExportAllAssets()
        {
#if USE_CYSHARP_UNITASK && USE_UNITY_EDITORCOROUTINES

            if (CsvFolderPathExist == false)
            {
                Debug.LogError($"CSV folder does not exists");
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
                CsvFolderPath = FullCsvFolderPath,
                AssetOutputFolderPath = AssetOutputFolderPath,
                ShowProgress = true,
            };

            EditorCoroutineUtility.StartCoroutine(Export(args), this);

#endif
        }
    }
}
