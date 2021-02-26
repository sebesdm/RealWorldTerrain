using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public GameObject player;

    void Start()
    {
        GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        MultiTerrain multiTerrain = new MultiTerrain(30, 40, 129, 129);

        TerrainHeightGenerator heightGenerator = new TerrainHeightGenerator(.15f);
        var heights = heightGenerator.GenerateHeights();
        multiTerrain.SetHeights(heights);

        TerrainTextureGenerator textureGenerator = new TerrainTextureGenerator();
        var splatmap = textureGenerator.GenerateSplatmap();
        multiTerrain.SetSplatmapData(splatmap);

        treeManager = new TreeManager(multiTerrain, 50000);
        //treeManager.SetTrees(player.transform.position);
        treeManager.SetTrees(player.transform.position);

        StartCoroutine("RedoTrees");
    }




    private IEnumerator RedoTrees()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            //treeManager.SetTrees(player.transform.position);
            treeManager.SetTrees(player.transform.position);
        }
    }




    private TreeManager treeManager;
}