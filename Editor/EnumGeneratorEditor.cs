using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
using Object = UnityEngine.Object;
using System.Collections.Generic;

public class EnumGeneratorEditor : EditorWindow
{
    private List<string> _folderPaths = new List<string>();  // Enumとして書き出すフォルダを指定するための変数
    private string _savePath;
    private string _className = "EnumClassName"; // Enumを保存するクラス名のデフォルト値
    private string _enumName = "EnumName"; // Enum名のデフォルト値

    private float _windowWidth;
    // Keeps track of the type of the selected object
    private Object _selectedObjectType = null;

    private readonly char[] _cantDefinedVariableName = new char[]{
        ' ', '-', '$', '&', ',', '/', '*', '+', '=', '!', '?', ':',
        '@', '#', '~', ';', '"', '\'', '<', '>', '%', '^',
        '.', '(', ')', '[', ']', '{', '}'
    };

    [MenuItem("Window/GenerateEnum")]
    public static void Open()
    {
        GetWindow<EnumGeneratorEditor>(false, "generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Enum Generator", EditorStyles.boldLabel);

        GUILayout.Space(10);

        _windowWidth = this.position.width;
        //Debug.Log(_windowWidth);

        // オブジェクトフォルダの選択ボタン
        // フォルダパスのリストの表示とコントロール
        for (int i = 0; i < _folderPaths.Count; i++)
        {
            GUILayout.BeginHorizontal();

            string pathLabelText = $"Object Folder Path #{i + 1}: {_folderPaths[i]}";
            int buttonWidth = 100;
            string trimmedPathLabelText = TrimTextToFit(pathLabelText, (int)_windowWidth - buttonWidth);
            GUILayout.Label(trimmedPathLabelText);

            GUILayout.FlexibleSpace(); // 空間を作成

            if (GUILayout.Button("Remove", GUILayout.Width(buttonWidth)))    //Removeボタン
            {
                _folderPaths.RemoveAt(i);
                --i;
                GUILayout.EndHorizontal();
                continue;
            }

            GUILayout.EndHorizontal();
        }

        // フォルダパスの追加ボタン
        if (GUILayout.Button("Add Object Folder"))
        {
            string newPath = EditorUtility.OpenFolderPanel("Select Folder", string.Empty, string.Empty);
            if (!string.IsNullOrEmpty(newPath))
            {
                _folderPaths.Add(newPath);
            }
        }

        // 保存フォルダの選択ボタン
        if (GUILayout.Button("Select Save Folder"))
        {
            _savePath = EditorUtility.OpenFolderPanel("Select Folder", _savePath, string.Empty);
        }

        // 保存フォルダのパス表示
        EditorGUILayout.LabelField(string.IsNullOrEmpty(_savePath) ? "Path is NULL" : $"Save Folder Path: {_savePath}");

        GUILayout.Space(20);

        // クラス名の入力フィールド
        _className = EditorGUILayout.TextField("Class Name", _className);

        // Enum名の入力フィールド
        _enumName = EditorGUILayout.TextField("Enum Name", _enumName);

        _selectedObjectType = EditorGUILayout.ObjectField("Select Object", _selectedObjectType, typeof(Object), true);

        GUILayout.Space(20);

        // Enum生成ボタン
        if (GUILayout.Button("Generate Enum"))
        {
            GenerateEnum();
        }

        // Create Enum with Selected Object Type
        if (GUILayout.Button("Generate Enum With Selected Object Type"))
        {
            GenerateEnumByType();
        }

    }

    private void GenerateEnum()
    {
        if (string.IsNullOrEmpty(_savePath))
        {
            Debug.LogError("[EnumGenerator] セーブ先がありません");
            return;
        }
        List<FileInfo> files = new();
        foreach (string folderPath in _folderPaths)
        {
            // 各ディレクトリに対して、ファイル一覧を取得し、それらを追加
            DirectoryInfo directory = new DirectoryInfo(folderPath);
            foreach (var item in directory.GetFiles())
            {
                files.Add(item);
            }
        }

        //DirectoryInfo directory = new DirectoryInfo(_folderPath);
        //FileInfo[] files = directory.GetFiles();

        string enumCode = "public enum " + _enumName + "\n";
        enumCode += "{\n";

        foreach (FileInfo file in files)
        {
            if (file.Name.Contains(".meta"))
            {
                continue;
            }
            string name = SanitizeVariableName(Path.GetFileNameWithoutExtension(file.Name));
            if (!string.IsNullOrEmpty(name))
            {
                enumCode += "    " + name + ",\n";
            }
        }

        enumCode += "}";

        string enumSavePath = Path.Combine(_savePath, _className + ".cs");

        // 既に同じ名前のファイルが存在する場合は上書き保存するか確認するポップアップを表示する
        if (File.Exists(enumSavePath))
        {
            bool overwrite = EditorUtility.DisplayDialog("File Already Exists",
                "ファイルが存在します、上書き保存しますか?", "Yes", "No");

            if (!overwrite)
            {
                return;
            }
        }

        File.WriteAllText(enumSavePath, enumCode);

        AssetDatabase.Refresh();

        Debug.Log("Enum generated successfully!");
    }

    private void GenerateEnumByType()
    {
        if (_selectedObjectType == null)
        {
            Debug.LogError("[EnumGenerator] オブジェクトが選択されていません");
            return;
        }

        if (string.IsNullOrEmpty(_savePath))
        {
            Debug.LogError("[EnumGenerator] セーブ先がありません");
            return;
        }

        List<FileInfo> files = new();
        foreach (string folderPath in _folderPaths)
        {
            // 各ディレクトリに対して、ファイル一覧を取得し、それらを追加
            DirectoryInfo directory = new DirectoryInfo(folderPath);
            foreach (var item in directory.GetFiles())
            {
                files.Add(item);
            }
        }

        Type objectType = _selectedObjectType.GetType();
        string enumCode = "public enum " + _enumName + "\n";
        enumCode += "{\n";

        foreach (FileInfo file in files)
        {
            if (file.Extension.Equals(".meta"))
            {
                continue;
            }
            // Unityの相対パスに変換（"Assets/"から始まるパス）
            string assetPath = "Assets" + file.FullName.Substring(Application.dataPath.Length);
            Object obj = AssetDatabase.LoadAssetAtPath(assetPath, objectType);
            if (obj != null)
            {
                string originalName = obj.name;
                enumCode += $"    {SanitizeVariableName(obj.name)},\n";
            }
        }

        enumCode += "}";

        string enumSavePath = Path.Combine(_savePath, _className + ".cs");

        // 既に同じ名前のファイルが存在する場合は上書き保存するか確認するポップアップを表示する
        if (File.Exists(enumSavePath))
        {
            bool overwrite = EditorUtility.DisplayDialog("File Already Exists", "同じ名前のファイルが存在します。上書き保存しますか?", "Yes", "No");
            if (!overwrite)
            {
                return;
            }
        }

        File.WriteAllText(enumSavePath, enumCode);

        AssetDatabase.Refresh();

        Debug.Log("Enum generated successfully with specified object type!");
    }
    private string TrimTextToFit(string text, int maxLength)
    {
        int fontSize = 6;

        maxLength = Mathf.Max(3 * fontSize, maxLength);

        if (text.Length * fontSize > maxLength)
        {
            return text.Substring(0, (maxLength / fontSize) - 3 * fontSize) + "...";
        }
        else
        {
            return text;
        }
    }
    private string SanitizeVariableName(string name)
    {
        return new string(name.Select(c => _cantDefinedVariableName.Contains(c) ? '_' : c).ToArray());
    }
}
