using UnityEngine;

public abstract class BaseManager<T> : MonoBehaviour where T : BaseManager<T>
{
    #region Properties
    public static T Instance { get; private set; }
    #endregion
    
    #region Fields
    [SerializeField] protected bool _dontDestroyOnLoad = false;
    #endregion
    
    #region Unity Methods
    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = (T)this;
        
        if (_dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
        
        OnAwake();
    }
    #endregion
    
    #region Protected Methods
    protected virtual void OnAwake() { }
    
    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion
}