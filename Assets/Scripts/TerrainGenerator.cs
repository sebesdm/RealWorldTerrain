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

    public MultiTerrainManager(int terrainDimensionsHeight, int terrainDimensionsWidth, List<(int, int)> terrainExclusions)
    {
        TerrainDimensionsHeight = terrainDimensionsHeight;
        TerrainDimensionsWidth = terrainDimensionsWidth;
        this.terrainExclusions = terrainExclusions;

        InitializeTerrains();
    }

    public void ForeachTerrain(Action<Terrain, Terrain, int, int> terrainAction)
    {
        for (int i = 0; i < terrains.Length; i++)
        {
            for (int j = 0; j < terrains[i].Length; j++)
            {
                if(terrainExclusions.Any(te => te.Item1 == i && te.Item2 == j))
                {
                    continue; // if we explicitly exclude some terrains, don't process them in the iteration
                }
                terrainAction(BaseTerrain, terrains[i][j], i, j);
            }
        }
    }

    public bool IsValidTerrainIndex(int row, int column)
    {
        return !(row < 0 && column < 0 && row >= TerrainDimensionsHeight && column >= TerrainDimensionsWidth);
    }

    public (int, int) GetTerrainIndexFromName(string name)
    {
        var coords = name.Split(new char[] { '_' });
        var row = int.Parse(coords[1]);
        var column = int.Parse(coords[2]);
        return (row, column);
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
    private List<(int, int)> terrainExclusions { get; set; }
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


public class TerrainTreeGenerator
{
    public TerrainTreeGenerator(MultiTerrainManager multiTerrainManager)
    {
        this.multiTerrainManager = multiTerrainManager;
    }
    public void DoTrees()
    {
        alphamap = Utility.LoadPNG(@"C:\Users\Moses\Desktop\heightmapper-alpha-1613486714451.png");
        multiTerrainManager.ForeachTerrain(ApplyTrees);
    }

    private void ApplyTrees(Terrain baseTerrain, Terrain terrain, int terrainOffsetX, int terrainOffsetY)
    {
        int xOffset = terrainOffsetX * terrain.terrainData.heightmapHeight;
        int yOffset = terrainOffsetY * terrain.terrainData.heightmapWidth;

        TerrainData terrainData = terrain.terrainData;

        List<TreeInstance> trees = new List<TreeInstance>();


        for (int y = 0; y < terrainData.alphamapHeight; y = y + 4)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x = x + 4)
            {
                Color c = alphamap.GetPixel(y - 1 + yOffset, x - 6 + xOffset); // Need to fecth the pixels reversed due to x, y flip for alphamaps

                if (c.r > .18f && c.g > .18f && c.b > .18f)
                {
                    int randomTreePrototype = UnityEngine.Random.Range(0, 2);
                    float randomScale = UnityEngine.Random.Range(.7f, 1f);

                    float randomXOffset = UnityEngine.Random.Range(0, .01f);
                    float randomYOffset = UnityEngine.Random.Range(0, .01f);

                    float xPos = (x / (float)terrainData.alphamapWidth) + randomXOffset;
                    float yPos = (y / (float)terrainData.alphamapHeight) + randomYOffset;

                    trees.Add(new TreeInstance() { position = new Vector3(yPos, 1000f, xPos), heightScale = randomScale, prototypeIndex = randomTreePrototype, widthScale = randomScale });
                }
            }
        }

        terrain.terrainData.treePrototypes = baseTerrain.terrainData.treePrototypes;
        terrain.terrainData.SetTreeInstances(trees.ToArray(), true);
    }

    private MultiTerrainManager multiTerrainManager;
    private Texture2D alphamap;
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
    public GameObject player;

    public float skyboxr = 0;



    void Start()
    {
        RenderSettings.skybox.SetFloat("_Rotation", skyboxr);

        List<(int, int)> terrainExclusions = new List<(int, int)>();
        //terrainExclusions.Add((1, 0));
        //terrainExclusions.Add((2, 0));
        //terrainExclusions.Add((3, 0));
        //terrainExclusions.Add((2, 1));
        //terrainExclusions.Add((3, 1));
        //terrainExclusions.Add((3, 2));
        //terrainExclusions.Add((3, 3));
        //terrainExclusions.Add((3, 8));
        //terrainExclusions.Add((2, 8));
        //terrainExclusions.Add((1, 8));
        //terrainExclusions.Add((0, 8));
        //terrainExclusions.Add((0, 7));
        //terrainExclusions.Add((1, 7));
        //terrainExclusions.Add((2, 7));
        //terrainExclusions.Add((3, 7));
        //terrainExclusions.Add((0, 6));
        //terrainExclusions.Add((0, 5));

        MultiTerrainManager multiTerrainManager = new MultiTerrainManager(4, 9, terrainExclusions);

        //TerrainHeightGenerator heightGenerator = new TerrainHeightGenerator(multiTerrainManager, .3f);
        //heightGenerator.DoHeights();

        //TerrainTextureGenerator textureGenerator = new TerrainTextureGenerator(multiTerrainManager);
        //textureGenerator.DoTexture();

        TerrainTreeGenerator treeGenerator = new TerrainTreeGenerator(multiTerrainManager);
        treeGenerator.DoTrees();

        terrainDisabler = new TerrainDissabler(multiTerrainManager, player);
    }

    public void Update()
    {
        terrainDisabler.CheckTerrains();
    }

    private TerrainDissabler terrainDisabler;
}

