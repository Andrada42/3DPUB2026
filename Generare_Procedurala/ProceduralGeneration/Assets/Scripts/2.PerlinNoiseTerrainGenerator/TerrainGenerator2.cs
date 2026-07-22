using System.Runtime.CompilerServices;
using TMPro.EditorUtilities;
using UnityEngine;

public class TerrainGenerator2 : MonoBehaviour
{
    private Vector2 octaveOffset;

    [Header("Seed")]
    public bool generateSeed = true;
    public int seed = 4247;

    [Header("Grid")]
    [Range(1, 6)] public int renderDistance = 3;

    [Header("Chunk")]
    [Range(16, 512)] public int width = 50;
    [Range(16, 512)] public int height = 50;
    [Range(0.2f, 3.0f)] public float cellSize = 2f;
    [Range(1, 512)] public float heightMultiplier = 15f;

    [Header("Biome Thresholds")]
    [Range(0.1f, 1.0f)] public float waterLevel = 0.35f;    // water between [0.0f, waterLevel]
    [Range(0.1f, 1.0f)] public float sandLevel = 0.4f;
    [Range(0.1f, 1.0f)] public float grassLevel = 0.65f;
    [Range(0.1f, 1.0f)] public float rockLevel = 0.8f;

    [Header("Perlin Noise")]
    [Range(1, 8)] public int octaves = 4;
    [Range(1, 50)] public int scale = 25;                   // zoom level
    public float lacunarity = 2.0f;
    public float persistence = 0.5f;

    [Header("Material")]
    public Material terrainMaterial;

    [Header("Biome Colors")]
    public Color water = new Color(0.15f, 0.35f, 0.75f);
    public Color sand = new Color(0.85f, 0.8f, 0.55f);
    public Color grass = new Color(0.25f, 0.65f, 0.25f);
    public Color rock = new Color(0.45f, 0.4f, 0.35f);
    public Color snow = Color.white;

    [Header("Scenery Props")]
    public GameObject shellPrefab;
    public GameObject treePrefab;
    public GameObject coalPrefab;
    [Range(0.0f, 0.5f)] public float shellDensity = 0.10f;   // Sansa de a aparea pe un vertex
    [Range(0.0f, 0.5f)] public float treeDensity = 0.10f;   // 10%
    [Range(0.0f, 0.5f)] public float coalDensity = 0.07f;   // 4%


    private void OnValidate()   // apelata la orice modificare in Inspector
    {
        if (terrainMaterial == null)
        {
            Debug.LogError($"Object '{gameObject.name}' doesn't have the 'terrainMaterial' field assigned!", this);
        }
    }


