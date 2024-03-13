using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FieldOfView : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private GameObject bob;
    private Mesh mesh;
    private Vector3 origin;
    private float startingAngle;
    private float fov;
    private float viewDist;

    private float circleStartingAngle;
    private float circleFov;
    private float circleViewDist;

    private List<Transform> visibleTargets = new List<Transform>();

    private void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        origin = Vector3.zero;
        fov = 90f;
        viewDist = 1f;
        circleViewDist = 0.5f;
        circleFov = 270f;

        //StartCoroutine("ScanForTargets", .2f);
    }

    // This Method handles creating the Cone of Vision
    private void LateUpdate()
    {
        int rayCount = 500;

        float currentAngle = startingAngle;
        float angleIncrease = fov / rayCount;

        Vector3[] vertices = new Vector3[(rayCount + 2) * 2 ];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rayCount * 2 * 3];

        vertices[0] = origin;

        int vertexIndex = 1;
        int triangleIndex = 0;

        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 vertex;
            RaycastHit2D raycastHit2D = Physics2D.Raycast(origin, AngleVector(currentAngle), viewDist, layerMask);

            //Debug.DrawRay(origin, AngleVector(currentAngle), Color.blue);

            if (raycastHit2D.collider == null)
            {
                vertex = origin + AngleVector(currentAngle) * viewDist;
            }
            else
            {
                vertex = raycastHit2D.point + new Vector2(AngleVector(currentAngle).x, AngleVector(currentAngle).y) * 0.1f;
                //vertex = raycastHit2D.point;
                //vertex = origin + AngleVector(currentAngle) * viewDist;
            }
            vertices[vertexIndex] = vertex;

            if (i > 0)
            {
                triangles[triangleIndex + 0] = 0;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex;

                triangleIndex += 3;
            }

            vertexIndex++;

            currentAngle -= angleIncrease;
        }

        int circleRayCount = 500;

        float circleAngle = circleStartingAngle;
        float circleAngleIncrease = 270f / circleRayCount;

        int circleVertexIndex = circleRayCount + 1;
        int circleTriangleIndex = 0 + circleRayCount * 3;

        for (int i = 0; i < circleRayCount; i++)
        {
            Vector3 circleVertex;
            RaycastHit2D raycastHit2D = Physics2D.Raycast(origin, AngleVector(circleAngle), circleViewDist, layerMask);

            //Debug.DrawRay(origin, AngleVector(circleAngle), Color.red);

            if (raycastHit2D.collider == null)
            {
                circleVertex = origin + AngleVector(circleAngle) * circleViewDist;
            }
            else
            {
                circleVertex = raycastHit2D.point + new Vector2(AngleVector(circleAngle).x, AngleVector(circleAngle).y) * .1f;
                //circleVertex = raycastHit2D.point;
                //Debug.Log(AngleVector(circleAngle));
            }

            vertices[circleVertexIndex] = circleVertex;

            if (i > 0)
            {
                triangles[circleTriangleIndex + 0] = 0;
                triangles[circleTriangleIndex + 1] = circleVertexIndex - 1;
                triangles[circleTriangleIndex + 2] = circleVertexIndex;

                circleTriangleIndex += 3;
            }

            circleVertexIndex++;

            circleAngle -= circleAngleIncrease;
        }

        triangles[circleTriangleIndex + 0] = 0;
        triangles[circleTriangleIndex + 1] = circleVertexIndex - 1;
        triangles[circleTriangleIndex + 2] = vertexIndex - vertexIndex + 1;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
    }

    // This Method calls the FindVisibleTargets method every (delay)

    //IEnumerator ScanForTargets(float delay)
    //{
    //    while (true)
    //    {
    //        yield return new WaitForSeconds(delay);
    //        FindVisibleTargets();
    //    }
    //}

    // This Method handles finding all objects with the "BehindMask" layer in the players Cone of Vision

    //public void FindVisibleTargets()
    //{
    //    visibleTargets.Clear();
    //    Collider2D[] targetsInView = Physics2D.OverlapCircleAll(origin, viewDist, targetMask);

    //    for (int i = 0; i < targetsInView.Length; i++)
    //    {
    //        Transform target = targetsInView[i].transform;
    //        Vector2 dirToTarget = (target.position - origin);

    //        Debug.Log(Vector2.Angle(bob.transform.up, dirToTarget));

    //        if (Vector2.Angle (bob.transform.up, dirToTarget) < fov / 2)
    //        {
    //            float distToTarget = Vector2.Distance(origin, target.position);

    //            if (!Physics2D.Raycast (origin, dirToTarget, distToTarget, layerMask))
    //            {
    //                visibleTargets.Add(target);
    //            }
    //        }
    //    }

    //    foreach (Transform visibleTarget in visibleTargets)
    //    {
    //        Debug.DrawLine(origin, visibleTarget.position, Color.red, 2.5f);

    //        if (visibleTarget.CompareTag("Wade"))
    //        {
    //            WadeScript wadeScript = visibleTarget.GetComponent<WadeScript>();
    //            wadeScript.StartCoroutine("UrFucked", 3f);
    //        }
    //    }
    //}


    // Miscellaneous Methods

    public void SetOrigin(Vector3 originSet)
    {
        origin = originSet;
    }
    public void SetAimDirection(Vector3 aimDirection)
    {
        startingAngle = VectorFloatAngle(aimDirection) + fov / 2;
        circleStartingAngle = VectorFloatAngle(aimDirection) + (circleFov + 360) / 2;
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
