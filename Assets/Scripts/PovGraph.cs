using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PovGraph : MonoBehaviour
{
    public static Graph graph;

    public ColorVue colorVue;

    /// <summary>
    /// Creates a graph from all of the pov nodes
    /// </summary>
    private void CreateGraph()
    {
        graph = new Graph();

        foreach (MeshRenderer mesh in GetComponentsInChildren<MeshRenderer>()) //retrieves all the cubes that are child of this object
        {
            Transform t = mesh.transform;
            if (t != transform)
            {
                int cluster = t.gameObject.layer - 10;  //the clusters are in layer 10-15
                GridPoint tmp = new GridPoint(true, t.position, cluster);  //creates a valid gridpoint with position and cluster nb

                graph.nodes.Add(tmp);  //add it to the graph
                graph.clusters[cluster].Add(tmp);  //add it to the cluster graph
            }
        }
        foreach (GridPoint p in graph.nodes)  //this will create connections
        {
            foreach (GridPoint tmp in graph.nodes)
            {
                if (p == tmp)  //if they are identical, no connection
                    continue;
                if (!Physics.SphereCast(p.pos, 0.2f, tmp.pos - p.pos, out RaycastHit hit, Vector3.Distance(p.pos, tmp.pos))) //checks if there is a collider between both nodes
                {
                    p.connections.Add(new Connection(Vector3.Distance(p.pos, tmp.pos), tmp));                                //if there isn't create a connection
                }
            }
        }
    }

    void Awake()
    {
        CreateGraph();
    }

    /// <summary>
    /// Draws the graph on the screen when in editor view and pressing on the povGraph object
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (graph == null || graph.nodes == null)
            CreateGraph();
        Vector3 size = new Vector3(0.2f, 0.1f, 0.2f);

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
                if (point.prev != null && (point.nodeState == nodeState.PATH || point.nodeState == nodeState.START_END) && colorVue == ColorVue.PATH)
                    Gizmos.DrawLine(point.prev.pos, point.pos);
            }
        }
    }
}
