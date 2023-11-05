using UnityEngine;
using UnityEngine.EventSystems;

public class MoveWithMouse : MonoBehaviour, IDragHandler, IEndDragHandler
{
  
    float leftMargin;
    float rightMargin;
    float topMargin;
    float bottomMargin;
    float middleMargin;
    RectTransform uiRectTransform;

    private void Start()
    {
        uiRectTransform = gameObject.GetComponent<RectTransform>();
    }

    public void setPosRange(float leftMargin, float rightMargin, float topMargin, float bottomMargin)
    {
        this.leftMargin = leftMargin;
        this.rightMargin = rightMargin;
        this.topMargin = topMargin;
        this.bottomMargin = bottomMargin;
        middleMargin = (leftMargin + rightMargin);
        //Debug.Log($"setPosRange leftMargin:{leftMargin} , rightMargin:{rightMargin} , topMargin:{topMargin} , bottomMargin:{bottomMargin}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        dragWithCamera(eventData);
    }

    private Vector3 mousePositionOnScreen;
    private Vector3 mousePositionInWorld;

    void dragWithCamera(PointerEventData eventData)
    {
        mousePositionOnScreen = eventData.position;
        mousePositionInWorld = Camera.main.ScreenToWorldPoint(mousePositionOnScreen);
        var lerpPos = Vector3.Lerp(transform.position, mousePositionInWorld, 1);
        lerpPos.Set(lerpPos.x, lerpPos.y, transform.position.z);
        transform.position = lerpPos;

        var anchPos = uiRectTransform.anchoredPosition;
        float posX = Mathf.Max(anchPos.x, leftMargin);
        posX = Mathf.Min(posX, rightMargin);
       
        float posY = Mathf.Min(anchPos.y, topMargin);
        posY = Mathf.Max(posY, bottomMargin);
        
        anchPos.Set(posX, posY);
        uiRectTransform.anchoredPosition = anchPos;
        //Debug.Log($"moving : {transform.position}");
    }

    public void moveToSide()
    {
        Vector3 nowPos = uiRectTransform.anchoredPosition;
        float nowPosX = (uiRectTransform.anchoredPosition.x > middleMargin) ? rightMargin : leftMargin;
        nowPos.Set(nowPosX, nowPos.y, nowPos.z);
        uiRectTransform.anchoredPosition = nowPos;
        //Debug.Log($"moveToSide {transform.position} , nowPosX : {nowPosX}");
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        moveToSide();
    }
}
