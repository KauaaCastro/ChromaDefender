using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Escurece a tela (fade para preto) e clareia de novo ao carregar a próxima cena.
/// Criado sozinho na primeira vez que é usado e persiste entre cenas (desenhado via OnGUI, sem Canvas).
///
/// Uso: StartCoroutine(ScreenFader.FadeOut(0.6f)); depois SceneManager.LoadScene(...).
/// A nova cena clareia automaticamente.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private static ScreenFader instance;

    private float alpha;
    private Texture2D blackTex;
    private bool fadeInOnNextLoad;
    private float fadeInDuration = 0.6f;

    public static bool IsBusy => instance != null && instance.alpha > 0.001f;

    private static ScreenFader EnsureInstance()
    {
        if (instance != null) return instance;
        var go = new GameObject("[ScreenFader]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ScreenFader>();
        return instance;
    }

    /// <summary>Escurece a tela. A próxima cena carregada clareia sozinha.</summary>
    public static IEnumerator FadeOut(float duration, float fadeInAfterLoad = 0.6f)
    {
        var f = EnsureInstance();
        f.fadeInOnNextLoad = true;
        f.fadeInDuration = fadeInAfterLoad;
        yield return f.FadeTo(1f, duration);
    }

    /// <summary>Clareia a tela (usado quando a troca de cena é cancelada).</summary>
    public static IEnumerator FadeIn(float duration)
    {
        var f = EnsureInstance();
        f.fadeInOnNextLoad = false;
        yield return f.FadeTo(0f, duration);
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        float start = alpha;
        float t = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        alpha = target;
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (!fadeInOnNextLoad) return;
        fadeInOnNextLoad = false;
        StopAllCoroutines();
        StartCoroutine(FadeInAfterLoad());
    }

    private IEnumerator FadeInAfterLoad()
    {
        alpha = 1f;
        // Espera os SpawnPoints posicionarem jogador e câmera antes de mostrar a cena.
        yield return null;
        yield return null;
        yield return FadeTo(0f, fadeInDuration);
    }

    private void OnGUI()
    {
        if (alpha <= 0.001f) return;
        if (blackTex == null)
        {
            blackTex = new Texture2D(1, 1);
            blackTex.SetPixel(0, 0, Color.black);
            blackTex.Apply();
        }
        GUI.depth = -1000; // por cima da HUD de progresso
        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTex);
        GUI.color = prev;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
}
