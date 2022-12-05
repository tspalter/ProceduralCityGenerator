using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Flatbush;
using System;

public struct Segment
{
    public Vector3 _from;
    public Vector3 _to;

    public Segment(Vector3 v1, Vector3 v2)
    {
        this._from = v1;
        this._to = v2;
    }

}

public struct Intersect
{
    public Vector3 point;
    public List<Segment> segments;
}

// Node located along any intersection or point along the simplified road polylines
public class Node : IQuadTreeObject
{
    Vector2 IQuadTreeObject.GetPosition() { return new Vector2(this._position.x, this._position.z); }                   
    public List<Segment> _segments = new List<Segment>();
    public List<Node> _neighbors = new List<Node>();
    public List<Node> adj = new List<Node>();
    public Vector3 _position = Vector3.zero;

    public Node(Vector3 value, List<Node> neighbors)
    {
        this._neighbors = neighbors;
        this._position = value;
    }

    public void addSegment(Segment seg)
    {
        this._segments.Add(seg);
    }

    public void addNeighbor(Node node)
    {
        if (node != this)
        {
            this._neighbors.Add(node);
            this._neighbors.Add(this);
        }
    }
}

public class Graph : MonoBehaviour
{
    float WIDTH = Screen.width;
    float HEIGHT = Screen.height;
    // functions related to intersection detection reproduced from isect.js
    public List<Intersect> bush(List<Segment> lines)
    {
        List<Intersect> intersections = new List<Intersect>();

        SpatialIndex index = new SpatialIndex(lines.Count);
        foreach (Segment line in lines)
            addToIndex(line, index);

        for (int i = 0; i < lines.Count; i++)
        {
            if (checkIntersection(lines[i], i, index, lines))
                return intersections; // stop early
        }

        return intersections;
    }

    private bool checkIntersection(Segment segment, int currentId, SpatialIndex index, List<Segment> lines)
    {
        float minX = segment._from.x;
        float maxX = segment._to.x;
        float minZ = segment._from.z;
        float maxZ = segment._to.z;
        float t;
        if (minX > maxX)
        {
            t = minX;
            minX = maxX;
            maxX = t;
        }
        if (minZ > maxZ)
        {
            t = minZ;
            minZ = maxZ;
            maxZ = t;
        }

        List<int> ids = (List<int>)index.Query(minX, minZ, maxX, maxZ);

        for (int i = 0; i < ids.Count; i++)
        {
            int segmentIndex = ids[i];
            if (segmentIndex <= currentId)
                continue; // we have either reported it or it is current

            Segment otherSegment = lines[segmentIndex];
            Vector3 point = intersectSegments(otherSegment, segment);

            if (point != new Vector3(Mathf.Infinity, 0f, Mathf.Infinity))
            {
                // find intersection
                float segA = (segment._to.z - segment._from.z) / (segment._to.x - segment._from.x);
                float segB = segment._from.z - (segA * segment._from.x);
                float segCheck = Mathf.Abs(point.z - (segA * point.x + segB));
                float otherA = (otherSegment._to.z - otherSegment._from.z) / (otherSegment._to.x - otherSegment._from.x);
                float otherB = otherSegment._from.z - (otherA * otherSegment._from.x);
                float otherCheck = Mathf.Abs(point.z - (otherA * point.x + otherB));

                if (segCheck < 0.0001f && otherCheck < 0.0001f)
                    return true;
            }
        }

        return false;
    }

    private Vector3 intersectSegments(Segment a, Segment b)
    {
        //  for more info on the source of this function check out this link
        //  https://stackoverflow.com/a/1968345/125351
        Vector3 aStart = a._from;
        Vector3 bStart = b._from;
        float p0_x = aStart.x;
        float p0_z = aStart.z;
        float p2_x = bStart.x;
        float p2_z = bStart.z;

        float s1_x = a._to.x - a._from.x;
        float s1_z = a._to.z - a._from.z;
        float s2_x = b._to.x - a._from.x;
        float s2_z = b._to.z - a._from.z;

        float div = s1_x * s2_z - s2_x * s1_z;

        float s = (s1_z * (p0_x - p2_x) - s1_x * (p0_z - p2_z)) / div;
        if (s < 0 || s > 1) 
        { 
            return new Vector3(Mathf.Infinity, 0f, Mathf.Infinity);
        }

        float t = (s2_x * (p2_z - p0_z) + s2_z * (p0_x - p2_x)) / div;

        if (t >= 0 && t <= 1)
        {
            return new Vector3(p0_x - (t * s1_x), p0_z - (t * s1_z));
        }

        return new Vector3(Mathf.Infinity, 0f, Mathf.Infinity);
    }

