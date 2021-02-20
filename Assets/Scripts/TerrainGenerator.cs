using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Utility
{
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
}

public class MultiTerrainManager
{
    public int TerrainDimensionsHeight { get; private set; }
    public int TerrainDimensionsWidth { get; private set; }
    public Terrain BaseTerrain { get; private set; }

    public MultiTerrainManager(int terrainDimensionsHeight, int terrainDimensionsWidth)
    {
        TerrainDimensionsHeight = terrainDimensionsHeight;
        TerrainDimensionsWidth = terrainDimensionsWidth;

        InitializeTerrains();
    }

    public void ForeachTerrain(Action<Terrain, Terrain, int, int> terrainAction)
    {
        for (int i = 0; i < terrains.Length; i++)
        {
            for (int j = 0; j < terrains[i].Length; j++)
            {
                terrainAction(BaseTerrain, terrains[i][j], i, j);
            }
        }
    }

    private void InitializeTerrains()
    {
        terrains = new Terrain[TerrainDimensionsHeight][];
        for (int i = 0; i < terrains.Length; i++)
        {
            terrains[i] = new Terrain[TerrainDimensionsWidth];
            for (int j = 0; j < terrains[i].Length; j++)
            {
                terrains[i][j] = GameObject.Find($"Terrain_{i}_{j}").GetComponent<Terrain>();
            }
        }
        BaseTerrain = terrains[0][0];

        ConnectTerrainsInScene();
    }

    private void ConnectTerrainsInScene()
    {
        for (int i = 0; i < terrains.Length; i++)
        {
            for (int j = 0; j < terrains[i].Length; j++)
            {
                Terrain leftNeighbor = j - 1 > 0 ? terrains[i][j - 1] : null;
                Terrain rightNeighbor = j + 1 < terrains[i].Length ? terrains[i][j + 1] : null;
                Terrain bottomNeighbor = i - 1 > 0 ? terrains[i - 1][j] : null;
                Terrain topNeighbor = i + 1 < terrains.Length ? terrains[i + 1][j] : null;

                terrains[i][j].SetNeighbors(leftNeighbor, topNeighbor, rightNeighbor, bottomNeighbor);
            }
        }
    }

    private Terrain[][] terrains { get; set; }
}

public class TerrainHeightGenerator
{
    public float terrainScale = 1f;
    public TerrainHeightGenerator(MultiTerrainManager multiTerrainManager, float terrainScale)
    {
        this.multiTerrainManager = multiTerrainManager;
        this.terrainScale = terrainScale;
    }


    public void DoHeights()
    {
        TerrainData terrainData = multiTerrainManager.BaseTerrain.terrainData;

        int terrainDataHeight = terrainData.heightmapHeight;
        int terrainDataWidth = terrainData.heightmapWidth;

        Texture2D bitmap = Utility.LoadPNG(@"C:\Users\Moses\Desktop\heightmapper-1613486714451.png");


        int bitmapHeight = bitmap.height;
        int bitmapWidth = bitmap.width;

        float[][][,] heightsInstances = CreateTerrainHeightsInstances(terrainDataHeight, terrainDataWidth);

        for (int i = 0; i < bitmapHeight; i++)
        {
            for (int j = 0; j < bitmapWidth; j++)
            {
                Color c = bitmap.GetPixel(j, i); // This needs to be reversed!!

                int heightsInstanceTopIndex = i / terrainDataHeight;
                int heightsInstanceRightIndex = j / terrainDataWidth;

                int heightsY = i % terrainDataHeight;
                int heightsX = j % terrainDataWidth;

                float[,] heights = heightsInstances[heightsInstanceTopIndex][heightsInstanceRightIndex];
                heights[heightsY, heightsX] = c.r * terrainScale;
            }

        }

        for (int i = 0; i < heightsInstances.Length; i++)
        {
            for (int j = 0; j < heightsInstances[i].Length; j++)
            {
                Terrain t = StepToTerrainNeighbor(multiTerrainManager.BaseTerrain, i, j);
                t.terrainData.SetHeights(0, 0, heightsInstances[i][j]);
            }
        }
    }

