using UnityEngine;

// Class for noise Waves
[System.Serializable]
public class Wave
{
    public float seed;
    public float frequency;
    public float amplitude;
}

public class TileGeneration : MonoBehaviour, IPoolInterface
{
    // Tile Settings

    [SerializeField]
    private float detailScale; // Increase to spread out the waves

    [SerializeField]
    public float heightScale; // Increase to change max height

    [SerializeField]
    private Wave[] waves;

    [SerializeField]
    private AnimationCurve heightCurve;

    [SerializeField]
    private Gradient gradient;

    // Mesh variables

    private Mesh _mesh;
    private MeshCollider _meshCol;
    private Vector3 _position;
    private int _sideVerts, meshScale;

    // Vertex data

    private Vector3[] meshVertices;
    private Color[] colors;
    private float[,] heightMap;

    // Noise data

    private float sampleX, sampleZ, noise, normalization;

    // Vertex traversal cached variables

    private int vertX, vertZ, neighborZBegin, neighborZEnd, neighborXBegin, neighborXEnd;
    private float neighborRadius = 1.0f;

    // Reference to pool system

    [SerializeField]
    private ObjectPoolList objPools;

    private void Awake()
    {
        _mesh = GetComponent<MeshFilter>().mesh;
        _meshCol = GetComponent<MeshCollider>();
        meshVertices = _mesh.vertices;
        colors = new Color[meshVertices.Length];

        _sideVerts = (int)Mathf.Sqrt(meshVertices.Length);
        meshScale = (int)(_mesh.bounds.size.x / (_sideVerts - 1));
        heightMap = new float[_sideVerts, _sideVerts];

        objPools.Initialise();
    }

    public void flattenSection(int zInitIndex, int xInitIndex, float radius, float height)
    {
        neighborZBegin = (int)Mathf.Max(0, zInitIndex - radius);
        neighborZEnd = (int)Mathf.Min(_sideVerts - 1, zInitIndex + radius);
        neighborXBegin = (int)Mathf.Max(0, xInitIndex - radius);
        neighborXEnd = (int)Mathf.Min(_sideVerts - 1, xInitIndex + radius);

        for (int zIndex = neighborZBegin; zIndex <= neighborZEnd; zIndex++)
        {
            for (int xIndex = neighborXBegin; xIndex <= neighborXEnd; xIndex++)
            {
                meshVertices[zIndex * _sideVerts + xIndex].y = height;
            }
        }

        // Update mesh
        _mesh.MarkDynamic();

        _mesh.vertices = meshVertices;
        _mesh.colors = colors;

        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();

        _meshCol.sharedMesh = _mesh;
    }

    public void genObjects(GameObject prefab, int amount)
    {
        // Invalid object
        if (objPools.getPool(prefab) == null)
        {
            return;
        }

        _position = this.transform.position;

        // Dont place object on the edge
        int sideBuffer = 2;

        float seed = Mathf.PerlinNoise(_position.x, _position.z);
        //Debug.Log(seed);

        //Search

        for (int zIndex = sideBuffer; zIndex < _sideVerts - sideBuffer; zIndex++)
        {
            for (int xIndex = sideBuffer; xIndex < _sideVerts - sideBuffer; xIndex++)
            {
                float heightValue = heightMap[zIndex, xIndex];

                // if the current tree noise value is the maximum one, place a tree in this location

                if (heightValue / heightScale < 0.6f && heightValue / heightScale > 0.2f && (zIndex * _sideVerts + xIndex) % 80 == 0)
                {
                    // Flatten given area around object

                    float radius = 1.0f;
                    //flattenSection(zIndex, xIndex, radius, maxValue);

                    // Get object from pool

                    GameObject objInst = objPools.getPool(prefab);

                    // Position object

                    Vector3 objPosition = new Vector3(xIndex * meshScale, heightMap[zIndex, xIndex], zIndex * meshScale);
                    objInst.transform.position = objPosition + _position;
                    objInst.transform.SetParent(this.transform);
                }
            }
        }
    }

    public float[,] generateTerrain()
    {
        _position = this.transform.position;
        // Generate height value for each vert based on Perlin noise
        for (int z = 0, v = 0; z < _sideVerts; z++)
        {
            for (int x = 0; x < _sideVerts; x++, v++)
            {
                sampleX = (meshVertices[v].x + _position.x) / detailScale;
                sampleZ = (meshVertices[v].z + _position.z) / detailScale;
                noise = 0f;
                normalization = 0f;
                foreach (Wave wave in waves)
                {
                    // generate noise value using PerlinNoise for a given Wave
                    noise += wave.amplitude * Mathf.PerlinNoise(sampleX * wave.frequency + wave.seed, sampleZ * wave.frequency + wave.seed);
                    normalization += wave.amplitude;
                }

                // normalize the noise value so that it is within 0 and 1
                noise /= normalization;

                // Set colors for shaders
                colors[v] = this.gradient.Evaluate(noise);

                // Set vertex height with noise and scale
                meshVertices[z * _sideVerts + x].y = this.heightCurve.Evaluate(noise) * heightScale;

                vertX = (int)(meshVertices[v].x / meshScale);
                vertZ = (int)(meshVertices[v].z / meshScale);

                heightMap[vertZ, vertX] = meshVertices[z * _sideVerts + x].y;
            }
        }
        // Update mesh
        _mesh.MarkDynamic();

        _mesh.vertices = meshVertices;
        _mesh.colors = colors;

        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();

        _meshCol.sharedMesh = _mesh;

        return heightMap;
    }

    private void OnDrawGizmosSelected()
    {
        if (meshVertices == null || heightMap == null)
        {
            return;
        }
        Gizmos.color = Color.black;
        for (int i = 0; i < meshVertices.Length; i++)
        {
            Gizmos.DrawSphere(this.meshVertices[i] + _position, 0.1f);
        }
        Gizmos.color = Color.red;
        for (int z = 0; z < _sideVerts; z++)
        {
            for (int x = 0; x < _sideVerts; x++)
            {
                Vector3 pos = new Vector3(x * meshScale, this.heightMap[z, x], z * meshScale);
                Gizmos.DrawSphere(pos + _position, 0.2f);
            }
        }
    }
}