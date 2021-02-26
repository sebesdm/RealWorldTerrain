using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TerrainHeightGenerator
{
    public float terrainScale = 1f;
    public TerrainHeightGenerator(float terrainScale)
    {
        this.terrainScale = terrainScale;
    }

    public float[,] GenerateHeights()
    {
        Texture2D bitmap = Utility.LoadPNG(@"C:\Users\Moses\Desktop\heightmapper-1613486714451.png");

        int bitmapHeight = bitmap.height;
        int bitmapWidth = bitmap.width;

        float[,] heights = new float[bitmapHeight, bitmapWidth];

        for (int i = 0; i < bitmapHeight; i++)
        {
            for (int j = 0; j < bitmapWidth; j++)
            {
                Color c = bitmap.GetPixel(j, i); // This needs to be reversed!!
                heights[i, j] = c.r * terrainScale;
            }

        }

        return heights;
    }
}