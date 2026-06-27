using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopUiHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Color normalColor = new Color(0.16f, 0.12f, 0.08f, 0.96f);
    public Color hoverColor = new Color(0.25f, 0.19f, 0.1f, 0.98f);
    public Color pressedColor = new Color(0.32f, 0.24f, 0.12f, 1f);
    public float hoverScale = 1.025f;
    public float pressedScale = 0.985f;
    public float smoothSpeed = 12f;

    private Image image;
    private bool isHovered;
    private bool isPressed;
    private Vector3 startScale;

    private void Awake()
    {
        image = GetComponent<Image>();
        startScale = transform.localScale;
    }

    private void Update()
    {
        Color targetColor = isPressed ? pressedColor : isHovered ? hoverColor : normalColor;
        float targetScale = isPressed ? pressedScale : isHovered ? hoverScale : 1f;

        if (image != null)
        {
            image.color = Color.Lerp(image.color, targetColor, Time.unscaledDeltaTime * smoothSpeed);
        }

        transform.localScale = Vector3.Lerp(transform.localScale, startScale * targetScale, Time.unscaledDeltaTime * smoothSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }
}
