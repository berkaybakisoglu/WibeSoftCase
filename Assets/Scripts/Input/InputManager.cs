using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : BaseManager<InputManager>
{
    #region Fields
    [SerializeField] private LayerMask gridLayerMask;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float dragCellCheckInterval = 0.1f;
    
    private bool isDragging = false;
    private float lastDragCheckTime = 0f;
    private HashSet<GridCell> interactedCells = new HashSet<GridCell>();
    #endregion
    
    #region Unity Methods
    protected override void OnAwake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            isDragging = true;
            interactedCells.Clear();
            
            HandleGridSelection();
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            if (Time.time - lastDragCheckTime >= dragCellCheckInterval)
            {
                lastDragCheckTime = Time.time;
                HandleGridSelection();
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }
    #endregion
    
    #region Private Methods
    private void HandleGridSelection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, gridLayerMask))
        {
            GridCell cell = hit.collider.GetComponent<GridCell>();
            
            if (cell != null && !interactedCells.Contains(cell))
            {
                interactedCells.Add(cell);
                
                GridManager.Instance.SelectCell(cell);
                Debug.Log(cell.name + " selected");
            }
        }
    }
    #endregion
} 