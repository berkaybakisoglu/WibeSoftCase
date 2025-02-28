using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "Buildings/Building Data")]
public class BuildingData : ScriptableObject
{
    #region Fields
    [Header("Basic Info")]
    public string buildingId;
    public string displayName;
    [Tooltip("Optional icon for UI representation")]
    public Sprite icon;
    public GameObject prefab;
    
    [Header("Placement Settings")]
    public Vector2Int size = new Vector2Int(1, 1);
    #endregion
} 