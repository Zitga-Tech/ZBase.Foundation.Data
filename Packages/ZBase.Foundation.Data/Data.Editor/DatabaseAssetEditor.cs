using UnityEditor;
using UnityEngine;

namespace ZBase.Foundation.Data.Editor
{
    [UnityEditor.CustomEditor(typeof(DatabaseAsset))]
    public sealed class DatabaseAssetEditor : UnityEditor.Editor
    {
        private Vector2 _scrollPos;

        public override void OnInspectorGUI()
        {
            if (this.target is not DatabaseAsset databaseAsset)
            {
                base.OnInspectorGUI();
                return;
            }

            var assets = databaseAsset._assets;

            if (assets == null || assets.Length < 1)
            {
                return;
            }

            var assetsSP = this.serializedObject.FindProperty(nameof(DatabaseAsset._assets));

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            for (var i = 0; i < assets.Length; i++)
            {
                var asset = assets[i];
                var referenceSP = assetsSP.GetArrayElementAtIndex(i)
                    .FindPropertyRelative(nameof(DatabaseAsset.Asset.reference));

                EditorGUILayout.BeginHorizontal(GUI.skin.box);

                EditorGUILayout.PrefixLabel(asset.name);
                EditorGUILayout.PropertyField(referenceSP, GUIContent.none);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
