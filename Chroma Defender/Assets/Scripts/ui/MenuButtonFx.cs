using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Efeito de hover do botÃ£o: cresce um pouco e acende a borda na cor da polaridade.</summary>
public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public Image border;
    public Color accent = Color.white;
    private bool active;
    private Vector3 baseScale = Vector3.one;

    private void Awake() => baseScale = transform.localScale;
    public void OnPointerEnter(PointerEventData e) => EventSystem.current?.SetSelectedGameObject(gameObject);
    public void OnPointerExit(PointerEventData e) { }
    public void OnSelect(BaseEventData e) => active = true;
    public void OnDeselect(BaseEventData e) => active = false;

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * (active ? 1.07f : 1f), Time.unscaledDeltaTime * 14f);
        if (border != null)
            border.color = Color.Lerp(border.color, active ? accent : new Color(accent.r, accent.g, accent.b, 0.35f), Time.unscaledDeltaTime * 14f);
    }
}

