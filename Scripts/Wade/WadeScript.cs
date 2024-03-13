using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WadeScript : MonoBehaviour
{
    [SerializeField] private LayerMask HiddenLayer;
    [SerializeField] private LayerMask ObjectLayer;
    [SerializeField] private float wadeSpeed;
    [SerializeField] GameObject beacon;

    private GameObject bob;
    private Rigidbody2D rb;

    private bool pissedOff;
    private float currentAngle;

    private void Awake()
    {
        bob = GameObject.FindGameObjectWithTag("Bob");
        rb = GetComponent<Rigidbody2D>();

        pissedOff = false;
    }

    // This just sets the target for Wade, making him rotate and move towards it
    private void Update()
    {

        Shit();
        //CastVisionRays();

    }

    // Whenever Wade runs into a trigger
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Bob"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void Shit()
    {
        if (pissedOff)
        {
            Vector2 targetPos;
            targetPos = new Vector2(bob.transform.position.x - transform.position.x, bob.transform.position.y - transform.position.y);

            Quaternion targetRotation = Quaternion.LookRotation(transform.forward, targetPos);
            Quaternion rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 100);

            rb.MoveRotation(rotation);
            rb.velocity = transform.up * wadeSpeed;
        }
    }

    private void CastVisionRays()
    {
        float currentAngle = VectorFloatAngle(transform.up);
        float raycastAngle = 90f;

        List<Vector2> hitPosList = new List<Vector2>();
        List<float> hitDistList = new List<float>();

        hitPosList.Clear();
        hitDistList.Clear();


        for (int i = 0; i < 4; i++)
        {
            Debug.DrawRay(transform.position, AngleVector(currentAngle + i * raycastAngle), Color.black);

            RaycastHit2D raycast2D = Physics2D.Raycast(transform.position, AngleVector(currentAngle + i * raycastAngle), 200f, ObjectLayer);

            if (raycast2D.collider == null)
            {
                Debug.Log("Bruh");
            }
            else 
            {
                Debug.Log("Hit!");
                hitPosList.Add(raycast2D.point);
                hitDistList.Add(Vector2.Distance(raycast2D.point, new Vector2(transform.position.x, transform.position.y)));
            }

            if (i == 3)
            {
                float bruh = Mathf.Max(hitDistList.ToArray());
                int moment = hitDistList.IndexOf(bruh);

                Vector2 markPos = hitPosList[moment];

                if (Physics2D.OverlapCircle(markPos, .2f).CompareTag("Beacon"))
                {
                    Debug.Log("Burp");
                }
                else
                {
                    Instantiate(beacon, markPos, transform.rotation);
                }
            }
        }
    }

    // Wade starts his wind up to chase the player
    IEnumerator UrFucked(float windUp)
    {
        Debug.Log("RUN!");

        if (!pissedOff)
        {
            yield return new WaitForSeconds(windUp);
            pissedOff = true;
            Debug.Log("Ya done goofed");
        }

    }

    private Vector3 AngleVector(float angle)
    {
        float angleRad = angle * (Mathf.PI / 180f);
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }

    private float VectorFloatAngle(Vector3 dir)
    {
        dir = dir.normalized;
        float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (n < 0) n += 360;

        return n;
    }
}
