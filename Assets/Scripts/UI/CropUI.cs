using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CropUI : MonoBehaviour
{
    #region Fields
    [SerializeField] private Button _broccoliButton;
    [SerializeField] private Button _cornButton;
    
    [SerializeField] private TextMeshProUGUI _selectedCropText;
    [SerializeField] private TextMeshProUGUI _broccoliHarvestedText;
    [SerializeField] private TextMeshProUGUI _cornHarvestedText;
    #endregion
    
    #region Unity Methods
    private void Start()
    {
        if (_broccoliButton != null) 
            _broccoliButton.onClick.AddListener(() => SelectCropType(0));
        if (_cornButton != null) 
            _cornButton.onClick.AddListener(() => SelectCropType(1));
        SetSelectedCropText();
        UpdateUI();
    }
    
    private void Update()
    {
        UpdateUI();
    }
    #endregion
    
    #region Private Methods
    private void SetSelectedCropText()
    {
        if (CropManager.Instance != null)
        {
            _selectedCropText.text = "Selected type " + CropManager.Instance.GetSelectedCropType();
        }
    }
    
    private void UpdateUI()
    {
        if (CropManager.Instance == null) return;
        
        if (_broccoliHarvestedText != null) _broccoliHarvestedText.text = $"Harvested: {CropManager.Instance.GetBroccoliHarvested()}";
        if (_cornHarvestedText != null) _cornHarvestedText.text = $"Harvested: {CropManager.Instance.GetCornHarvested()}";
    }
    
    private void SelectCropType(int cropTypeIndex)
    {
        if (CropManager.Instance != null)
        {
            CropManager.Instance.SelectCropType(cropTypeIndex);
            SetSelectedCropText();
        }
    }
    #endregion
} 