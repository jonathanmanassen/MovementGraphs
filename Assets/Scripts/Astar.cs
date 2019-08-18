using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#region PriorityQueue

/// <summary>
/// Simple Priority Queue class that sorts on dequeue
/// </summary>
public class PriorityQueue<T>
{
    private List<KeyValuePair<T, float>> elements = new List<KeyValuePair<T, float>>();

    public int Count
    {
        get { return elements.Count; }
    }

    public void Enqueue(T item, float priority)
    {
        elements.Add(new KeyValuePair<T, float>(item, priority));
    }

    // Returns the Vector3Int that has the lowest priority
    public T Dequeue()
    {
        int bestIndex = 0;

        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i].Value < elements[bestIndex].Value)
            {
                bestIndex = i;
            }
        }

        T bestItem = elements[bestIndex].Key;
        elements.RemoveAt(bestIndex);
        return bestItem;
    }
}

#endregion

public class Astar : MonoBehaviour
{
    public bool createLookup = false;

    GridPoint start;
    GridPoint goal;
    float[,] lookupRegular;
    float[,] lookupPOV;
    float[,] currentLookup;

    Vector3[] pos = new Vector3[3] { new Vector3(3.9f, 0, 4.3f), new Vector3(4.45f, 0, -3.4f), new Vector3(-3.3f, 0f, -1f) };

    void Start()
    {
        if (createLookup)
        {
            lookupRegular = CreateLookupTable("Assets/Lookup/LookupTable.txt", RegularGrid.graph);
            lookupPOV = CreateLookupTable("Assets/Lookup/LookupTablePOV.txt", PovGraph.graph);
        }
        else
        {
            lookupRegular = LoadLookup("Assets/Lookup/LookupTable.txt");
            lookupPOV = LoadLookup("Assets/Lookup/LookupTablePOV.txt");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            CreateAndMakePath(HeuristicNull, RegularGrid.graph, lookupRegular);
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            CreateAndMakePath(HeuristicEuclidian, RegularGrid.graph, lookupRegular);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            CreateAndMakePath(HeuristicCluster, RegularGrid.graph, lookupRegular);
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            CreateAndMakePath(HeuristicNull, PovGraph.graph, lookupPOV, false);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            CreateAndMakePath(HeuristicEuclidian, PovGraph.graph, lookupPOV, false);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            CreateAndMakePath(HeuristicCluster, PovGraph.graph, lookupPOV, false);
        }
    }

    #region lookup

    /// <summary>
    /// Creates the lookup Table for the cluster algorithm
    /// </summary>
    float[,] CreateLookupTable(string file, Graph graph)
    {
        string path = file;
        StreamWriter writer = new StreamWriter(path, false); //creates a writable file

        float[,] lookup = new float[6, 6];
        for (int i = 0; i < 6; i++)
            for (int j = 0; j < 6; j++)
                lookup[i, j] = 0;
        for (int i = 0; i < 6; i++)   //double loop on all clusters
        {
            for (int j = 0; j < 6; j++)
            {
                if (i != j && lookup[i, j] == 0) //no need to calculate the pair both ways
                {
                    float min = Mathf.Infinity;
                    foreach (GridPoint start in graph.clusters[i])
                    {
                        foreach (GridPoint goal in graph.clusters[j])
                        {
                            this.start = start;
                            this.goal = goal;

                            FindPath(HeuristicEuclidian);  //performs a*

                            if (goal.costSoFar < min)
                                min = goal.costSoFar;  //checks the smallest cost for a path to all nodes of each cluster
                            CleanGraph(graph);
                        }
                    }
                    lookup[i, j] = min;
                    lookup[j, i] = min;
                }
                writer.WriteLine(lookup[i, j]); //write the results to a file to avoid doing this at every start.
            }
        }
        writer.Close();
        return lookup;
    }

    /// <summary>
    /// Loads the lookup Table for the cluster algorithm
    /// </summary>
    float[,] LoadLookup(string file)
    {
        string path = file;
        int i = 0;
        int j = 0;

        StreamReader reader = new StreamReader(path);
        string str;

        float [,]lookup = new float[6, 6];
        while ((str = reader.ReadLine()) != null)
        {
            lookup[i, j] = float.Parse(str);
            j++;
            if (j == 6)
            {
                i++;
                j = 0;
            }
        }
        reader.Close();
        return lookup;
    }

    #endregion

    /// <summary>
    /// Resets the variables linked to A* in the graph
    /// </summary>
    void CleanGraph(Graph graph)
    {
        foreach (GridPoint point in graph.nodes)
        {
            point.nodeState = nodeState.NONE;
            point.costSoFar = 0;
            point.prev = null;
        }
    }

