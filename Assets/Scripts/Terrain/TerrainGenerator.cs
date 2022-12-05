using System;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int depth = 5;
    public int width = 256;
    public int height = 256;

    public float scale = 5.0f;

    public float offsetX = 100.0f;
    public float offsetY = 100.0f;

    void Start()
    {
        offsetX = UnityEngine.Random.Range(0.0f, 9999.9f);
        offsetY = UnityEngine.Random.Range(0.0f, 9999.9f);
    }

    void Update()
    {
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }

    TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;

        terrainData.size = new Vector3(width, depth, height);

        terrainData.SetHeights(0, 0, GenerateHeights());

        return terrainData;
    }

    float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = CalculateHeight(x, y);
            }
        }

        return heights;
    }

    float CalculateHeight(int x, int y)
    {
        float xCoord = (float)x / width * scale + offsetX;
        float yCoord = (float)y / height * scale + offsetY;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }
}
