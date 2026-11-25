#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Chroma Defender > Criar Menu de Pausa.
/// O PauseMenuController também se cria sozinho em tempo de execução caso não esteja na cena.
/// </summary>
public static class PauseMenuBuilder
{
    [MenuItem("Chroma Defender/Criar Menu de Pausa")]
    public static void Build()
    {
        var old = Object.FindFirstObjectByType<PauseMenuController>();
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var go = new GameObject("MenuPausa");
        Undo.RegisterCreatedObjectUndo(go, "Criar Menu de Pausa");
        var ctrl = go.AddComponent<PauseMenuController>();
        ctrl.EnsureUI();

        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("[Pausa] Menu de Pausa + Quadro de Controles criado na cena (teclas ESC ou P).");
    }
}
#endif
