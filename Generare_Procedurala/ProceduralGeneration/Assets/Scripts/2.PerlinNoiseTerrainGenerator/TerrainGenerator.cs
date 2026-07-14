using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{

    private Mesh mesh;
    private Vector2 octaveOffset;


    [Header("Seed")]
    public bool generateSeed = true;
    public int seed = 4247;

    [Header("Grid Dimensions")]
    [Range(16, 512)] public int width = 256;
    [Range(16, 512)] public int height = 256;
    [Range(1, 512)] public float heightMultiplier = 15f;
    [Range(0.2f, 3.0f)] public float cellSize = 1f;

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

    [Header("Biome Colors")]
    public Color water = new Color(0.15f, 0.35f, 0.75f);
    public Color sand = new Color(0.85f, 0.8f, 0.55f);
    public Color grass = new Color(0.25f, 0.65f, 0.25f);
    public Color rock = new Color(0.45f, 0.4f, 0.35f);
    public Color snow = Color.white;


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
        System.Random rng = new System.Random(seed);
        octaveOffset = new Vector2(rng.Next(-10000, 10000), rng.Next(-10000, 10000));

        mesh = new Mesh();
        mesh.name = "ProceduralTerrain";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;


        int vertexCountX = width + 1;
        int vertexCountZ = height + 1;

        Vector3[] vertices = new Vector3[vertexCountX * vertexCountZ]; // vector de pozitii
        Vector2[] uvs = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[width * height * 6];

        for (int z = 0; z < vertexCountZ; z++)
        {
            for (int x = 0; x < vertexCountX; x++)
            {
                float noiseValue = GetPerlinNoise(x, z);
                float y = noiseValue * heightMultiplier;

                int index = z * vertexCountX + x;
                vertices[index] = new Vector3(x * cellSize, y, z * cellSize);   // coordonatele
                uvs[index] = new Vector2((float) x / width, (float) z / height);
                colors[index] = GetBiomeColor(noiseValue);
            }
        }

        int id = 0;
        for(int z = 0; z < height; z++)     // creste inainte (sus dar nu in inaltime) ^
        {
            for (int x = 0; x < width; x++) // creste spre dreapta >
            {
                int bottomLeft = z * vertexCountX + x;      // (0, 0)
                int bottomRight = bottomLeft + 1;           // (1, 0)
                int topLeft = (z + 1) * vertexCountX + x;   // (0, 1)
                int topRight = topLeft + 1;                 // (1, 1)

                // Clockwhise => vedem fata de sus a obiectului
                triangles[id++] = bottomLeft;
                triangles[id++] = topLeft;
                triangles[id++] = topRight;

                triangles[id++] = topRight;
                triangles[id++] = bottomRight;
                triangles[id++] = bottomLeft;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.RecalculateNormals();  // normalele la suprafata
        mesh.RecalculateBounds();   // limitele mesh-ului

        GetComponent<MeshFilter>().mesh = mesh;
        MeshCollider collider = GetComponent<MeshCollider>();
    
        if (collider == null)
            collider = gameObject.AddComponent<MeshCollider>();

        collider.sharedMesh = mesh;
    
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
        if (noiseValue < waterLevel)
            return water;

        if (noiseValue < sandLevel)
            return sand;

        if (noiseValue < grassLevel)
            return grass;

        if (noiseValue < rockLevel)
            return rock;

        return snow;
    }
}
