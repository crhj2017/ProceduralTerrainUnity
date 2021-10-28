using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New ObjectPoolList", menuName = "Object Pools", order = 51)]
public class ObjectPoolList : ScriptableObject
{
    [SerializeField]
    private List<ObjectPool> objToPool = new List<ObjectPool>();

    private Dictionary<GameObject, Queue<GameObject>> objPools;

    // Cached data
    GameObject instance, pooledInst;
    Queue<GameObject> poolQueue;

    /*
     Create temporary queue of items in create pool and the add that to the dictionary of obj pools.
     Create method for getting and adding object back

         */

    public void Initialise()
    {
        objPools = new Dictionary<GameObject, Queue<GameObject>>();
        for (int x = 0; x < objToPool.Count; x++)
        {
            CreatePool(objToPool[x].prefab, objToPool[x].poolSize);
        }
    }

    public void CreatePool(GameObject _prefab, int _poolSize)
    {
        poolQueue = new Queue<GameObject>();

        for (int x = 0; x < _poolSize; x++)
        {
            instance = (GameObject)Instantiate(_prefab);
            instance.SetActive(false);

            poolQueue.Enqueue(instance);
        }

        objPools.Add(_prefab, poolQueue);
    }

    public GameObject getPool(GameObject _prefab)
    {
        if (objPools.ContainsKey(_prefab))
        {
            pooledInst = objPools[_prefab].Dequeue();
            pooledInst.SetActive(true);
            objPools[_prefab].Enqueue(pooledInst);
            return pooledInst;
        }
        return null;
    }
}

[System.Serializable]
public class ObjectPool
{
    public GameObject prefab;
    public int poolSize;
}