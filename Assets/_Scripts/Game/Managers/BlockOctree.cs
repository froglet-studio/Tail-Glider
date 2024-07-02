﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CosmicShore.Core;
using System;

public class BlockOctree
{
    private class OctreeNode
    {
        public Vector3 Center { get; private set; }
        public float Size { get; private set; }
        public int BlockCount { get; set; }
        public OctreeNode[] Children { get; private set; }
        public List<TrailBlock> Blocks { get; private set; }

        public OctreeNode(Vector3 center, float size)
        {
            Center = center;
            Size = size;
            BlockCount = 0;
            Children = new OctreeNode[8];
            Blocks = new List<TrailBlock>();
        }

        public bool IsLeaf => Children[0] == null;
    }

    private OctreeNode root;
    private float minSize;

    public BlockOctree(Vector3 center, float size, float minSize)
    {
        root = new OctreeNode(center, size);
        this.minSize = minSize;
    }

    public void AddBlock(TrailBlock block)
    {
        AddBlockRecursive(root, block);
    }

    public void RemoveBlock(TrailBlock block)
    {
        RemoveBlockRecursive(root, block);
    }

    public List<Vector3> FindDensestRegions(int count)
    {
        List<OctreeNode> densestNodes = new List<OctreeNode>();
        FindDensestNodesRecursive(root, count, densestNodes);
        return densestNodes.Select(n => n.Center).ToList();
    }

    // ... Include all the private methods from the previous implementation ...
    // (AddBlockRecursive, RemoveBlockRecursive, SplitNode, GetOctant,
    // FindDensestNodesRecursive, InsertSorted)
    private void AddBlockRecursive(OctreeNode node, TrailBlock block)
    {
        node.BlockCount++;

        if (node.Size <= minSize || node.Blocks.Count < 2)//8
        {
            node.Blocks.Add(block);
            return;
        }

        if (node.IsLeaf)
        {
            SplitNode(node);
        }

        int octant = GetOctant(node, block.transform.position);
        AddBlockRecursive(node.Children[octant], block);
    }

    private void SplitNode(OctreeNode node)
    {
        float halfSize = node.Size / 2;
        for (int i = 0; i < 8; i++)
        {
            Vector3 newCenter = node.Center + new Vector3(
                ((i & 1) == 0 ? -halfSize : halfSize) / 2,
                ((i & 2) == 0 ? -halfSize : halfSize) / 2,
                ((i & 4) == 0 ? -halfSize : halfSize) / 2
            );
            node.Children[i] = new OctreeNode(newCenter, halfSize);
            Debug.Log($"split {newCenter}");
        }

        foreach (var block in node.Blocks)
        {
            int octant = GetOctant(node, block.transform.position);
            AddBlockRecursive(node.Children[octant], block);
        }
        node.Blocks.Clear();
    }

    private int GetOctant(OctreeNode node, Vector3 position)
    {
        int octant = 0;
        if (position.x >= node.Center.x) octant |= 1;
        if (position.y >= node.Center.y) octant |= 2;
        if (position.z >= node.Center.z) octant |= 4;
        return octant;
    }

    private bool RemoveBlockRecursive(OctreeNode node, TrailBlock block)
    {
        if (node.IsLeaf)
        {
            bool removed = node.Blocks.Remove(block);
            if (removed) node.BlockCount--;
            return removed;
        }

        int octant = GetOctant(node, block.transform.position);
        bool removed2 = RemoveBlockRecursive(node.Children[octant], block);
        if (removed2) node.BlockCount--;
        return removed2;
    }

    private void FindDensestNodesRecursive(OctreeNode node, int count, List<OctreeNode> densestNodes)
    {
        if (node.IsLeaf || node.Size <= minSize)
        {
            InsertSorted(densestNodes, node, count);
            return;
        }

        foreach (var child in node.Children)
        {
            if (child != null && child.BlockCount > 0)
            {
                FindDensestNodesRecursive(child, count, densestNodes);
            }
        }
    }

    private void InsertSorted(List<OctreeNode> list, OctreeNode node, int maxCount)
    {
        int index = list.FindIndex(n => (n.BlockCount / Math.Pow(n.Size, 3) < node.BlockCount / Math.Pow(node.Size, 3)));
        if (index == -1) index = list.Count;
        list.Insert(index, node);
        Debug.Log($"rk");
        Debug.Log(String.Join("; ", list));
        if (list.Count > maxCount) list.RemoveAt(maxCount);
    }

    public double GetBlockDensityAtPosition(Vector3 position)
    {
        return GetBlockDensityRecursive(root, position);
    }

    private double GetBlockDensityRecursive(OctreeNode node, Vector3 position)
    {
        if (node.IsLeaf)
        {
            return node.BlockCount / Math.Pow(node.Size, 3);
        }

        int octant = GetOctant(node, position);
        if (node.Children[octant] != null)
        {
            return GetBlockDensityRecursive(node.Children[octant], position);
        }

        return 0.0;
    }
}