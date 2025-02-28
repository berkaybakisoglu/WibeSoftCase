using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BuildingState
{
    Normal,
    Selected,
    ValidPlacement,
    InvalidPlacement
}

public abstract class BaseBuilding : MonoBehaviour
{
    #region Fields
    [SerializeField] protected BuildingData _data;
    
    // Runtime state
    protected Vector2Int _gridPosition;
    protected List<GridCell> _occupiedCells = new List<GridCell>();
    protected BuildingState _state = BuildingState.Normal;
    protected int _rotationIndex = 0; // 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°
    #endregion
    
    #region Properties
    public BuildingData Data => _data;
    public Vector2Int GridPosition => _gridPosition;
    public Vector2Int Size => (_rotationIndex % 2 == 1) ? 
        new Vector2Int(_data.size.y, _data.size.x) : _data.size;
    public BuildingState State => _state;
    public int RotationIndex => _rotationIndex;
    public float RotationDegrees => _rotationIndex * 90f;
    public bool IsRotated => _rotationIndex % 2 == 1; // True if rotated 90° or 270°
    #endregion
    
    #region Public Methods
    public virtual void Initialize(BuildingData data, Vector2Int position)
    {
        _data = data;
        SetPosition(position);
    }
    
    public virtual void SetPosition(Vector2Int position)
    {
        _gridPosition = position;
        
        // Calculate the world position based on the grid position
        Vector3 worldPos = GridManager.Instance.GridToWorldPosition(position);
        
        // Position the building at the corner of the grid cell, not the center
        transform.position = worldPos;
        
        UpdateOccupiedCells();
    }
    
    public virtual void Rotate()
    {
        _rotationIndex = (_rotationIndex + 1) % 4; // Cycle through 4 possible rotations
        transform.rotation = Quaternion.Euler(0, _rotationIndex * 90, 0);
        
        // Update the occupied cells after rotation
        UpdateOccupiedCells();
    }
    
    public virtual void SetRotation(int rotationIndex)
    {
        _rotationIndex = Mathf.Clamp(rotationIndex, 0, 3);
        transform.rotation = Quaternion.Euler(0, _rotationIndex * 90, 0);
        UpdateOccupiedCells();
    }
    
    public virtual void Select()
    {
        _state = BuildingState.Selected;
        // Visual feedback for selection - implemented by derived classes
    }
    
    public virtual void Deselect()
    {
        _state = BuildingState.Normal;
        // Remove visual feedback - implemented by derived classes
    }
    
    public virtual void SetPlacementState(bool isValid)
    {
        _state = isValid ? BuildingState.ValidPlacement : BuildingState.InvalidPlacement;
        // Update visual feedback - implemented by derived classes
    }
    
    public virtual void ConfirmPlacement()
    {
        _state = BuildingState.Normal;
        // Final placement setup - implemented by derived classes
    }
    #endregion
    
    #region Protected Methods
    protected virtual void UpdateOccupiedCells()
    {
        _occupiedCells.Clear();
        
        Vector2Int actualSize = Size; // Accounts for rotation
        
        for (int x = 0; x < actualSize.x; x++)
        {
            for (int z = 0; z < actualSize.y; z++)
            {
                Vector2Int cellPos = _gridPosition + new Vector2Int(x, z);
                GridCell cell = GridManager.Instance.GetCellAtGridPosition(cellPos);
                
                if (cell != null)
                {
                    _occupiedCells.Add(cell);
                }
            }
        }
    }
    #endregion
} 