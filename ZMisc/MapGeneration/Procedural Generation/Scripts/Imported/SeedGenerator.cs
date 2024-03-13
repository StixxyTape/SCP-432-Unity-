using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeedGenerator : MonoBehaviour
{
    private void Awake()
    {
        int seed = Random.Range(0, 100000);
        GetComponent<Generator2D>().seed = 10;
    }
}
