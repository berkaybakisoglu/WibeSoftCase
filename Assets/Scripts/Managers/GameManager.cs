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
    
}