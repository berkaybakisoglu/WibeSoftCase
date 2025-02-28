using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using Random = UnityEngine.Random;

/// <summary>
/// Manages a pool of harvested item objects for efficient reuse
/// </summary>
public class HarvestedItemPool : MonoBehaviour
{
    #region Properties
    public static HarvestedItemPool Instance { get; private set; }
    #endregion
    
    #region Fields
    [System.Serializable]
    public class HarvestedItemConfig
    {
        public CropType cropType;
        public GameObject harvestedItemPrefab;
        public int initialPoolSize = 10;
    }
    
    [Header("Pool Configuration")]
    [SerializeField] private List<HarvestedItemConfig> _itemConfigs = new List<HarvestedItemConfig>();
    [SerializeField] private Transform _poolParent;
    
    [Header("Animation Settings")]
    [SerializeField] private float _animationDuration = 0.5f;
    [SerializeField] private float _jumpHeight = 0.5f;
    [SerializeField] private float _rotationAmount = 360f;
    [SerializeField] private Ease _jumpEase = Ease.OutQuad;
    
    [Header("Multiple Items Settings")]
    [SerializeField] private float _delayBetweenItems = 0.1f;
    [SerializeField] private int _maxSimultaneousItems = 5;
    [SerializeField] private bool _useStaggeredArrival = true;
    
    // Dictionary to store pools for each crop type
    private Dictionary<CropType, Queue<GameObject>> _itemPools = new Dictionary<CropType, Queue<GameObject>>();
    
    // Track active animations
    private int _activeAnimationCount = 0;
    #endregion
    
    #region Unity Methods
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Create pool parent if not assigned
        if (_poolParent == null)
        {
            GameObject poolParentObj = new GameObject("HarvestedItemPool");
            _poolParent = poolParentObj.transform;
            _poolParent.SetParent(transform);
        }
        
