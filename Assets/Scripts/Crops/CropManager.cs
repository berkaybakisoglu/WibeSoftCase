using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CropType
{
    Broccoli,
    Corn
}

public class CropManager : BaseManager<CropManager>
{
    #region Fields
    [Header("Crop Prefabs")]
    [SerializeField] private GameObject _broccoliPrefab;
    [SerializeField] private GameObject _cornPrefab;
    
    [Header("Inventory")]
    [SerializeField] private int _broccoliHarvested = 0;
    [SerializeField] private int _cornHarvested = 0;
    
    private CropType _selectedCropType = CropType.Corn;
    
    private Dictionary<Vector2Int, Crop> _plantedCrops = new Dictionary<Vector2Int, Crop>();
    
    [SerializeField] private bool _allowDragInteractionDuringAnimation = true;
    private float _lastBusyMessageTime = 0f;
    [SerializeField] private float _busyMessageDuration = 1.5f;
    
    [Header("Harvested Item Visuals")]
    [SerializeField] private bool _enableHarvestVisuals = true;
    [SerializeField] private HarvestedItemPool _harvestedItemPool;
    #endregion
    
    #region Unity Methods
    protected override void OnAwake()
    {
     
        if (_harvestedItemPool == null)
        {
            _harvestedItemPool = FindObjectOfType<HarvestedItemPool>();
        }
    }
    
