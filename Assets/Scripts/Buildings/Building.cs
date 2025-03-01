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
        if (!Application.isPlaying)
            return;
            
        Vector2Int actualSize = Size;
        float cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
        
        Gizmos.color = Color.yellow;
        for (int x = 0; x < actualSize.x; x++)
        {
            for (int z = 0; z < actualSize.y; z++)
            {
                Vector2Int cellPos = _gridPosition + new Vector2Int(x, z);
                Vector3 worldPos = GridManager.Instance != null ? 
                    GridManager.Instance.GridToWorldPosition(cellPos) : 
                    new Vector3((cellPos.x + 0.5f) * cellSize, 0, (cellPos.y + 0.5f) * cellSize);
                
                Gizmos.DrawWireCube(
                    new Vector3(worldPos.x, 0.1f, worldPos.z), 
                    new Vector3(cellSize, 0.1f, cellSize)
                );
            }
        }
        
        if (_modelTransform != null)
        {
            Gizmos.color = Color.blue;
            Bounds bounds = CalculateModelBounds();
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
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
    }
    #endregion
    
    #region Private Methods
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
    
    private void LogDebugInfo(string prefix)
    {
        if (_data == null || _modelTransform == null)
            return;
            
        Vector2Int actualSize = Size;
        Bounds bounds = CalculateModelBounds();
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{prefix}: Rotation={_rotationIndex * 90}°, " +
                  $"Size={actualSize.x}x{actualSize.y}, " +
                  $"ModelScale={_modelTransform.localScale}, " +
                  $"ModelWorldPos={_modelTransform.position}, " +
                  $"BuildingPos={transform.position}");
        #endif
    }
    
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
    
    private void CreateCellVisualizers()
    {
        ClearCellVisualizers();
        
        int maxSize = Mathf.Max(_data.size.x, _data.size.y);
        int totalCells = maxSize * maxSize;
        float cellSize = GridManager.Instance.CellSize;
        
        for (int i = 0; i < totalCells; i++)
        {
            GameObject visualizer = Instantiate(_gridCellVisualizerPrefab, transform);
            visualizer.transform.localScale = new Vector3(cellSize, _cellVisualizerHeight * 2, cellSize);
            visualizer.SetActive(false);
            _cellVisualizers.Add(visualizer);
        }
    }
    
    private void ShowCellVisualizers(bool isValidPlacement)
    {
        if (_cellVisualizers.Count == 0 || _gridCellVisualizerPrefab == null)
            return;
            
        _showingCellVisualizers = true;
        
        UpdateCellVisualizerPositions();
        
        CheckCellValidity();
    }
    
    private void UpdateCellVisualizerPositions()
    {
        Vector2Int actualSize = Size;
        float cellSize = GridManager.Instance.CellSize;
        
        for (int i = 0; i < _cellVisualizers.Count; i++)
        {
            if (i < actualSize.x * actualSize.y)
            {
                int x = i % actualSize.x;
                int z = i / actualSize.x;
                
                Vector3 worldPos;
                
                Vector2Int cellPos = _gridPosition + new Vector2Int(x, z);
                
                worldPos = GridManager.Instance.GridToWorldPosition(cellPos);
                worldPos.y += _cellVisualizerHeight;
                
                GameObject visualizer = _cellVisualizers[i];
                visualizer.transform.position = worldPos;
                
                visualizer.transform.localScale = new Vector3(cellSize, _cellVisualizerHeight * 2, cellSize);
                
                visualizer.SetActive(true);
            }
            else
            {
                _cellVisualizers[i].SetActive(false);
            }
        }
    }
    
    private void CheckCellValidity()
    {
        Vector2Int actualSize = Size;
        
        for (int x = 0; x < actualSize.x; x++)
        {
            for (int z = 0; z < actualSize.y; z++)
            {
                int index = z * actualSize.x + x;
                if (index < _cellVisualizers.Count)
                {
                    Vector2Int cellPos = _gridPosition + new Vector2Int(x, z);
                    bool isCellValid = IsCellValid(cellPos);
                    
                    GameObject visualizer = _cellVisualizers[index];
                    MeshRenderer renderer = visualizer.GetComponent<MeshRenderer>();
                    
                    if (renderer != null)
                    {
                        renderer.material = isCellValid ? _validCellMaterial : _invalidCellMaterial;
                    }
                }
            }
        }
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
    
    private void HideCellVisualizers()
    {
        _showingCellVisualizers = false;
        
        foreach (var visualizer in _cellVisualizers)
        {
            if (visualizer != null)
            {
                visualizer.SetActive(false);
            }
        }
    }
    
    private void ClearCellVisualizers()
    {
        foreach (var visualizer in _cellVisualizers)
        {
            if (visualizer != null)
            {
                Destroy(visualizer);
            }
        }
        
        _cellVisualizers.Clear();
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
    
    private void EnsureBuildingHasCollider()
    {
        Collider[] existingColliders = GetComponentsInChildren<Collider>();
        
        if (existingColliders.Length == 0)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            
            Bounds bounds = CalculateModelBounds();
            
            Vector3 localSize = new Vector3(
                bounds.size.x / transform.lossyScale.x,
                bounds.size.y / transform.lossyScale.y,
                bounds.size.z / transform.lossyScale.z
            );
            
            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            
            boxCollider.size = localSize;
            boxCollider.center = localCenter;
            
            boxCollider.size = new Vector3(boxCollider.size.x * 0.9f, boxCollider.size.y, boxCollider.size.z * 0.9f);
            
            Debug.Log($"Added BoxCollider to building {name} with local size {boxCollider.size}");
        }
        else
        {
            foreach (Collider collider in existingColliders)
            {
                if (!collider.enabled)
                {
                    collider.enabled = true;
                }
            }
        }
    }
    #endregion
} 