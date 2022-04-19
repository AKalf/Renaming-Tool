using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class RenameAssetsInFolder : EditorWindow {

#region  Options fields

    /// <summary> Rename gameobjects with specified component or rename children of a gameobject </summary>
    bool findGameObjectsBasedOnType = false;

    /// <summary> Should names be entirely replaced or just modified? </summary>
    bool overwriteEntireName = false;

    /// <summary> Holder for the object used as reference for type </summary>
    Object objectTypeHolder = new Object();

    /// <summary> The type of the object used as refernece </summary>
    System.Type typeOfObjects = null;

    /// <summary> The objects to be drawn for renaming </summary>
    List<Object> objectsToBeRenamed = new List<Object>();

    /// <summary> Current names of objects to be renamed </summary>
    string[] nameOfObjects = new string[0];

    /// <summary> Substrings to be removed from names </summary>
    List<string> stringsToBeRemoved = new List<string>();

#endregion

#region Foldouts fields

    /// <summary> Is renaming targeting scene gameobjects? </summary>
    bool foldoutRenameGameobjects = false;

    /// <summary> Is renaming targeting assets? </summary>
    bool foldoutRenameAssets = false;

    bool foldoutObjectsToDraw = true;

    bool foldoutNamesOfObjects = true;

#endregion

#region  Scrolls
    Vector2 scrollWindow = Vector2.zero;
    Vector2 scrollObjectsInFolders = Vector2.zero;
    Vector2 scrollNamesOfObjects = Vector2.zero;
#endregion

#region  Overwrite Renaming

    string nameToOverwriteWith = "";

    bool incrementNamesWithNumber = true;

#endregion

#region Additive Renaming

    public List<string> pathsToFolders = new List<string>();

    string newPathToFolder = "";

    string stringToAddBeforeName = "";

    string stringToAddAfterName = "";

#endregion

#region  Replacement Renaming
    private string stringToReplace = "";
    private string stringToReplaceWith = "";
#endregion


    // Add menu named "My Window" to the Window menu
    [MenuItem("Tools/Rename assets")]
    static void Init() {
        // Get existing open window or if none, make a new one:
        RenameAssetsInFolder window = (RenameAssetsInFolder)EditorWindow.GetWindow(typeof(RenameAssetsInFolder));
        window.Show();
    }

    void OnGUI() {
        // scrolling for the entire video
        scrollWindow = EditorGUILayout.BeginScrollView(scrollWindow);

        bool prev = foldoutRenameGameobjects;
        foldoutRenameGameobjects = EditorGUILayout.Foldout(foldoutRenameGameobjects, "Rename gameobjects");
        if (foldoutRenameGameobjects) {
            foldoutRenameAssets = false;
        }
        prev = foldoutRenameAssets;
        foldoutRenameAssets = EditorGUILayout.Foldout(foldoutRenameAssets, "Rename assets");
        if (foldoutRenameAssets) {
            foldoutRenameGameobjects = false;
        }

        if (foldoutRenameAssets) {
            DrawFolderPathFields();
        }
        else if (foldoutRenameGameobjects) {
            findGameObjectsBasedOnType = EditorGUILayout.ToggleLeft("Get objects based on type", findGameObjectsBasedOnType);
        }

        if (foldoutRenameAssets || foldoutRenameGameobjects) {

            DrawTypeField();

            if (objectsToBeRenamed.Count > 0) {
                DrawObjectsToBeRenamed();

                overwriteEntireName = EditorGUILayout.Toggle("Overwrite entire name ", overwriteEntireName);
                if (overwriteEntireName) {
                    DrawOverwriteRenaming();
                }
                else {
                    DrawAdditiveRenaming();
                }
                RemoveStringsFromAllNames();
                if (GUILayout.Button("Set first character of name to upper")) {
                    UpperCaseFirstLetterInName();
                }
                DrawReplaceButton();
            }
            else {
                EditorGUILayout.HelpBox("No objects for renaming found", MessageType.Error);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawObjectsToBeRenamed() {

        foldoutObjectsToDraw = EditorGUILayout.Foldout(foldoutObjectsToDraw, "Show objects in folders");

        if (foldoutObjectsToDraw) {

            scrollObjectsInFolders = EditorGUILayout.BeginScrollView(scrollObjectsInFolders);

            if (objectsToBeRenamed.Count > 0) {
                for (int i = 0; i < objectsToBeRenamed.Count; i++) {

                    Object obj = EditorGUILayout.ObjectField(objectsToBeRenamed[i], typeOfObjects, foldoutRenameGameobjects);

                    if (obj == null) {
                        objectsToBeRenamed.Remove(objectsToBeRenamed[i]);
                    }
                    else if (obj != objectsToBeRenamed[i]) {
                        Debug.Log("\n Changing " + objectsToBeRenamed[i].name + " with " + obj.name);
                        objectsToBeRenamed[i] = obj;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawTypeField() {
        EditorGUILayout.LabelField("Class Type of objects to look for (set null for all)", EditorStyles.boldLabel);
        string msg = "";
        Object objectOnField = EditorGUILayout.ObjectField(objectTypeHolder, typeof(Object), true);

        if (objectTypeHolder != objectOnField) {
            msg += "Object selected: " + objectOnField;
        }
        objectTypeHolder = objectOnField;
        // if (objectTypeHolder == null && foldoutRenameGameobjects) {
        //     return;
        // }
        // Case empty (therefore renaming is target everything)
        if (objectTypeHolder == null && typeOfObjects != typeof(UnityEngine.Object)) {
            msg += ("\n\n objectTypeHolder is null and  typeOfObjects is not UnityEngine.Object.\n Renaming set to target everything, setting typeOfObjects as UnityEngine.Object");
            typeOfObjects = typeof(Object);
            if (!foldoutRenameGameobjects && !foldoutRenameAssets) {
                if (pathsToFolders.Count > 0) {
                    msg += ("\n Mode set to *Asset Renaming*, pathsToFolders: " + pathsToFolders.Count);
                    foldoutRenameAssets = true;
                    foldoutRenameGameobjects = false;
                }
                else {
                    // msg += ("\n Mode set to *GameObjects Renaming*");
                    // foldoutRenameGameobjects = true;
                    // foldoutRenameAssets = false;
                }
                findGameObjectsBasedOnType = false;
            }
        }
        else if (objectTypeHolder != null && typeOfObjects != objectTypeHolder.GetType()) {
            // Case that reference object is type of Monobehaviour script (therefore renaming is targeting gameobjects)
            if (objectTypeHolder.GetType() == typeof(MonoScript) && typeOfObjects != ((MonoScript)objectTypeHolder).GetClass()) {
                msg += "\n\n" + objectTypeHolder.GetType() + " is not type of Monoscrpt and " + typeOfObjects + " != " + ((MonoScript)objectTypeHolder).GetClass().Name +
                    "\n Renaming is targeting GameObjects (MonoBehaviour)\n";
                typeOfObjects = ((MonoScript)objectTypeHolder).GetClass();
                foldoutRenameGameobjects = true;
                msg += "\n foldoutRenameGameobjects = true";
                findGameObjectsBasedOnType = true;
                msg += ", findGameObjectsBasedOnType = true";
                foldoutRenameAssets = false;
                msg += ", foldoutRenameAssets = false";
            }
            // Case that reference is a Gameobject (therefore renaming is targeting it's children)
            else if (objectTypeHolder is GameObject) {
                msg += "\n\n objectTypeHolder is Gameobject \n Renaming set to target gameobject's children\n";
                typeOfObjects = objectTypeHolder.GetType();
                foldoutRenameGameobjects = true;
                msg += "\n foldoutRenameGameobjects = true";
                foldoutRenameAssets = false;
                msg += ", foldoutRenameAssets = false";
                findGameObjectsBasedOnType = false;
                msg += ", findGameObjectsBasedOnType = false";
            }
            // Case of renaming asset
            else if (AssetDatabase.Contains(objectTypeHolder) && objectTypeHolder.GetType() != typeof(MonoScript)) {
                msg += "\n\n AssetDatabase contains " + objectTypeHolder.name + " &&  objectTypeHolder type (" + objectTypeHolder.GetType() + ") is not Monoscript" +
                    "\n Renaming set to target assets\n";
                typeOfObjects = objectTypeHolder.GetType();
                foldoutRenameGameobjects = false;
                msg += "\n foldoutRenameGameobjects = false";
                findGameObjectsBasedOnType = false;
                msg += ", findGameObjectsBasedOnType = false";
                foldoutRenameAssets = true;
                msg += ", foldoutRenameAssets = true";
            }
        }
        if (msg.Trim() != "") {
            Debug.Log(msg + "\n\n");
        }
        FindObjectsToBeRenamed();
        EditorGUILayout.HelpBox("Type selected: " + typeOfObjects + "\n \n Debug: " + msg, MessageType.Info);
    }

    private void DrawFolderPathFields() {
        EditorGUILayout.LabelField("Folder paths", EditorStyles.boldLabel);
        for (int i = 0; i < pathsToFolders.Count; i++) {
            string path = EditorGUILayout.TextField(pathsToFolders[i]);
            if (path == "") {
                pathsToFolders.Remove(pathsToFolders[i]);
                FindObjectsToBeRenamed();
            }
            else if (path != pathsToFolders[i]) {
                pathsToFolders[i] = path;
                FindObjectsToBeRenamed();
            }
        }
        newPathToFolder = EditorGUILayout.TextField(newPathToFolder);
        if (newPathToFolder != "" && !pathsToFolders.Contains(newPathToFolder)) {
            pathsToFolders.Add(newPathToFolder);
            newPathToFolder = "";
            FindObjectsToBeRenamed();
        }
    }

    private void FindObjectsToBeRenamed() {

        string msg = "";
        if (objectsToBeRenamed.Count > 0) {
            msg = "Clearing old objects to be renamed: " + objectsToBeRenamed.Count;
            objectsToBeRenamed.Clear();
        }


        // Case find assets from folders
        if (foldoutRenameAssets && pathsToFolders.Count > 0) {
            msg += ("\n\n foldoutRenameAssets == true, pathsToFolders.Count == " + pathsToFolders.Count);
            msg += "\n Case getting assets from folders";

            string[] guidPaths = UnityEditor.AssetDatabase.FindAssets("t:" + typeOfObjects.Name, pathsToFolders.ToArray());

            if (guidPaths.Length > 0) {
                nameOfObjects = new string[guidPaths.Length];
                msg += "\n Total objects found for renaming: " + guidPaths.Length;
                for (int i = 0; i < guidPaths.Length; i++) {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guidPaths[i]);
                    Object objAtPath = AssetDatabase.LoadAssetAtPath(path, typeOfObjects);
                    nameOfObjects[i] = objAtPath.name;
                    objectsToBeRenamed.Add(objAtPath);
                    msg += "\n " + objAtPath.name + " is added to be renamed ";
                }
            }
            else {
                msg += "\n No assets of type: " + typeOfObjects.Name + " were found !!!";
            }

        }
        // Case find gameobjects based on component
        else if (foldoutRenameGameobjects && findGameObjectsBasedOnType) {
            msg += ("\n\n foldoutRenameGameobjects == true &&  findGameObjectsBasedOnType == true" +
                "\n Getting objects from scene based on component");

            Object[] objs = GameObject.FindObjectsOfType(typeOfObjects);

            if (objs.Length > 0) {
                nameOfObjects = new string[objs.Length];
                msg += "\n Total objects found for renaming: " + objs.Length;
                for (int i = 0; i < objs.Length; i++) {
                    nameOfObjects[i] = objs[i].name;
                    objectsToBeRenamed.Add(objs[i]);
                    msg += "\n " + objs[i].name + " is added to be renamed ";
                }
            }
            else {
                msg += "\n No GameObject of type: " + typeOfObjects.Name + " were found !!!";
            }
        }
        // Case find gameobjects based on gamobject children
        else if (foldoutRenameGameobjects && !findGameObjectsBasedOnType) {
            msg += ("\n\n foldoutRenameGameobjects == true &&  findGameObjectsBasedOnType == false" +
                "\n Getting objects from based on gameobject's children");
            List<Transform> allChildren = new List<Transform>();

            // case where a parent object has not been specified (through objectTypeHolder, so we take each root gameobject of the current scene as parent gameobject)
            if (objectTypeHolder == null) {
                GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                msg += "\n Case: parent object has not been specified, each root gameobject of the current scene is set as parent gameobject.\n Total roots: " + roots.Length;
                foreach (GameObject root in roots) {
                    //allChildren.Add(root.transform);
                    allChildren = allChildren.Concat(RecursiveChildrenDiscovery(root.transform)).ToList();
                    Debug.Log(msg + "\n\n");
                }
            }
            // case parent object has been specified (through objectTypeHolder)
            else if (objectTypeHolder is GameObject) {
                GameObject parent = objectTypeHolder as GameObject;
                msg += "\n Case: parent object has been specified (through objectTypeHolder), " + parent.name;
                allChildren = RecursiveChildrenDiscovery(parent.transform);
                //allChildren.Insert(0, parent.transform);
                Debug.Log(msg + "\n\n");
            }

            if (allChildren.Count > 0) {
                nameOfObjects = new string[allChildren.Count];
                msg += "\n Children found: " + allChildren.Count;
                for (int child = 0; child < allChildren.Count; child++) {
                    nameOfObjects[child] = allChildren[child].name;
                    objectsToBeRenamed.Add(allChildren[child]);
                }
                Debug.Log(msg + "\n\n");
            }
            else {
                msg += "\n No children were retrieved";
                Debug.Log(msg + "\n\n");

            }
        }


        if (objectsToBeRenamed.Count == 0) {
            if (foldoutRenameAssets) {
                EditorGUILayout.HelpBox("No Assets of type: " + typeOfObjects.Name + " were found in folders!", MessageType.Error);
                //Debug.LogError ("No Assets of type: " + typeOfObjects.Name + " were found!");
            }
            else if (findGameObjectsBasedOnType && foldoutRenameGameobjects) {
                EditorGUILayout.HelpBox("No Gameobjects with component: " + typeOfObjects.Name + " were found in scene!", MessageType.Error);
                //Debug.LogError ("No Gameobjects with component: " + typeOfObjects.Name + " were found!");
            }
            else if (foldoutRenameGameobjects) {
                EditorGUILayout.HelpBox("No children of gameobject: " + typeOfObjects.Name + " were found!", MessageType.Error);
                //Debug.LogError ("No children of gameobject: " + typeOfObjects.Name + " were found!");
            }

        }
    }

    private List<Transform> RecursiveChildrenDiscovery(Transform parent) {
        List<Transform> allChildren = new List<Transform>();

        for (int child = 0; child < parent.childCount; child++) {
            allChildren.Add(parent.GetChild(child));
            if (parent.GetChild(child).childCount > 0) {
                List<Transform> childChildren = RecursiveChildrenDiscovery(parent.GetChild(child));
                if (childChildren.Count > 0) {
                    foreach (Transform childChild in childChildren) {
                        allChildren.Add(childChild);
                    }
                }
            }
        }
        return allChildren;

    }

    private void SetObjectsNames() {
        nameOfObjects = new string[objectsToBeRenamed.Count];
        for (int i = 0; i < objectsToBeRenamed.Count; i++) {
            nameOfObjects[i] = objectsToBeRenamed[i].name;
        }
    }

    private void UpperCaseFirstLetterInName() {
        for (int i = 0; i < objectsToBeRenamed.Count; i++) {
            char[] nameAsChars = objectsToBeRenamed[i].name.ToCharArray();
            nameAsChars[0] = nameAsChars[0].ToString().ToUpper().ToCharArray()[0];
            string newName = "";
            foreach (char c in nameAsChars) {
                newName += c;
            }
            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(objectsToBeRenamed[i].GetInstanceID()), newName);
        }
    }

    private void DrawOverwriteRenaming() {
        string msg = "\n\n ## Draw Overwrite Renaming ## \n";
        EditorGUILayout.LabelField("The name to change to", EditorStyles.boldLabel);
        nameToOverwriteWith = EditorGUILayout.TextField(nameToOverwriteWith);
        msg += "\n new name: " + nameToOverwriteWith;
        incrementNamesWithNumber = EditorGUILayout.Toggle("Add increment number to end ", incrementNamesWithNumber, GUILayout.MinWidth(150));
        msg += "\n incrementation: " + incrementNamesWithNumber;
        if (incrementNamesWithNumber == false && typeOfObjects == typeof(UniversalAppMediaItem)) {
            EditorGUILayout.HelpBox("UniversalMediaItems must NOT have the same name. Please choose *incremented* renaming.", MessageType.Error);
        }
        else if (GUILayout.Button("Change names to: " + nameToOverwriteWith)) {
            if (incrementNamesWithNumber) {
                int increment = 0;

                foreach (Object obj in objectsToBeRenamed) {
                    if (obj is Transform) {
                        ((Transform)obj).gameObject.name = nameToOverwriteWith + " (" + increment + ")";
                    }
                    else {
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj.GetInstanceID()), nameToOverwriteWith + '.' + increment);
                    }
                    increment++;
                }
            }
            else {
                foreach (Object obj in objectsToBeRenamed) {
                    if (obj is Transform) {
                        ((Transform)obj).gameObject.name = nameToOverwriteWith;
                    }
                    else {
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj.GetInstanceID()), nameToOverwriteWith);
                    }
                }
            }
            Debug.Log(msg);
            SetObjectsNames();
        }
    }

    private void DrawAdditiveRenaming() {
        string msg = "\n\n ## Draw Additive Renaiming ## \n";
        EditorGUILayout.LabelField("String to add before current name: ");

        stringToAddBeforeName = EditorGUILayout.TextField(stringToAddBeforeName);
        msg += "\n string to add before name " + stringToAddBeforeName;
        foldoutNamesOfObjects = EditorGUILayout.Foldout(foldoutNamesOfObjects, "Show names of objects");

        if (foldoutNamesOfObjects) {

            scrollNamesOfObjects = EditorGUILayout.BeginScrollView(scrollNamesOfObjects);

            if (nameOfObjects.Length > 0) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Object whose name will be changed");
                EditorGUILayout.LabelField("New name of the object");
                EditorGUILayout.EndHorizontal();
                for (int i = 0; i < objectsToBeRenamed.Count; i++) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(objectsToBeRenamed[i], typeOfObjects, foldoutRenameGameobjects);
                    nameOfObjects[i] = EditorGUILayout.TextField(nameOfObjects[i]);
                    msg += "\n Object:  " + objectsToBeRenamed[i].name + " obj to be renamed name: " + nameOfObjects[i];

                    if (objectsToBeRenamed[i].name != nameOfObjects[i]) {
                        if (objectsToBeRenamed[i] is Transform) {
                            ((Transform)objectsToBeRenamed[i]).gameObject.name = nameOfObjects[i];
                        }
                        else {
                            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(objectsToBeRenamed[i].GetInstanceID()), nameOfObjects[i]);
                        }
                        Debug.Log(msg);
                    }
                    EditorGUILayout.EndHorizontal();
                }

            }

            EditorGUILayout.EndScrollView();

        }
        EditorGUILayout.LabelField("String to add after current name: ");
        stringToAddAfterName = EditorGUILayout.TextField(stringToAddAfterName);

        if (GUILayout.Button("Change names to: " + stringToAddBeforeName + "CurrentName" + stringToAddAfterName)) {

            for (int i = 0; i < objectsToBeRenamed.Count; i++) {
                if (objectsToBeRenamed[i] != null) {
                    if (foldoutRenameAssets) {
                        if (nameOfObjects[i] != null || nameOfObjects[i] != "") {

                            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(objectsToBeRenamed[i].GetInstanceID()), stringToAddBeforeName + nameOfObjects[i] + stringToAddAfterName);
                        }
                        else {
                            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(objectsToBeRenamed[i].GetInstanceID()), stringToAddBeforeName + objectsToBeRenamed[i].name + stringToAddAfterName);
                        }
                    }
                    else if (foldoutRenameGameobjects) {
                        if (nameOfObjects[i] != null || nameOfObjects[i] != "") {

                            objectsToBeRenamed[i].name = stringToAddBeforeName + nameOfObjects[i] + stringToAddAfterName;
                        }
                        else {
                            objectsToBeRenamed[i].name = stringToAddBeforeName + objectsToBeRenamed[i].name + stringToAddAfterName;
                        }
                    }
                }
            }
            SetObjectsNames();
        }
    }

    private void DrawReplaceButton() {

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("String to replace: ", GUILayout.Width(250));
        stringToReplace = EditorGUILayout.TextField(stringToReplace);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("String to replace with: ", GUILayout.Width(250));
        stringToReplaceWith = EditorGUILayout.TextField(stringToReplaceWith);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Replace *" + stringToReplaceWith + "* with: *" + stringToReplaceWith + "*")) {
            foreach (Object obj in objectsToBeRenamed) {
                if (obj.name.Contains(stringToReplace)) {
                    string[] split = obj.name.Split(new string[] { stringToReplace }, System.StringSplitOptions.RemoveEmptyEntries);
                    string newName = split[0];
                    for (int i = 1; i < split.Length; i++) {
                        newName += stringToReplaceWith + split[i];
                    }
                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj.GetInstanceID()), newName);
                }
            }
        }
    }

    private void RemoveStringsFromAllNames() {
        EditorGUILayout.LabelField("Strings to remove from names");

        for (int i = 0; i < stringsToBeRemoved.Count; i++) {
            string stringToBeRemoved = EditorGUILayout.TextField(stringsToBeRemoved[i]);
            if (stringToBeRemoved == "") {
                stringsToBeRemoved.Remove(stringsToBeRemoved[i]);
            }
            else if (stringToBeRemoved != stringsToBeRemoved[i]) {
                stringsToBeRemoved[i] = stringToBeRemoved;
            }
        }

        string newStringToBeRemoved = "";
        newStringToBeRemoved = EditorGUILayout.TextField(newStringToBeRemoved);
        if (newStringToBeRemoved != "" && !pathsToFolders.Contains(newStringToBeRemoved)) {
            stringsToBeRemoved.Add(newStringToBeRemoved);
            newStringToBeRemoved = "";
        }
        if (GUILayout.Button("Remove strings from objects names")) {
            for (int i = 0; i < objectsToBeRenamed.Count; i++) {
                if (objectsToBeRenamed[i] != null) {
                    string[] newNameSplitted = objectsToBeRenamed[i].name.Split(stringsToBeRemoved.ToArray(), System.StringSplitOptions.RemoveEmptyEntries);
                    string newName = "";
                    foreach (string s in newNameSplitted) {
                        newName += s;
                    }
                    if (foldoutRenameAssets) {
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(objectsToBeRenamed[i].GetInstanceID()), newName);
                    }
                    else if (foldoutRenameGameobjects) {
                        objectsToBeRenamed[i].name = newName;
                    }
                }
            }
            SetObjectsNames();

        }

    }
}