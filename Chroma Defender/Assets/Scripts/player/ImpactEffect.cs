using System.Collections;
using UnityEngine;

/// <summary>
/// Efeito de impacto / explosão gerado no golpe final do ataque especial ou no parry bem-sucedido.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ImpactEffect : MonoBehaviour
{
    [SerializeField] private Sprite[] animationFrames;
    [SerializeField] private float frameRate = 0.06f;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (animationFrames != null && animationFrames.Length > 0)
        {
            StartCoroutine(PlayAnimationRoutine());
        }
        else
        {
            Destroy(gameObject, 0.5f);
        }
    }

    private IEnumerator PlayAnimationRoutine()
    {
        for (int i = 0; i < animationFrames.Length; i++)
        {
            sr.sprite = animationFrames[i];
            yield return new WaitForSeconds(frameRate);
        }

        Destroy(gameObject);
    }
}
