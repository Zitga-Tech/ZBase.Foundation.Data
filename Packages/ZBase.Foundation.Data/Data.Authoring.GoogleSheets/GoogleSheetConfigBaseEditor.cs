using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZBase.Foundation.Data.Authoring.GoogleSheets
{
    [CustomEditor(typeof(GoogleSheetConfigBase), true)]
    internal class GoogleSheetConfigBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty _relativeServiceAccountFilePath;
        private SerializedProperty _relativeSpreadSheetIdFilePath;
        private SerializedProperty _relativeOutputFolderPath;

        private GUIContent _labelServiceAccountFilePath;
        private GUIContent _labelSpreadSheetIdFilePath;
        private GUIContent _labelOutputFolderPath;

        private void OnEnable()
        {
            if (this.target is not GoogleSheetConfigBase config)
            {
                return;
            }

            var so = this.serializedObject;

            _relativeServiceAccountFilePath = so.FindProperty(nameof(GoogleSheetConfigBase._relativeServiceAccountFilePath));
            _relativeSpreadSheetIdFilePath = so.FindProperty(nameof(GoogleSheetConfigBase._relativeSpreadSheetIdFilePath));
            _relativeOutputFolderPath = so.FindProperty(nameof(GoogleSheetConfigBase._relativeOutputFolderPath));

            _labelServiceAccountFilePath = new GUIContent(
                  "Service Account File Path"
                , "Path to the Google Service Account credential JSON file. The file path is relative to the Assets folder."
            );

            _labelSpreadSheetIdFilePath = new GUIContent(
                  "Spreadsheet Id File Path"
                , "Path to the file contains the Spreadsheet ID to export. The file path is relative to the Assets folder."
            );

            _labelOutputFolderPath = new GUIContent(
                  "Relative Output Folder"
                , "Path to the folder contains the exported assets. The folder path is relative to the Assets folder."
            );
        }

        public override void OnInspectorGUI()
        {
            if (this.target is not GoogleSheetConfigBase config)
            {
                OnInspectorGUI();
                return;
            }

            EditorGUILayout.LabelField("Google Sheets", EditorStyles.boldLabel);
            {
                EditorGUILayout.Space();
                {
                    EditorGUILayout.BeginVertical();

                    if (config.ServiceAccountFileExist)
                    {
                        EditorGUILayout.HelpBox(config.ServiceAccountFilePath, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("File must exist on the disk", MessageType.Error);
                    }

                    EditorGUILayout.BeginHorizontal();
                    DrawRelativePath(config, _relativeServiceAccountFilePath, _labelServiceAccountFilePath);

                    var openFilePanel = false;

                    if (GUILayout.Button("Browse", GUILayout.Width(65)))
                    {
                        openFilePanel = true;
                        BrowseFile("Choose a Google Service Account credential file", "*", _relativeServiceAccountFilePath, config);
                    }

                    if (openFilePanel == false)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.Space();
                {
                    EditorGUILayout.BeginVertical();

                    if (config.SpreadSheetIdFilePathExist)
                    {
                        EditorGUILayout.HelpBox(config.SpreadSheetIdFilePath, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("File must exist on the disk", MessageType.Error);
                    }

                    EditorGUILayout.BeginHorizontal();
                    DrawRelativePath(config, _relativeSpreadSheetIdFilePath, _labelSpreadSheetIdFilePath);

                    var openFilePanel = false;

                    if (GUILayout.Button("Browse", GUILayout.Width(65)))
                    {
                        openFilePanel = true;
                        BrowseFile("Choose a Spreadsheet Id file", "*", _relativeSpreadSheetIdFilePath, config);
                    }

                    if (openFilePanel == false)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);
            {
                EditorGUILayout.Space();
                {
                    EditorGUILayout.BeginVertical();

                    if (config.OutputFolderExist)
                    {
                        EditorGUILayout.HelpBox(config.FullOutputFolderPath, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Folder must exist on the disk", MessageType.Error);
                    }

                    EditorGUILayout.BeginHorizontal();
                    DrawRelativePath(config, _relativeOutputFolderPath, _labelOutputFolderPath);

                    var openFolderPanel = false;

                    if (GUILayout.Button("Browse", GUILayout.Width(65)))
                    {
                        openFolderPanel = true;
                        BrowseFolder("Choose a folder", _relativeOutputFolderPath, config);
                    }

                    if (openFolderPanel == false)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                    }
                }
            }

            EditorGUILayout.Space();
            {
                var color = GUI.color;
                GUI.color = Color.green;

                if (GUILayout.Button("Export All Assets", GUILayout.Height(30)))
                {
                    config.ExportAllAssets();
                }

                GUI.color = color;
            }

            EditorGUILayout.Space();
            {
                if (GUILayout.Button("Locate Database Asset", GUILayout.Height(30)))
                {
                    config.LocateDatabaseAsset();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void BrowseFile(string title, string extension, SerializedProperty property, Object config)
        {
            var path = EditorUtility.OpenFilePanel(title, "Assets", extension);

            if (string.IsNullOrWhiteSpace(path) == false)
            {
                path = Path.GetRelativePath(Application.dataPath, path).Replace('\\', '/');

                property.stringValue = path;
                Undo.RecordObject(config, property.propertyPath);
            }
        }

        private void BrowseFolder(string title, SerializedProperty property, Object config)
        {
            var path = EditorUtility.OpenFolderPanel(title, "Assets", "Assets");

            if (string.IsNullOrWhiteSpace(path) == false)
            {
                path = Path.GetRelativePath(Application.dataPath, path).Replace('\\', '/');

                property.stringValue = path;
                Undo.RecordObject(config, property.propertyPath);
            }
        }

        private void DrawRelativePath(Object obj, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property, label);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(obj, property.propertyPath);
            }
        }
    }
}
