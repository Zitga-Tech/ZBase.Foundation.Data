using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZBase.Foundation.Data.Authoring.Configs.GoogleSheets
{
    [CustomEditor(typeof(DatabaseGoogleSheetConfigBase), true)]
    internal class DatabaseGoogleSheetConfigBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty _relativeServiceAccountFilePath;
        private SerializedProperty _relativeSpreadsheetIdFilePath;
        private SerializedProperty _listOfSpreadsheets;
        private SerializedProperty _relativeAssetOutputFolderPath;
        private SerializedProperty _relativeCsvOutputFolderPath;
        private SerializedProperty _csvFolderPerSpreadsheet;
        private SerializedProperty _cleanCsvOutputFolder;
        private SerializedProperty _cleanCsvOutputSubFolders;
        private SerializedProperty _commentOutFileNameIfPossible;

        private GUIContent _labelServiceAccountFilePath;
        private GUIContent _labelSpreadsheetIdFilePath;
        private GUIContent _labelListOfSpreadsheets;
        private GUIContent _labelOutputFolderPath;
        private GUIContent _labelCsvFolderPerSpreadsheet;
        private GUIContent _labelCleanCsvOutputFolder;
        private GUIContent _labelCleanCsvOutputSubFolders;
        private GUIContent _labelCommentOutFileNameIfPossible;

        private void OnEnable()
        {
            var so = this.serializedObject;

            _relativeServiceAccountFilePath = so.FindProperty(nameof(DatabaseGoogleSheetConfigBase._relativeServiceAccountFilePath));
            _relativeSpreadsheetIdFilePath = so.FindProperty(nameof(DatabaseGoogleSheetConfigBase._relativeSpreadsheetIdFilePath));
            _listOfSpreadsheets = so.FindProperty(nameof(DatabaseGoogleSheetConfigBase._listOfSpreadsheets));
            _relativeAssetOutputFolderPath = so.FindProperty(nameof(DatabaseGoogleSheetConfigBase._relativeAssetOutputFolderPath));
            _relativeCsvOutputFolderPath = so.FindProperty(nameof(DatabaseGoogleSheetConfigBase._relativeCsvOutputFolderPath));
            _csvFolderPerSpreadsheet = so.FindProperty(nameof(DatabaseGoogleSheetConfigBase._csvFolderPerSpreadsheet));
            _cleanCsvOutputFolder = so.FindProperty(nameof(DatabaseGoogleSheetConfigBase._cleanCsvOutputFolder));
            _cleanCsvOutputSubFolders = so.FindProperty(nameof(DatabaseGoogleSheetConfigBase._cleanCsvOutputSubFolders));
            _commentOutFileNameIfPossible = so.FindProperty(nameof(DatabaseGoogleSheetConfigBase._commentOutFileNameIfPossible));

            _labelServiceAccountFilePath = new GUIContent(
                  "Service Account File Path"
                , "Path to the Google Service Account credential JSON file. The file path is relative to the Assets folder."
            );

            _labelSpreadsheetIdFilePath = new GUIContent(
                  "Spreadsheet Id File Path"
                , "Path to the file contains the Spreadsheet ID to export. The file path is relative to the Assets folder."
            );

            _labelListOfSpreadsheets = new GUIContent(
                  "List of Spreadsheets"
                , $"Whether this Spreadsheet contains a list of other Spreadsheets?"
            );

            _labelOutputFolderPath = new GUIContent(
                  "Relative Output Folder"
                , "Path to the folder contains the exported assets. The folder path is relative to the Assets folder."
            );

            _labelCsvFolderPerSpreadsheet = new GUIContent(
                  "Folder Per Spreadsheet"
                , "Each Spreadsheet will have a separated folder to contain their sheets."
            );

            _labelCleanCsvOutputFolder = new GUIContent(
                  "Clean Output Folder"
                , "Delete the output folder before exporting."
            );
            
            _labelCleanCsvOutputSubFolders = new GUIContent(
                  "Clean Output Sub-folders"
                , "Delete the output sub-folder before exporting."
            );

            _labelCommentOutFileNameIfPossible = new GUIContent(
                  "Comment Out File Name If Possible"
                , "If the name of a Spreadsheet is commented out, all CSV files exported from it would be commented out too."
            );
        }

        public override void OnInspectorGUI()
        {
            if (this.target is not DatabaseGoogleSheetConfigBase config)
            {
                OnInspectorGUI();
                return;
            }

            EditorGUILayout.LabelField("Google Sheets", EditorStyles.boldLabel);
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                {
                    if (config.ServiceAccountFileExist)
                    {
                        EditorGUILayout.HelpBox(config.ServiceAccountFilePath, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("File must exist on the disk", MessageType.Error);
                    }

                    EditorGUILayout.BeginHorizontal();
                    DrawProperty(config, _relativeServiceAccountFilePath, _labelServiceAccountFilePath);

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
                EditorGUILayout.BeginVertical();
                {
                    if (config.SpreadsheetIdFilePathExist)
                    {
                        EditorGUILayout.HelpBox(config.SpreadsheetIdFilePath, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("File must exist on the disk", MessageType.Error);
                    }

                    EditorGUILayout.BeginHorizontal();
                    DrawProperty(config, _relativeSpreadsheetIdFilePath, _labelSpreadsheetIdFilePath);

                    var openFilePanel = false;

                    if (GUILayout.Button("Browse", GUILayout.Width(65)))
                    {
                        openFilePanel = true;
                        BrowseFile("Choose a Spreadsheet Id file", "*", _relativeSpreadsheetIdFilePath, config);
                    }

                    if (openFilePanel == false)
                    {
                        EditorGUILayout.EndHorizontal();

                        DrawProperty(config, _listOfSpreadsheets, _labelListOfSpreadsheets);

                        if (config.ListOfSpreadsheets)
                        {
                            EditorGUILayout.HelpBox(
                                  "The sheet must be named `files` and must contain these columns:\n"
                                + "• id: an integer\n"
                                + "• file_name: name of a Spreadsheet\n"
                                + "• file_id: id of a Spreadsheet\n"
                                + "• type: must be 'application/vnd.google-apps.spreadsheet'"
                                , MessageType.Info
                            );
                        }

                        EditorGUILayout.EndVertical();
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                {
                    if (config.AssetOutputFolderExist)
                    {
                        EditorGUILayout.HelpBox(config.FullAssetOutputFolderPath, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Folder must exist on the disk", MessageType.Error);
                    }

                    EditorGUILayout.BeginHorizontal();
                    DrawProperty(config, _relativeAssetOutputFolderPath, _labelOutputFolderPath);

                    var openFolderPanel = false;

                    if (GUILayout.Button("Browse", GUILayout.Width(65)))
                    {
                        openFolderPanel = true;
                        BrowseFolder("Choose a folder", _relativeAssetOutputFolderPath, config);
                    }

                    if (openFolderPanel == false)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                {
                    var color = GUI.color;
                    GUI.color = Color.green;

                    var enabled = GUI.enabled;
                    GUI.enabled = enabled
                        && config.ServiceAccountFileExist
                        && config.SpreadsheetIdFilePathExist
                        && config.AssetOutputFolderExist
                        ;

                    if (GUILayout.Button("Export All Assets", GUILayout.Height(25)))
                    {
                        config.ExportDataTableAssets();
                    }

                    GUI.enabled = enabled;
                    GUI.color = color;
                }

                {
                    var enabled = GUI.enabled;
                    GUI.enabled = enabled && config.DatabaseFileExist;

                    if (GUILayout.Button("Locate Database Asset", GUILayout.Height(25)))
                    {
                        config.LocateDatabaseAsset();
                    }

                    GUI.enabled = enabled;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("CSV", EditorStyles.boldLabel);
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                {
                    if (config.CsvOutputFolderExist)
                    {
                        EditorGUILayout.HelpBox(config.FullCsvOutputFolderPath, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Folder must exist on the disk", MessageType.Error);
                    }

                    EditorGUILayout.BeginHorizontal();
                    DrawProperty(config, _relativeCsvOutputFolderPath, _labelOutputFolderPath);

                    var openFolderPanel = false;

                    if (GUILayout.Button("Browse", GUILayout.Width(65)))
                    {
                        openFolderPanel = true;
                        BrowseFolder("Choose a folder", _relativeCsvOutputFolderPath, config);
                    }

                    if (openFolderPanel == false)
                    {
                        EditorGUILayout.EndHorizontal();

                        DrawProperty(config, _csvFolderPerSpreadsheet, _labelCsvFolderPerSpreadsheet);
                        DrawProperty(config, _cleanCsvOutputFolder, _labelCleanCsvOutputFolder);

                        if (config.CsvFolderPerSpreadsheet)
                        {
                            DrawProperty(config, _cleanCsvOutputSubFolders, _labelCleanCsvOutputSubFolders);
                        }

                        DrawProperty(config, _commentOutFileNameIfPossible, _labelCommentOutFileNameIfPossible);

                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                {
                    var enabled = GUI.enabled;
                    GUI.enabled = enabled
                        && config.ServiceAccountFileExist
                        && config.SpreadsheetIdFilePathExist
                        && config.CsvOutputFolderExist
                        ;

                    if (GUILayout.Button("Export To CSV Files", GUILayout.Height(25)))
                    {
                        config.ExportCsvFiles();
                    }

                    GUI.enabled = enabled;
                }
                EditorGUILayout.EndHorizontal();
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

        private void DrawProperty(Object obj, SerializedProperty property, GUIContent label)
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
