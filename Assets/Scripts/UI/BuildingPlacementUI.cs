using UnityEngine;
using UnityEngine.UI;

public class BuildingPlacementUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text _instructionsText;
    [SerializeField] private Text _currentBuildingText;
    [SerializeField] private Text _statusText;
    [SerializeField] private GameObject _placementPanel;
    [SerializeField] private GameObject _repositioningPanel;
    [SerializeField] private GameObject _selectionPanel;
    
    [Header("Key Bindings Display")]
    [SerializeField] private Text _placementKeysText;
    [SerializeField] private Text _repositioningKeysText;
    [SerializeField] private Text _selectionKeysText;
    
    private BuildingManager _buildingManager;
    private BuildingPlacementTester _placementTester;
    private bool _isRepositioningMode = false;
    
    private void Start()
    {
        _buildingManager = BuildingManager.Instance;
        _placementTester = FindObjectOfType<BuildingPlacementTester>();
        
        if (_buildingManager == null)
        {
            Debug.LogError("BuildingManager not found!");
            enabled = false;
            return;
        }
        
        // Subscribe to events
        _buildingManager.OnBuildingSelected += HandleBuildingSelected;
        _buildingManager.OnBuildingDeselected += HandleBuildingDeselected;
        _buildingManager.OnBuildingPlaced += HandleBuildingPlaced;
        
        // Add a field to BuildingManager to track repositioning mode
        AddRepositioningModeTracking();
        
        // Initialize UI
        UpdateUI();
        
        // Set up key binding texts
        if (_placementKeysText != null)
        {
            _placementKeysText.text = 
                "F: Start Placement\n" +
                "R: Rotate\n" +
                "Space: Confirm\n" +
                "Esc: Cancel\n" +
                "E/Q: Cycle Buildings";
        }
        
        if (_repositioningKeysText != null)
        {
            _repositioningKeysText.text = 
                "T: Start Repositioning\n" +
                "R: Rotate\n" +
                "Space: Confirm\n" +
                "Esc: Cancel";
        }
        
        if (_selectionKeysText != null)
        {
            _selectionKeysText.text = 
                "Left Click: Select\n" +
                "Right Click: Deselect\n" +
                "T: Reposition\n" +
                "Delete: Remove";
        }
    }
    
    private void OnDestroy()
    {
        if (_buildingManager != null)
        {
            _buildingManager.OnBuildingSelected -= HandleBuildingSelected;
            _buildingManager.OnBuildingDeselected -= HandleBuildingDeselected;
            _buildingManager.OnBuildingPlaced -= HandleBuildingPlaced;
        }
    }
    
    private void Update()
    {
        // Check if the placement tester is in repositioning mode
        if (_placementTester != null)
        {
            _isRepositioningMode = IsRepositioningMode();
        }
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        bool isInPlacementMode = _buildingManager.IsInPlacementMode();
        Building selectedBuilding = _buildingManager.GetSelectedBuilding();
        Building placementBuilding = _buildingManager.GetPlacementBuilding();
        
        // Update panels visibility
        if (_placementPanel != null)
            _placementPanel.SetActive(isInPlacementMode);
            
        if (_repositioningPanel != null)
            _repositioningPanel.SetActive(_isRepositioningMode);
            
        if (_selectionPanel != null)
            _selectionPanel.SetActive(selectedBuilding != null && !isInPlacementMode);
        
        // Update status text
        if (_statusText != null)
        {
            if (_isRepositioningMode)
            {
                _statusText.text = "Repositioning Building";
            }
            else if (isInPlacementMode)
            {
                _statusText.text = "Placing New Building";
            }
            else if (selectedBuilding != null)
            {
                _statusText.text = "Building Selected: " + selectedBuilding.Data.displayName;
            }
            else
            {
                _statusText.text = "No Building Selected";
            }
        }
        
        // Update instructions
        if (_instructionsText != null)
        {
            if (_isRepositioningMode)
            {
                _instructionsText.text = "Reposition the building on a valid location.\nPress R to rotate, Space to confirm, or Esc to cancel.";
            }
            else if (isInPlacementMode)
            {
                _instructionsText.text = "Position the building on a valid location.\nPress R to rotate, Space to confirm, or Esc to cancel.";
            }
            else if (selectedBuilding != null)
            {
                _instructionsText.text = "Press T to reposition the selected building.\nPress Delete to remove it.";
            }
            else
            {
                _instructionsText.text = "Press F to place a new building.\nClick on a building to select it.";
            }
        }
    }
    
    private void HandleBuildingSelected(Building building)
    {
        UpdateUI();
    }
    
    private void HandleBuildingDeselected()
    {
        UpdateUI();
    }
    
    private void HandleBuildingPlaced(Building building)
    {
        _isRepositioningMode = false;
        UpdateUI();
    }
    
    // Check if we're in repositioning mode by using reflection to access the private field in BuildingPlacementTester
    private bool IsRepositioningMode()
    {
        if (_placementTester == null)
            return false;
            
        System.Reflection.FieldInfo fieldInfo = _placementTester.GetType().GetField("_isRepositioningBuilding", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (fieldInfo != null)
        {
            return (bool)fieldInfo.GetValue(_placementTester);
        }
        
        return false;
    }
    
    // Add a method to BuildingManager to check if we're in repositioning mode
    private void AddRepositioningModeTracking()
    {
        // We can also check the _isInRepositionMode field in BuildingManager
        System.Reflection.FieldInfo fieldInfo = _buildingManager.GetType().GetField("_isInRepositionMode", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (fieldInfo != null)
        {
            // Subscribe to the Update event to check this field
            StartCoroutine(CheckRepositioningMode(fieldInfo));
        }
    }
    
    private System.Collections.IEnumerator CheckRepositioningMode(System.Reflection.FieldInfo fieldInfo)
    {
        while (true)
        {
            if (fieldInfo != null && _buildingManager != null)
            {
                bool isInRepositionMode = (bool)fieldInfo.GetValue(_buildingManager);
                if (isInRepositionMode)
                {
                    _isRepositioningMode = true;
                }
            }
            
            yield return null;
        }
    }
} 