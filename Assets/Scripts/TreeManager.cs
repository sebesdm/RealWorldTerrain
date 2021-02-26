using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class TreeManager
{
    public TreeManager(MultiTerrain terrain, int treeCount)
    {
        this.terrain = terrain;
        this.treeCount = treeCount;
        GenerateTreePool();
    }

    private void GenerateTreePool()
    {
        for (int i = 0; i < treeCount; i++)
        {
            var t = 2f * Math.PI * UnityEngine.Random.Range(0f, 1f);
            var u = UnityEngine.Random.Range(0f, 1f) + UnityEngine.Random.Range(0f, 1f);
            var r = (u > 1 ? 2 - u : u) * 1000;

            float x = ((float)Math.Sin(t) * r);
            float z = ((float)Math.Cos(t) * r);

            int prototypeIndex = UnityEngine.Random.Range(0f, 1f) > .65f ? 1 : 0;

            trees.Add(new TreeInstance() { heightScale = .8f, widthScale = .8f, prototypeIndex = prototypeIndex, position = new Vector3(x, 1000, z) });
        }
    }

    public void SetTrees(Vector3 center, bool cullNearTiles)
    {
        terrain.SetTreeInstances(trees, center, cullNearTiles);
    }

    List<TreeInstance> trees = new List<TreeInstance>();

    private MultiTerrain terrain;
    private int treeCount;
}