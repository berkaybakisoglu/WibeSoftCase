using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CropState
{
    Seed,
    Growing,
    Ready,
    Harvested
}

public abstract class Crop : MonoBehaviour
{
    #region Fields
    [Header("Crop Settings")]
    [SerializeField] protected int _growthStages = 3;
    [SerializeField] protected float _growthTimePerStage = 10f;
    [SerializeField] protected int _harvestYield = 4;
    [SerializeField] protected float _growthSpeedMultiplier = 1.0f;
    
    [Header("Visuals")]
    [SerializeField] protected GameObject[] _growthStageModels;
    [SerializeField] private GameObject _readyToHarvestEffect;
    
    [Header("Tool Animations")]
    [SerializeField] protected GameObject _harvestToolPrefab;
    [SerializeField] protected float _toolAnimationDuration = 1.5f;
    
    protected GameObject _harvestToolInstance;
    
    protected CropState _currentState = CropState.Seed;
    protected int _currentGrowthStage = 0;
    protected float _growthTimer = 0f;
    protected GridCell _occupiedCell;
    protected bool _isAnimatingTool = false;
    
    public System.Action<Crop> OnCropHarvested;
    #endregion
    
    #region Properties
    public CropState CurrentState => _currentState;
    public int CurrentGrowthStage => _currentGrowthStage;
    public GridCell OccupiedCell => _occupiedCell;
    public bool IsReadyToHarvest => _currentState == CropState.Ready;
    public bool IsAnimatingTool => _isAnimatingTool;
    #endregion
    
    #region Public Methods
    public virtual void Initialize(GridCell cell)
    {
        _occupiedCell = cell;
        _currentState = CropState.Seed;
        _currentGrowthStage = 0;
        _growthTimer = 0f;
        
        if (_harvestToolPrefab != null && _harvestToolInstance == null)
        {
            _harvestToolInstance = Instantiate(_harvestToolPrefab, transform);
            _harvestToolInstance.SetActive(false);
        }
        
        UpdateVisuals();
        
        _occupiedCell.SetOccupied(gameObject);
    }
    
    public virtual void Harvest()
    {
        if (_currentState != CropState.Ready || _isAnimatingTool)
            return;
        
        StartCoroutine(ShowHarvestAnimation(() => {
            SetState(CropState.Harvested);
            OnCropHarvested?.Invoke(this);
            
            if (_occupiedCell != null)
            {
                _occupiedCell.SetUnoccupied();
            }
            gameObject.SetActive(false);
            Destroy(gameObject,1f);
        }));
    }
    
    public virtual int GetHarvestYield()
    {
        return _harvestYield;
    }
    #endregion
    
    #region Unity Methods
    protected virtual void Update()
    {
        if (_currentState == CropState.Growing)
        {
            Grow();
        }
    }
    
    protected virtual void OnDestroy()
    {
        if (_harvestToolInstance != null)
        {
            Destroy(_harvestToolInstance);
        }
    }
    #endregion
    
    #region Protected Methods
    protected virtual void Grow()
    {
        _growthTimer += Time.deltaTime * _growthSpeedMultiplier;
        
        if (_growthTimer >= _growthTimePerStage)
        {
            _growthTimer = 0f;
            _currentGrowthStage++;
            
            UpdateVisuals();
            
            if (_currentGrowthStage >= _growthStages)
            {
                SetState(CropState.Ready);
            }
        }
    }
    
    protected virtual IEnumerator ShowHarvestAnimation(System.Action onComplete)
    {
        if (_harvestToolInstance == null)
        {
            onComplete?.Invoke();
            yield break;
        }
        
        _isAnimatingTool = true;
        
        try
        {
            UpdateToolPositions();
            _harvestToolInstance.SetActive(true);
            
            Quaternion startRotation = Quaternion.Euler(0, 0, 45);
            Quaternion swingRotation = Quaternion.Euler(0, 0, -45);
            Quaternion endRotation = Quaternion.Euler(0, 0, 45);
            
            _harvestToolInstance.transform.localRotation = startRotation;
            
            float elapsed = 0;
            float duration = _toolAnimationDuration * 0.5f;
            
            while (elapsed < duration)
            {
                _harvestToolInstance.transform.localRotation = Quaternion.Lerp(startRotation, swingRotation, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(_toolAnimationDuration * 0.2f);
            
            elapsed = 0;
            duration = _toolAnimationDuration * 0.3f;
            
            while (elapsed < duration)
            {
                _harvestToolInstance.transform.localRotation = Quaternion.Lerp(swingRotation, endRotation, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            _harvestToolInstance.SetActive(false);
            
            onComplete?.Invoke();
        }
        finally
        {
            if (_harvestToolInstance != null)
            {
                _harvestToolInstance.SetActive(false);
            }
            _isAnimatingTool = false;
        }
    }
    
    protected virtual void SetState(CropState newState)
    {
        _currentState = newState;
        
        switch (newState)
        {
            case CropState.Seed:
                _currentGrowthStage = 0;
                _growthTimer = 0f;
                if (_readyToHarvestEffect != null)
                {
                    _readyToHarvestEffect.SetActive(false);
                }
                break;
                
            case CropState.Growing:
                if (_currentGrowthStage == 0)
                {
                    _growthTimer = 0f;
                }
                break;
                
            case CropState.Ready:
                _currentGrowthStage = _growthStages - 1;
                if (_readyToHarvestEffect != null)
                {
                    _readyToHarvestEffect.SetActive(true);
                }
                break;
                
            case CropState.Harvested:
                if (_readyToHarvestEffect != null)
                {
                    _readyToHarvestEffect.SetActive(false);
                }
                break;
        }
        
        UpdateVisuals();
    }
    
    protected virtual void UpdateVisuals()
    {
        foreach (GameObject model in _growthStageModels)
        {
            if (model != null)
            {
                model.SetActive(false);
            }
        }
        
        if (_currentGrowthStage < _growthStageModels.Length && _growthStageModels[_currentGrowthStage] != null)
        {
            _growthStageModels[_currentGrowthStage].SetActive(true);
        }
        
        // Update tool positions when visuals change
        UpdateToolPositions();
    }
    
    protected virtual void UpdateToolPositions()
    {
        // Update harvest tool position if it exists
        if (_harvestToolInstance != null)
        {
            Vector3 topPosition = GetActiveModelTopPosition();
            topPosition.x += 0.3f; // Offset to the side
            _harvestToolInstance.transform.localPosition = topPosition;
        }
    }
    
    protected virtual Vector3 GetActiveModelTopPosition()
    {
        if (_currentGrowthStage < _growthStageModels.Length && _growthStageModels[_currentGrowthStage] != null)
        {
            GameObject activeModel = _growthStageModels[_currentGrowthStage];
            Renderer renderer = activeModel.GetComponent<Renderer>();
            
            if (renderer != null)
            {
                // Get the top of the model based on its bounds in world space
                // and convert to local space for proper positioning
                Vector3 worldTop = renderer.bounds.center + new Vector3(0, renderer.bounds.extents.y, 0);
                return transform.InverseTransformPoint(worldTop);
            }
            
            // If no renderer, try to get the top based on the model's transform
            return activeModel.transform.localPosition + new Vector3(0, activeModel.transform.localScale.y * 0.5f, 0);
        }
        
        // Default fallback position if no active model
        return new Vector3(0, 0.5f, 0);
    }
    #endregion
} 