    void Start()
    {
        if (generateSeed)
            seed = Random.Range(0, 10000);
        GenerateMap();
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (generateSeed)
                seed = Random.Range(0, 10000);
            GenerateMap();
        }
    }


    public void GenerateMap()
    {
        CleanChunks();

        System.Random rng = new System.Random(seed);
        octaveOffset = new Vector2(rng.Next(-10000, 10000), rng.Next(-10000, 10000));


        for (int z = 0; z < renderDistance; z++)
            for (int x = 0; x < renderDistance; x++)
            {
                // Coordonatele chunk-ului in raport cu gridul (distanta dintre 2 chunkuri adiacente = 1)
                float gridX = x - (renderDistance - 1) / 2f;
                float gridZ = z - (renderDistance - 1) / 2f;

                // Coordonatele chunk-ului in raport cu lumea
                float worldPosX = gridX * width * cellSize;
                float worldPosZ = gridZ * height * cellSize;

                GenerateChunk(x + z * renderDistance, worldPosX, worldPosZ);
            }
    }

    public void CleanChunks()
    {
        // Stergem chunk-urile randate in trecut = toti copiii
        foreach (Transform childTransform in transform) // transform = componenta obiectului curent, ce se comporta ca o lista ce contine componentele transform ale copiilor
        {
            Destroy(childTransform.gameObject);         // .gameObject deoarece ne trebuie obiectul, nu componenta sa
        }
    }

    public void GenerateChunk(int id, float worldPosX, float worldPosZ)
    {
        // Cream un copil
        GameObject chunkObj = new GameObject($"Chunk_{id}");
        chunkObj.transform.parent = this.transform;
        chunkObj.transform.localPosition = new Vector3(worldPosX, 0, worldPosZ);



        Mesh mesh = new Mesh();
        mesh.name = $"ProceduralTerrain_{id}";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;    // Pt a permite multi vertcsi

        int vertexCountX = width + 1;
        int vertexCountZ = height + 1;

        Vector3[] vertices = new Vector3[vertexCountX * vertexCountZ];  // vector de pozitii
        Vector2[] uvs = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[width * height * 6];

        for (int z = 0; z < vertexCountZ; z++)
        {
            for (int x = 0; x < vertexCountX; x++)
            {
                float globalX = worldPosX + x * cellSize;
                float globalZ = worldPosZ + z * cellSize;

                float noiseValue = GetPerlinNoise(globalX, globalZ);
                float y = noiseValue * heightMultiplier;

                int index = z * vertexCountX + x;
                //vertices[index] = new Vector3(x * cellSize, y, z * cellSize);     // coordonatele punctului (considerand coltul stanga-jos originea => x = z = 0)
                float localX = (x - width / 2f) * cellSize;
                float localZ = (z - height / 2f) * cellSize;
                Vector3 localPos = new Vector3(localX, y, localZ);
                
                vertices[index] = localPos;                                         // coordonatele punctului (considerand centrul originea => localX = localZ = 0)
                uvs[index] = new Vector2((float)x / width, (float)z / height);
                colors[index] = GetBiomeColor(noiseValue).linear;                   // sRGB/Gamma -> Linear (Deoarece proiectul foloseste Linear Rendering)

                PlaceScenery(chunkObj.transform, localPos, noiseValue);
            }
        }

        int ct = 0;
        for (int z = 0; z < height; z++)        // creste inainte (sus dar nu in inaltime) ^
        {
            for (int x = 0; x < width; x++)     // creste spre dreapta >
            {
                int bottomLeft = z * vertexCountX + x;      // (0, 0)
                int bottomRight = bottomLeft + 1;           // (1, 0)
                int topLeft = (z + 1) * vertexCountX + x;   // (0, 1)
                int topRight = topLeft + 1;                 // (1, 1)

                // Clockwhise => vedem fata de sus a obiectului
                triangles[ct++] = bottomLeft;
                triangles[ct++] = topLeft;
                triangles[ct++] = topRight;

                triangles[ct++] = topRight;
                triangles[ct++] = bottomRight;
                triangles[ct++] = bottomLeft;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.RecalculateNormals();  // normalele la suprafata
        mesh.RecalculateBounds();   // limitele mesh-ului


        // Adaugam componentele necesare copilului
        MeshFilter meshFilter = chunkObj.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = chunkObj.AddComponent<MeshRenderer>();
        meshRenderer.material = terrainMaterial;

        MeshCollider meshCollider = chunkObj.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    public float GetPerlinNoise(float x, float y)   // returns values between [0.0f, 1.0f]
    {
        float frequency = 1;
        float amplitude = 1;
        float noiseHeight = 0;
        float amplitudeSum = 0;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x + octaveOffset.x) / scale * frequency;
            float sampleY = (y + octaveOffset.y) / scale * frequency;
            // scale: fara el, Mathf.PerlinNoise(sampleX, sampleY) primeste niste valori intregi consecutive (obtinute din x si y: 1, 2, ...), cu o distanta prea mare intre ele (o unitate)
            // => zgomot de purici in loc de tranzitii line.

            // frequency: 1, 2, 4, 8
            // => valoarea devine mai mare
            // => pe masura ce trecem la octava urmatoare, facem opusul lui scale pentru a ne apropia de neregularitati cat mai drastice (zgomot de purici eventual)

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
            // Mathf.PerlinNoise(x, y) returneaza valoarea [0.0f, 1.0f] corespunzatoare coordonatelor (x, y) de pe harta Perlin
            // Aceasta harta e infinita dar fixa => pt a avea harti diferite e nevoie sa ne mutam pe harta, prin octaveOffset

            noiseHeight += perlinValue * amplitude;
            // amplitude: 1, 0.5, 0.25, 0.125
            // => prima octava e baza (se aduna o valoare mare), ultima adauga doar detalii (o valoare foarte mica)

            amplitudeSum += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return noiseHeight / amplitudeSum;
    }

    public Color GetBiomeColor(float noiseValue)
    {
        if (noiseValue < waterLevel) return water;
        if (noiseValue < sandLevel) return sand;
        if (noiseValue < grassLevel) return grass;
        if (noiseValue < rockLevel) return rock;
        return snow;
    }

    public void PlaceScenery(Transform parentChunk, Vector3 localPos, float noiseValue)
    {
        if (noiseValue > waterLevel && noiseValue < sandLevel)
        {
            if (Random.value < shellDensity)
                CreateObject(shellPrefab, parentChunk, localPos);
            return;
        }
        
        if (noiseValue > sandLevel && noiseValue < grassLevel)
        {
            if (Random.value < treeDensity)
                CreateObject(treePrefab, parentChunk, localPos);
        }
        if (noiseValue > grassLevel && noiseValue < rockLevel)
        {
            if (Random.value < coalDensity)
                CreateObject(coalPrefab, parentChunk, localPos);
        }
    }

    private void CreateObject(GameObject prefab, Transform parent, Vector3 localPos)
    {
        GameObject obj = Instantiate(prefab, parent);
        obj.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        float randomScale = Random.Range(1.0f, 1.3f);
        obj.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
        float yOffset = randomScale / 2f;
        yOffset -= 0.7f;
        obj.transform.localPosition = new  Vector3(localPos.x, localPos.y + yOffset, localPos.z);
    }
}
