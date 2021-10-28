using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class Tile
{
    public GameObject theTile;
    public float creationTime;
    public float[,] heightMap;

    public Tile(GameObject t, float ct, float[,] hMap)
    {
        theTile = t;
        creationTime = ct;
        heightMap = hMap;
    }
}

public class LevelGeneration : MonoBehaviour
{
    // Prefabs for generation
    [SerializeField]
    private GameObject tilePrefab;

    [SerializeField]
    private Transform playerTransform;

    // Mesh settings
    [SerializeField]
    private float sidePlaneLength = 25f;

    [SerializeField]
    private int meshScale = 4;

    // Mesh Data
    private Mesh newMesh;

    private float _sizeX, _sizeZ;

    // Level settings
    [SerializeField]
    private float radiusTiles = 2.0f;

    [SerializeField]
    private int updateRadius = 1;

    // Level data
    private Vector3 _levelOrigin, tilePos, _origin = Vector3.zero;

    // Player position data
    private int playerMoveX, playerMoveZ;

    private int playerTileX, playerTileZ;

    // Tile Management
    private Hashtable tiles = new Hashtable();

    private Tile newTile;
    private float updateTime;
    private GameObject tile;
    private string tilename;

    // Tile Pool
    private Queue<GameObject> tilePool = new Queue<GameObject>();

    private float tilePoolSize;

    // Tile spawn
    private GameObject objToSpawn;

    private IPoolInterface pooledObj;

    // Object data
    [SerializeField]
    private GameObject enemy, enemyV1, tree;

    [SerializeField]
    private int count;

    private void Start()
    {
        _levelOrigin = playerTransform.position;

        tilePoolSize = Mathf.Pow(3 * radiusTiles, 2.0f);

        // Create tile mesh
        tilePrefab.GetComponent<MeshFilter>().sharedMesh = createMesh((int)sidePlaneLength, (int)sidePlaneLength);

        _sizeX = newMesh.bounds.size.x;
        _sizeZ = newMesh.bounds.size.z;

        // Create tile pool
        for (float z = 0; z < tilePoolSize; z++)
        {
            GameObject t = (GameObject)Instantiate(tilePrefab, _origin, Quaternion.identity);
            t.SetActive(false);
            tilePool.Enqueue(t);
        }

        tileGen();
    }

    private void LateUpdate()
    {
        // How far the player has moved from last update
        playerMoveX = (int)(playerTransform.position.x - _levelOrigin.x);
        playerMoveZ = (int)(playerTransform.position.z - _levelOrigin.z);

        // Load new tiles
        if (Mathf.Abs(playerMoveX) >= updateRadius * sidePlaneLength || Mathf.Abs(playerMoveZ) >= updateRadius * sidePlaneLength)
        {
            playerTileX = (int)(Mathf.Floor(playerTransform.position.x / _sizeX) * _sizeX);
            playerTileZ = (int)(Mathf.Floor(playerTransform.position.z / _sizeZ) * _sizeZ);

            tileGen();
            //objectSpawn(1.0f, enemy,0,2);
            objectSpawn(1.0f, enemyV1, 0, 2);
        }
    }

    private void tileGen()
    {
        updateTime = Time.realtimeSinceStartup;

        for (float x = -radiusTiles; x < radiusTiles; x++)
        {
            for (float z = -radiusTiles; z < radiusTiles; z++)
            {
                // Get unique tile name based on position
                tilePos.x = (float)(x * _sizeX + playerTileX);
                tilePos.y = 0f;
                tilePos.z = (float)(z * _sizeZ + playerTileZ);

                tilename = "Tile_" + ((int)(tilePos.x)).ToString() + "_" + ((int)(tilePos.z)).ToString();

                // If empty then intialise and store in Hashtable
                if (!tiles.ContainsKey(tilename))
                {
                    float[,] heightMap;
                    tile = spawnTile(tilePos, out heightMap);
                    tile.name = tilename;
                    newTile = new Tile(tile, updateTime, heightMap);
                    tiles.Add(tilename, newTile);
                }
                // Else update the tiles last updateTime
                else
                {
                    (tiles[tilename] as Tile).creationTime = updateTime;
                }
            }
        }

        // Update terrain hashtable and add old tiles back to queue
        Hashtable newTerrain = new Hashtable();
        foreach (Tile tls in tiles.Values)
        {
            if (tls.creationTime != updateTime)
            {
                tilePool.Enqueue(tls.theTile);
                tls.theTile.SetActive(false);
            }
            else
            {
                newTerrain.Add(tls.theTile.name, tls);
            }
        }

        //Update origin of movement
        _levelOrigin = playerTransform.position;
        tiles = newTerrain;
    }

    private void objectSpawn(float radius, GameObject prefab, float min, float max)
    {
        for (float x = -radius; x < radius; x++)
        {
            for (float z = -radius; z < radius; z++)
            {
                // Get unique tile name based on position
                tilePos.x = (float)(x * _sizeX + playerTileX);
                tilePos.y = 0f;
                tilePos.z = (float)(z * _sizeZ + playerTileZ);

                tilename = "Tile_" + ((int)(tilePos.x)).ToString() + "_" + ((int)(tilePos.z)).ToString();

                int amount = (int)Random.Range(min, max);

                (tiles[tilename] as Tile).theTile.GetComponent<IPoolInterface>().genObjects(prefab, amount);
            }
        }
    }

    public GameObject spawnTile(Vector3 pos, out float[,] hMap)
    {
        // Get tiles
        objToSpawn = tilePool.Dequeue();

        // Position and show
        objToSpawn.transform.position = pos;
        objToSpawn.SetActive(true);

        // Get pool interface
        pooledObj = objToSpawn.GetComponent<IPoolInterface>();

        // Generate terrain and trees, return heightmap of tile
        hMap = pooledObj.generateTerrain();
        pooledObj.genObjects(tree, 10);
        pooledObj.genObjects(enemy, 1);

        return objToSpawn;
    }

    private Mesh createMesh(int width, int height)
    {
        newMesh = new Mesh();
        newMesh.name = "ScriptedMesh";

        Vector3[] vertices = new Vector3[(width + 1) * (height + 1)];

        for (int i = 0, z = 0; z <= width; z++)
        {
            for (int x = 0; x <= height; x++)
            {
                vertices[i] = new Vector3(x * meshScale, 0, z * meshScale);
                i++;
            }
        }

        int[] triangles = new int[width * height * 6];

        for (int ti = 0, vi = 0, y = 0; y < height; y++, vi++)
        {
            for (int x = 0; x < width; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + width + 1;
                triangles[ti + 5] = vi + width + 2;
            }
        }
        newMesh.MarkDynamic();
        newMesh.vertices = vertices;
        newMesh.triangles = triangles;
        newMesh.RecalculateBounds();
        newMesh.RecalculateNormals();
        newMesh.OptimizeIndexBuffers();

        return newMesh;
    }
}