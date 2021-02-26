using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TerrainTextureGenerator
{
    public TerrainTextureGenerator()
    {
    }

    public float[,,] GenerateSplatmap()
    {
        Texture2D bitmap = Utility.LoadPNG(@"C:\Users\Moses\Desktop\heightmapper-alpha-1613486714451.png");

        int bitmapHeight = bitmap.height;
        int bitmapWidth = bitmap.width;

        int WATER = 0;
        int LEAF_GROUND = 1;
        int GRASS = 2;
        int ROCKY_GROUND = 3;

        float[,,] splatmapData = new float[bitmapWidth, bitmapHeight, 4];

        for (int y = 0; y < bitmapHeight; y++)
        {
            for (int x = 0; x < bitmapWidth; x++)
            {
                Color c = bitmap.GetPixel(y, x); // Need to fecth the pixels reversed due to x, y flip for alphamaps

                if (c.r < .18f && c.g < .18f && c.b < .18f)
                {
                    splatmapData[x, y, WATER] = 1;
                    splatmapData[x, y, LEAF_GROUND] = 0;
                    splatmapData[x, y, GRASS] = 0;
                    splatmapData[x, y, ROCKY_GROUND] = 0;
                }
                else
                {
                    splatmapData[x, y, WATER] = 0;
                    splatmapData[x, y, LEAF_GROUND] = .05f;
                    splatmapData[x, y, GRASS] = .05f;
                    splatmapData[x, y, ROCKY_GROUND] = .9f;
                }
            }
        }

        return splatmapData;
    }
}