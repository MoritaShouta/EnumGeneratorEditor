using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
using Object = UnityEngine.Object;
using System.Collections.Generic;

public class EnumGeneratorEditor : EditorWindow
{
    private List<string> _folderPaths = new List<string>();  // Enum�Ƃ��ď����o���t�H���_���w�肷�邽�߂̕ϐ�
    private string _savePath;
    private string _className = "EnumClassName"; // Enum��ۑ�����N���X���̃f�t�H���g�l
    private string _enumName = "EnumName"; // Enum���̃f�t�H���g�l

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

        // �I�u�W�F�N�g�t�H���_�̑I���{�^��
        // �t�H���_�p�X�̃��X�g�̕\���ƃR���g���[��
        for (int i = 0; i < _folderPaths.Count; i++)
        {
            GUILayout.BeginHorizontal();

            string pathLabelText = $"Object Folder Path #{i + 1}: {_folderPaths[i]}";
            int buttonWidth = 100;
            string trimmedPathLabelText = TrimTextToFit(pathLabelText, (int)_windowWidth - buttonWidth);
            GUILayout.Label(trimmedPathLabelText);

            GUILayout.FlexibleSpace(); // ��Ԃ��쐬

            if (GUILayout.Button("Remove", GUILayout.Width(buttonWidth)))    //Remove�{�^��
            {
                _folderPaths.RemoveAt(i);
                --i;
                GUILayout.EndHorizontal();
                continue;
            }

            GUILayout.EndHorizontal();
        }

        // �t�H���_�p�X�̒ǉ��{�^��
        if (GUILayout.Button("Add Object Folder"))
        {
            string newPath = EditorUtility.OpenFolderPanel("Select Folder", string.Empty, string.Empty);
            if (!string.IsNullOrEmpty(newPath))
            {
                _folderPaths.Add(newPath);
            }
        }

        // �ۑ��t�H���_�̑I���{�^��
        if (GUILayout.Button("Select Save Folder"))
        {
            _savePath = EditorUtility.OpenFolderPanel("Select Folder", _savePath, string.Empty);
        }

        // �ۑ��t�H���_�̃p�X�\��
        EditorGUILayout.LabelField(string.IsNullOrEmpty(_savePath) ? "Path is NULL" : $"Save Folder Path: {_savePath}");

        GUILayout.Space(20);

        // �N���X���̓��̓t�B�[���h
        _className = EditorGUILayout.TextField("Class Name", _className);

        // Enum���̓��̓t�B�[���h
        _enumName = EditorGUILayout.TextField("Enum Name", _enumName);

        _selectedObjectType = EditorGUILayout.ObjectField("Select Object", _selectedObjectType, typeof(Object), true);

        GUILayout.Space(20);

        // Enum�����{�^��
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
            Debug.LogError("[EnumGenerator] �Z�[�u�悪����܂���");
            return;
        }
        List<FileInfo> files = new();
        foreach (string folderPath in _folderPaths)
        {
            // �e�f�B���N�g���ɑ΂��āA�t�@�C���ꗗ���擾���A������ǉ�
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

        // ���ɓ������O�̃t�@�C�������݂���ꍇ�͏㏑���ۑ����邩�m�F����|�b�v�A�b�v��\������
        if (File.Exists(enumSavePath))
        {
            bool overwrite = EditorUtility.DisplayDialog("File Already Exists",
                "�t�@�C�������݂��܂��A�㏑���ۑ����܂���?", "Yes", "No");

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
            Debug.LogError("[EnumGenerator] �I�u�W�F�N�g���I������Ă��܂���");
            return;
        }

        if (string.IsNullOrEmpty(_savePath))
        {
            Debug.LogError("[EnumGenerator] �Z�[�u�悪����܂���");
            return;
        }

        List<FileInfo> files = new();
        foreach (string folderPath in _folderPaths)
        {
            // �e�f�B���N�g���ɑ΂��āA�t�@�C���ꗗ���擾���A������ǉ�
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
            // Unity�̑��΃p�X�ɕϊ��i"Assets/"����n�܂�p�X�j
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

        // ���ɓ������O�̃t�@�C�������݂���ꍇ�͏㏑���ۑ����邩�m�F����|�b�v�A�b�v��\������
        if (File.Exists(enumSavePath))
        {
            bool overwrite = EditorUtility.DisplayDialog("File Already Exists", "�������O�̃t�@�C�������݂��܂��B�㏑���ۑ����܂���?", "Yes", "No");
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
