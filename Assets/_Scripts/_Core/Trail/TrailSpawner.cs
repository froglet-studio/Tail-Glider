﻿using StarWriter.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TrailSpawner : MonoBehaviour
{
    [SerializeField] GameObject trail;

    public float offset = 0f;
    public float trailPeriod = .1f;
    public float trailLength = 20;
    public float waitTime = .5f;            // Time until the trail block appears - camera dependent
    public float startDelay = 2.1f;

    static GameObject TrailContainer;

    readonly Queue<GameObject> trailList = new();
    bool spawnerEnabled = true;

    private void OnEnable()
    {
        GameManager.onPhoneFlip += OnPhoneFlip;
        GameManager.onDeath += PauseTrailSpawner;
        GameManager.onExtendGamePlay += RestartTrailSpawnerAfterDelay;
    }

    private void OnDisable()
    {
        GameManager.onPhoneFlip -= OnPhoneFlip;
        GameManager.onDeath -= PauseTrailSpawner;
        GameManager.onExtendGamePlay -= RestartTrailSpawnerAfterDelay;
    }

    void PauseTrailSpawner()
    {
        spawnerEnabled = false;
    }
    void RestartTrailSpawnerAfterDelay()
    {
        StartCoroutine(RestartSpawnerAfterDelayCoroutine());
    }
    IEnumerator RestartSpawnerAfterDelayCoroutine()
    {
        yield return new WaitForSeconds(waitTime);
        spawnerEnabled = true;
    }

    // TODO: verify things still work then remove this
    private static void ResetTrailContainer()
    {
        for (var i = TrailContainer.transform.childCount-1; i >= 0; i--)
        {
            var child = TrailContainer.transform.GetChild(i).gameObject;
            Destroy(child);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (TrailContainer == null)
        {
            TrailContainer = new GameObject();
            TrailContainer.name = "TrailContainer";
            // TODO: verify things still work then remove this
            //GameManager.onPlayGame += ResetTrailContainer;
            //DontDestroyOnLoad(TrailContainer);  // TODO: this is probably not awesome ¯\_(ツ)_/¯
        }

        StartCoroutine(SpawnTrailCoroutine());
    }

    private void OnPhoneFlip(bool state)
    {
        if (gameObject == GameObject.FindWithTag("Player"))
        {
            waitTime = state ? 1.5f : 0.5f;
        }
    }

    IEnumerator SpawnTrailCoroutine()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            if (Time.deltaTime < .1f && spawnerEnabled)
            {
                var Block = Instantiate(trail);
                Block.transform.SetPositionAndRotation(transform.position - transform.forward * offset, transform.rotation);
                Block.transform.parent = TrailContainer.transform;
                Block.GetComponent<Trail>().waitTime = waitTime;

                trailList.Enqueue(Block);
                if (trailList.Count > trailLength / trailPeriod)
                {
                    StartCoroutine(ShrinkTrailCoroutine());
                }
            }

            yield return new WaitForSeconds(trailPeriod);
        }
    }

    IEnumerator ShrinkTrailCoroutine()
    {
        var size = 1f;
        var Block = trailList.Dequeue();
        var initialTransformSize = Block.transform.localScale;
        var initialColliderSize = Block.GetComponent<BoxCollider>().size;

        while (size > 0)
        {
            size -= .5f * Time.deltaTime;
            Block.transform.localScale = initialTransformSize * size;
            Block.GetComponent<BoxCollider>().size = initialColliderSize * size;
            yield return null;
        }

        Destroy(Block);
    }
}