    private void addToIndex(Segment line, SpatialIndex index)
    {
        float minX = line._from.x;
        float maxX = line._to.x;
        float minZ = line._from.z;
        float maxZ = line._to.z;
        float t;
        if (minX > maxX)
        {
            t = minX;
            minX = maxX;
            maxX = t;
        }
        if (minZ > maxZ)
        {
            t = minZ;
            minZ = maxZ;
            maxZ = t;
        }

        index.Add(minX, minZ, maxX, maxZ);
    }

    // global vars
    public List<Node> _nodes;
    public List<Vector3> _intersections;

    private void Start()
    {
        this._nodes = new List<Node>();
        this._intersections = new List<Vector3>();
    }

    // create a graph from a set of streamlines
    // finds all intersections, and creates a list of Nodes
    public Graph (List<List<Vector3>> streamlines, float dstep, bool deleteDangling = false)
    {
        List<Intersect> intersections = bush(this.streamlinesToSegments(streamlines));
        Rect r = new Rect(this._nodes[0]._position, new Vector3(WIDTH, 0f, HEIGHT));
        QuadTree<Node> quadTree = new QuadTree<Node>(100, r);
        float nodeAddRadius = 0.001f;

        // Add all segment start and endpoints
        foreach (List<Vector3> streamline in streamlines)
        {
            for (int i = 0; i < streamline.Count; i++)
            {
                Node node = new Node(streamline[i], new List<Node>());
                if (i > 0)
                    node.addSegment(this.vectorsToSegment(streamline[i - 1], streamline[i]));

                if (i < streamline.Count - 1)
                    node.addSegment(this.vectorsToSegment(streamline[i], streamline[i + 1]));

                this.fuzzyAddToQuadtree(quadTree, node, nodeAddRadius);
            }
        }

        // Add all intersections
        foreach (Intersect intersection in intersections)
        {
            Node node = new Node(intersection.point, new List<Node>());
            foreach (Segment s in intersection.segments)
                node.addSegment(s);
            this.fuzzyAddToQuadtree(quadTree, node, nodeAddRadius);
        }

        // For each simplified streamline, build list of nodes in order along streamline
        foreach (List<Vector3> streamline in streamlines)
        {
            for (int i = 0; i < streamline.Count - 1; i++)
            {
                List<Node> nodesAlongSegment = this.getNodesAlongSegment(this.vectorsToSegment(streamline[i], streamline[i + 1]), quadTree, nodeAddRadius, dstep);
                if (nodesAlongSegment.Count > 1)
                {
                    for (int j = 0; j < nodesAlongSegment.Count - 1; j++)
                    {
                        nodesAlongSegment[j].addNeighbor(nodesAlongSegment[j + 1]);
                    }
                }
                else
                {
                    Debug.LogError("Error Graph.cs: segment with less than 2 nodes");
                }
            }
        }
  
        foreach (Node n in quadTree.RetrieveObjectsInArea(r))
        {
            if (deleteDangling)
            {
                this.deleteDanglingNodes(n, quadTree);
            }
            n.adj = n._neighbors;
        }

        this._nodes = quadTree.RetrieveObjectsInArea(r);
        this._intersections = new List<Vector3>();
        foreach (Intersect i in intersections)
            this._intersections.Add(i.point);
    }

    // Remove dangling edges from graph to facilitate polygon finding
    private void deleteDanglingNodes(Node n, QuadTree<Node> quadTree)
    {
        if (n._neighbors.Count == 1)
        {
            quadTree.Remove(n);
            foreach (Node neighbor in n._neighbors)
            {
                neighbor._neighbors.Remove(n);
                this.deleteDanglingNodes(neighbor, quadTree);
            }
        }
    }

