using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    #region Fields
    [SerializeField] private Material _defaultMaterial;
    [SerializeField] private Material _validPlacementMaterial;
    [SerializeField] private Material _invalidPlacementMaterial;
    
    [SerializeField] private MeshRenderer _meshRenderer;
    
    private Collider _cellCollider;
    private float _cellHeight;
    #endregion
    
    #region Properties
    public Vector2Int GridPosition { get; private set; }
    public bool IsOccupied { get; private set; }
    public GameObject OccupyingObject { get; private set; }
    #endregion
    
    #region Unity Methods
    private void Awake()
    {
        _cellCollider = GetComponent<Collider>();
        
        if (_cellCollider != null)
        {
            _cellHeight = _cellCollider.bounds.extents.y;
        }
        else
        {
            _cellHeight = 0.1f;
        }
    }
    #endregion
    
    #region Public Methods
    public void Initialize(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
        IsOccupied = false;
        OccupyingObject = null;
        ResetMaterial();
    }
    
    public void SetOccupied(GameObject occupyingObject)
    {
        IsOccupied = true;
        OccupyingObject = occupyingObject;
        ResetMaterial();
    }
    
    public void SetUnoccupied()
    {
        IsOccupied = false;
        OccupyingObject = null;
        ResetMaterial();
    }
    
    public void ShowValidPlacement()
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.material = _validPlacementMaterial;
        }
    }
    
    public void ShowInvalidPlacement()
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.material = _invalidPlacementMaterial;
        }
    }
    
    public void ResetMaterial()
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.material = _defaultMaterial;
        }
    }
    
    public Vector3 GetTopPosition()
    {
        return transform.position + new Vector3(0, _cellHeight, 0);
    }
    
    public float GetHeight()
    {
        return _cellHeight;
    }
    #endregion
} 