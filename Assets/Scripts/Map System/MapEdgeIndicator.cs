using UnityEngine;

public class MapEdgeIndicator : MonoBehaviour
{
    public RectTransform rect;

    void Awake()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();
    }

    public void SetPosition(Vector2 pos)
    {
        rect.anchoredPosition = pos;
    }

    public void SetRotation(float angle)
    {
        rect.rotation = Quaternion.Euler(0, 0, angle);
    }
}