public class TerrainDissabler
{
    public TerrainDissabler(MultiTerrainManager terrainManager, GameObject player)
    {
        this.terrainManager = terrainManager;
        this.player = player;
    }

    public void CheckTerrains()
    {
        Ray ray = new Ray(player.transform.position, Vector3.down);
        Physics.Raycast(ray, out RaycastHit hitInfo, 5000);
        var collideObj = hitInfo.collider.gameObject;

        (int, int) index = terrainManager.GetTerrainIndexFromName(collideObj.name);
        var row = index.Item1;
        var column = index.Item2;

        List<(int, int)> terrainsToActivate = new List<(int, int)>();
        terrainsToActivate.Add((row, column)); // Current Player Terrain

        if(terrainManager.IsValidTerrainIndex(row - 1, column - 1))
        {
            terrainsToActivate.Add((row - 1, column - 1));
        }
        if (terrainManager.IsValidTerrainIndex(row, column - 1))
        {
            terrainsToActivate.Add((row, column - 1));
        }
        if (terrainManager.IsValidTerrainIndex(row + 1, column - 1))
        {
            terrainsToActivate.Add((row + 1, column - 1));
        }
        if (terrainManager.IsValidTerrainIndex(row + 1, column))
        {
            terrainsToActivate.Add((row + 1, column));
        }
        if (terrainManager.IsValidTerrainIndex(row - 1, column))
        {
            terrainsToActivate.Add((row - 1, column));
        }
        if (terrainManager.IsValidTerrainIndex(row - 1, column + 1))
        {
            terrainsToActivate.Add((row - 1, column + 1));
        }
        if (terrainManager.IsValidTerrainIndex(row, column + 1))
        {
            terrainsToActivate.Add((row, column + 1));
        }
        if (terrainManager.IsValidTerrainIndex(row + 1, column + 1))
        {
            terrainsToActivate.Add((row + 1, column + 1));
        }

        terrainManager.ForeachTerrain((bt, t, indexX, indexY) =>
        {
            if (terrainsToActivate.Any(tta => tta.Item1 == indexX && tta.Item2 == indexY))
            {
                t.enabled = true;
            }
            else
            {
                t.enabled = false;
            }
        });
    }

    private MultiTerrainManager terrainManager;
    private GameObject player;
}
