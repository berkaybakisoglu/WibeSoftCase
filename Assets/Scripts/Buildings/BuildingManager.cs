using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;


public class BuildingManager : BaseManager<BuildingManager>
{
    #region Fields
    [Header("Building Configuration")]
    [SerializeField] private BuildingData[] _availableBuildings;
    [SerializeField] private LayerMask _gridLayerMask;
    
    // Building state tracking
    private BuildingData _selectedBuildingData;
    private Building _placementBuilding;
    private Building _selectedBuilding;
    private bool _isInPlacementMode = false;
    private bool _isInRepositionMode = false;
    

    private List<Building> _placedBuildings = new List<Building>();

    public event Action<Building> OnBuildingSelected;
    public event Action OnBuildingDeselected;
    public event Action<Building> OnBuildingPlaced;
    public event Action<Building> OnBuildingRemoved;
    #endregion
    
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
        
        GameObject buildingObj = Instantiate(buildingData.prefab);
        _placementBuilding = buildingObj.GetComponent<Building>();
        
        if (_placementBuilding != null)
        {
            _placementBuilding.Initialize(buildingData, Vector2Int.zero);
            UpdatePlacementPosition();
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
        
        FreeCells(building);
        _placedBuildings.Remove(building);
        _placementBuilding = building;
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
            
        FreeCells(building);
        _placedBuildings.Remove(building);
        OnBuildingRemoved?.Invoke(building);
        
        if (_selectedBuilding == building)
        {
            _selectedBuilding = null;
            OnBuildingDeselected?.Invoke();
        }
        
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
            FreeCells(_selectedBuilding);
            _selectedBuilding.Rotate();
            
            bool isValid = IsValidPlacement(_selectedBuilding.GridPosition, _selectedBuilding.Size);
            
            if (isValid)
            {
                OccupyCells(_selectedBuilding);
            }
            else
            {
                // Rotate back if invalid (add 3 mod 4 = subtract 1 mod 4)
                _selectedBuilding.SetRotation((_selectedBuilding.RotationIndex + 3) % 4);
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
        
        OccupyCells(_placementBuilding);
        _placementBuilding.ConfirmPlacement();
        _placedBuildings.Add(_placementBuilding);
        OnBuildingPlaced?.Invoke(_placementBuilding);
        
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
                _placedBuildings.Add(_placementBuilding);
                OccupyCells(_placementBuilding);
                _placementBuilding.ConfirmPlacement();
            }
            else
            {
                Destroy(_placementBuilding.gameObject);
            }
            
            _placementBuilding = null;
        }
        
        _isInPlacementMode = false;
        _isInRepositionMode = false;
    }
    
    public void HandleMouseClick(Vector3 worldPosition)
    {
        if (_isInPlacementMode || _isInRepositionMode)
        {
            ConfirmPlacement();
            return;
        }
        
        // Try to select a building via raycast
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            Building building = hit.collider.GetComponentInParent<Building>();
            
            if (building != null)
            {
                SelectBuilding(building);
                return;
            }
        }
        
     
        GridCell cell = GridManager.Instance.GetCellAtPosition(worldPosition);
        
        if (cell != null)
        {
            Building building = GetBuildingAtCell(cell);
            
            if (building != null)
            {
                SelectBuilding(building);
                return;
            }
        }
        
        DeselectCurrentBuilding();
    }
    
    public void HandleMouseRightClick()
    {
        DeselectCurrentBuilding();
    }

    public void HandleMousePosition(Vector3 worldPosition)
    {
        if (_isInPlacementMode || _isInRepositionMode)
        {
            if (_placementBuilding != null)
            {
                GridCell cell = GridManager.Instance.GetCellAtPosition(worldPosition);
                
                if (cell != null)
                {
                    _placementBuilding.SetPosition(cell.GridPosition);
                    UpdatePlacementValidation();
                }
            }
        }
    }
    #endregion
    
    #region Private Methods
    private void HandleCellSelected(GridCell cell)
    {
        if (_isInPlacementMode || _isInRepositionMode)
        {
            if (_placementBuilding != null)
            {
                _placementBuilding.SetPosition(cell.GridPosition);
                UpdatePlacementValidation();
            }
        }
        else
        {
            Building building = GetBuildingAtCell(cell);
            
            if (building != null)
            {
                SelectBuilding(building);
            }
            else
            {
                DeselectCurrentBuilding();
            }
        }
    }
    
    private Building GetBuildingAtCell(GridCell cell)
    {
        if (cell == null)
            return null;
            
        foreach (Building building in _placedBuildings)
        {
            Vector2Int buildingPos = building.GridPosition;
            Vector2Int buildingSize = building.Size;
            
            bool isWithinX = cell.GridPosition.x >= buildingPos.x && cell.GridPosition.x < buildingPos.x + buildingSize.x;
            bool isWithinY = cell.GridPosition.y >= buildingPos.y && cell.GridPosition.y < buildingPos.y + buildingSize.y;
            
            if (isWithinX && isWithinY)
            {
                return building;
            }
        }
        
        return null;
    }
    
    private void UpdatePlacementPosition()
    {
        if (_placementBuilding == null)
            return;
            
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _gridLayerMask))
        {
            HandleMousePosition(hit.point);
        }
    }
    
    private bool IsValidPlacement(Vector2Int position, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int cellPos = position + new Vector2Int(x, z);
                
             
                GridCell cell = GridManager.Instance.GetCellAtGridPosition(cellPos);
                
                if (cell == null)
                {
                    return false; 
                }
                
                if (cell.IsOccupied && !(_isInRepositionMode && _placementBuilding != null && 
                    cellPos.x >= _placementBuilding.GridPosition.x && 
                    cellPos.x < _placementBuilding.GridPosition.x + _placementBuilding.Size.x &&
                    cellPos.y >= _placementBuilding.GridPosition.y && 
                    cellPos.y < _placementBuilding.GridPosition.y + _placementBuilding.Size.y))
                {
                    return false; 
                }
            }
        }
        
        return true;
    }

    private void OccupyCells(Building building)
    {
        if (building == null)
            return;
            
        Vector2Int position = building.GridPosition;
        Vector2Int size = building.Size;
        
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
        if (building == null)
            return;
            
        Vector2Int position = building.GridPosition;
        Vector2Int size = building.Size;
        
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int cellPos = position + new Vector2Int(x, z);
                GridCell cell = GridManager.Instance.GetCellAtGridPosition(cellPos);
                
                if (cell != null)
                {
                    cell.SetUnoccupied();
                }
            }
        }
    }

    private void SelectBuilding(Building building)
    {
        if (building == _selectedBuilding)
            return;
            
        DeselectCurrentBuilding();
        
        _selectedBuilding = building;
        _selectedBuilding.Select();
        
        OnBuildingSelected?.Invoke(_selectedBuilding);
    }
    
    private void DeselectCurrentBuilding()
    {
        if (_selectedBuilding != null)
        {
            _selectedBuilding.Deselect();
            OnBuildingDeselected?.Invoke();
            _selectedBuilding = null;
        }
    }
    
    private Building GetBuildingAtGridPosition(Vector2Int gridPosition)
    {
        foreach (Building building in _placedBuildings)
        {
            Vector2Int buildingPos = building.GridPosition;
            Vector2Int buildingSize = building.Size;
            
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