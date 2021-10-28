using UnityEngine;

public interface IPoolInterface
{
    float[,] generateTerrain();

    void genObjects(GameObject prefab, int amount);
}