using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceCells : MonoBehaviour
{
    [SerializeField] private GameObject defaultCell;
    [SerializeField] private int cellAmount;
    [SerializeField] private LayerMask layerMask;

    public List<Vector2> points = new List<Vector2>();

    private void Awake()
    {
        SpawnCells();
    }

    public void SpawnCells()
    {
        for (int i = 0; i < cellAmount; i++)
        {
            int xLoc = Random.Range(-10, 10);
            int yLoc = Random.Range(-10, 10);

            //int xScale = Random.Range(1, 6);
            //int yScale = Random.Range(1, 6);

            Vector2 spawnLoc = new Vector2(xLoc, yLoc);
            

            //if (!Physics.CheckBox(spawnLoc, new Vector3(xScale + 2, yScale + 2, 0)))
            //{
            //    GameObject cell = Instantiate(defaultCell, spawnLoc, transform.rotation);
            //    cell.transform.localScale = new Vector2(xScale, yScale);
            //}
        }


    }
}    