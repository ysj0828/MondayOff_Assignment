using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Random = System.Random;
using DG.Tweening;

public class Obstacle : MonoBehaviour
{
    public Obstacle()
    {
        broken = true;
    }

    private int ballRequirement;
    private int ballEntered;

    public int CutCascades = 5;
    public float ForceOnBreak = 100f;

    [SerializeField] private Camera cam;

    private Vector3 offset = new Vector3(1f, 0f, 0f);
    public TextMeshProUGUI ballRequirementText;

    private bool edgeSet;
    private Vector3 edgeVertex;
    private Vector2 edgeUV;
    private Plane edgePlane = new Plane();

    [SerializeField] private bool broken = false;

    private void Start()
    {
        if (broken)
        {
            Invoke("DOTweenBrokenPieces", 0.5f);
        }

        else
        {
            cam = Camera.main;
            Random r = new Random();
            ballRequirement = r.Next(0, 20);
            ballRequirementText.text = $"{ballRequirement}";
        }
    }

    private void DOTweenBrokenPieces()
    {
        float duration = 1.0f;
        float strength = 0.75f;

        transform.DOShakePosition(duration, strength);
        transform.DOShakeRotation(duration, strength);
        transform.DOShakeScale(duration, strength).OnComplete(() =>
        {
            transform.DOScale(Vector3.zero, 1f).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        });
    }

    private void Update()
    {
        if (broken) return;
        Vector3 textPos = cam.WorldToScreenPoint(transform.position + offset);
        if (ballRequirementText.transform.position != textPos) ballRequirementText.transform.position = textPos;
        // if (Input.GetKeyDown(KeyCode.P)) DestroyObstacle();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (broken || other.CompareTag("Wall")) return;

        ballEntered++;

        if (ballEntered >= ballRequirement)
        {
            //break & effect;
            DestroyObstacle();

            ballRequirementText.gameObject.SetActive(false);
        }
    }

    private void DestroyObstacle()
    {
        Mesh originalMesh = GetComponent<MeshFilter>().mesh;
        originalMesh.RecalculateBounds();
        List<NewMeshPart> parts = new List<NewMeshPart>();
        List<NewMeshPart> subParts = new List<NewMeshPart>();

        NewMeshPart mainPart = new NewMeshPart()
        {
            UV = originalMesh.uv,
            Vertices = originalMesh.vertices,
            Normals = originalMesh.normals,
            Triangles = new int[originalMesh.subMeshCount][],
            Bounds = originalMesh.bounds
        };
        for (int i = 0; i < originalMesh.subMeshCount; i++)
            mainPart.Triangles[i] = originalMesh.GetTriangles(i);

        parts.Add(mainPart);

        for (int c = 0; c < CutCascades; c++)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                Bounds bounds = parts[i].Bounds;
                bounds.Expand(0.5f);

                Plane plane = new Plane(UnityEngine.Random.onUnitSphere, new Vector3(UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                                                                                   UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                                                                                   UnityEngine.Random.Range(bounds.min.z, bounds.max.z)));


                subParts.Add(CreateNewMeshPart(parts[i], plane, true));
                subParts.Add(CreateNewMeshPart(parts[i], plane, false));
            }
            parts = new List<NewMeshPart>(subParts);
            subParts.Clear();
        }

        for (int i = 0; i < parts.Count; i++)
        {
            parts[i].MakeGameobject(this);
            parts[i].GO.GetComponent<Rigidbody>().AddForceAtPosition(parts[i].Bounds.center * ForceOnBreak, transform.position);
        }

        Destroy(gameObject);
    }

    private NewMeshPart CreateNewMeshPart(NewMeshPart original, Plane plane, bool left)
    {
        NewMeshPart partMesh = new NewMeshPart() { };
        Ray ray1 = new Ray();
        Ray ray2 = new Ray();

        for (int i = 0; i < original.Triangles.Length; i++)
        {
            int[] triangles = original.Triangles[i];
            edgeSet = false;

            for (int j = 0; j < triangles.Length; j += 3)
            {
                bool sideA = plane.GetSide(original.Vertices[triangles[j]]) == left;
                bool sideB = plane.GetSide(original.Vertices[triangles[j + 1]]) == left;
                bool sideC = plane.GetSide(original.Vertices[triangles[j + 2]]) == left;

                int sideCount = (sideA ? 1 : 0) +
                                (sideB ? 1 : 0) +
                                (sideC ? 1 : 0);
                if (sideCount == 0)
                {
                    continue;
                }
                if (sideCount == 3)
                {
                    partMesh.AddTriangle(i,
                                         original.Vertices[triangles[j]], original.Vertices[triangles[j + 1]], original.Vertices[triangles[j + 2]],
                                         original.Normals[triangles[j]], original.Normals[triangles[j + 1]], original.Normals[triangles[j + 2]],
                                         original.UV[triangles[j]], original.UV[triangles[j + 1]], original.UV[triangles[j + 2]]);
                    continue;
                }

                //cut points
                int singleIndex = sideB == sideC ? 0 : sideA == sideC ? 1 : 2;

                ray1.origin = original.Vertices[triangles[j + singleIndex]];
                Vector3 dir1 = original.Vertices[triangles[j + ((singleIndex + 1) % 3)]] - original.Vertices[triangles[j + singleIndex]];
                ray1.direction = dir1;
                plane.Raycast(ray1, out float enter1);
                float lerp1 = enter1 / dir1.magnitude;

                ray2.origin = original.Vertices[triangles[j + singleIndex]];
                Vector3 dir2 = original.Vertices[triangles[j + ((singleIndex + 2) % 3)]] - original.Vertices[triangles[j + singleIndex]];
                ray2.direction = dir2;
                plane.Raycast(ray2, out float enter2);
                float lerp2 = enter2 / dir2.magnitude;

                //first vertex = ancor
                AddEdge(i,
                        partMesh,
                        left ? plane.normal * -1f : plane.normal,
                        ray1.origin + ray1.direction.normalized * enter1,
                        ray2.origin + ray2.direction.normalized * enter2,
                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));

                if (sideCount == 1)
                {
                    partMesh.AddTriangle(i,
                                        original.Vertices[triangles[j + singleIndex]],
                                        ray1.origin + ray1.direction.normalized * enter1,
                                        ray2.origin + ray2.direction.normalized * enter2,
                                        original.Normals[triangles[j + singleIndex]],
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 2) % 3)]], lerp2),
                                        original.UV[triangles[j + singleIndex]],
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));

                    continue;
                }

                if (sideCount == 2)
                {
                    partMesh.AddTriangle(i,
                                        ray1.origin + ray1.direction.normalized * enter1,
                                        original.Vertices[triangles[j + ((singleIndex + 1) % 3)]],
                                        original.Vertices[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.Normals[triangles[j + ((singleIndex + 1) % 3)]],
                                        original.Normals[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.UV[triangles[j + ((singleIndex + 1) % 3)]],
                                        original.UV[triangles[j + ((singleIndex + 2) % 3)]]);
                    partMesh.AddTriangle(i,
                                        ray1.origin + ray1.direction.normalized * enter1,
                                        original.Vertices[triangles[j + ((singleIndex + 2) % 3)]],
                                        ray2.origin + ray2.direction.normalized * enter2,
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.Normals[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 2) % 3)]], lerp2),
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.UV[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));
                    continue;
                }


            }
        }

        partMesh.FillArrays();

        return partMesh;
    }

    private void AddEdge(int subMesh, NewMeshPart partMesh, Vector3 normal, Vector3 vertex1, Vector3 vertex2, Vector2 uv1, Vector2 uv2)
    {
        if (!edgeSet)
        {
            edgeSet = true;
            edgeVertex = vertex1;
            edgeUV = uv1;
        }
        else
        {
            edgePlane.Set3Points(edgeVertex, vertex1, vertex2);

            partMesh.AddTriangle(subMesh,
                                edgeVertex,
                                edgePlane.GetSide(edgeVertex + normal) ? vertex1 : vertex2,
                                edgePlane.GetSide(edgeVertex + normal) ? vertex2 : vertex1,
                                normal,
                                normal,
                                normal,
                                edgeUV,
                                uv1,
                                uv2);
        }
    }
}

