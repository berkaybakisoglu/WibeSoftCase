using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CornCrop : Crop
{
    #region Fields
    [Header("Corn Specific")]
    [SerializeField] private float _waterBonus = 0.2f;
    [SerializeField] private float _waterBonusDuration = 30f;
    
    [Header("Watering Tool")]
    [SerializeField] private GameObject _wateringPotPrefab;
    
    private GameObject _wateringPotInstance;
    private bool _isWatered = false;
    private float _waterTimer = 0f;
    #endregion
    
    #region Public Methods
    public override void Initialize(GridCell cell)
    {
        base.Initialize(cell);
        
        if (_wateringPotPrefab != null && _wateringPotInstance == null)
        {
            _wateringPotInstance = Instantiate(_wateringPotPrefab, transform);
            _wateringPotInstance.SetActive(false);
        }
        
        SetState(CropState.Growing);
    }
    
    public void Water()
    {
        _isWatered = true;
        _waterTimer = _waterBonusDuration;
        _growthSpeedMultiplier = 1f + _waterBonus;
        
        StartCoroutine(ShowWateringAnimation(() => {
            Debug.Log("Watering complete!");
        }));
    }
    
    public override int GetHarvestYield()
    {
        return _isWatered ? _harvestYield + 2 : _harvestYield;
    }
    #endregion
    
    #region Unity Methods
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        if (_wateringPotInstance != null)
        {
            Destroy(_wateringPotInstance);
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (_isWatered)
        {
            _waterTimer -= Time.deltaTime;
            
            if (_waterTimer <= 0f)
            {
                _isWatered = false;
                _growthSpeedMultiplier = 1f;
                Debug.Log("Water bonus expired");
            }
        }
    }
    #endregion
    
    #region Protected Methods
    protected override void UpdateToolPositions()
    {
        base.UpdateToolPositions();
        
        if (_wateringPotInstance != null)
        {
            Vector3 topPosition = GetActiveModelTopPosition();
            topPosition.x -= 0.3f; // Offset to the opposite side of harvest tool
            _wateringPotInstance.transform.localPosition = topPosition;
        }
    }
    
    protected override void Grow()
    {
        float growthMultiplier = _isWatered ? 1f + _waterBonus : 1f;
        
        _growthTimer += Time.deltaTime * growthMultiplier;
        
        if (_growthTimer >= _growthTimePerStage)
        {
            _growthTimer = 0f;
            _currentGrowthStage++;
            
            UpdateVisuals();
            
            if (_currentGrowthStage >= _growthStages - 1)
            {
                SetState(CropState.Ready);
            }
        }
    }
    
    protected virtual IEnumerator ShowWateringAnimation(System.Action onComplete)
    {
        if (_wateringPotInstance == null)
        {
            onComplete?.Invoke();
            yield break;
        }
        
        _isAnimatingTool = true;
        
        try
        {
            UpdateToolPositions();
            _wateringPotInstance.SetActive(true);
            
            Quaternion startRotation = Quaternion.Euler(0, 0, 0);
            Quaternion pourRotation = Quaternion.Euler(-60, 0, 0);
            Quaternion endRotation = Quaternion.Euler(0, 0, 0);
            
            _wateringPotInstance.transform.localRotation = startRotation;
            
            float elapsed = 0;
            float duration = _toolAnimationDuration * 0.3f;
            
            while (elapsed < duration)
            {
                _wateringPotInstance.transform.localRotation = Quaternion.Lerp(startRotation, pourRotation, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(_toolAnimationDuration * 0.4f);
            
            elapsed = 0;
            duration = _toolAnimationDuration * 0.3f;
            
            while (elapsed < duration)
            {
                _wateringPotInstance.transform.localRotation = Quaternion.Lerp(pourRotation, endRotation, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            _wateringPotInstance.SetActive(false);
            
            onComplete?.Invoke();
        }
        finally
        {
            if (_wateringPotInstance != null)
            {
                _wateringPotInstance.SetActive(false);
            }
            _isAnimatingTool = false;
        }
    }
    #endregion
} 