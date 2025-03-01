using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : BaseBuilding
{
    #region Fields
    [Header("Visual Feedback")]
    [SerializeField] private MeshRenderer[] _buildingRenderers;
    [SerializeField] private Material _defaultMaterial;
    [SerializeField] private Material _selectedMaterial;
    [SerializeField] private Material _validPlacementMaterial;
    [SerializeField] private Material _invalidPlacementMaterial;
    
    [Header("Grid Visualization")]
    [SerializeField] private float _cellVisualizerHeight = 0.05f;
    
    [Header("Scale Settings")]
    [SerializeField] private bool _autoScaleToFitGrid = true;
    [SerializeField] private Transform _modelTransform;
    [SerializeField] private Vector3 _modelOffset = Vector3.zero;
    
    [Header("Layer Settings")]
    [SerializeField] private string _buildingLayerName = "Building";
    
    private List<GameObject> _cellVisualizers = new List<GameObject>();
    private bool _showingCellVisualizers = false;
    private bool _isInPlacementMode = false;
    #endregion
    
    #region Unity Methods
    private void Awake()
    {
        if (_buildingRenderers == null || _buildingRenderers.Length == 0)
        {
            _buildingRenderers = GetComponentsInChildren<MeshRenderer>();
        }
        
        if (_modelTransform == null)
        {
            _modelTransform = transform;
        }
        
        // Set the layer for this building and all its children
        SetLayerRecursively(gameObject, LayerMask.NameToLayer(_buildingLayerName));
    }
    
    private void OnDestroy()
    {
        ClearCellVisualizers();
    }
    
    private void OnMouseEnter()
    {
        if (_state != BuildingState.Selected && !_isInPlacementMode)
        {
            // Change material to indicate hover
            Material[] originalMaterials = new Material[_buildingRenderers.Length];
            for (int i = 0; i < _buildingRenderers.Length; i++)
            {
                if (_buildingRenderers[i] != null)
                {
                    // Store original material
                    originalMaterials[i] = _buildingRenderers[i].material;
                    
                    // Create a temporary material with emissive properties
                    Material hoverMaterial = new Material(_buildingRenderers[i].material);
                    hoverMaterial.EnableKeyword("_EMISSION");
                    hoverMaterial.SetColor("_EmissionColor", Color.yellow * 0.5f);
                    
                    _buildingRenderers[i].material = hoverMaterial;
                }
            }
        }
    }
    
    private void OnMouseExit()
    {
        if (_state != BuildingState.Selected && !_isInPlacementMode)
        {
            // Restore original material
            UpdateMaterials(_state == BuildingState.Normal ? _defaultMaterial : 
                           (_state == BuildingState.ValidPlacement ? _validPlacementMaterial : _invalidPlacementMaterial));
        }
    }
    
    #endregion
    
    #region Public Methods
    public override void Initialize(BuildingData data, Vector2Int position)
    {
        base.Initialize(data, position);
        
        if (_autoScaleToFitGrid)
        {
            ScaleModelToFitGrid();
        }
        else
        {
            PositionModelWithOffset();
        }
    }
    
    public override void Select()
    {
        base.Select();
        UpdateMaterials(_selectedMaterial);
    }
    
    public override void Deselect()
    {
        base.Deselect();
        UpdateMaterials(_defaultMaterial);
        HideCellVisualizers();
    }
    
    public override void SetPlacementState(bool isValid)
    {
        base.SetPlacementState(isValid);
        _isInPlacementMode = true;
        UpdateMaterials(isValid ? _validPlacementMaterial : _invalidPlacementMaterial);
        
        ShowCellVisualizers();
    }
    
    public override void ConfirmPlacement()
    {
        base.ConfirmPlacement();
        _isInPlacementMode = false;
        UpdateMaterials(_defaultMaterial);
        HideCellVisualizers();
    }
    
    public override void SetPosition(Vector2Int position)
    {
        base.SetPosition(position);
        
        if (_showingCellVisualizers)
        {
            UpdateCellVisualizerPositions();
        }
    }
    
    public override void Rotate()
    {
        base.Rotate();
        
        if (_showingCellVisualizers)
        {
            UpdateCellVisualizerPositions();
        }
        
        if (_autoScaleToFitGrid)
        {
            ScaleModelToFitGrid();
        }
        else
        {
            PositionModelWithOffset();
        }
    }
    
    public override void SetRotation(int rotationIndex)
    {
        base.SetRotation(rotationIndex);
        
        if (_autoScaleToFitGrid)
        {
            ScaleModelToFitGrid();
        }
        else
        {
            PositionModelWithOffset();
        }
    }
    #endregion
    
    #region Private Methods
    private void UpdateMaterials(Material material)
    {
        if (material == null)
            return;
            
        foreach (var renderer in _buildingRenderers)
        {
            if (renderer != null)
            {
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }
                renderer.materials = materials;
            }
        }
    }
    
    private void ShowCellVisualizers()
    {
        ClearCellVisualizers();
        if (GridCellVisualizerPool.Instance == null)
            return;
        
        _showingCellVisualizers = true;
        _cellVisualizers = GridCellVisualizerPool.Instance.CreateVisualizersForBuilding(
            _gridPosition, 
            Size, 
            GridManager.Instance.CellSize, 
            _cellVisualizerHeight,
            gameObject
        );
    }
    
    private void UpdateCellVisualizerPositions()
    {
        ClearCellVisualizers();
        _cellVisualizers = GridCellVisualizerPool.Instance.CreateVisualizersForBuilding(
            _gridPosition, 
            Size, 
            GridManager.Instance.CellSize, 
            _cellVisualizerHeight,
            gameObject
        );
    }
    
    private void HideCellVisualizers()
    {
        _showingCellVisualizers = false;
        ClearCellVisualizers();
    }
    
    private void ClearCellVisualizers()
    {
        foreach (var visualizer in _cellVisualizers)
        {
            if (visualizer != null)
            {
                    GridCellVisualizerPool.Instance.ReturnVisualizer(visualizer);
            }
        }
        _cellVisualizers.Clear();
    }
    
    private bool IsCellValid(Vector2Int cellPos)
    {
        if (!GridManager.Instance.IsValidGridPosition(cellPos.x, cellPos.y))
            return false;
            
        GridCell cell = GridManager.Instance.GetCellAtGridPosition(cellPos);
        if (cell == null)
            return false;
            
        if (cell.IsOccupied)
        {
            if (cell.OccupyingObject != gameObject)
                return false;
        }
        
        return true;
    }
    
    private void ScaleModelToFitGrid()
    {
        if (_data == null || _modelTransform == null)
            return;
            
        float cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
        
        Vector2Int actualSize = Size;
        float totalWidth = actualSize.x * cellSize;
        float totalHeight = actualSize.y * cellSize;
        
        Quaternion originalRotation = _modelTransform.localRotation;
        _modelTransform.localRotation = Quaternion.identity;
        
        Vector3 originalScale = _modelTransform.localScale;
        _modelTransform.localScale = Vector3.one;
        
        Bounds originalBounds = CalculateModelBounds();
        
        _modelTransform.localRotation = originalRotation;
        
        if (originalBounds.size == Vector3.zero)
        {
            _modelTransform.localScale = originalScale;
            Debug.LogWarning("Could not calculate model bounds for scaling");
            return;
        }
        
        float scaleX = totalWidth / originalBounds.size.x;
        float scaleZ = totalHeight / originalBounds.size.z;
        
        float scale = Mathf.Min(scaleX, scaleZ);
        
        _modelTransform.localScale = new Vector3(scale, scale, scale);
        
        PositionModelWithOffset();
    }
    
    private Bounds CalculateModelBounds()
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool boundsInitialized = false;
        
        if (_modelTransform == null)
            return bounds;
        
        Renderer[] renderers = _modelTransform.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                if (!boundsInitialized)
                {
                    bounds = renderer.bounds;
                    boundsInitialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }
        
        if (!boundsInitialized)
        {
            Collider[] colliders = _modelTransform.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                if (collider != null)
                {
                    if (!boundsInitialized)
                    {
                        bounds = collider.bounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(collider.bounds);
                    }
                }
            }
        }
        
        if (!boundsInitialized)
        {
            Debug.LogWarning("Could not find any renderers or colliders to calculate bounds. Using default bounds.");
            bounds = new Bounds(_modelTransform.position, Vector3.one);
        }
        
        return bounds;
    }
    
    private void PositionModelWithOffset()
    {
        if (_modelTransform == null || _modelTransform == transform)
            return;
            
        Vector3 gridCenter = CalculateGridCenterWorldPosition();
        
        _modelTransform.position = gridCenter;
        
        if (_modelOffset != Vector3.zero)
        {
            Vector3 rotatedOffset = transform.rotation * _modelOffset;
            _modelTransform.position += rotatedOffset;
        }
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Model positioned at grid center: {gridCenter}, final position: {_modelTransform.position}");
        #endif
    }
    
    private Vector3 CalculateGridCenterWorldPosition()
    {
        Vector2Int actualSize = Size;
        
        if (actualSize.x == 1 && actualSize.y == 1)
        {
            return GridManager.Instance.GridToWorldPosition(_gridPosition);
        }
        
        Vector2Int minCorner = _gridPosition;
        Vector2Int maxCorner = _gridPosition + new Vector2Int(actualSize.x - 1, actualSize.y - 1);
        
        Vector3 minWorldPos = GridManager.Instance.GridToWorldPosition(minCorner);
        Vector3 maxWorldPos = GridManager.Instance.GridToWorldPosition(maxCorner);
        
        Vector3 centerPos = (minWorldPos + maxWorldPos) * 0.5f;
        
        return centerPos;
    }
    
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        
        obj.layer = layer;
        
        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
    #endregion
} 