public class NewMeshPart
{
    private List<Vector3> verticies = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<List<int>> triangles = new List<List<int>>();
    private List<Vector2> UVs = new List<Vector2>();
    public Vector3[] Vertices;
    public Vector3[] Normals;
    public int[][] Triangles;
    public Vector2[] UV;
    public GameObject GO;
    public Bounds Bounds = new Bounds();

    public NewMeshPart()
    {

    }

    public void AddTriangle(int submesh, Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector3 normal1, Vector3 normal2, Vector3 normal3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        if (triangles.Count - 1 < submesh)
            triangles.Add(new List<int>());

        triangles[submesh].Add(verticies.Count);
        verticies.Add(vert1);
        triangles[submesh].Add(verticies.Count);
        verticies.Add(vert2);
        triangles[submesh].Add(verticies.Count);
        verticies.Add(vert3);
        normals.Add(normal1);
        normals.Add(normal2);
        normals.Add(normal3);
        UVs.Add(uv1);
        UVs.Add(uv2);
        UVs.Add(uv3);

        Bounds.min = Vector3.Min(Bounds.min, vert1);
        Bounds.min = Vector3.Min(Bounds.min, vert2);
        Bounds.min = Vector3.Min(Bounds.min, vert3);
        Bounds.max = Vector3.Min(Bounds.max, vert1);
        Bounds.max = Vector3.Min(Bounds.max, vert2);
        Bounds.max = Vector3.Min(Bounds.max, vert3);
    }

    public void FillArrays()
    {
        Vertices = verticies.ToArray();
        Normals = normals.ToArray();
        UV = UVs.ToArray();
        Triangles = new int[triangles.Count][];
        for (int i = 0; i < triangles.Count; i++)
            Triangles[i] = triangles[i].ToArray();
    }

    public void MakeGameobject(Obstacle original)
    {
        GO = new GameObject(original.name);
        GO.transform.position = original.transform.position;
        GO.transform.rotation = original.transform.rotation;
        GO.transform.localScale = original.transform.localScale;

        Mesh mesh = new Mesh();
        mesh.name = original.GetComponent<MeshFilter>().mesh.name;

        mesh.vertices = Vertices;
        mesh.normals = Normals;
        mesh.uv = UV;
        for (int i = 0; i < Triangles.Length; i++)
            mesh.SetTriangles(Triangles[i], i, true);
        Bounds = mesh.bounds;

        MeshRenderer renderer = GO.AddComponent<MeshRenderer>();
        renderer.materials = original.GetComponent<MeshRenderer>().materials;

        MeshFilter filter = GO.AddComponent<MeshFilter>();
        filter.mesh = mesh;

        // MeshCollider collider = GO.AddComponent<MeshCollider>();
        // collider.convex = true;

        Rigidbody rigidbody = GO.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        Obstacle obstacle = GO.AddComponent<Obstacle>();
        // obstacle.CutCascades = original.CutCascades;
        // obstacle.ForceOnBreak = original.ForceOnBreak;
    }

}
