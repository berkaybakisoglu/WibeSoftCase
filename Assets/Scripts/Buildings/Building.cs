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
    [SerializeField] private GameObject _gridCellVisualizerPrefab;
    [SerializeField] private Material _validCellMaterial;
    [SerializeField] private Material _invalidCellMaterial;
    [SerializeField] private float _cellVisualizerHeight = 0.05f;
    
    [Header("Scale Settings")]
    [SerializeField] private bool _autoScaleToFitGrid = true;
    [SerializeField] private Transform _modelTransform;
    [SerializeField] private Vector3 _modelOffset = Vector3.zero;
    
    private List<GameObject> _cellVisualizers = new List<GameObject>();
    private bool _showingCellVisualizers = false;
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
        
        if (_gridCellVisualizerPrefab == null)
        {
            CreateDefaultCellVisualizerPrefab();
        }
        
        EnsureBuildingHasCollider();
    }
    
    private void OnDestroy()
    {
        ClearCellVisualizers();
    }
    
    private void OnDrawGizmos()
    {
        if (_data == null)
            return;
            
        Vector2Int size = (_rotationIndex % 2 == 1) ? 
            new Vector2Int(_data.size.y, _data.size.x) : _data.size;
            
        Vector3 center = CalculateGridCenterWorldPosition();
        
        float cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
        Vector3 worldSize = new Vector3(size.x * cellSize, 0.1f, size.y * cellSize);
        
        Gizmos.color = Color.yellow;
        
        Matrix4x4 originalMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0, _rotationIndex * 90, 0), Vector3.one);
        
        Gizmos.DrawWireCube(Vector3.zero, worldSize);
        
        Gizmos.matrix = originalMatrix;
    }
    #endregion
    
    #region Public Methods
    public override void Initialize(BuildingData data, Vector2Int position)
    {
        base.Initialize(data, position);
        
        if (_gridCellVisualizerPrefab != null && _cellVisualizers.Count == 0)
        {
            CreateCellVisualizers();
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
        UpdateMaterials(isValid ? _validPlacementMaterial : _invalidPlacementMaterial);
        
        ShowCellVisualizers(isValid);
    }
    
    public override void ConfirmPlacement()
    {
        base.ConfirmPlacement();
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
        
        if (_showingCellVisualizers)
        {
            UpdateCellVisualizerPositions();
        }
    }
    #endregion
    
    #region Private Methods
    private void LogDebugInfo(string prefix)
    {
        Vector2Int size = Size;
        Vector3 worldPos = transform.position;
        Vector3 localScale = _modelTransform.localScale;
        Vector3 localPos = _modelTransform.localPosition;
        
        Debug.Log($"{prefix} - GridPos: {_gridPosition}, Size: {size}, " +
                  $"WorldPos: {worldPos}, ModelScale: {localScale}, ModelLocalPos: {localPos}");
    }
    
    private void UpdateMaterials(Material material)
    {
        if (_buildingRenderers == null || _buildingRenderers.Length == 0)
            return;
            
        if (material != null)
        {
            foreach (MeshRenderer renderer in _buildingRenderers)
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
    
    private void CreateCellVisualizers()
    {
        ClearCellVisualizers();
        
        Vector2Int size = Size;
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                GameObject visualizer = Instantiate(_gridCellVisualizerPrefab, transform);
                visualizer.SetActive(false);
                _cellVisualizers.Add(visualizer);
            }
        }
    }
    
    private void ShowCellVisualizers(bool isValidPlacement)
    {
        if (_cellVisualizers.Count == 0)
            return;
            
        _showingCellVisualizers = true;
        
        UpdateCellVisualizerPositions();
        
        CheckCellValidity();
        
        foreach (GameObject visualizer in _cellVisualizers)
        {
            visualizer.SetActive(true);
        }
    }
    
    private void UpdateCellVisualizerPositions()
    {
        if (_cellVisualizers.Count == 0)
            return;
            
        Vector2Int size = Size;
        float cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
        
        int index = 0;
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                if (index < _cellVisualizers.Count)
                {
                    GameObject visualizer = _cellVisualizers[index];
                    
                    Vector3 localPos = new Vector3(
                        (x + 0.5f) * cellSize - (size.x * cellSize / 2),
                        _cellVisualizerHeight,
                        (z + 0.5f) * cellSize - (size.y * cellSize / 2)
                    );
                    
                    Quaternion rotation = Quaternion.Euler(0, _rotationIndex * 90, 0);
                    localPos = rotation * localPos;
                    
                    visualizer.transform.localPosition = localPos;
                    visualizer.transform.localScale = new Vector3(cellSize, 0.01f, cellSize);
                    
                    index++;
                }
            }
        }
    }
    
    private void CheckCellValidity()
    {
        if (_cellVisualizers.Count == 0)
            return;
            
        Vector2Int size = Size;
        
        int index = 0;
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                if (index < _cellVisualizers.Count)
                {
                    GameObject visualizer = _cellVisualizers[index];
                    Vector2Int cellPos = _gridPosition + new Vector2Int(x, z);
                    
                    bool isValid = IsCellValid(cellPos);
                    
                    MeshRenderer renderer = visualizer.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.material = isValid ? _validCellMaterial : _invalidCellMaterial;
                    }
                    
                    index++;
                }
            }
        }
    }
    
    private bool IsCellValid(Vector2Int cellPos)
    {
        GridCell cell = GridManager.Instance.GetCellAtGridPosition(cellPos);
        if (cell == null)
            return false;
            
        if (cell.IsOccupied)
        {
            if (_state == BuildingState.ValidPlacement || _state == BuildingState.InvalidPlacement)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private void HideCellVisualizers()
    {
        if (_cellVisualizers.Count == 0)
            return;
            
        _showingCellVisualizers = false;
        
        foreach (GameObject visualizer in _cellVisualizers)
        {
            visualizer.SetActive(false);
        }
    }
    
    private void ClearCellVisualizers()
    {
        if (_cellVisualizers.Count == 0)
            return;
            
        foreach (GameObject visualizer in _cellVisualizers)
        {
            if (visualizer != null)
            {
                Destroy(visualizer);
            }
        }
        
        _cellVisualizers.Clear();
        _showingCellVisualizers = false;
    }
    
    private void CreateDefaultCellVisualizerPrefab()
    {
        GameObject visualizerPrefab = new GameObject("DefaultCellVisualizer");
        visualizerPrefab.transform.SetParent(transform);
        visualizerPrefab.transform.localPosition = Vector3.zero;
        
        MeshFilter meshFilter = visualizerPrefab.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateQuadMesh();
        
        MeshRenderer meshRenderer = visualizerPrefab.AddComponent<MeshRenderer>();
        
        if (_validCellMaterial == null)
        {
            _validCellMaterial = new Material(Shader.Find("Standard"));
            _validCellMaterial.color = new Color(0, 1, 0, 0.5f);
        }
        
        if (_invalidCellMaterial == null)
        {
            _invalidCellMaterial = new Material(Shader.Find("Standard"));
            _invalidCellMaterial.color = new Color(1, 0, 0, 0.5f);
        }
        
        _validCellMaterial.SetFloat("_Mode", 3);
        _validCellMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _validCellMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _validCellMaterial.SetInt("_ZWrite", 0);
        _validCellMaterial.DisableKeyword("_ALPHATEST_ON");
        _validCellMaterial.EnableKeyword("_ALPHABLEND_ON");
        _validCellMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        _validCellMaterial.renderQueue = 3000;
        
        _invalidCellMaterial.SetFloat("_Mode", 3);
        _invalidCellMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _invalidCellMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _invalidCellMaterial.SetInt("_ZWrite", 0);
        _invalidCellMaterial.DisableKeyword("_ALPHATEST_ON");
        _invalidCellMaterial.EnableKeyword("_ALPHABLEND_ON");
        _invalidCellMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        _invalidCellMaterial.renderQueue = 3000;
        
        meshRenderer.material = _validCellMaterial;
        
        visualizerPrefab.SetActive(false);
        
        _gridCellVisualizerPrefab = visualizerPrefab;
    }
    
    private Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, 0.5f)
        };
        
        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        Vector3[] normals = new Vector3[4]
        {
            Vector3.up,
            Vector3.up,
            Vector3.up,
            Vector3.up
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.normals = normals;
        
        return mesh;
    }
    
    private void ScaleModelToFitGrid()
    {
        if (_modelTransform == null || _data == null)
            return;
            
        Bounds modelBounds = CalculateModelBounds();
        
        float cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
        
        Vector2Int gridSize = Size;
        Vector3 targetSize = new Vector3(gridSize.x * cellSize, modelBounds.size.y, gridSize.y * cellSize);
        
        float scaleX = targetSize.x / modelBounds.size.x;
        float scaleZ = targetSize.z / modelBounds.size.z;
        
        float scale = Mathf.Min(scaleX, scaleZ);
        
        _modelTransform.localScale = new Vector3(scale, scale, scale);
        
        PositionModelWithOffset();
    }
    
    private Bounds CalculateModelBounds()
    {
        MeshRenderer[] renderers = _modelTransform.GetComponentsInChildren<MeshRenderer>();
        
        if (renderers.Length == 0)
        {
            return new Bounds(_modelTransform.position, Vector3.one);
        }
        
        Bounds bounds = renderers[0].bounds;
        
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        
        Vector3 localCenter = _modelTransform.InverseTransformPoint(bounds.center);
        Vector3 localExtents = bounds.extents;
        
        Bounds localBounds = new Bounds(localCenter, localExtents * 2);
        
        return localBounds;
    }
    
    private void PositionModelWithOffset()
    {
        if (_modelTransform == null || _data == null)
            return;
            
        Vector3 gridCenter = CalculateGridCenterWorldPosition();
        
        Vector3 offsetToGridCenter = gridCenter - transform.position;
        
        Quaternion rotation = Quaternion.Euler(0, _rotationIndex * 90, 0);
        Vector3 rotatedModelOffset = rotation * _modelOffset;
        
        _modelTransform.position = transform.position + offsetToGridCenter + rotatedModelOffset;
    }
    
    private Vector3 CalculateGridCenterWorldPosition()
    {
        float cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
        
        Vector2Int gridSize = Size;
        
        Vector3 centerOffset = new Vector3(
            gridSize.x * cellSize / 2,
            0,
            gridSize.y * cellSize / 2
        );
        
        Vector3 gridOrigin = GridManager.Instance.GridToWorldPosition(_gridPosition);
        Vector3 gridCenter = gridOrigin + centerOffset;
        
        return gridCenter;
    }
    
    private void EnsureBuildingHasCollider()
    {
        Collider collider = GetComponent<Collider>();
        
        if (collider == null)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            
            Vector2Int size = Size;
            float cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
            
            boxCollider.size = new Vector3(size.x * cellSize, 1f, size.y * cellSize);
            boxCollider.center = new Vector3(size.x * cellSize / 2, 0.5f, size.y * cellSize / 2);
        }
    }
    #endregion
} 