using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class BuildingManager : BaseManager<BuildingManager>
{
    [Header("Building Configuration")]
    [SerializeField] private BuildingData[] _availableBuildings;
    [SerializeField] private LayerMask _gridLayerMask;
    public BuildingData[] AvailableBuildings => _availableBuildings;
    // State tracking
    private BuildingData _selectedBuildingData;
    private Building _placementBuilding;
    private Building _selectedBuilding;
    private bool _isInPlacementMode = false;
    private bool _isInRepositionMode = false;
    
    // Building tracking
    private List<Building> _placedBuildings = new List<Building>();
    
    // Events
    public event Action<Building> OnBuildingSelected;
    public event Action OnBuildingDeselected;
    public event Action<Building> OnBuildingPlaced;
    public event Action<Building> OnBuildingRemoved;
    
    #region Unity Methods
    protected override void OnAwake()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.OnCellSelected += HandleCellSelected;
        }
    }
    
    private void Update()
    {
        if (_isInPlacementMode || _isInRepositionMode)
        {
            UpdatePlacementPosition();
        }
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        if (GridManager.Instance != null)
        {
            GridManager.Instance.OnCellSelected -= HandleCellSelected;
        }
    }
    #endregion
    
    #region Public Methods
    public void StartPlacement(string buildingId)
    {
        // Find the building data
        BuildingData data = System.Array.Find(_availableBuildings, b => b.buildingId == buildingId);
        
        if (data == null)
        {
            Debug.LogError($"Building data not found for ID: {buildingId}");
            return;
        }
        
        StartPlacement(data);
    }
    
    public void StartPlacement(BuildingData buildingData)
    {
        DeselectCurrentBuilding();
        CancelPlacement();
        
        _selectedBuildingData = buildingData;
        _isInPlacementMode = true;
        
        // Create the placement preview
        GameObject buildingObj = Instantiate(buildingData.prefab);
        _placementBuilding = buildingObj.GetComponent<Building>();
        
        if (_placementBuilding != null)
        {
            _placementBuilding.Initialize(buildingData, Vector2Int.zero);
            UpdatePlacementPosition(); // Position at mouse immediately
            UpdatePlacementValidation();
        }
        else
        {
            Debug.LogError("Building prefab does not have a Building component!");
            Destroy(buildingObj);
            _isInPlacementMode = false;
        }
    }
    
    public void StartRepositioning(Building building)
    {
        if (building == null)
            return;
            
        DeselectCurrentBuilding();
        CancelPlacement();
        
        _isInRepositionMode = true;
        
        // Free up the cells occupied by this building
        FreeCells(building);
        
        // Remove from placed buildings list temporarily
        _placedBuildings.Remove(building);
        
        // Set as the current placement building
        _placementBuilding = building;
        
        // Update validation
        UpdatePlacementValidation();
    }
    
    public void DeleteSelectedBuilding()
    {
        if (_selectedBuilding != null)
        {
            DeleteBuilding(_selectedBuilding);
        }
    }
    
    public void DeleteBuilding(Building building)
    {
        if (building == null)
            return;
            
        // Free up the cells
        FreeCells(building);
        
        // Remove from list
        _placedBuildings.Remove(building);
        
        // Notify listeners
        OnBuildingRemoved?.Invoke(building);
        
        // If this was the selected building, clear selection
        if (_selectedBuilding == building)
        {
            _selectedBuilding = null;
            OnBuildingDeselected?.Invoke();
        }
        
        // Destroy the game object
        Destroy(building.gameObject);
    }
    
    public void RotateBuilding()
    {
        if (_placementBuilding != null)
        {
            _placementBuilding.Rotate();
            UpdatePlacementValidation();
        }
        else if (_selectedBuilding != null)
        {
            // First, free up the occupied cells
            FreeCells(_selectedBuilding);
            
            // Rotate the building
            _selectedBuilding.Rotate();
            
            // Check if the new position is valid
            bool isValid = IsValidPlacement(_selectedBuilding.GridPosition, _selectedBuilding.Size);
            
            if (isValid)
            {
                // Re-occupy cells with the new rotation
                OccupyCells(_selectedBuilding);
            }
            else
            {
                // Rotate back if invalid
                _selectedBuilding.SetRotation((_selectedBuilding.RotationIndex + 3) % 4); // Rotate back (add 3 mod 4 = subtract 1 mod 4)
                OccupyCells(_selectedBuilding);
                Debug.Log("Cannot rotate building here!");
            }
        }
    }
    
    public Building GetPlacementBuilding()
    {
        return _placementBuilding;
    }
    
    public Building GetSelectedBuilding()
    {
        return _selectedBuilding;
    }
    
    public bool IsInPlacementMode()
    {
        return _isInPlacementMode || _isInRepositionMode;
    }
    
    public void UpdatePlacementValidation()
    {
        if (_placementBuilding == null)
            return;
            
        bool isValid = IsValidPlacement(_placementBuilding.GridPosition, _placementBuilding.Size);
        _placementBuilding.SetPlacementState(isValid);
    }
    
    public void ConfirmPlacement()
    {
        if (_placementBuilding == null)
            return;
            
        bool isValid = IsValidPlacement(_placementBuilding.GridPosition, _placementBuilding.Size);
        
        if (!isValid)
        {
            Debug.Log("Cannot place building here!");
            return;
        }
        
        // Mark cells as occupied
        OccupyCells(_placementBuilding);
        
        // Confirm placement
        _placementBuilding.ConfirmPlacement();
        _placedBuildings.Add(_placementBuilding);
        
        // Notify listeners
        OnBuildingPlaced?.Invoke(_placementBuilding);
        
        // Reset state
        Building placedBuilding = _placementBuilding;
        _placementBuilding = null;
        _isInPlacementMode = false;
        _isInRepositionMode = false;
    }
    
    public void CancelPlacement()
    {
        if (_placementBuilding != null)
        {
            if (_isInRepositionMode)
            {
                // Put the building back in the list
                _placedBuildings.Add(_placementBuilding);
                OccupyCells(_placementBuilding);
                _placementBuilding.ConfirmPlacement();
            }
            else
            {
                // Destroy the preview
                Destroy(_placementBuilding.gameObject);
            }
        }
        
        _placementBuilding = null;
        _isInPlacementMode = false;
        _isInRepositionMode = false;
    }
    
    public void HandleMouseClick(Vector3 worldPosition)
    {
        // Don't handle clicks if we're in placement mode
        if (_isInPlacementMode || _isInRepositionMode)
            return;
            
        // Cast a ray to find buildings
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        
        Building clickedBuilding = null;
        
        // Find the first hit that has a Building component
        foreach (RaycastHit hit in hits)
        {
            Building building = hit.collider.GetComponentInParent<Building>();
            if (building != null)
            {
                clickedBuilding = building;
                break;
            }
        }
        
        // If we found a building, select it
        if (clickedBuilding != null)
        {
            SelectBuilding(clickedBuilding);
        }
        else
        {
            // If we didn't hit a building, check if we clicked on the grid
            if (GridManager.Instance != null)
            {
                // Try to get the grid position from the world position
                Vector2Int gridPosition = GridManager.Instance.WorldToGridPosition(worldPosition);
                
                // Check if there's a building at this position
                Building buildingAtPosition = GetBuildingAtGridPosition(gridPosition);
                
                if (buildingAtPosition != null)
                {
                    SelectBuilding(buildingAtPosition);
                }
                else
                {
                    // If we didn't hit a building, deselect the current one
                    DeselectCurrentBuilding();
                }
            }
        }
    }
    
    public void HandleMouseRightClick()
    {
        // Right-click always deselects the current building
        DeselectCurrentBuilding();
    }
    
    public void HandleMousePosition(Vector3 worldPosition)
    {
        if (_placementBuilding != null && (_isInPlacementMode || _isInRepositionMode))
        {
            Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(worldPosition);
            
            if (_placementBuilding.GridPosition != gridPos)
            {
                _placementBuilding.SetPosition(gridPos);
                UpdatePlacementValidation();
            }
        }
    }
    #endregion
    
    #region Private Methods
    private void HandleCellSelected(GridCell cell)
    {
        if (_isInPlacementMode || _isInRepositionMode)
            return;
            
        // Check if there's a building at this cell
        Building buildingAtCell = GetBuildingAtCell(cell);
        
        if (buildingAtCell != null)
        {
            SelectBuilding(buildingAtCell);
        }
        else if (_selectedBuilding != null)
        {
            DeselectCurrentBuilding();
        }
    }
    
    private Building GetBuildingAtCell(GridCell cell)
    {
        foreach (var building in _placedBuildings)
        {
            Vector2Int buildingSize = building.Size;
            Vector2Int buildingPos = building.GridPosition;
            
            for (int x = 0; x < buildingSize.x; x++)
            {
                for (int z = 0; z < buildingSize.y; z++)
                {
                    if (buildingPos.x + x == cell.GridPosition.x && 
                        buildingPos.y + z == cell.GridPosition.y)
                    {
                        return building;
                    }
                }
            }
        }
        
        return null;
    }
    
    private void UpdatePlacementPosition()
    {
        if (_placementBuilding == null)
            return;
            
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _gridLayerMask))
        {
            Vector3 worldPos = hit.point;
            HandleMousePosition(worldPos);
        }
    }
    
    private bool IsValidPlacement(Vector2Int position, Vector2Int size)
    {
        // Check if all cells are valid and unoccupied
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int cellPos = position + new Vector2Int(x, z);
                
                // Check if cell is within grid bounds
                if (!GridManager.Instance.IsValidGridPosition(cellPos.x, cellPos.y))
                    return false;
                    
                // Check if cell is unoccupied
                GridCell cell = GridManager.Instance.GetCellAtGridPosition(cellPos);
                if (cell == null)
                    return false;
                    
                if (cell.IsOccupied)
                {
                    // If the cell is occupied by another object, it's invalid
                    // During repositioning, the building's own cells are temporarily freed
                    if (cell.OccupyingObject != _placementBuilding?.gameObject)
                        return false;
                }
            }
        }
        
        return true;
    }
    
    private void OccupyCells(Building building)
    {
        Vector2Int size = building.Size;
        Vector2Int position = building.GridPosition;
        
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int cellPos = position + new Vector2Int(x, z);
                GridCell cell = GridManager.Instance.GetCellAtGridPosition(cellPos);
                
                if (cell != null)
                {
                    cell.SetOccupied(building.gameObject);
                }
            }
        }
    }
    
    private void FreeCells(Building building)
    {
        Vector2Int size = building.Size;
        Vector2Int position = building.GridPosition;
        
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int cellPos = position + new Vector2Int(x, z);
                GridCell cell = GridManager.Instance.GetCellAtGridPosition(cellPos);
                
                if (cell != null && cell.OccupyingObject == building.gameObject)
                {
                    cell.SetUnoccupied();
                }
            }
        }
    }
    
    private void SelectBuilding(Building building)
    {
        // Deselect the current building first
        DeselectCurrentBuilding();
        
        // Select the new building
        _selectedBuilding = building;
        _selectedBuilding.Select();
        
        // Notify listeners
        OnBuildingSelected?.Invoke(_selectedBuilding);
    }
    
    private void DeselectCurrentBuilding()
    {
        if (_selectedBuilding != null)
        {
            _selectedBuilding.Deselect();
            _selectedBuilding = null;
            
            OnBuildingDeselected?.Invoke();
        }
    }
    
    private Building GetBuildingAtGridPosition(Vector2Int gridPosition)
    {
        // Check if any of the placed buildings occupy this grid position
        foreach (Building building in _placedBuildings)
        {
            Vector2Int buildingPos = building.GridPosition;
            Vector2Int buildingSize = building.Size;
            
            // Check if the grid position is within the building's bounds
            bool isWithinX = gridPosition.x >= buildingPos.x && gridPosition.x < buildingPos.x + buildingSize.x;
            bool isWithinY = gridPosition.y >= buildingPos.y && gridPosition.y < buildingPos.y + buildingSize.y;
            
            if (isWithinX && isWithinY)
            {
                return building;
            }
        }
        
        return null;
    }
    #endregion
} 