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
                _selectedBuilding.SetRotation((_selectedBuilding.RotationIndex + 3) % 4); 
                OccupyCells(_selectedBuilding);
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
        }

        _placementBuilding = null;
        _isInPlacementMode = false;
        _isInRepositionMode = false;
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

    public void SelectBuildingDirectly(Building building)
    {
        if (building != null && !_isInPlacementMode && !_isInRepositionMode)
        {
            // Check if the building is in our placed buildings list
            if (_placedBuildings.Contains(building))
            {
                SelectBuilding(building);
            }
        }
    }

    #endregion

    #region Private Methods

    private void HandleCellSelected(GridCell cell)
    {
        if (_isInPlacementMode || _isInRepositionMode)
            return;

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
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int cellPos = position + new Vector2Int(x, z);

                if (!GridManager.Instance.IsValidGridPosition(cellPos.x, cellPos.y))
                    return false;

                GridCell cell = GridManager.Instance.GetCellAtGridPosition(cellPos);
                if (cell == null)
                    return false;

                if (cell.IsOccupied)
                {
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
            _selectedBuilding = null;

            OnBuildingDeselected?.Invoke();
        }
    }
    

    #endregion
}