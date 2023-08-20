using UnityEditor;
using UnityEngine;

namespace ZBase.Foundation.Data.Authoring.Configs.GoogleSheets
{
    partial class DatabaseGoogleSheetConfig<TDatabaseAsset, TSheetContainer>
    {
        public override void LocateDatabaseAsset()
        {
            if (DatabaseFileExist == false)
            {
                Debug.LogError($"Database file does not exists");
                return;
            }

            Selection.activeObject = AssetDatabase.LoadAssetAtPath(
                  DatabaseFilePath
                , typeof(ScriptableObject)
            );
        }
    }
}
