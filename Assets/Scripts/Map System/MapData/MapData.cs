using UnityEngine;

[CreateAssetMenu(menuName = "Map/MapData")]
public class MapData : ScriptableObject
{
    public Vector2 worldMin;
    public Vector2 worldMax;
}