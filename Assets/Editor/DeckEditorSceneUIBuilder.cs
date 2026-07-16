using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class DeckEditorSceneUIBuilder
{
    [MenuItem("Tools/Deck Editor/Rebuild Editable UI")]
    public static void RebuildEditableUI()
    {
        const string scenePath = "Assets/Scenes/DeckEditor.unity";
        if (EditorSceneManager.GetActiveScene().path != scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        DeckEditorSceneController controller = Object.FindFirstObjectByType<DeckEditorSceneController>();
        if (controller == null)
        {
            Debug.LogError("The DeckEditor scene does not contain a DeckEditorSceneController.");
            return;
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create Event System");
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        controller.RebuildEditableInterface();
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorSceneManager.SaveScene(controller.gameObject.scene);
        Selection.activeGameObject = controller.gameObject;
        Debug.Log("Deck Editor UI rebuilt as an editable scene hierarchy.");
    }
}
