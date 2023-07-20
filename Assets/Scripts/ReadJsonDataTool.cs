using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Drawing;
using UnityEditor.VersionControl;
using UnityEngine.Assertions;
using Unity.VisualScripting;
using static UnityEditor.Progress;
using static Unity.VisualScripting.Member;
using Unity.VisualScripting.FullSerializer;
using System.Xml;
using static UnityEditor.PlayerSettings;
using static UnityEngine.UIElements.UxmlAttributeDescription;

[CustomEditor(typeof(InitialScript))]
public class ReadJsonDataTool : Editor
{
    InitialScript scriptName;
    GameObject scriptGameObject;

    string dir = "";
    string nameProperty = "";
    string saveDir = "";

    bool readSuccess;

    JObject data = new JObject();
    JObject newData = new JObject();

    Dictionary<string, bool> showData = new Dictionary<string, bool>();
    Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();
    Dictionary<string, string> prefabDictName = new Dictionary<string, string>();
    JObject prefabDataLoad = new JObject();

    List<string> prefabGO = new List<string>();
    List<string> parentNameList = new List<string>();

    private void OnEnable()
    {
        scriptName = (InitialScript)target;
        scriptGameObject = (GameObject)GameObject.Find(scriptName.name);
    }
    public override void OnInspectorGUI()
    {
        dir = EditorGUILayout.TextField("Json Path", dir);

        GUILayout.Space(10);

        if (GUILayout.Button("Load data", GUILayout.Width(420)))
        {
            try
            {
                data = LoadJson(dir);
                CreateParentGameObjectForm(data);
                SaveDataDisplay();
                readSuccess = true;
            }
            catch
            {
                Debug.Log("Load Data Failed");
            }
        }

        if (readSuccess)
        {
            CreateParentGameObjectForm(data);
            SaveDataDisplay();
            readSuccess = false;
        }
        else
        {
            try
            {
                newData = LoadJson(saveDir);
                foreach (JProperty prop in newData.Properties())
                {
                    if (prop.Name == "current_dir")
                    {
                        dir = prop.Value.ToString();
                    }
                    else
                    {
                        JObject testObject = new JObject();

                        prefabDataLoad = JsonConvert.DeserializeObject<JObject>(newData["prefabs"].ToString());
                    }
                }

                foreach (JProperty entry in prefabDataLoad.Properties())
                {
                    Dictionary<string, string> prefabDataDisplay = JsonConvert.DeserializeObject<Dictionary<string, string>>(entry.Value.ToString());

                    if (!showData.ContainsKey(entry.Name))
                    {
                        showData.Add(entry.Name, true);
                        parentNameList.Add(entry.Name);
                    }
                    showData[entry.Name] = EditorGUILayout.Foldout(showData[entry.Name], entry.Name);

                    if (showData[entry.Name])
                    {
                        if (Selection.activeTransform)
                        {
                            foreach (KeyValuePair<string, string> entryChild in prefabDataDisplay)
                            {
                                EditorGUI.indentLevel++;
                                if (!prefabDict.ContainsKey(entryChild.Key))
                                {
                                    GameObject prefabObject = (GameObject)LoadPrefabFromFile(entryChild.Value.ToString());
                                    prefabDict.Add(entryChild.Key, prefabObject);
                                    prefabDictName.Add(entryChild.Key, entryChild.Value);
                                }
                                EditorGUI.BeginChangeCheck();
                                prefabDict[entryChild.Key] = (GameObject)EditorGUILayout.ObjectField(entryChild.Key, prefabDict[entryChild.Key], typeof(GameObject));
                                if (EditorGUI.EndChangeCheck())
                                {
                                    prefabDictName[entryChild.Key] = prefabDict[entryChild.Key].name;
                                    SaveDataDisplay();
                                }
                                EditorGUI.indentLevel--;
                            }
                        }
                    }
                }
                SaveDataDisplay();
            }
            catch
            {
                Debug.LogError("Load Json Data Failed");
            }
        }
    }

    private void CreateChildGameObject(JObject dataReader, Dictionary<string, string> prefabChildDict)
    {
        foreach (JProperty child in dataReader.Properties())
        {
            foreach (KeyValuePair<string, string> entry in prefabChildDict)
            {
                string checkChildObject = entry.Key.Split("_")[0];
                if (child.Name == checkChildObject)
                {
                    GameObject go = new GameObject();
                    go.name = child.Name;


                }
            }
        }
    }

