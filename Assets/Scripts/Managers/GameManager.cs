using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : BaseManager<GameManager>
{
    #region Fields
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private InputManager _inputManager;
    [SerializeField] private CropManager _cropManager;
    #endregion

    #region Unity Methods
    protected override void OnAwake()
    {
        if (_gridManager == null) _gridManager = FindObjectOfType<GridManager>();
        if (_inputManager == null) _inputManager = FindObjectOfType<InputManager>();
        if (_cropManager == null) _cropManager = FindObjectOfType<CropManager>();
    }
    #endregion
}