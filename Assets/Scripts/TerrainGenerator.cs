using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public float terrainScale = 1f;


    void Start()
    {
        Terrain terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        TerrainData terrainData = terrain.terrainData;
        int terrainDataWidth = terrainData.heightmapWidth;
        int terrainDataHeight = terrainData.heightmapHeight;
        float[,] heights = terrainData.GetHeights(0, 0, terrainDataWidth, terrainDataHeight);

        Texture2D bitmap = LoadPNG(@"C:\Users\Moses\Desktop\heightmapper-1613486714451.png");
        Texture2D alphamap = LoadPNG(@"C:\Users\Moses\Desktop\heightmapper-alpha-1613486714451.png");

        int bitmapHeight = bitmap.height;
        int bitmapWidth = bitmap.width;



        for (int i = 0; i < terrainDataWidth; i++)
        {
            for (int j = 0; j < terrainDataHeight; j++)
            {
                Color c = bitmap.GetPixel(j, i);

                heights[i, j] = c.r * terrainScale;
            }
        }

        terrainData.SetHeights(0, 0, heights);

        ApplyTexture(terrain, alphamap);
        //ApplyTrees(terrain, alphamap);
    }

    public static Texture2D LoadPNG(string filePath)
    {

        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }

    //void ApplyTrees(Terrain terrain, Texture2D alphamap)
    //{



    //    terrain.drawTreesAndFoliage = true;



    //    // Get a reference to the terrain data
    //    TerrainData terrainData = terrain.terrainData;

    //    // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
    //    float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

    //    List<TreeInstance> trees = new List<TreeInstance>();
    //    for (int y = 0; y < terrainData.alphamapHeight; y++)
    //    {
    //        for (int x = 0; x < terrainData.alphamapWidth; x++)
    //        {
    //            Color c = alphamap.GetPixel(y - 1, x - 6); // Need to fecth the pixels reversed due to x, y flip for alphamaps

    //            float y_01 = (float)y / (float)terrainData.alphamapHeight;
    //            float x_01 = (float)x / (float)terrainData.alphamapWidth;

    //            if (!(c.r < .18f && c.g < .18f && c.b < .18f))
    //            {
    //                if(y % 100 == 0 && x % 100 == 0)
    //                {
    //                    TreeInstance tree = new TreeInstance();
    //                    //tree.heightScale = 1;
    //                    //tree.widthScale = 1;
    //                    tree.prototypeIndex = 0;
    //                    tree.position = new Vector3(x, y, 0);
    //                    trees.Add(tree);


    //                }
    //            }


    //        }
    //    }

    //    terrain.terrainData.SetTreeInstances(trees.ToArray(), true);
    //}

    void ApplyTexture(Terrain terrain, Texture2D alphamap)
    {
        int WATER = 0;
        int LEAF_GROUND = 1;
        int GRASS = 2;
        int ROCKY_GROUND = 3;

        // Get a reference to the terrain data
        TerrainData terrainData = terrain.terrainData;

        // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Normalise x/y coordinates to range 0-1 
                float y_01 = (float)y / (float)terrainData.alphamapHeight;
                float x_01 = (float)x / (float)terrainData.alphamapWidth;

                // Sample the height at this location (note GetHeight expects int coordinates corresponding to locations in the heightmap array)
                float height = terrainData.GetHeight(Mathf.RoundToInt(y_01 * terrainData.heightmapHeight), Mathf.RoundToInt(x_01 * terrainData.heightmapWidth));

                // Calculate the normal of the terrain (note this is in normalised coordinates relative to the overall terrain dimensions)
                Vector3 normal = terrainData.GetInterpolatedNormal(y_01, x_01);

                // Calculate the steepness of the terrain
                float steepness = terrainData.GetSteepness(y_01, x_01);

                // Setup an array to record the mix of texture weights at this point
                float[] splatWeights = new float[terrainData.alphamapLayers];

                Color c = alphamap.GetPixel(y - 1, x - 6); // Need to fecth the pixels reversed due to x, y flip for alphamaps

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

                    if (height > 70f)
                    {
                        splatmapData[x, y, LEAF_GROUND] = .05f;
                        splatmapData[x, y, GRASS] = .05f;
                        splatmapData[x, y, ROCKY_GROUND] = .9f;
                    }
                    else if (height < 20f)
                    {
                        splatmapData[x, y, LEAF_GROUND] = .5f;
                        splatmapData[x, y, GRASS] = .5f;
                        splatmapData[x, y, ROCKY_GROUND] = .0f;
                    }
                    else
                    {
                        splatmapData[x, y, LEAF_GROUND] = .75f;
                        splatmapData[x, y, GRASS] = .15f;
                        splatmapData[x, y, ROCKY_GROUND] = .1f;
                    }
                }












                //// Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                //float z = splatWeights.Sum();

                //// Loop through each terrain texture
                //for (int i = 0; i < terrainData.alphamapLayers; i++)
                //{

                //    // Normalize so that sum of all texture weights = 1
                //    splatWeights[i] /= z;

                //    // Assign this point to the splatmap array
                //    splatmapData[x, y, i] = splatWeights[i];
                //}
            }
        }

        // Finally assign the new splatmap to the terrainData:
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }
}
