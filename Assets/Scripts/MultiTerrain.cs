using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MultiTerrain
{
    private Terrain[][] terrainTiles;

    public MultiTerrain(Vector2 tileSize, int totalHeight, int totalWidth)
    {
        // Implement this to abstract the need to determine tile counts 
    }

    public MultiTerrain(int terrainRows, int terrainColumns, int terrainWidth, int terrainHeight)
    {
        terrainTiles = new Terrain[terrainRows][];

        for (int row = 0; row < terrainRows; row++)
        {
            terrainTiles[row] = new Terrain[terrainColumns];

            for (int column = 0; column < terrainColumns; column++)
            {
                TerrainData terrainData = new TerrainData();
                terrainData.name = $"TerrainData_{row}_{column}";
                terrainData.heightmapResolution = terrainHeight;
                terrainData.baseMapResolution = terrainHeight;
                terrainData.alphamapResolution = terrainHeight;
                terrainData.size = new Vector3(terrainWidth, 1000, terrainHeight);

                GameObject terrain = Terrain.CreateTerrainGameObject(terrainData);
                terrain.name = $"Terrain_{row}_{column}";

                terrainTiles[row][column] = terrain.GetComponent<Terrain>();
            }
        }

        Terrain placeholderTerrain = GameObject.Find("PlaceholderTerrain").GetComponent<Terrain>();
        GameObject.Find("PlaceholderTerrain").SetActive(false);

        ForeachTerrainTile((terrainTile, terrainTileIndex) =>
        {
            terrainTile.transform.Translate(Vector3.right * terrainWidth * terrainTileIndex.Column);
            terrainTile.transform.Translate(Vector3.forward * terrainHeight * terrainTileIndex.Row);
            terrainTile.terrainData.terrainLayers = placeholderTerrain.terrainData.terrainLayers;
            terrainTile.terrainData.treePrototypes = placeholderTerrain.terrainData.treePrototypes;
        });

        ConnectTerrainsInScene();
    }

    public void SetTreeInstances(IEnumerable<TreeInstance> treeInstances, Vector3 center, bool cullNearTiles = true)
    {
        // Tree instances on a tile have a min of 0, max of 1.  Normalized.
        IEnumerable<TerrainTileIndex> nearTerrainTiles = Enumerable.Empty<TerrainTileIndex>();
        if (cullNearTiles)
        {
            nearTerrainTiles = GetNearTerrainTiles(center);
        }
        var treePositions = treeInstances.Select(ti => NormalizeTreeInstancePosition(ti, center)).Where(_ => !_.Item3).ToList();

        ForeachTerrainTile((terrainTile, terrainTileIndex) =>
        {
            if(nearTerrainTiles.Any(ntt => ntt.Row == terrainTileIndex.Row && ntt.Column == terrainTileIndex.Column))
            {
                return;
            }

            var treePositionsForTile = treePositions.Where(tp => tp.Item2.Row == terrainTileIndex.Row && tp.Item2.Column == terrainTileIndex.Column).ToList();
            var treeInstancesForTile = treePositionsForTile.Select(tpft => tpft.Item1).ToList();
            
            terrainTiles[terrainTileIndex.Row][terrainTileIndex.Column].terrainData.SetTreeInstances(treeInstancesForTile.ToArray(), true);
        });
    }

    private IEnumerable<TerrainTileIndex> GetNearTerrainTiles(Vector3 center)
    {
        int maxRowIndex = terrainTiles.Length - 1;
        int maxColumnIndex = terrainTiles[0].Length - 1;

        float dimension = terrainTiles[0][0].terrainData.size.x;
        int terrainTileRowIndex = (int)(center.z / dimension);
        int terrainTileColumnIndex = (int)(center.x / dimension);

        List<TerrainTileIndex> nearTerrainTiles = new List<TerrainTileIndex>();

        nearTerrainTiles.Add(new TerrainTileIndex() { Row = terrainTileRowIndex, Column = terrainTileColumnIndex });
        for(int i = 6; i >= 0; i--)
        {
            for (int j = 6 - i; j >= 0; j--)
            {
                if (terrainTileRowIndex + i <= maxRowIndex && terrainTileColumnIndex + j <= maxColumnIndex)
                {
                    nearTerrainTiles.Add(new TerrainTileIndex() { Row = terrainTileRowIndex + i, Column = terrainTileColumnIndex + j });
                }

                if (terrainTileRowIndex - i >= 0 && terrainTileColumnIndex - j >= 0)
                {
                    nearTerrainTiles.Add(new TerrainTileIndex() { Row = terrainTileRowIndex - i, Column = terrainTileColumnIndex - j });
                }

                if (terrainTileRowIndex + i <= maxRowIndex && terrainTileColumnIndex - j >= 0)
                {
                    nearTerrainTiles.Add(new TerrainTileIndex() { Row = terrainTileRowIndex + i, Column = terrainTileColumnIndex - j });
                }

                if (terrainTileRowIndex - i >= 0 && terrainTileColumnIndex + j <= maxColumnIndex)
                {
                    nearTerrainTiles.Add(new TerrainTileIndex() { Row = terrainTileRowIndex - i, Column = terrainTileColumnIndex + j });
                }
            }
        }

        return nearTerrainTiles;
    }

    private (TreeInstance, TerrainTileIndex, bool) NormalizeTreeInstancePosition(TreeInstance treeInstance, Vector3 center)
    {
        Vector3 position = treeInstance.position; // absolute position on the full multi terrain
        position.x += center.z;
        position.z += center.x;

        float dimension = terrainTiles[0][0].terrainData.size.x;

        int terrainTileRowIndex = (int)(position.x / dimension);
        int terrainTileColumnIndex = (int)(position.z / dimension);

        float terrainTileZIndex = position.x - (terrainTileRowIndex * dimension);
        float terrainTileXIndex = position.z - (terrainTileColumnIndex * dimension);

        float normalizedTerrainTileXIndex = terrainTileXIndex / dimension;
        float normalizedTerrainTileZIndex = terrainTileZIndex / dimension;

        bool isWater = true;
        if(terrainTileXIndex >= 0 && terrainTileZIndex >= 0)
        {
            isWater = terrainTiles[terrainTileRowIndex][terrainTileColumnIndex].terrainData.GetAlphamaps((int)terrainTileXIndex, (int)terrainTileZIndex, 1, 1)[0, 0, 0] == 1;
        }

        treeInstance.position = new Vector3(normalizedTerrainTileXIndex, 1000, normalizedTerrainTileZIndex);

        return (treeInstance, new TerrainTileIndex() { Row = terrainTileRowIndex, Column = terrainTileColumnIndex }, isWater);
    }

    public void SetSplatmapData(float[,,] splatmapData)
    {
        int maxX = splatmapData.GetLength(0);
        int maxY = splatmapData.GetLength(1);
        int maxZ = splatmapData.GetLength(2);

        ForeachTerrainTile((terrainTile, terrainTileIndex) =>
        {
            int size = terrainTile.terrainData.baseMapResolution;

            int rowOffset = terrainTileIndex.Row * size;
            int columnOffset = terrainTileIndex.Column * size;

            float[,,] splatmapSegment = new float[size, size, maxZ];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (rowOffset + x < maxX && columnOffset + y < maxY)
                    {
                        for (int z = 0; z < maxZ; z++)
                        {
                            splatmapSegment[x, y, z] = splatmapData[rowOffset + x, columnOffset + y, z];
                        }
                    }
                }
            }

            terrainTile.terrainData.SetAlphamaps(0, 0, splatmapSegment);
        });
    }

    public void SetHeights(float[,] heights)
    {
        int maxX = heights.GetLength(0);
        int maxY = heights.GetLength(1);

        ForeachTerrainTile((terrainTile, terrainTileIndex) =>
        {
            int size = terrainTile.terrainData.baseMapResolution;

            int rowOffset = terrainTileIndex.Row * size;
            int columnOffset = terrainTileIndex.Column * size;

            float[,] heightsSegment = new float[size, size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (rowOffset + x < maxX && columnOffset + y < maxY)
                    {
                        heightsSegment[x, y] = heights[rowOffset + x, columnOffset + y];
                    }
                }
            }

            terrainTile.terrainData.SetHeights(0, 0, heightsSegment);
        });
    }

    private void ForeachTerrainTile(Action<Terrain, TerrainTileIndex> terrainTileAction)
    {
        for (int row = 0; row < terrainTiles.Length; row++)
        {
            for (int column = 0; column < terrainTiles[row].Length; column++)
            {
                terrainTileAction(terrainTiles[row][column], new TerrainTileIndex()
                {
                    Row = row,
                    Column = column
                });
            }
        }
    }

    private void ConnectTerrainsInScene()
    {
        for (int i = 0; i < terrainTiles.Length; i++)
        {
            for (int j = 0; j < terrainTiles[i].Length; j++)
            {
                Terrain leftNeighbor = j - 1 > 0 ? terrainTiles[i][j - 1] : null;
                Terrain rightNeighbor = j + 1 < terrainTiles[i].Length ? terrainTiles[i][j + 1] : null;
                Terrain bottomNeighbor = i - 1 > 0 ? terrainTiles[i - 1][j] : null;
                Terrain topNeighbor = i + 1 < terrainTiles.Length ? terrainTiles[i + 1][j] : null;

                terrainTiles[i][j].SetNeighbors(leftNeighbor, topNeighbor, rightNeighbor, bottomNeighbor);
            }
        }
    }
}
