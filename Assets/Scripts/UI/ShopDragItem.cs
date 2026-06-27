using UnityEngine;
using UnityEngine.EventSystems;

public class ShopDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RuntimeGameUI owner;
    public PlayerInventory.InventorySlot item;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 0.72f;
        canvasGroup.blocksRaycasts = false;
        owner?.BeginShopItemDrag(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner?.DragShopItem(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        owner?.EndShopItemDrag(this, eventData.position);
    }
}