    private Terrain StepToTerrainNeighbor(Terrain baseTerrain, int topSteps, int rightSteps)
    {
        Terrain terrainToRetrieve = baseTerrain;

        for (int i = 0; i < topSteps; i++)
        {
            terrainToRetrieve = terrainToRetrieve.topNeighbor;
        }
        for (int i = 0; i < rightSteps; i++)
        {
            terrainToRetrieve = terrainToRetrieve.rightNeighbor;
        }

        return terrainToRetrieve;
    }

    private float[][][,] CreateTerrainHeightsInstances(int terrainDataHeight, int terrainDataWidth)
    {
        float[][][,] heightsInstances = new float[multiTerrainManager.TerrainDimensionsHeight][][,];
        for (int i = 0; i < heightsInstances.Length; i++)
        {
            float[][,] heightsInstanceRow = new float[multiTerrainManager.TerrainDimensionsWidth][,];
            heightsInstances[i] = heightsInstanceRow;

            for (int j = 0; j < heightsInstanceRow.Length; j++)
            {
                float[,] heightsInstance = new float[terrainDataHeight, terrainDataWidth];
                heightsInstanceRow[j] = heightsInstance;
            }
        }
        return heightsInstances;
    }

    private MultiTerrainManager multiTerrainManager;
}

public class TerrainTextureGenerator
{
    public TerrainTextureGenerator(MultiTerrainManager multiTerrainManager)
    {
        this.multiTerrainManager = multiTerrainManager;
    }
    public void DoTexture()
    {
        alphamap = Utility.LoadPNG(@"C:\Users\Moses\Desktop\heightmapper-alpha-1613486714451.png");
        multiTerrainManager.ForeachTerrain(SetTerrainTextures);
        multiTerrainManager.ForeachTerrain(ApplyTexture);
    }

    private void ApplyTexture(Terrain baseTerrain, Terrain terrain, int terrainOffsetX, int terrainOffsetY)
    {
        int xOffset = terrainOffsetX * terrain.terrainData.heightmapHeight;
        int yOffset = terrainOffsetY * terrain.terrainData.heightmapWidth;


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
                Color c = alphamap.GetPixel(y - 1 + yOffset, x - 6 + xOffset); // Need to fecth the pixels reversed due to x, y flip for alphamaps

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

        // Finally assign the new splatmap to the terrainData:
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    private void SetTerrainTextures(Terrain baseTerrain, Terrain terrain, int terrainIndexX, int terrainIndexY)
    {
        terrain.terrainData.terrainLayers = baseTerrain.terrainData.terrainLayers;
        terrain.materialTemplate = baseTerrain.materialTemplate;
    }

    private MultiTerrainManager multiTerrainManager;
    private Texture2D alphamap;
}

public class TerrainGenerator : MonoBehaviour
{
    void Start()
    {
        MultiTerrainManager multiTerrainManager = new MultiTerrainManager(6, 9);

        //TerrainHeightGenerator heightGenerator = new TerrainHeightGenerator(multiTerrainManager, .3f);
        //heightGenerator.DoHeights();

        //TerrainTextureGenerator textureGenerator = new TerrainTextureGenerator(multiTerrainManager);
        //textureGenerator.DoTexture();

        List<TreeInstance> trees = new List<TreeInstance>();
        for (float i = 0; i < 1; i = i + .05f)
        {
            for (float j = 0; j < 1; j = j + .05f)
            {
                float castDistance = 150;

                Physics.Raycast(new Vector3(i * 1000, castDistance, j * 1000), Vector3.down, out RaycastHit hitinfo, 1000);
                Debug.DrawRay(new Vector3(i * 1000, castDistance, j * 1000), Vector3.down, Color.red, 1000, true);

                if(hitinfo.distance < 110)
                {
                    trees.Add(new TreeInstance() { position = new Vector3(i, castDistance, j), heightScale = .6f, prototypeIndex = 0, widthScale = .6f });
                }
            }
        }


        multiTerrainManager.ForeachTerrain((bt, t, x, y) =>
        {


            t.terrainData.treePrototypes = bt.terrainData.treePrototypes;




            TerrainData terrainData = t.terrainData;
            terrainData.SetTreeInstances(trees.ToArray(), true);

        });







    }
}