    // given a segment, step along segment and find all nodes along it
    private List<Node> getNodesAlongSegment(Segment segment, QuadTree<Node> quadTree, float radius, float step)
    {
        // walk dstep along each streamline, adding nodes within dstep/2
        // and connected to this streamline (fuzzy - nodeAddRadius) to list, removing from
        // quadtree and adding them all back at the end

        List<Node> foundNodes = new List<Node>();
        List<Node> nodesAlongSegment = new List<Node>();

        Vector3 start = segment._from;
        Vector3 end = segment._to;

        Vector3 diffVector = end - start;
        step = Mathf.Min(step, diffVector.magnitude / 2); // Min of 2 step along vector
        int steps = Mathf.CeilToInt(diffVector.magnitude / step);
        float diffVectorLength = diffVector.magnitude;

        for (int i = 0; i <= steps; i++)
        {
            Vector3 currentPoint = start + (diffVector * (i / steps));

            // Order nodes, not by 'closeness', but by dot product
            List<Node> nodesToAdd = new List<Node>();
            Node closestNode = quadTree.RetrieveObjectsInArea(new Rect(currentPoint.x, currentPoint.y, radius + step / 2, radius + step / 2))[0];

            while (closestNode != null)
            {
                quadTree.Remove(closestNode);
                foundNodes.Add(closestNode);

                bool nodeOnSegment = false;
                foreach (Segment s in closestNode._segments)
                {
                    if (this.fuzzySegmentsEqual(s, segment))
                    {
                        nodeOnSegment = true;
                        break;
                    }
                }

                if (nodeOnSegment)
                    nodesToAdd.Add(closestNode);

                closestNode = quadTree.RetrieveObjectsInArea(new Rect(currentPoint.x, currentPoint.z, radius + step / 2, radius + step / 2))[0];
            }

            nodesToAdd.Sort(delegate (Node first, Node second) {
                return this.dotProductToSegment(first, start, diffVector) - this.dotProductToSegment(second, start, diffVector);
            });
            foreach (Node n in nodesToAdd)
                nodesAlongSegment.Add(n);
        }

        foreach (Node n in foundNodes)
            quadTree.Insert(n);
        return nodesAlongSegment;
    }

    private int dotProductToSegment(Node first, Vector3 start, Vector3 diffVector)
    {
        Vector3 dotVector = first._position - start;
        return (int)Vector3.Dot(diffVector, dotVector);
    }

    private bool fuzzySegmentsEqual(Segment s1, Segment s2, float tolerance = 0.0001f)
    {
        // from
        if (s1._from.x - s2._from.x > tolerance)
            return false;
        if (s1._from.z - s2._from.z > tolerance)
            return false;

        // to
        if (s1._to.x - s2._to.x > tolerance)
            return false;
        if (s1._to.z - s2._to.z > tolerance)
            return false;

        return true;
    }

    private void fuzzyAddToQuadtree(QuadTree<Node> quadTree, Node node, float nodeAddRadius)
    {
        // only add if there isn't a node within radius
        // Remember to check for double radius when querying tree, or point might be missed
        List<Node> existingNodes = quadTree.RetrieveObjectsInArea(new Rect(node._position.x, node._position.z, nodeAddRadius * 2, nodeAddRadius * 2));
        if (existingNodes.Count == 0)
            quadTree.Insert(node);
        else
        {
            Node existingNode = existingNodes[0];
            foreach (Node neighbor in node._neighbors)
                existingNode.addNeighbor(neighbor);
            foreach (Segment seg in node._segments)
                existingNode.addSegment(seg);
        }

    }

    private List<Segment> streamlinesToSegments(List<List<Vector3>> streamlines)
    {
        List<Segment> output = new List<Segment>();
        foreach (List<Vector3> s in streamlines)
        {
            for (int i = 0; i < s.Count - 1; i++)
            {
                output.Add(this.vectorsToSegment(s[i], s[i + 1]));
            }
        }

        return output;
    }

    private Segment vectorsToSegment(Vector3 v1, Vector3 v2)
    {
        return new Segment(v1, v2);
    }
}
