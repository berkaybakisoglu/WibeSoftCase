using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GridCellVisualizerPool : MonoBehaviour
{
    #region Properties
    public static GridCellVisualizerPool Instance { get; private set; }
    #endregion
    
    #region Fields
    [Header("Pool Configuration")]
    [SerializeField] private GameObject _visualizerPrefab;
    [SerializeField] private int _initialPoolSize = 50;
    [SerializeField] private Transform _poolParent;
    
    [Header("Materials")]
    [SerializeField] private Material _validCellMaterial;
    [SerializeField] private Material _invalidCellMaterial;
    
    private Queue<GameObject> _visualizerPool = new Queue<GameObject>();
    private List<GameObject> _activeVisualizers = new List<GameObject>();
    
    #endregion
    
    #region Unity Methods
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        

        if (_poolParent == null)
        {
            GameObject poolParentObj = new GameObject("GridCellVisualizerPool");
            _poolParent = poolParentObj.transform;
            _poolParent.SetParent(transform);
        }
        InitializePool();
    }
    #endregion
    
    #region Private Methods
    private void InitializePool()
    {
        for (int i = 0; i < _initialPoolSize; i++)
        {
            GameObject visualizer = CreateVisualizer();
            _visualizerPool.Enqueue(visualizer);
        }
    }
    
    private GameObject CreateVisualizer()
    {
        GameObject visualizer = Instantiate(_visualizerPrefab, _poolParent);
        visualizer.SetActive(false);
        return visualizer;
    }
    
    #endregion
    
    #region Public Methods

    public GameObject GetVisualizer(bool isValid = true)
    {
        GameObject visualizer;
        
        if (_visualizerPool.Count == 0)
        {
            visualizer = CreateVisualizer();
        }
        else
        {
            visualizer = _visualizerPool.Dequeue();
        }
        
        MeshRenderer renderer = visualizer.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = isValid ? _validCellMaterial : _invalidCellMaterial;
        }
        
        _activeVisualizers.Add(visualizer);
        return visualizer;
    }
    
    public void ReturnVisualizer(GameObject visualizer)
    {
        if (visualizer == null)
            return;
        
        visualizer.SetActive(false);
        visualizer.transform.SetParent(_poolParent);
        
        _activeVisualizers.Remove(visualizer);
        _visualizerPool.Enqueue(visualizer);
    }
    
    public List<GameObject> CreateVisualizersForBuilding(Vector2Int gridPosition, Vector2Int size, float cellSize, float height = 0.05f, GameObject buildingObject = null)
    {
        List<GameObject> visualizers = new List<GameObject>();
        
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int cellPos = gridPosition + new Vector2Int(x, z);
                bool isValid = IsCellValid(cellPos, buildingObject);
                
                GameObject visualizer = GetVisualizer(isValid);
                Vector3 worldPos = GridManager.Instance.GridToWorldPosition(cellPos);
                worldPos.y += height;
                visualizer.transform.position = worldPos;
                
                visualizer.transform.localScale = new Vector3(cellSize, 1, cellSize);
                
                visualizer.SetActive(true);
                visualizers.Add(visualizer);
            }
        }
        
        return visualizers;
    }
    
    private bool IsCellValid(Vector2Int cellPos, GameObject buildingObject = null)
    {
        if (!GridManager.Instance.IsValidGridPosition(cellPos.x, cellPos.y))
            return false;
            
        GridCell cell = GridManager.Instance.GetCellAtGridPosition(cellPos);
        if (cell == null)
            return false;
            
        if (cell.IsOccupied)
        {
            if (buildingObject != null && cell.OccupyingObject == buildingObject)
                return true;
                
            return false;
        }
        
        return true;
    }
    #endregion
} 