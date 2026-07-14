using UnityEngine;

public class BiomeMapGenerator3 : MonoBehaviour
{
    // private => nu ai acces din editor
    private Texture2D biomeTexture;
    private Texture2D rawNoiseTexture;
    private Vector2 octaveOffset;


    // public => ai acces din editor
    [Header("Renderer")]
    public Renderer biomeRenderer;
    public Renderer rawNoiseRenderer;

    [Header("Seed")]
    public bool generateSeed = true;
    public int seed = 4247;

    [Header("Grid Dimensions")]
    [Range(16, 512)] public int width = 256;
    [Range(16, 512)] public int height = 256;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (generateSeed)
            seed = Random.Range(0, 10000);
        GenerateMap();
    }

    // Update is called once per frame
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
        biomeTexture = CreateEmptyTexture();
        rawNoiseTexture = CreateEmptyTexture();

        System.Random rng = new System.Random(seed);
        octaveOffset = new Vector2(rng.Next(-10000, 10000), rng.Next(-10000, 10000));


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float noiseValue = GetPerlinNoise(x, y);
                Color biomeColor = GetBiomeColor(noiseValue);
                biomeTexture.SetPixel(x, y, biomeColor);

                Color grayscaleColor = new Color(noiseValue, noiseValue, noiseValue);
                rawNoiseTexture.SetPixel(x, y, grayscaleColor);
            }
        }

        biomeTexture.Apply();
        biomeRenderer.material.mainTexture = biomeTexture;

        rawNoiseTexture.Apply();
        rawNoiseRenderer.material.mainTexture = rawNoiseTexture;
    }

    public Texture2D CreateEmptyTexture()
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;      // face textura pixelata
        texture.wrapMode = TextureWrapMode.Clamp;   // repeta ultimul rand pe pixeli de pe margini in loc de a face tiling
        return texture;
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
