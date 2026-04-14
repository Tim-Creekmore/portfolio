using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TestServerSetup
{
    [MenuItem("Tools/Add Test Server to Scene")]
    static void AddTestServer()
    {
        if (Object.FindObjectOfType<TestServer>() != null)
        {
            Debug.Log("[TestServer] Already exists in scene.");
            return;
        }

        var parent = GameObject.Find("World");
        var go = new GameObject("TestServer");
        if (parent != null)
            go.transform.SetParent(parent.transform);

        go.AddComponent<TestServer>();
        Undo.RegisterCreatedObjectUndo(go, "Add TestServer");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("[TestServer] Added to scene. Save with Ctrl+S.");
    }
}