    private void SaveDataDisplay()
    {
        string currentDir = dir;

        Dictionary<string, string> dataSave = new Dictionary<string, string>();

        Dictionary<string, Dictionary<string, string>> dataObjectSetupSave = SetupDictToSave(parentNameList, prefabDictName);
        string prefabSave = JsonConvert.SerializeObject(dataObjectSetupSave);
        if (dataSave.Count == 0)
        {
            dataSave.Add("current_dir", currentDir);
            dataSave.Add("prefabs", prefabSave);
        }

        string finalSave = JsonConvert.SerializeObject(dataSave);
        File.WriteAllText(saveDir, finalSave);
    }

    public JObject LoadJson(string filePath)
    {
        if (readSuccess)
        {
            List<string> splitPathFile = new List<string>(dir.Replace(".", "\\").Split("\\"));
            string jsonFileName = splitPathFile[splitPathFile.Count - 2];

            scriptGameObject.name = jsonFileName;
        }

        JObject data = JObject.Parse(File.ReadAllText(filePath));
        return data;
    }

    private void CreateParentGameObjectForm(JObject dataReader)
    {
        foreach (JProperty child in dataReader.Properties())
        {
           
            if (!showData.ContainsKey(child.Name))
            {
                showData.Add(child.Name, true);
                parentNameList.Add(child.Name);

                GameObject parentGO = new GameObject();
                parentGO.name = child.Name;

                parentGO.transform.SetParent(scriptGameObject.transform, false);
            }
            showData[child.Name] = EditorGUILayout.Foldout(showData[child.Name], child.Name);

            if (showData[child.Name])
            {
                if (Selection.activeTransform)
                {
                    List<string> items = JsonConvert.DeserializeObject<List<string>>(child.Value.ToString());
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (!prefabDict.ContainsKey(items[i]))
                        {
                            GameObject prefabObject = (GameObject)LoadPrefabFromFile(items[i].ToString());
                            prefabDict.Add(prefabObject.name, prefabObject);
                            prefabDictName.Add(prefabObject.name, prefabObject.name);
                        }
                    }
                    LoadObjectData(child.Name);
                }
            }
        }
    }

    private Dictionary<string, Dictionary<string, string>> SetupDictToSave(List<string> parentList, Dictionary<string, string> childPrefabDict)
    {
        Dictionary<string, Dictionary<string, string>> dataSetup = new Dictionary<string, Dictionary<string, string>>();
        for (int i = 0; i < parentList.Count; i++)
        {
            if (!dataSetup.ContainsKey(parentList[i]))
            {
                dataSetup.Add(parentList[i], new Dictionary<string, string>());
                foreach (KeyValuePair<string, string> entry in childPrefabDict)
                {
                    if (entry.Key.Split("_")[0] == parentList[i])
                    {
                        dataSetup[parentList[i]].Add(entry.Key, entry.Value);
                    }
                }
            }
        }
        return dataSetup;
    }

    private void LoadObjectData(string fileName)
    {
        JObject childData = new JObject();
        string filePath = System.IO.Path.GetDirectoryName(dir) + "\\" + fileName + "\\" + fileName + ".json";
        childData = LoadJson(filePath);
        EditorGUI.indentLevel++;
        foreach (JProperty child in childData.Properties())
        {
            bool checkParentObject = false;
            JObject newDataChild = (JObject)child.Value;

            GameObject childGO = new GameObject();
            GameObject parentGO = GameObject.Find(fileName);
            childGO.name = child.Name;

            childGO.transform.SetParent(parentGO.transform, false);

            if (!newDataChild.ContainsKey("prefab"))
            {
                checkParentObject = true;
            }

            foreach (JProperty child2 in newDataChild.Properties())
            {
                Debug.Log(child2.Value.ToString());
                if (checkParentObject)
                {

                    if (child2.Value.ToString() == "childs")
                    {
                        LoadObjectData(child.Name);
                    } else
                    {

                    }
                } else
                {
                    if (child2.Value.ToString() == "prefab")
                    {
                        GameObject prefabObject = (GameObject)LoadPrefabFromFile(child2.Value.ToString());

                        GameObject pNewObject = (GameObject)Instantiate(prefabObject, Vector3.zero, Quaternion.identity);

                        pNewObject.name = name;

                        pNewObject.transform.SetParent(childGO.transform, false);
                    }
                    else
                    {

                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            prefabDict[child.Name] = (GameObject)EditorGUILayout.ObjectField(child.Name, prefabDict[child.Name], typeof(GameObject));
            if (EditorGUI.EndChangeCheck())
            {
                prefabDictName[child.Name] = prefabDict[child.Name].name;
            }
        }
        EditorGUI.indentLevel--;
    }

    private UnityEngine.Object LoadPrefabFromFile(string filename)
    {
        var loadedObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + filename + ".prefab");
        if (loadedObject == null)
        {
            throw new FileNotFoundException("...no file found - please check the configuration");
        }
        return loadedObject;
    }
}
