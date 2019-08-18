using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegularGrid : MonoBehaviour
{
    public ColorVue colorVue;

    readonly float step = 0.3f;
    public static GridPoint[,] grid;
    public static Graph graph;

    private void Awake()
    {
        CreateGrid();
        graph = new Graph(grid);
    }

    /// <summary>
    /// Creates a grid by traversing the plane with a step and placing nodes along the way
    /// </summary>
    void CreateGrid()
    {
        int j = 0;
        grid = new GridPoint[(int)(10f / step + 0.5f), (int)(10f / step + 0.5f)]; //the size of the grid depends on the step (distance between nodes)
        Vector3 dist = new Vector3(step / 2f, 0.1f, step / 2f);
        Vector3 distPlane = new Vector3(step / 2f, 0.5f, step / 2f);
        for (float y = -5 + step / 2; y < 5; y += step, j++)   // traverses the plane vertically
        {
            int i = 0;
            for (float x = -5 + step / 2; x < 5; x += step, i++)  // traverses the plane horizontally
            {
                Vector3 pos = new Vector3(x, 0, y);

                Collider[] colliders = Physics.OverlapBox(pos, distPlane, Quaternion.identity);  //gets the plane collider (to know the layer of the object which determines the cluster)
                if (colliders.Length == 0)
                    grid[i, j] = new GridPoint(false, pos, 0); //if it doesn't collide with a plane it is not valid (first parameter)
                else
                    grid[i, j] = new GridPoint(!Physics.CheckBox(pos, dist, Quaternion.identity), pos, colliders[0].gameObject.layer - 10); //otherwise it is valid if it does not collide with a wall)
            }
        }
        CreateConnections();
    }

    /// <summary>
    /// Creates the connections between the nodes by adding the valid nodes around it
    /// </summary>
    private void CreateConnections()
    {
        for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
                if (grid[x, y].valid)
                {
                    for (int i = x - 1; i <= x + 1; i++)
                    {
                        if (i < 0 || i >= grid.GetLength(0))
                            continue;
                        for (int j = y - 1; j <= y + 1; j++)
                        {
                            if (j < 0 || j >= grid.GetLength(1) || (i == x && j == y))
                                continue;

                            if (grid[i, j].valid)
                                grid[x, y].connections.Add(new Connection(Vector3.Distance(grid[x, y].pos, grid[i, j].pos), grid[i, j]));
                        }
                    }
                }
    }

    /// <summary>
    /// Draws the graph on the screen when in editor view and pressing on the RegularGrid object
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        CreateGrid();
        if (graph == null || graph.nodes == null)
            graph = new Graph(grid);
        Vector3 size = new Vector3(step * 0.75f, 0.1f, step * 0.75f);

        foreach (GridPoint point in graph.nodes)
        {
            if (point.valid == true)
            {
                if (colorVue == ColorVue.CLUSTER)
                    Gizmos.color = Misc.GetColorWithVar(point.cluster);
                else if (colorVue == ColorVue.CONNECTIONS)
                    Gizmos.color = Misc.GetColorWithVar(point.connections.Count);
                else if (colorVue == ColorVue.PATH)
                    Gizmos.color = Misc.GetColorWithVar((int)point.nodeState + 2);
                else if (colorVue == ColorVue.NONE)
                    Gizmos.color = Color.white;

                Gizmos.DrawCube(point.pos, size);
            }
        }
    }
}
