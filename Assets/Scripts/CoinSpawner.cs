using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    [SerializeField] GameObject pfCoin;

    // Start is called before the first frame update
    void Start()
    {
        for (int k=0; k<500; k++)
        {
            Vector3 spawnPosition = new Vector3(Random.value * 1000 - 500, 1000, Random.value * 1000 - 500);
            float Yposition = Terrain.activeTerrain.SampleHeight(spawnPosition) + 1.2f;
            Instantiate(pfCoin, new Vector3(spawnPosition.x, Yposition, spawnPosition.z), Quaternion.Euler(new Vector3(270, 0, 90)));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
