using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] Transform vfxHit;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0, 0, 300 * Time.deltaTime), Space.Self);
    }

    public void OnTriggerEnter()
    {
        Instantiate(vfxHit, transform.position, vfxHit.transform.rotation);
        Destroy(gameObject);
    }
}
