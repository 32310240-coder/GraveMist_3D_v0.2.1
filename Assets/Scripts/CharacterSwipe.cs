using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterSwipe : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    Vector2 startPos;

    public GameFlowController manager;

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 endPos = eventData.position;

        float deltaY = endPos.y - startPos.y;

        if (deltaY > 120f)
        {
            manager.ConfirmCharacter();
        }
    }
}