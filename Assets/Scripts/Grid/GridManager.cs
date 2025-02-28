using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GridManager : BaseManager<GridManager>
{
    #region Fields
    [Header("Grid Settings")]
    [SerializeField] private int _gridWidth = 10;
    [SerializeField] private int _gridHeight = 10;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private GameObject _gridCellPrefab;
    [SerializeField] private Transform _gridParent;
    
    private GridCell[,] _grid;
    #endregion
    
    #region Properties
    public event Action<GridCell> OnCellSelected;
    public float CellSize => _cellSize;
    #endregion
    
    #region Unity Methods
    protected override void OnAwake()
    {
        InitializeGrid();
    }
    #endregion
    
    #region Private Methods
    private void InitializeGrid()
    {
        _grid = new GridCell[_gridWidth, _gridHeight];
        
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int z = 0; z < _gridHeight; z++)
            {
                // Position at the center of the cell
                Vector3 worldPosition = new Vector3(
                    x * _cellSize + _cellSize/2, 
                    0, 
                    z * _cellSize + _cellSize/2
                );
                
                GameObject cellObject = Instantiate(_gridCellPrefab, worldPosition, Quaternion.identity, _gridParent);
                cellObject.name = $"GridCell_{x}_{z}";
                
                GridCell cell = cellObject.GetComponent<GridCell>();
                cell.Initialize(new Vector2Int(x, z));
                
                _grid[x, z] = cell;
            }
        }
    }
    #endregion
    
    #region Public Methods
    public GridCell GetCellAtPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / _cellSize);
        int z = Mathf.FloorToInt(worldPosition.z / _cellSize);
        
        if (IsValidGridPosition(x, z))
        {
            return _grid[x, z];
        }
        
        return null;
    }
    
    public GridCell GetCellAtGridPosition(Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition.x, gridPosition.y))
        {
            return _grid[gridPosition.x, gridPosition.y];
        }
        
        return null;
    }
    
    public List<GridCell> GetCellsInArea(Vector2Int startPosition, Vector2Int size)
    {
        List<GridCell> cells = new List<GridCell>();
        
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int position = new Vector2Int(startPosition.x + x, startPosition.y + z);
                
                if (IsValidGridPosition(position.x, position.y))
                {
                    cells.Add(_grid[position.x, position.y]);
                }
            }
        }
        
        return cells;
    }
    
    public bool AreCellsFree(Vector2Int startPosition, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int position = new Vector2Int(startPosition.x + x, startPosition.y + z);
                
                if (!IsValidGridPosition(position.x, position.y) || _grid[position.x, position.y].IsOccupied)
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    public void SelectCell(GridCell cell)
    {
        OnCellSelected?.Invoke(cell);
    }
    
    public bool IsValidGridPosition(int x, int z)
    {
        return x >= 0 && x < _gridWidth && z >= 0 && z < _gridHeight;
    }
    
    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        // Convert grid position to world position (center of the cell)
        return new Vector3(
            gridPosition.x * _cellSize + _cellSize/2, 
            0, 
            gridPosition.y * _cellSize + _cellSize/2
        );
    }
    
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / _cellSize);
        int z = Mathf.FloorToInt(worldPosition.z / _cellSize);
        
        return new Vector2Int(x, z);
    }
    #endregion
} 