    private void Start()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.OnCellSelected += HandleCellSelected;
        }
    }
    
    private void OnDestroy()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.OnCellSelected -= HandleCellSelected;
        }
    }
    #endregion
    
    #region Private Methods
    private void HandleCellSelected(GridCell cell)
    {
        bool anyAnimating = false;
        foreach (var crop in _plantedCrops.Values)
        {
            if (crop.IsAnimatingTool)
            {
                anyAnimating = true;
                break;
            }
        }
        
        if (anyAnimating && !_allowDragInteractionDuringAnimation)
        {
            if (Time.time - _lastBusyMessageTime > _busyMessageDuration)
            {
                Debug.Log("Please wait for the current action to complete!");
                _lastBusyMessageTime = Time.time;
            }
            return;
        }
        
        if (cell.IsOccupied && _plantedCrops.TryGetValue(cell.GridPosition, out Crop cellCrop) && cellCrop.IsAnimatingTool)
        {
            return;
        }
        
        SmartInteract(cell);
    }
    
    private void PlantCrop(GridCell cell)
    {
        if (cell.IsOccupied)
        {
            Debug.Log("Cell is already occupied!");
            return;
        }
        
        GameObject cropPrefab = GetCropPrefab(_selectedCropType);
        
        if (cropPrefab != null)
        {
            Vector3 position = cell.GetTopPosition();
            GameObject cropObject = Instantiate(cropPrefab, position, Quaternion.identity);
            
            Crop crop = cropObject.GetComponent<Crop>();
            if (crop != null)
            {
                crop.Initialize(cell);
                crop.OnCropHarvested += OnCropHarvested;
                
                _plantedCrops[cell.GridPosition] = crop;
                
                Debug.Log($"Planted {_selectedCropType} at {cell.GridPosition}");
            }
            else
            {
                Debug.LogError("Crop prefab does not have a Crop component!");
                Destroy(cropObject);
            }
        }
    }
    
    private void HarvestCrop(GridCell cell)
    {
        if (!cell.IsOccupied || !_plantedCrops.TryGetValue(cell.GridPosition, out Crop crop))
        {
            Debug.Log("No crop to harvest!");
            return;
        }
        
        if (!crop.IsReadyToHarvest)
        {
            Debug.Log("Crop is not ready to harvest yet!");
            return;
        }
        
        crop.Harvest();
    }
    
    private void WaterCrop(GridCell cell)
    {
        if (!cell.IsOccupied || !_plantedCrops.TryGetValue(cell.GridPosition, out Crop crop))
        {
            Debug.Log("No crop to water!");
            return;
        }
        
        if (crop.CurrentState == CropState.Ready)
        {
            Debug.Log("Crop is already ready to harvest!");
            return;
        }
        
        if (crop.CurrentState == CropState.Harvested)
        {
            Debug.Log("Cannot water a harvested crop!");
            return;
        }
        
        Debug.Log($"Watered crop at {cell.GridPosition}");
    }
    
    private void SmartInteract(GridCell cell)
    {
        if (!cell.IsOccupied)
        {
            PlantCrop(cell);
            return;
        }
        
        if (!_plantedCrops.TryGetValue(cell.GridPosition, out Crop crop))
        {
            Debug.Log("Cell is occupied but not by a crop!");
            return;
        }
        
        switch (crop.CurrentState)
        {
            case CropState.Ready:
                HarvestCrop(cell);
                break;
                
            case CropState.Seed:
            case CropState.Growing:
                WaterCrop(cell);
                break;
                
            case CropState.Harvested:
                Debug.Log("This crop has already been harvested!");
                break;
        }
    }
    
    private void OnCropHarvested(Crop crop)
    {
        if (crop is BroccoliCrop)
        {
            _broccoliHarvested += crop.GetHarvestYield();
            Debug.Log($"Harvested Broccoli! Total: {_broccoliHarvested}");
            
            if (_enableHarvestVisuals && _harvestedItemPool != null)
            {
                _harvestedItemPool.SpawnHarvestedItems(CropType.Broccoli, crop.transform.position, crop.GetHarvestYield());
            }
        }
        else if (crop is CornCrop)
        {
            _cornHarvested += crop.GetHarvestYield();
            Debug.Log($"Harvested Corn! Total: {_cornHarvested}");
            
            if (_enableHarvestVisuals && _harvestedItemPool != null)
            {
                _harvestedItemPool.SpawnHarvestedItems(CropType.Corn, crop.transform.position, crop.GetHarvestYield());
            }
        }
        if (_plantedCrops.ContainsKey(crop.GetComponent<Crop>().OccupiedCell.GridPosition))
        {
            _plantedCrops.Remove(crop.GetComponent<Crop>().OccupiedCell.GridPosition);
        }
    }
    
    private GameObject GetCropPrefab(CropType cropType)
    {
        switch (cropType)
        {
            case CropType.Broccoli:
                return _broccoliPrefab;
                
            case CropType.Corn:
                return _cornPrefab;
                
            default:
                Debug.LogError($"Unknown crop type: {cropType}");
                return null;
        }
    }
    #endregion
    
    #region Public Methods
    public Crop GetCropAtPosition(Vector2Int gridPosition)
    {
        if (_plantedCrops.TryGetValue(gridPosition, out Crop crop))
        {
            return crop;
        }
        
        return null;
    }
    
    public bool HasCropOfType<T>(Vector2Int gridPosition) where T : Crop
    {
        if (_plantedCrops.TryGetValue(gridPosition, out Crop crop))
        {
            return crop is T;
        }
        
        return false;
    }
    
    public T GetCropAs<T>(Vector2Int gridPosition) where T : Crop
    {
        if (_plantedCrops.TryGetValue(gridPosition, out Crop crop) && crop is T typedCrop)
        {
            return typedCrop;
        }
        
        return null;
    }
    
    public string GetSelectedCropType() => _selectedCropType.ToString();
    
    public void SelectCropType(int cropTypeIndex)
    {
        if (System.Enum.IsDefined(typeof(CropType), cropTypeIndex))
        {
            _selectedCropType = (CropType)cropTypeIndex;
            Debug.Log($"Selected crop type: {_selectedCropType}");
        }
    }
    
    public int GetBroccoliHarvested() => _broccoliHarvested;
    
    public int GetCornHarvested() => _cornHarvested;
    
    public bool IsAnyInteractionInProgress()
    {
        foreach (var crop in _plantedCrops.Values)
        {
            if (crop.IsAnimatingTool)
            {
                return true;
            }
        }
        
        return false;
    }
    #endregion
}