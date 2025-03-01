using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class BuildingUI : MonoBehaviour
{
    #region Fields
    [Header("Building Selection Panel")]
    [SerializeField] private GameObject buildingSelectionPanel;
    [SerializeField] private Transform buildingButtonsContainer;
    [SerializeField] private Button buildingButtonPrefab;
    
    [Header("Building Placement Controls")]
    [SerializeField] private GameObject placementControlsPanel;
    [SerializeField] private Button rotateButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Vector3 controlsPanelOffset = new Vector3(0, 2f, 0);
    
    [Header("Building Management Controls")]
    [SerializeField] private GameObject managementControlsPanel;
    [SerializeField] private Button repositionButton;
    [SerializeField] private Button deleteButton;
    
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    
    private BuildingManager buildingManager;
    private Building selectedBuilding;
    private Building placementBuilding;
    private bool isInPlacementMode = false;
    private bool isInRepositionMode = false;
    #endregion
    
    #region Unity Methods
    private void Start()
    {
        buildingManager = BuildingManager.Instance;
        
        if (buildingManager == null)
        {
            Debug.LogError("BuildingManager not found!");
            enabled = false;
            return;
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        buildingManager.OnBuildingSelected += HandleBuildingSelected;
        buildingManager.OnBuildingDeselected += HandleBuildingDeselected;
        buildingManager.OnBuildingPlaced += HandleBuildingPlaced;
        
        SetupBuildingButtons();
        SetupControlButtons();
        UpdateUI();
    }
    
    private void OnDestroy()
    {
        if (buildingManager != null)
        {
            buildingManager.OnBuildingSelected -= HandleBuildingSelected;
            buildingManager.OnBuildingDeselected -= HandleBuildingDeselected;
            buildingManager.OnBuildingPlaced -= HandleBuildingPlaced;
        }
    }
    
    private void Update()
    {
        isInPlacementMode = buildingManager.IsInPlacementMode();
        placementBuilding = buildingManager.GetPlacementBuilding();
        
        UpdateControlPanelPositions();
        
        UpdateUI();
    }
    #endregion
    
    #region Private Methods
    private void SetupBuildingButtons()
    {
        foreach (Transform child in buildingButtonsContainer)
        {
            Destroy(child.gameObject);
        }

        var availableBuildings = BuildingManager.Instance.AvailableBuildings;
        
        if (availableBuildings == null || availableBuildings.Length == 0)
        {
            Debug.LogWarning("No available buildings found in BuildingManager!");
            return;
        }
        
        for (int i = 0; i < availableBuildings.Length; i++)
        {
            BuildingData buildingData = availableBuildings[i];
            Button button = Instantiate(buildingButtonPrefab, buildingButtonsContainer);
            
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = buildingData.displayName;
            }
            
            BuildingData capturedData = buildingData;
            button.onClick.AddListener(() => StartPlacingBuilding(capturedData));
        }
    }
    
    private BuildingData[] GetAvailableBuildingsFromManager()
    {
        System.Reflection.FieldInfo fieldInfo = buildingManager.GetType().GetField("_availableBuildings", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (fieldInfo != null)
        {
            return (BuildingData[])fieldInfo.GetValue(buildingManager);
        }
        
        Debug.LogError("Could not access available buildings from BuildingManager or BuildingPlacementTester!");
        return null;
    }
    
    private void SetupControlButtons()
    {
        if (rotateButton != null)
            rotateButton.onClick.AddListener(RotateBuilding);
        
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmPlacement);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelPlacement);
        
        if (repositionButton != null)
            repositionButton.onClick.AddListener(StartRepositioningBuilding);
        
        if (deleteButton != null)
            deleteButton.onClick.AddListener(DeleteSelectedBuilding);
    }
    
    private void UpdateUI()
    {
        if (buildingSelectionPanel != null)
            buildingSelectionPanel.SetActive(!isInPlacementMode && !isInRepositionMode);
        
        if (placementControlsPanel != null)
            placementControlsPanel.SetActive(isInPlacementMode || isInRepositionMode);
        
        if (managementControlsPanel != null)
            managementControlsPanel.SetActive(selectedBuilding != null && !isInPlacementMode && !isInRepositionMode);
    }
    
    private void UpdateControlPanelPositions()
    {
        if (placementControlsPanel != null && placementBuilding != null)
        {
            Vector3 worldPos = placementBuilding.transform.position;
            
            worldPos += controlsPanelOffset;
            
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            
            RectTransform canvasRect = placementControlsPanel.transform.parent.GetComponent<RectTransform>();
            Vector2 viewportPosition = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
            Vector2 canvasPosition = new Vector2(
                ((viewportPosition.x * 2) - 1) * canvasRect.sizeDelta.x * 0.5f,
                ((viewportPosition.y * 2) - 1) * canvasRect.sizeDelta.y * 0.5f
            );
            
            Canvas parentCanvas = placementControlsPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                placementControlsPanel.transform.position = screenPos;
            }
            else
            {
                ((RectTransform)placementControlsPanel.transform).anchoredPosition = canvasPosition;
            }
            
            Debug.Log($"Building world pos: {placementBuilding.transform.position}, Screen pos: {screenPos}, Canvas pos: {canvasPosition}");
        }
        
        if (managementControlsPanel != null && selectedBuilding != null && !isInPlacementMode && !isInRepositionMode)
        {
            Vector3 worldPos = selectedBuilding.transform.position;
            
            worldPos += new Vector3(0, 2f, 0);
            
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            
            RectTransform canvasRect = managementControlsPanel.transform.parent.GetComponent<RectTransform>();
            Vector2 viewportPosition = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
            Vector2 canvasPosition = new Vector2(
                ((viewportPosition.x * 2) - 1) * canvasRect.sizeDelta.x * 0.5f,
                ((viewportPosition.y * 2) - 1) * canvasRect.sizeDelta.y * 0.5f
            );
            
            Canvas parentCanvas = managementControlsPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                managementControlsPanel.transform.position = screenPos;
            }
            else
            {
                ((RectTransform)managementControlsPanel.transform).anchoredPosition = canvasPosition;
            }
        }
    }
    #endregion
    
    #region Button Actions
    private void StartPlacingBuilding(BuildingData buildingData)
    {
        if (buildingData != null)
        {
            buildingManager.StartPlacement(buildingData);
            isInPlacementMode = true;
        }
    }
    
    private void RotateBuilding()
    {
        buildingManager.RotateBuilding();
    }
    
    private void ConfirmPlacement()
    {
        buildingManager.ConfirmPlacement();
        isInPlacementMode = false;
        isInRepositionMode = false;
    }
    
    private void CancelPlacement()
    {
        buildingManager.CancelPlacement();
        isInPlacementMode = false;
        isInRepositionMode = false;
    }
    
    private void StartRepositioningBuilding()
    {
        if (selectedBuilding != null)
        {
            buildingManager.StartRepositioning(selectedBuilding);
            isInRepositionMode = true;
        }
    }
    
    private void DeleteSelectedBuilding()
    {
        buildingManager.DeleteSelectedBuilding();
    }
    #endregion
    
    #region Event Handlers
    private void HandleBuildingSelected(Building building)
    {
        selectedBuilding = building;
        UpdateUI();
    }
    
    private void HandleBuildingDeselected()
    {
        selectedBuilding = null;
        UpdateUI();
    }
    
    private void HandleBuildingPlaced(Building building)
    {
        isInPlacementMode = false;
        isInRepositionMode = false;
        UpdateUI();
    }
    #endregion
} 