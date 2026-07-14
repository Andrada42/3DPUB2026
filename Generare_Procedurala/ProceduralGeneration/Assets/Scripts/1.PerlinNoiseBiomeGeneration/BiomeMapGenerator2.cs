using Unity.VisualScripting;
using UnityEngine;

public class BiomeMapGenerator2 : MonoBehaviour
{
    // private => nu ai acces din editor
    private Texture2D texture;
    private Vector2 altitudeOffset;
    private Vector2 moistureOffset;


    // public => ai acces din editor
    [Header("Renderer")]
    public Renderer targetRenderer;

    [Header("Seed")]
    public bool generateSeeds = true;
    public int altitudeSeed = 4247;
    public int moistureSeed = 4742;

    [Header("Grid Dimensions")]
    [Range(16, 512)] public int width = 256;
    [Range(16, 512)] public int height = 256;

    [Header("Perlin Noise")]
    [Range(1, 8)] public int octaves = 4;
    [Range(1, 50)] public int scale = 25;                   // zoom level
    public float lacunarity = 2.0f;
    public float persistence = 0.5f;

    [Header("Biome Colors")]
    public Color tundra = new Color(0.75f, 0.88f, 0.88f);
    public Color borealForest = new Color(0.55f, 0.73f, 0.47f);
    public Color temperateSeasonalforest = new Color(0.48f, 0.67f, 0.37f);
    public Color temperateRainforest = new Color(0.38f, 0.62f, 0.38f);
    public Color tropicalRainforest = new Color(0.12f, 0.44f, 0.17f);
    public Color tropicalSeasonalforest = new Color(0.62f, 0.58f, 0.13f);
    public Color woodland = new Color(0.8f, 0.43f, 0.22f);
    public Color temperateDesert = new Color(0.92f, 0.8f, 0.44f);
    public Color subtropicalDesert = new Color(0.85f, 0.7f, 0.32f);


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (generateSeeds)
        {
            altitudeSeed = Random.Range(0, 10000);
            moistureSeed = Random.Range(0, 10000);
        }
        GenerateMap();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (generateSeeds)
            {
                altitudeSeed = Random.Range(0, 10000);
                moistureSeed = Random.Range(0, 10000);
            }
            GenerateMap();
        }
    }


    public void GenerateMap()
    {
        texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;      // face textura pixelata
        texture.wrapMode = TextureWrapMode.Clamp;   // repeta ultimul rand pe pixeli de pe margini in loc de a face tiling

        System.Random altitudeRng = new System.Random(altitudeSeed);
        altitudeOffset = new Vector2(altitudeRng.Next(-10000, 10000), altitudeRng.Next(-10000, 10000));
        System.Random moistureRng = new System.Random(moistureSeed);
        moistureOffset = new Vector2(moistureRng.Next(-10000, 10000), moistureRng.Next(-10000, 10000));

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float altitude = GetPerlinNoise(x, y, altitudeOffset);
                float moisture = GetPerlinNoise(x, y, moistureOffset);
                Color color = GetBiomeColor(altitude, moisture);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        targetRenderer.material.mainTexture = texture;
    }

    public float GetPerlinNoise(float x, float y, Vector2 offset)   // returns values between [0.0f, 1.0f]
    {
        float frequency = 1;
        float amplitude = 1;
        float noiseHeight = 0;
        float amplitudeSum = 0;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x + offset.x) / scale * frequency;
            float sampleY = (y + offset.y) / scale * frequency;
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
    public Color GetBiomeColor(float altitude, float moisture)  // altitudine mare => temperatura mica
    {
        float temperature = 1.0f - altitude;

        if (temperature < 0.25f)
        {
            return tundra;
        }
        if (temperature < 0.35f)
        {
            if (moisture < 0.08f) return temperateDesert;
            if (moisture < 0.12f) return woodland;
            if (moisture < 0.5f) return borealForest;
            return borealForest;
        }

        if (temperature < 0.75f)
        {
            if (moisture < 0.19f)
            {
                return temperateDesert;
            }
            if (moisture < 0.25f)
            {
                return woodland;
            }
            if (moisture < 0.5f)
            {
                return temperateSeasonalforest;
            }
            return temperateRainforest;
        }
        else
        {
            if (moisture < 0.3f)
            {
                return subtropicalDesert;
            }
            if (moisture < 0.6f)
            {
                return tropicalSeasonalforest;
            }
            return tropicalRainforest;
        }
    }
}
