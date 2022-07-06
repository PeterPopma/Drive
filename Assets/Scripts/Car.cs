using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    [SerializeField] Transform wheelBL;
    [SerializeField] Transform wheelBR;
    [SerializeField] Transform wheelFL;
    [SerializeField] Transform wheelFR;
    private ThirdPersonController thirdPersonController;

    void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();
    }

    // Update is called once per frame
    void Update()
    {
        wheelBL.Rotate(new Vector3(thirdPersonController.Speed * Time.deltaTime * 100, 0, 0), Space.Self);
        wheelBR.Rotate(new Vector3(thirdPersonController.Speed * Time.deltaTime * 100, 0, 0), Space.Self);
        wheelFL.Rotate(new Vector3(thirdPersonController.Speed * Time.deltaTime * 100, 0, 0), Space.Self);
        wheelFR.Rotate(new Vector3(thirdPersonController.Speed * Time.deltaTime * 100, 0, 0), Space.Self);

        // respawn
        if (transform.position.x < -500 || transform.position.x > 500 || transform.position.z < -500 || transform.position.z > 500)
        {
            transform.position = new Vector3(UnityEngine.Random.value * 1000 - 500, 1000, UnityEngine.Random.value * 1000 - 500);
            transform.position = new Vector3(transform.position.x, Terrain.activeTerrain.SampleHeight(transform.position), transform.position.z);
        }
    }

}
