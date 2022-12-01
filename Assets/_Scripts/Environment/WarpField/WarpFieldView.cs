using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpFieldView : MonoBehaviour
{

    [SerializeField] GameObject snow;

    GameObject[,,] crystalLattice;
    [SerializeField] bool reset;

    [SerializeField] int nodesPerSide = 4;
    [SerializeField] float nodeScaler = 5f;

    float nodeSize = 0;

    WarpFieldData warpFieldData;

    float nodeDistanceX;
    float nodeDistanceY;
    float nodeDistanceZ;

    public int seed;

    private GameObject TemporaryContainer;

    private void OnEnable()
    {
        //Crystal.OnCrystalMove += ChangeSnowSize;
    }

    private void OnDisable()
    {
        //Crystal.OnCrystalMove -= ChangeSnowSize;
    }

    // Start is called before the first frame update
    void OnDrawGizmosSelected()
    {
        if (TemporaryContainer == null)
        {
            TemporaryContainer = new GameObject();
        }
        else
        {

            foreach (var element in TemporaryContainer.GetComponentsInChildren<Transform>())
            {
                if (element != TemporaryContainer.transform)
                {
                    element.gameObject.GetComponent<Renderer>().material = null;
                }
            }
        }

        warpFieldData = GetComponent<WarpFieldData>();
        warpFieldData.Init();
        warpFieldData.UpdateScriptableObjectValues();

        nodeDistanceX = warpFieldData.fieldWidth / nodesPerSide;
        nodeDistanceY = warpFieldData.fieldHeight / nodesPerSide;
        nodeDistanceZ = warpFieldData.fieldThickness / nodesPerSide;

        MakeShards();
        ChangeShards();
    }

    void MakeShards()
    {
        if (crystalLattice == null || reset)
        {

            foreach (var element in GetComponentsInChildren<Transform>())
            {
                if (element != transform && element != snow.transform)
                {
                    element.SetParent(TemporaryContainer.transform);
                }
            }

            crystalLattice = new GameObject[nodesPerSide * 2, nodesPerSide * 2, nodesPerSide * 2];
            for (int x = -nodesPerSide; x < nodesPerSide; x++)
            {
                for (int y = -nodesPerSide; y < nodesPerSide; y++)
                {
                    for (int z = -nodesPerSide; z < nodesPerSide; z++)
                    {
                        GameObject tempSnow = Instantiate(snow);
                        tempSnow.transform.SetParent(transform, true);
                        crystalLattice[x + nodesPerSide, y + nodesPerSide, z + nodesPerSide] = tempSnow;
                    }
                }
            }
            reset = false;
        }
    }

    void ChangeShards()
    {
        int newSeed = seed;
        for (int x = 0; x < nodesPerSide * 2; x++)
        {
            for (int y = 0; y < nodesPerSide * 2; y++)
            {
                for (int z = 0; z < nodesPerSide * 2; z++)
                {
                    var node = crystalLattice[x, y, z].transform;
                    newSeed++;
                    Random.InitState(newSeed);

                    node.transform.position = new Vector3((x - nodesPerSide) * nodeDistanceX + Random.Range(-nodeDistanceX / 2, nodeDistanceX / 2),
                                                          (y - nodesPerSide) * nodeDistanceY + Random.Range(-nodeDistanceY / 2, nodeDistanceY / 2),
                                                          (z - nodesPerSide) * nodeDistanceZ + Random.Range(-nodeDistanceZ / 2, nodeDistanceZ / 2));

                    Vector3 hybridVector = warpFieldData.HybridVector(node);

                    node.transform.localScale = Vector3.forward * nodeScaler * 3 +
                        Vector3.one * (hybridVector.magnitude * nodeScaler);
                    node.forward = -hybridVector;

                    //node.transform.localScale =
                    //    Vector3.velocityDirection * (flowVector.magnitude * nodeScaler + nodeSize) +
                    //    Vector3.one * (flowVector.magnitude * nodeScalerOverThree + nodeSize);
                    //node.velocityDirection = flowVector;
                }
            }
        }
    }
}