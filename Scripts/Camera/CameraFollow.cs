using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private GameObject bob;

    // Follow the player
    private void Update()
    {
        transform.position = new Vector3(bob.transform.position.x, bob.transform.position.y, transform.position.z);
    }
}