        InitializePools();
    }
    #endregion
    
    #region Private Methods
    private void InitializePools()
    {
        foreach (var config in _itemConfigs)
        {
            if (!_itemPools.ContainsKey(config.cropType))
            {
                _itemPools[config.cropType] = new Queue<GameObject>();
                
                // Pre-populate pool with initial size
                for (int i = 0; i < config.initialPoolSize; i++)
                {
                    GameObject item = CreateNewItem(config.cropType);
                    _itemPools[config.cropType].Enqueue(item);
                }
            }
        }
    }
    
    private GameObject CreateNewItem(CropType cropType)
    {
        HarvestedItemConfig config = _itemConfigs.Find(c => c.cropType == cropType);
        
        if (config == null || config.harvestedItemPrefab == null)
        {
            Debug.LogError($"No prefab configured for crop type: {cropType}");
            return null;
        }
        
        GameObject item = Instantiate(config.harvestedItemPrefab, _poolParent);
        item.SetActive(false);
        
        // Store original scale for animations
        OriginalScale scaleComponent = item.AddComponent<OriginalScale>();
        scaleComponent.originalScale = item.transform.localScale;
        
        return item;
    }
    
    private IEnumerator SpawnItemsInBatches(CropType cropType, Vector3 startPosition, int totalCount, Action onComplete = null)
    {
        int remainingCount = totalCount;
        int batchSize = Mathf.Min(_maxSimultaneousItems, totalCount);
        int completedCount = 0;
        
        while (remainingCount > 0)
        {
            int currentBatchSize = Mathf.Min(batchSize, remainingCount);
            int batchCompletedCount = 0;
            
            Action batchComplete = () => {
                batchCompletedCount++;
                completedCount++;
                
                if (batchCompletedCount >= currentBatchSize)
                {
                    // This batch is complete
                    if (completedCount >= totalCount)
                    {
                        // All items are complete
                        onComplete?.Invoke();
                    }
                }
            };
            
            for (int i = 0; i < currentBatchSize; i++)
            {
                int itemIndex = totalCount - remainingCount + i;
                StartCoroutine(AnimateHarvestedItem(cropType, startPosition, i * _delayBetweenItems, itemIndex, totalCount, batchComplete));
            }
            
            remainingCount -= currentBatchSize;
            
            // Wait for this batch to complete before starting the next one
            yield return new WaitForSeconds(_animationDuration + (currentBatchSize * _delayBetweenItems));
        }
    }
    
    private IEnumerator AnimateHarvestedItem(CropType cropType, Vector3 startPosition, float delay, int itemIndex, int totalCount, Action onAnimationComplete = null)
    {
        _activeAnimationCount++;
        
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }
        
        GameObject item = GetFromPool(cropType);
        
        if (item == null)
        {
            _activeAnimationCount--;
            onAnimationComplete?.Invoke();
            yield break;
        }
        
        // Reset item state
        item.transform.position = startPosition;
        item.transform.rotation = Quaternion.identity;
        
        // Get original scale
        OriginalScale scaleComponent = item.GetComponent<OriginalScale>();
        if (scaleComponent != null)
        {
            item.transform.localScale = scaleComponent.originalScale * 0.1f; // Start small
        }
        
        item.SetActive(true);
        
        // Calculate a random offset direction for multiple items
        Vector3 randomOffset = Vector3.zero;
        if (totalCount > 1)
        {
            float angle = (360f / totalCount) * itemIndex;
            if (_useStaggeredArrival)
            {
                angle += Random.Range(-15f, 15f); // Add some randomness
            }
            
            float radius = 0.5f + (0.2f * itemIndex % 3); // Vary the radius
            randomOffset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius
            );
        }
        
        Vector3 targetPosition = startPosition + randomOffset + Vector3.up * 0.1f;
        
        // Create a sequence of animations
        Sequence sequence = DOTween.Sequence();
        
        // Scale up
        sequence.Join(item.transform.DOScale(scaleComponent != null ? scaleComponent.originalScale : Vector3.one, _animationDuration * 0.5f).SetEase(Ease.OutBack));
        
        // Jump up and to target position
        sequence.Join(item.transform.DOJump(targetPosition, _jumpHeight, 1, _animationDuration).SetEase(_jumpEase));
        
        // Rotate
        sequence.Join(item.transform.DORotate(new Vector3(0, _rotationAmount, 0), _animationDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
        
        // Wait for animation to complete
        yield return sequence.WaitForCompletion();
        
        // Small bounce at the end
        sequence = DOTween.Sequence();
        sequence.Append(item.transform.DOScale(
            scaleComponent != null ? scaleComponent.originalScale * 1.1f : Vector3.one * 1.1f, 
            0.1f).SetEase(Ease.OutQuad));
        sequence.Append(item.transform.DOScale(
            scaleComponent != null ? scaleComponent.originalScale : Vector3.one, 
            0.1f).SetEase(Ease.OutQuad));
        
        yield return sequence.WaitForCompletion();
        
        // Wait a moment before returning to pool
        yield return new WaitForSeconds(0.5f);
        
        ReturnToPool(item, cropType);
        _activeAnimationCount--;
        
        onAnimationComplete?.Invoke();
    }
    
    private class OriginalScale : MonoBehaviour
    {
        public Vector3 originalScale;
    }
    #endregion
    
    #region Public Methods
    public GameObject GetFromPool(CropType cropType)
    {
        if (!_itemPools.ContainsKey(cropType) || _itemPools[cropType].Count == 0)
        {
            // If pool doesn't exist or is empty, create a new item
            if (!_itemPools.ContainsKey(cropType))
            {
                _itemPools[cropType] = new Queue<GameObject>();
            }
            
            GameObject newItem = CreateNewItem(cropType);
            if (newItem != null)
            {
                return newItem;
            }
            else
            {
                return null;
            }
        }
        
        // Get item from pool
        GameObject item = _itemPools[cropType].Dequeue();
        return item;
    }
    
    public void ReturnToPool(GameObject item, CropType cropType)
    {
        if (item == null)
            return;
        
        // Reset item state
        item.SetActive(false);
        item.transform.SetParent(_poolParent);
        
        // Return to appropriate pool
        if (!_itemPools.ContainsKey(cropType))
        {
            _itemPools[cropType] = new Queue<GameObject>();
        }
        
        _itemPools[cropType].Enqueue(item);
    }
    
    public void SpawnHarvestedItems(CropType cropType, Vector3 startPosition, int count = 1, Action onComplete = null)
    {
        if (count <= 0)
        {
            onComplete?.Invoke();
            return;
        }
        
        if (count > _maxSimultaneousItems)
        {
            // For large numbers of items, spawn in batches
            StartCoroutine(SpawnItemsInBatches(cropType, startPosition, count, onComplete));
        }
        else
        {
            // For small numbers, spawn all at once with delays
            int completedCount = 0;
            
            Action itemComplete = () => {
                completedCount++;
                if (completedCount >= count)
                {
                    onComplete?.Invoke();
                }
            };
            
            for (int i = 0; i < count; i++)
            {
                StartCoroutine(AnimateHarvestedItem(cropType, startPosition, i * _delayBetweenItems, i, count, itemComplete));
            }
        }
    }
    
    public void TestHarvestAnimation(CropType cropType, int count)
    {
        Vector3 testPosition = transform.position + Vector3.up;
        SpawnHarvestedItems(cropType, testPosition, count, () => {
            Debug.Log($"Test animation complete for {count} items of type {cropType}");
        });
    }
    #endregion
} 