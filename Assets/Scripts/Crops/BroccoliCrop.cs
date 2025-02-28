using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BroccoliCrop : Crop
{
    #region Public Methods
    public override void Initialize(GridCell cell)
    {
        base.Initialize(cell);
        SetState(CropState.Growing);
    }
    #endregion
    
    #region Protected Methods
    protected override void Grow()
    {
        _growthTimer += Time.deltaTime * _growthSpeedMultiplier;
        
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
    #endregion
} 