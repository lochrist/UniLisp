using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NativeAPITests
{
    public static GameObject CreateNewSceneObject(string name)
    {
        return new GameObject(name);
    }

    public static void SelectObject(GameObject obj)
    {
        Selection.activeObject = obj;
    }
}