    /// <summary>
    /// Determines the start and goal nodes, performs A* and create the path
    /// </summary>
    void CreateAndMakePath(System.Func<GridPoint, float> Heuristic, Graph graph, float[,] lookup, bool smooth = true)
    {
        CleanGraph(graph);

        int random = Random.Range(0, 3);

        currentLookup = lookup;
        start = CreateStartGoalPoints(graph, pos[random], -1, true);

        int randomGoal;
        do
        {
            randomGoal = Random.Range(0, 3);
        } while (random == randomGoal);
        
        goal = CreateStartGoalPoints(graph, pos[randomGoal], start.cluster);
        FindPath(Heuristic);

        GridPoint current = goal;

        List<Vector3> path = new List<Vector3>();
        while (!current.Equals(start))
        {
            current.nodeState = nodeState.PATH;
            path.Insert(0, current.pos);
            current = current.prev;
        }
        path.Add(goal.pos);
        if (smooth)
            path = smoothPath(path, graph);

        start.nodeState = nodeState.START_END;
        goal.nodeState = nodeState.START_END;

        AIMovement.instance.SetPath(path);
        start.prev = null;
    }

    /// <summary>
    /// Smooths out the path for the Regular Grid
    /// </summary>
    List<Vector3>   smoothPath(List<Vector3> path, Graph graph)
    {
        List<Vector3> newPath = new List<Vector3>();

        newPath.Add(path[0]);
        for (int i = 2; i < path.Count - 1; i++)
        {
            Vector3 pos = newPath[newPath.Count - 1];
            Vector3 direction = path[i] - newPath[newPath.Count - 1];
            float distance = Vector3.Distance(path[i], newPath[newPath.Count - 1]);

            if (Physics.SphereCast(pos, 0.2f, direction, out RaycastHit hit, distance) || Physics.Raycast(pos, direction, distance))
            {
                newPath.Add(path[i - 1]);
                graph.nodes.Find(x => x.pos == path[i - 1]).nodeState = nodeState.SMOOTH_PATH;
            }
        }
        newPath.Add(path[path.Count - 1]);
        return newPath;
    }

    #region Heuristic

    /// <summary>
    /// Dijkstra algorithm
    /// </summary>
    float HeuristicNull(GridPoint node)
    {
        return 0;
    }

    /// <summary>
    /// Returns the distance between current node and goal node
    /// </summary>
    float HeuristicEuclidian(GridPoint node)
    {
        return Vector3.Distance(node.pos, goal.pos);
    }

    /// <summary>
    /// Returns the lookup value between both clusters
    /// </summary>
    float HeuristicCluster(GridPoint node)
    {
        if (start.cluster == goal.cluster)
            return HeuristicEuclidian(node);
        return currentLookup[node.cluster, goal.cluster];
    }

    #endregion

    /// <summary>
    /// Performs A*
    /// </summary>
    void FindPath(System.Func<GridPoint, float> Heuristic)
    {
        PriorityQueue<GridPoint> list = new PriorityQueue<GridPoint>(); //open List

        list.Enqueue(start, 0f);

        while (list.Count > 0)  //while there are still nodes to process
        {
            GridPoint current = list.Dequeue();

            if (goal.costSoFar != 0 && current.costSoFar + Heuristic(current) > goal.costSoFar) //if the estimated total-cost is bigger than the costSoFar of the goalNode A* is finished
                break;

            if (current.Equals(goal))
            {
                continue;
            }

            foreach (Connection conn in current.connections)
            {
                float newCost = current.costSoFar + conn.distance;
                if (conn.neighbour.costSoFar == 0 || newCost < conn.neighbour.costSoFar) //if it has not been processed or has been found again in a faster path process node
                {
                    conn.neighbour.costSoFar = newCost;
                    conn.neighbour.prev = current;

                    float priority = newCost + Heuristic(conn.neighbour);
                    list.Enqueue(conn.neighbour, priority);
                    conn.neighbour.nodeState = nodeState.OPEN;
                }
            }
        }
    }

    /// <summary>
    /// Finds Start / End node from position
    /// </summary>
    private GridPoint CreateStartGoalPoints(Graph graph, Vector3 pos, int avoidLayer = -1, bool place = false, int i = 0)
    {
        Vector3 distPlane = new Vector3(0f, 0.5f, 0f);

        Collider[] colliders = Physics.OverlapBox(pos, distPlane, Quaternion.identity);
        int layer = colliders[0].gameObject.layer - 10;
        float min = Mathf.Infinity;

        GridPoint tmp = graph.clusters[layer][0];
        foreach (GridPoint p in graph.clusters[layer])
        {
            float dist = Vector3.Distance(pos, p.pos);
            if (dist < min)
            {
                min = dist;
                tmp = p;
            }
        }
        if (place)
            AIMovement.instance.transform.position = new Vector3(pos.x, -0.25f, pos.z);
        return tmp;
    }
}
