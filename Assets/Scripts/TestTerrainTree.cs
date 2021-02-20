using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTerrainTree : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Terrain t = GetComponent<Terrain>();

        TerrainData terrainData = t.terrainData;

        TreePrototype treePrototype = terrainData.treePrototypes[0];

        terrainData.SetTreeInstances(new List<TreeInstance>() { new TreeInstance() { position = new Vector3(.5f, 100, .5f), heightScale = 1, prototypeIndex = 0, widthScale = 1 } }.ToArray(), true);


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
