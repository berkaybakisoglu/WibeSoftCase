using UnityEngine;
using UnityEngine.UI;

public class BuildingPlacementTester : MonoBehaviour
{
    [Header("Building Configuration")]
    [SerializeField] private BuildingData[] _testBuildings;
    [SerializeField] private KeyCode _rotateKey = KeyCode.R;
    [SerializeField] private KeyCode _confirmKey = KeyCode.Space;
    [SerializeField] private KeyCode _cancelKey = KeyCode.Escape;
    
    [Header("Test Controls")]
    [SerializeField] private int _currentBuildingIndex = 0;
    [SerializeField] private KeyCode _nextBuildingKey = KeyCode.E;
    [SerializeField] private KeyCode _previousBuildingKey = KeyCode.Q;
    [SerializeField] private KeyCode _repositionKey = KeyCode.T;
    
    [Header("UI Elements (Optional)")]
    [SerializeField] private Text _currentBuildingText;
    
    private BuildingManager _buildingManager;
    private bool _isPlacingBuilding = false;
    private bool _isRepositioningBuilding = false;
    
    private void Start()
    {
        _buildingManager = BuildingManager.Instance;
        
        if (_buildingManager == null)
        {
            Debug.LogError("BuildingManager not found!");
            enabled = false;
            return;
        }
        
        UpdateBuildingText();
    }
    
    private void Update()
    {
        // Start placement of current building
        if (Input.GetKeyDown(KeyCode.F) && !_isPlacingBuilding && !_isRepositioningBuilding)
        {
            StartPlacingCurrentBuilding();
        }
        
        // Cycle through buildings
        if (Input.GetKeyDown(_nextBuildingKey) && !_isPlacingBuilding && !_isRepositioningBuilding)
        {
            CycleToNextBuilding();
        }
        
        if (Input.GetKeyDown(_previousBuildingKey) && !_isPlacingBuilding && !_isRepositioningBuilding)
        {
            CycleToPreviousBuilding();
        }
        
        // Start repositioning the selected building
        if (Input.GetKeyDown(_repositionKey) && !_isPlacingBuilding && !_isRepositioningBuilding)
        {
            StartRepositioningSelectedBuilding();
        }
        
        // Handle placement/repositioning controls
        if (_isPlacingBuilding || _isRepositioningBuilding)
        {
            // Rotate building
            if (Input.GetKeyDown(_rotateKey))
            {
                _buildingManager.RotateBuilding();
            }
            
            // Confirm placement
            if (Input.GetKeyDown(_confirmKey))
            {
                _buildingManager.ConfirmPlacement();
                _isPlacingBuilding = false;
                _isRepositioningBuilding = false;
            }
            
            // Cancel placement
            if (Input.GetKeyDown(_cancelKey))
            {
                _buildingManager.CancelPlacement();
                _isPlacingBuilding = false;
                _isRepositioningBuilding = false;
            }
        }
        
        // Delete selected building
        if (Input.GetKeyDown(KeyCode.Delete) && _buildingManager.GetSelectedBuilding() != null)
        {
            _buildingManager.DeleteSelectedBuilding();
        }
        
        // Select buildings by clicking on them (handled by BuildingManager)
        if (Input.GetMouseButtonDown(0) && !_isPlacingBuilding && !_isRepositioningBuilding)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // Let the BuildingManager handle the click
                _buildingManager.HandleMouseClick(hit.point);
            }
        }
        
        // Right-click to deselect
        if (Input.GetMouseButtonDown(1) && !_isPlacingBuilding && !_isRepositioningBuilding)
        {
            _buildingManager.HandleMouseRightClick();
        }
    }
    
    private void StartPlacingCurrentBuilding()
    {
        if (_testBuildings == null || _testBuildings.Length == 0)
        {
            Debug.LogWarning("No test buildings assigned!");
            return;
        }
        
        if (_currentBuildingIndex >= 0 && _currentBuildingIndex < _testBuildings.Length)
        {
            BuildingData buildingData = _testBuildings[_currentBuildingIndex];
            _buildingManager.StartPlacement(buildingData);
            _isPlacingBuilding = true;
        }
    }
    
    private void StartRepositioningSelectedBuilding()
    {
        Building selectedBuilding = _buildingManager.GetSelectedBuilding();
        
        if (selectedBuilding != null)
        {
            _buildingManager.StartRepositioning(selectedBuilding);
            _isRepositioningBuilding = true;
        }
        else
        {
            Debug.Log("No building selected to reposition!");
        }
    }
    
    private void CycleToNextBuilding()
    {
        if (_testBuildings == null || _testBuildings.Length == 0)
            return;
            
        _currentBuildingIndex = (_currentBuildingIndex + 1) % _testBuildings.Length;
        UpdateBuildingText();
    }
    
    private void CycleToPreviousBuilding()
    {
        if (_testBuildings == null || _testBuildings.Length == 0)
            return;
            
        _currentBuildingIndex = (_currentBuildingIndex - 1 + _testBuildings.Length) % _testBuildings.Length;
        UpdateBuildingText();
    }
    
    private void UpdateBuildingText()
    {
        if (_currentBuildingText != null && _testBuildings != null && _testBuildings.Length > 0)
        {
            if (_currentBuildingIndex >= 0 && _currentBuildingIndex < _testBuildings.Length)
            {
                BuildingData building = _testBuildings[_currentBuildingIndex];
                _currentBuildingText.text = $"Current Building: {building.displayName} ({_currentBuildingIndex + 1}/{_testBuildings.Length})";
            }
        }
    }
} 