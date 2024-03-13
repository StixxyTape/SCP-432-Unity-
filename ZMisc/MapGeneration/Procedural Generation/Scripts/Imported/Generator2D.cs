using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;

public class Generator2D : MonoBehaviour
{
    enum CellType
    {
        None,
        Room,
        Hallway,
        Light,
        Pipe,
        PipeCorner
    }

    static readonly Vector2Int[] neighbors = {
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
    };

    class Room
    {
        public RectInt bounds;

        public Room(Vector2 location, Vector2 size)
        {
            bounds = new RectInt(new Vector2Int(((int)location.x), ((int)location.y)),
                                 new Vector2Int(((int)size.x), ((int)size.y))
                                );
        }

        public static bool Intersect(Room a, Room b)
        {
            return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x)
                || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y));
        }
    }

    [SerializeField]
    Vector2Int size;
    [SerializeField]
    int roomCount;
    [SerializeField]
    Vector2Int roomMaxSize;
    [SerializeField]
    GameObject cubePrefab;
    [SerializeField]
    GameObject wallPrefab;
    [SerializeField]
    GameObject wallParent;
    [SerializeField]
    GameObject hallwayPrefab;
    [SerializeField]
    GameObject hallwayParent;

    [SerializeField]
    GameObject lightBulb;
    [SerializeField]
    GameObject lightBulbParent;
    [SerializeField, Range(0.0f, 1f)]
    float lightBulbSpawnRate;

    [SerializeField]
    GameObject pipeMain;
    [SerializeField]
    GameObject pipeCorner;
    [SerializeField]
    GameObject pipeEdge;
    [SerializeField]
    GameObject pipeParent;
    [SerializeField, Range(0.0f, 1f)]
    float pipeSpawnRate;
    [SerializeField]
    private LayerMask pipeWallCheckLayer;
    [SerializeField]
    private LayerMask pipeCheckLayer;

    private List<Vector2> pipeStartLoc = new List<Vector2>();


    Random random;
    Grid2D<CellType> grid;
    Grid2D<CellType> objectGrid;
    List<Room> rooms;
    Delaunay2D delaunay;
    HashSet<Prim.Edge> selectedEdges;

    public int seed;

    void Start()
    {
        Generate();
    }

    void Generate()
    {
        random = new Random(seed);

        //roomCount = random.Next(25, 200);
        //size = new Vector2Int(random.Next(20, 50), random.Next(20, 50));

        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        objectGrid = new Grid2D<CellType>(grid.Size, grid.Offset);
        rooms = new List<Room>();

        PlaceRooms();
        Triangulate();
        CreateHallways();
        PathfindHallways();
    }

    void PlaceRooms()
    {
        for (int i = 0; i < roomCount; i++)
        {
            Vector2 roomSize = Vector2.one;

            Vector2 location = new Vector2(
                random.Next(3, size.x - 3),
                random.Next(3, size.y - 3)
            );
            
            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location + new Vector2(-1, -1), roomSize + new Vector2(2, 2));

            foreach (var room in rooms)
            {
                if (Room.Intersect(room, buffer))
                {
                    add = false;
                    break;
                }
            }

            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x
                || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y)
            {
                add = false;
            }

            if (add)
            {
                rooms.Add(newRoom);

                foreach (var pos in newRoom.bounds.allPositionsWithin)
                {
                    grid[pos] = CellType.Room;
                }
            }
        }
    }

    void Triangulate()
    {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms)
        {
            vertices.Add(new Vertex<Room>(room.bounds.position + ((Vector2)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay2D.Triangulate(vertices);
    }

    void CreateHallways()
    {
        List<Prim.Edge> edges = new List<Prim.Edge>();

        foreach (var edge in delaunay.Edges)
        {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);

        selectedEdges = new HashSet<Prim.Edge>(mst);
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (var edge in remainingEdges)
        {
            if (random.NextDouble() < 0.125)
            {
                selectedEdges.Add(edge);
            }
        }
    }

    void PathfindHallways()
    {
        DungeonPathfinder2D aStar = new DungeonPathfinder2D(size);

        List<Vector2Int> roomDelta = new List<Vector2Int>();

        int pipeStart = 0;

        foreach (var edge in selectedEdges)
        {
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;

            var startPosf = startRoom.bounds.center;
            var endPosf = endRoom.bounds.center;
            var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
            var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);

            var path = aStar.FindPath(startPos, endPos, (DungeonPathfinder2D.Node a, DungeonPathfinder2D.Node b) => {
                var pathCost = new DungeonPathfinder2D.PathCost();

                pathCost.cost = Vector2Int.Distance(b.Position, endPos);    //heuristic

                if (grid[b.Position] == CellType.Room)
                {
                    pathCost.cost += 10;
                }
                else if (grid[b.Position] == CellType.None)
                {
                    pathCost.cost += 5;
                }
                else if (grid[b.Position] == CellType.Hallway)
                {
                    pathCost.cost += 1;
                }

                if (grid[b.Position] == CellType.Hallway)
                {
                    pathCost.traversable = false;
                }
                else
                {
                    pathCost.traversable = true;
                }

                return pathCost;
            });

            if (path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    var current = path[i];

                    if (grid[current] == CellType.None)
                    {
                        grid[current] = CellType.Hallway;
                    }

                    if (i > 0)
                    {
                        var prev = path[i - 1];
                        var delta = current - prev;

                        if (grid[current] != CellType.Room)
                        {
                            PlaceHallway(current, delta);
                        }

                        if (i > 1)
                        {
                            var prevDelta = prev - path[i - 2];

                            if (grid[prev] == CellType.Hallway)
                            {
                                CreateWalls(prev, prevDelta);
                            }

                            if (grid[current] == CellType.Room)
                            {
                                roomDelta.Add(delta);
                            }
                        }
                    }
                }
            }

        }

        foreach (Room room in rooms)
        {
            int pipeCriteria = 0;

            foreach (Vector2Int offset in neighbors)
            {
                if (grid[room.bounds.position + offset] == CellType.None)
                {
                    pipeCriteria += 1;
                }

                if (pipeCriteria >= 3 && random.NextDouble() < pipeSpawnRate && pipeStart < 10 && objectGrid[room.bounds.position] != CellType.Pipe)
                {
                    objectGrid[room.bounds.position] = CellType.Pipe;

                    pipeStartLoc.Add(room.bounds.position);

                    PlacePipes(room.bounds.position);

                    pipeStart += 1;
                }


            }

            if (grid[room.bounds.position] == CellType.Room)
            {
                PlaceHallway(room.bounds.position, roomDelta[rooms.IndexOf(room)]);
                PlaceRoomWalls(room.bounds.position, roomDelta[rooms.IndexOf(room)]);
                grid[room.bounds.position] = CellType.Hallway;
                
            }
            
        }

    }

    void PlaceHallway(Vector2Int location, Vector2Int direction)
    {
        Instantiate(hallwayPrefab, new Vector3(location.x, location.y, 0), Quaternion.Euler(0, 0, -90), hallwayParent.transform);
    }
    void CreateWalls(Vector2Int prev, Vector2Int prevDelta)
    {
        if (prevDelta.y == 0)
        {
            if (prevDelta.x != 0)
            {
                if (grid[new Vector2Int(prev.x, prev.y + 1)] == CellType.None)
                {
                    Instantiate(wallPrefab, new Vector3(prev.x, prev.y + 0.5f), Quaternion.Euler(0, 0, 90), wallParent.transform);

                    if (random.NextDouble() < lightBulbSpawnRate && objectGrid[prev] != CellType.Light)
                    {
                        Instantiate(lightBulb, new Vector3(prev.x, prev.y + 0.335f), Quaternion.Euler(new Vector3(0, 0, 180)), lightBulbParent.transform);
                        objectGrid[prev] = CellType.Light;
                    }
                }
                if (grid[new Vector2Int(prev.x, prev.y - 1)] == CellType.None)
                {
                    Instantiate(wallPrefab, new Vector3(prev.x, prev.y - 0.5f), Quaternion.Euler(0, 0, 90), wallParent.transform);

                    if (random.NextDouble() < lightBulbSpawnRate && objectGrid[prev] != CellType.Light)
                    {
                        Instantiate(lightBulb, new Vector3(prev.x, prev.y - 0.35f), Quaternion.identity, lightBulbParent.transform);
                        objectGrid[prev] = CellType.Light;
                    }
                }
            }

            if (grid[prev + prevDelta] == CellType.None)
            {
                if (prevDelta.x < 0)
                {
                    Instantiate(wallPrefab, new Vector3(prev.x - 0.5f, prev.y), Quaternion.Euler(0, 0, 0), wallParent.transform);

                    if (random.NextDouble() < lightBulbSpawnRate && objectGrid[prev] != CellType.Light)
                    {
                        Instantiate(lightBulb, new Vector3(prev.x - 0.35f, prev.y), Quaternion.Euler(new Vector3(0, 0, 270)), lightBulbParent.transform);
                        objectGrid[prev] = CellType.Light;
                    }
                }
                else if (prevDelta.x > 0)
                {
                    Instantiate(wallPrefab, new Vector3(prev.x + 0.5f, prev.y), Quaternion.Euler(0, 0, 0), wallParent.transform);

                    if (random.NextDouble() < lightBulbSpawnRate && objectGrid[prev] != CellType.Light)
                    {
                        Instantiate(lightBulb, new Vector3(prev.x + 0.35f, prev.y), Quaternion.Euler(new Vector3(0, 0, 90)), lightBulbParent.transform);
                        objectGrid[prev] = CellType.Light;
                    }
                }
            }
        }

        if (prevDelta.x == 0)
        {
            if (prevDelta.y != 0)
            {
                if (grid[new Vector2Int(prev.x + 1, prev.y)] == CellType.None)
                {
                    Instantiate(wallPrefab, new Vector3(prev.x + 0.5f, prev.y), Quaternion.identity, wallParent.transform);

                    if (random.NextDouble() < lightBulbSpawnRate && objectGrid[prev] != CellType.Light)
                    {
                        Instantiate(lightBulb, new Vector3(prev.x + 0.35f, prev.y), Quaternion.Euler(new Vector3(0, 0, 90)), lightBulbParent.transform);
                        objectGrid[prev] = CellType.Light;
                    }
                }
                if (grid[new Vector2Int(prev.x - 1, prev.y)] == CellType.None)
                {
                    Instantiate(wallPrefab, new Vector3(prev.x - 0.5f, prev.y), Quaternion.identity, wallParent.transform);

                    if (random.NextDouble() < lightBulbSpawnRate && objectGrid[prev] != CellType.Light)
                    {
                        Instantiate(lightBulb, new Vector3(prev.x - 0.35f, prev.y), Quaternion.Euler(new Vector3(0, 0, 270)), lightBulbParent.transform);
                        objectGrid[prev] = CellType.Light;
                    }
                }
            }

            if (grid[prev + prevDelta] == CellType.None)
            {
                if (prevDelta.y < 0)
                {
                    Instantiate(wallPrefab, new Vector3(prev.x, prev.y - 0.5f), Quaternion.Euler(0, 0, 90), wallParent.transform);

                    if (random.NextDouble() < lightBulbSpawnRate && objectGrid[prev] != CellType.Light)
                    {
                        Instantiate(lightBulb, new Vector3(prev.x, prev.y - 0.35f), Quaternion.identity, lightBulbParent.transform);
                        objectGrid[prev] = CellType.Light;
                    }
                }
                else if (prevDelta.y > 0)
                {
                    Instantiate(wallPrefab, new Vector3(prev.x, prev.y + 0.5f), Quaternion.Euler(0, 0, 90), wallParent.transform);

                    if (random.NextDouble() < lightBulbSpawnRate && objectGrid[prev] != CellType.Light)
                    {
                        Instantiate(lightBulb, new Vector3(prev.x, prev.y + 0.35f), Quaternion.Euler(new Vector3(0, 0, 180)), lightBulbParent.transform);
                        objectGrid[prev] = CellType.Light;
                    }
                }
            }
        }
    }

    void PlaceRoomWalls(Vector2Int current, Vector2Int delta)
    {
        if (delta.y == 0)
        {
            if (grid[new Vector2Int(current.x, current.y + 1)] == CellType.None)
            {
                Instantiate(wallPrefab, new Vector3(current.x, current.y + 0.5f), Quaternion.Euler(0, 0, 90), wallParent.transform);

                if (random.NextDouble() < lightBulbSpawnRate && objectGrid[current] != CellType.Light)
                {
                    Instantiate(lightBulb, new Vector3(current.x, current.y + 0.35f), Quaternion.Euler(new Vector3(0, 0, 180)), lightBulbParent.transform);
                    objectGrid[current] = CellType.Light;
                }
            }
            if (grid[new Vector2Int(current.x, current.y - 1)] == CellType.None)
            {
                Instantiate(wallPrefab, new Vector3(current.x, current.y - 0.5f), Quaternion.Euler(0, 0, 90), wallParent.transform);

                if (random.NextDouble() < lightBulbSpawnRate && objectGrid[current] != CellType.Light)
                {
                    Instantiate(lightBulb, new Vector3(current.x, current.y - 0.35f), Quaternion.identity, lightBulbParent.transform);
                    objectGrid[current] = CellType.Light;
                }
            }

            if (grid[current + delta] == CellType.None)
            {
                if (delta.x < 0)
                {
                    Instantiate(wallPrefab, new Vector3(current.x - 0.5f, current.y, 0), Quaternion.identity, wallParent.transform);

                    if (random.NextDouble() < lightBulbSpawnRate && objectGrid[current] != CellType.Light)
                    {
                        Instantiate(lightBulb, new Vector3(current.x - 0.35f, current.y), Quaternion.Euler(new Vector3(0, 0, 270)), lightBulbParent.transform);
                        objectGrid[current] = CellType.Light;
                    }
                }

                else if (delta.x > 0)
                {
                    Instantiate(wallPrefab, new Vector3(current.x + 0.5f, current.y, 0), Quaternion.identity, wallParent.transform);

                    if (random.NextDouble() < lightBulbSpawnRate && objectGrid[current] != CellType.Light)
                    {
                        Instantiate(lightBulb, new Vector3(current.x + 0.35f, current.y), Quaternion.Euler(new Vector3(0, 0, 90)), lightBulbParent.transform);
                        objectGrid[current] = CellType.Light;
                    }
                }

            }

            if (delta.x < 0 && grid[current - delta] == CellType.None)
            {
                Instantiate(wallPrefab, new Vector3(current.x + 0.5f, current.y), Quaternion.identity, wallParent.transform);

                if (random.NextDouble() < lightBulbSpawnRate && objectGrid[current] != CellType.Light)
                {
                    Instantiate(lightBulb, new Vector3(current.x + 0.35f, current.y), Quaternion.Euler(new Vector3(0, 0, 90)), lightBulbParent.transform);
                    objectGrid[current] = CellType.Light;
                }
            }

            else if (delta.x > 0 && grid[current - delta] == CellType.None)
            {
                Instantiate(wallPrefab, new Vector3(current.x - 0.5f, current.y), Quaternion.identity, wallParent.transform);

                if (random.NextDouble() < lightBulbSpawnRate && objectGrid[current] != CellType.Light)
                {
                    Instantiate(lightBulb, new Vector3(current.x - 0.35f, current.y), Quaternion.Euler(new Vector3(0, 0, 270)), lightBulbParent.transform);
                    objectGrid[current] = CellType.Light;
                }
            }
                
        }

        if (delta.x == 0)
        {
            if (grid[new Vector2Int(current.x + 1, current.y)] == CellType.None)
            {
                Instantiate(wallPrefab, new Vector3(current.x + 0.5f, current.y), Quaternion.identity, wallParent.transform);

                if (random.NextDouble() < lightBulbSpawnRate && objectGrid[current] != CellType.Light)
                {
                    Instantiate(lightBulb, new Vector3(current.x + 0.35f, current.y), Quaternion.Euler(new Vector3(0, 0, 90)), lightBulbParent.transform);
                    objectGrid[current] = CellType.Light;
                }
            }
            if (grid[new Vector2Int(current.x - 1, current.y)] == CellType.None)
            {
                Instantiate(wallPrefab, new Vector3(current.x - 0.5f, current.y), Quaternion.identity, wallParent.transform);

                if (random.NextDouble() < lightBulbSpawnRate && objectGrid[current] != CellType.Light)
                {
                    Instantiate(lightBulb, new Vector3(current.x - 0.35f, current.y), Quaternion.Euler(new Vector3(0, 0, 270)), lightBulbParent.transform);
                    objectGrid[current] = CellType.Light;
                }
            }

            if (grid[current + delta] == CellType.None)
            {
                if (delta.y < 0)
                {
                    Instantiate(wallPrefab, new Vector3(current.x, current.y - 0.5f), Quaternion.Euler(0, 0, 90), wallParent.transform);

                    if (random.NextDouble() < lightBulbSpawnRate && objectGrid[current] != CellType.Light)
                    {
                        Instantiate(lightBulb, new Vector3(current.x, current.y - 0.35f), Quaternion.identity, lightBulbParent.transform);
                        objectGrid[current] = CellType.Light;
                    }
                }
                    
                else if (delta.y > 0)
                {
                    Instantiate(wallPrefab, new Vector3(current.x, current.y + 0.5f), Quaternion.Euler(0, 0, 90), wallParent.transform);

                    if (random.NextDouble() < lightBulbSpawnRate && objectGrid[current] != CellType.Light)
                    {
                        Instantiate(lightBulb, new Vector3(current.x, current.y + 0.35f), Quaternion.Euler(new Vector3(0, 0, 180)), lightBulbParent.transform);
                        objectGrid[current] = CellType.Light;
                    }
                }
                    
            }

            if (delta.y < 0 && grid[current - delta] == CellType.None)
            {
                Instantiate(wallPrefab, new Vector3(current.x, current.y + 0.5f), Quaternion.Euler(0, 0, 90), wallParent.transform);

                if (random.NextDouble() < lightBulbSpawnRate && objectGrid[current] != CellType.Light)
                {
                    Instantiate(lightBulb, new Vector3(current.x, current.y + 0.35f), Quaternion.Euler(new Vector3(0, 0, 180)), lightBulbParent.transform);
                    objectGrid[current] = CellType.Light;
                }
            }
                
            else if (delta.y > 0 && grid[current - delta] == CellType.None)
            {
                Instantiate(wallPrefab, new Vector3(current.x, current.y - 0.5f), Quaternion.Euler(0, 0, 90), wallParent.transform);

                if (random.NextDouble() < lightBulbSpawnRate && objectGrid[current] != CellType.Light)
                {
                    Instantiate(lightBulb, new Vector3(current.x, current.y - 0.35f), Quaternion.identity, lightBulbParent.transform);
                    objectGrid[current] = CellType.Light;
                }
            }
                
        }
    }

    void PlacePipes(Vector2Int position)
    {
        List<Vector2Int> lastPos = new List<Vector2Int>();
        lastPos.Add(position);
        List<Vector2Int> lastPos2 = new List<Vector2Int>();

        Vector3 rotation = Vector3.zero;
        Vector2Int newPosition = position;
        Vector2 newOffset = Vector2.zero;

        bool keepBranching = true;
        int clockCount = 0;

        int pipeSpawnCounter = 0;

        Vector2Int delta = Vector2Int.zero;

        foreach (Vector2Int offset in neighbors)
        {
            if (grid[offset + position] == CellType.Hallway || grid[offset + position] == CellType.Room)
            {
                delta = offset;
            }
        }

        while (keepBranching)
        { 
            int succesfulBranch = 0;
            pipeSpawnCounter -= 1;

            // If moving up
            if (delta.y > 0)
            {
                rotation = Vector3.zero;

                clockCount = 0;
                newOffset = new Vector2(-0.295f, 0);
            }
            // If moving right
            if (delta.x > 0)
            {
                rotation = rotation = new Vector3(0, 0, 90);

                clockCount = 1;
                newOffset = new Vector2(0, 0.295f);
            }
            // If moving down
            if (delta.y < 0)
            {
                rotation = new Vector3(0, 0, 180);

                clockCount = 2;
                newOffset = new Vector2(0.295f, 0);
            }
            // If moving Left
            if (delta.x < 0)
            {
                rotation = rotation = new Vector3(0, 0, 270);

                clockCount = 3;
                newOffset = new Vector2(0, -0.295f);
            }

            for (int i = 0; i < neighbors.Length; i++)
            {
                if (newPosition + neighbors[clockCount] == lastPos[0] ||
                    Physics2D.Raycast(newPosition, neighbors[clockCount], 1f, pipeWallCheckLayer).collider != null ||
                    grid[newPosition + neighbors[clockCount]] == CellType.None)
                {
                    clockCount += 1;

                    if (clockCount >= neighbors.Length)
                    {
                        clockCount -= neighbors.Length;
                    }


                }
                else if (newPosition + neighbors[clockCount] != lastPos[0])
                {
                    if (grid[newPosition + neighbors[clockCount]] == CellType.Hallway || grid[newPosition + neighbors[clockCount]] == CellType.Room)
                    {
                        // Rounding Corners

                        //If going up
                        if (newPosition.y > lastPos[0].y && newPosition != position)
                        {
                            // If going left
                            if (newPosition.x + neighbors[clockCount].x - newPosition.x < 0)
                            {
                                GameObject corner = Instantiate(pipeCorner, new Vector3(newPosition.x - 0.295f, newPosition.y - 0.3512f), Quaternion.identity, pipeParent.transform);
                                corner.GetComponent<SpriteRenderer>().flipX = true;
                                
                                pipeSpawnCounter = 1;

                                objectGrid[newPosition] = CellType.PipeCorner;
                                Debug.Log(newPosition + "PipeCorner");

                                if (objectGrid[lastPos[0]] == CellType.PipeCorner)
                                {
                                    Instantiate(pipeEdge, new Vector3(newPosition.x - .295f, newPosition.y - .5f), Quaternion.identity, pipeParent.transform);
                                }
                            }
                            // If going right
                            else if (newPosition.x + neighbors[clockCount].x - newPosition.x > 0)
                            {
                                GameObject corner = Instantiate(pipeCorner, new Vector3(newPosition.x - .295f, newPosition.y + .2388f), Quaternion.identity, pipeParent.transform);

                                Instantiate(pipeMain, new Vector3(newPosition.x + 0.1f, newPosition.y + .295f), Quaternion.Euler(new Vector3(0, 0, 90)), pipeParent.transform);
                                Instantiate(pipeMain, new Vector3(newPosition.x - .295f, newPosition.y), Quaternion.identity, pipeParent.transform);

                                pipeSpawnCounter = 1;

                                objectGrid[newPosition] = CellType.PipeCorner;
                                Debug.Log(newPosition + "PipeCorner");
                            }
                        }
                        // If going left
                        if (newPosition.x < lastPos[0].x && newPosition != position)
                        {
                            // If going up
                            if (newPosition.y + neighbors[clockCount].y - newPosition.y > 0)
                            {
                                GameObject corner = Instantiate(pipeCorner, new Vector3(newPosition.x - .295f, newPosition.y - .23875f), Quaternion.identity, pipeParent.transform);
                                corner.GetComponent<SpriteRenderer>().flipY = true;

                                Instantiate(pipeMain, new Vector3(newPosition.x - .295f, newPosition.y), Quaternion.identity, pipeParent.transform);
                                Instantiate(pipeMain, new Vector3(newPosition.x, newPosition.y - .295f), Quaternion.Euler(new Vector3(0, 0, 90)), pipeParent.transform);

                                pipeSpawnCounter = 1;

                                objectGrid[newPosition] = CellType.PipeCorner;
                                Debug.Log(newPosition + "PipeCorner");
                            }
                            // If going down
                            else if (newPosition.y + neighbors[clockCount].y - newPosition.y < 0)
                            {
                                GameObject corner = Instantiate(pipeCorner, new Vector3(newPosition.x + .295f, newPosition.y - .3512f), Quaternion.identity, pipeParent.transform);

                                pipeSpawnCounter = 1;

                                objectGrid[newPosition] = CellType.PipeCorner;
                                Debug.Log(newPosition + "PipeCorner");

                                if (objectGrid[lastPos[0]] == CellType.PipeCorner)
                                {
                                    Instantiate(pipeEdge, new Vector3(newPosition.x + .5f, newPosition.y -.295f), Quaternion.Euler(new Vector3(0, 0, 90)), pipeParent.transform);
                                }
                            }
                        }
                        // If going right
                        if (newPosition.x > lastPos[0].x && newPosition != position)
                        {
                            // If going up
                            if (newPosition.y + neighbors[clockCount].y - newPosition.y > 0)
                            {
                                GameObject corner = Instantiate(pipeCorner, new Vector3(newPosition.x - .295f, newPosition.y + .3513f), Quaternion.identity, pipeParent.transform);
                                corner.GetComponent<SpriteRenderer>().flipY = true;
                                corner.GetComponent<SpriteRenderer>().flipX = true;

                                pipeSpawnCounter = 1;

                                objectGrid[newPosition] = CellType.PipeCorner;
                                Debug.Log(newPosition + "PipeCorner");

                                if (objectGrid[lastPos[0]] == CellType.PipeCorner)
                                {
                                    Instantiate(pipeEdge, new Vector3(newPosition.x - .5f, newPosition.y + .295f), Quaternion.Euler(new Vector3(0, 0, 90)), pipeParent.transform);
                                }
                            }
                            // If going down
                            else if (newPosition.y + neighbors[clockCount].y - newPosition.y < 0)
                            {
                                GameObject corner = Instantiate(pipeCorner, new Vector3(newPosition.x + .295f, newPosition.y + .2388f), Quaternion.identity, pipeParent.transform);
                                corner.GetComponent<SpriteRenderer>().flipX = true;

                                Instantiate(pipeMain, new Vector3(newPosition.x + .295f, newPosition.y), Quaternion.identity, pipeParent.transform);
                                Instantiate(pipeMain, new Vector3(newPosition.x, newPosition.y + .295f), Quaternion.Euler(new Vector3(0, 0, 90)), pipeParent.transform);

                                pipeSpawnCounter = 1;

                                objectGrid[newPosition] = CellType.PipeCorner;
                                Debug.Log(newPosition + "PipeCorner");
                            }
                        }
                        // If going down
                        if (newPosition.y < lastPos[0].y && newPosition != position)
                        {
                            // If going left
                            if (newPosition.x + neighbors[clockCount].x - newPosition.x < 0)
                            {
                                GameObject corner = Instantiate(pipeCorner, new Vector3(newPosition.x + .295f, newPosition.y - .2387f), Quaternion.identity, pipeParent.transform);
                                corner.GetComponent<SpriteRenderer>().flipY = true;
                                corner.GetComponent<SpriteRenderer>().flipX = true;

                                Instantiate(pipeMain, new Vector3(newPosition.x, newPosition.y - .295f), Quaternion.Euler(new Vector3(0, 0, 90)), pipeParent.transform);
                                Instantiate(pipeMain, new Vector3(newPosition.x + .295f, newPosition.y), Quaternion.identity, pipeParent.transform);

                                pipeSpawnCounter = 1;

                                objectGrid[newPosition] = CellType.PipeCorner;
                                Debug.Log(newPosition + "PipeCorner");
                            }
                            // If going right
                            else if (newPosition.x + neighbors[clockCount].x - newPosition.x > 0)
                            {
                                GameObject corner = Instantiate(pipeCorner, new Vector3(newPosition.x + .295f, newPosition.y + .3513f), Quaternion.identity, pipeParent.transform);
                                corner.GetComponent<SpriteRenderer>().flipY = true;

                                pipeSpawnCounter = 1;

                                objectGrid[newPosition] = CellType.PipeCorner;
                                Debug.Log(newPosition + "PipeCorner");

                                if (objectGrid[lastPos[0]] == CellType.PipeCorner)
                                {
                                    Instantiate(pipeEdge, new Vector3(newPosition.x + .295f, newPosition.y + .5f), Quaternion.identity, pipeParent.transform);
                                }
                            }
                        }

                        

                        if (pipeSpawnCounter <= 0)
                        {
                            Instantiate(pipeMain, new Vector3(newPosition.x + newOffset.x, newPosition.y + newOffset.y), Quaternion.Euler(rotation), pipeParent.transform);
                            objectGrid[newPosition] = CellType.Pipe;
                            Debug.Log(newPosition+"Pipe");
                        }

                        if (lastPos[0] != Vector2Int.zero)
                        {
                            lastPos2.Clear();
                            lastPos2.Add(lastPos[0]);
                        }

                        lastPos.Clear();
                        lastPos.Add(newPosition);

                        newPosition += neighbors[clockCount];
                        succesfulBranch += 1;
                        delta = newPosition - lastPos[0];

                        i = neighbors.Length;
                    }
                }

            }

            if (succesfulBranch == 0)
            {
                keepBranching = false;
                Instantiate(pipeMain, new Vector3(newPosition.x + newOffset.x, newPosition.y + newOffset.y), Quaternion.Euler(rotation), pipeParent.transform);
            }
        }

    }
}
