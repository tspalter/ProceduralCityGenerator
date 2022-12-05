using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PolygonParams
{
    public int maxLength;
    public float minArea;
    public float shrinkSpacing;
    public float chanceNoDivide;
    public PolygonParams(int max, float min, float shrink, float chance)
    {
        this.maxLength = max;
        this.minArea = min;
        this.shrinkSpacing = shrink;
        this.chanceNoDivide = chance;
    }
}

// Finds polygon in a graph, used for finding lots and parks
public class PolygonFinder : MonoBehaviour
{
    public List<List<Vector3>> _polygons = new List<List<Vector3>>();
    public List<List<Vector3>> _shrunkPolygons = new List<List<Vector3>>();
    public List<List<Vector3>> _dividedPolygons = new List<List<Vector3>>();
    public List<List<Vector3>> _toShrink = new List<List<Vector3>>();
    public List<List<Vector3>> _toDivide = new List<List<Vector3>>();

    List<Node> _nodes;
    PolygonParams _parameters;
    TensorField _tensorField;

    private void Start()
    {
        this._nodes = new List<Node>();
        this._parameters = new PolygonParams();
        this._tensorField = new TensorField();
    }
    public PolygonFinder(List<Node> nodes, PolygonParams parameters, TensorField tensorField)
    {
        this._nodes = nodes;
        this._parameters = parameters;
        this._tensorField = tensorField;
    }

    public List<List<Vector3>> getPolygons()
    {
        if (this._dividedPolygons.Count > 0)
        {
            return this._dividedPolygons;
        }
        if (this._shrunkPolygons.Count > 0)
        {
            return this._shrunkPolygons;
        }

        return this._polygons;

    }

    public void Reset()
    {
        this._toShrink = new List<List<Vector3>>();
        this._toDivide = new List<List<Vector3>>();
        this._polygons = new List<List<Vector3>>();
        this._shrunkPolygons = new List<List<Vector3>>();
        this._dividedPolygons = new List<List<Vector3>>();
    }

    public bool update()
    {
        bool change = false;
        if (this._toShrink.Count > 0)
        {
            List<Vector3> pop = this._toShrink[this._toShrink.Count - 1];
            this._toShrink.RemoveAt(this._toShrink.Count - 1);
            if (this.stepShrink(pop))
                change = true;
        }

        if (this._toDivide.Count > 0)
        {
            List<Vector3> pop = this._toDivide[this._toDivide.Count - 1];
            this._toDivide.RemoveAt(this._toDivide.Count - 1);
            if (this.stepDivide(pop))
                change = true;
        }

        return change;
    }

    // properly shrink polygon so the edges are all the same distance from the road
    public void shrink(bool animate = false)
    {
        if (this._polygons.Count == 0)
            this.findPolygons();

        if (animate)
        {
            if (this._polygons.Count == 0)
                return;

            this._toShrink = this._polygons;
        }
        else
        {
            this._shrunkPolygons = new List<List<Vector3>>();
            foreach (List<Vector3> p in this._polygons)
                this.stepShrink(p);
        }
    }

    private bool stepShrink(List<Vector3> pop)
    {
        List<Vector3> shrunk = PolygonUtil.resizeGeometry(pop, -this._parameters.shrinkSpacing);
        if (shrunk.Count > 0)
        {
            this._shrunkPolygons.Add(shrunk);
            return true;
        }

        return false;
    }

    public void divide(bool animate = false)
    {
        if (this._polygons.Count == 0)
            this.findPolygons();

        if (animate)
        {
            if (this._polygons.Count == 0)
                return;

            this._toDivide = this._polygons;
        }
        else
        {
            this._toDivide = new List<List<Vector3>>();
            foreach (List<Vector3> p in this._polygons)
                this.stepDivide(p);
        }
    }

    private bool stepDivide(List<Vector3> pop)
    {
        if (this._parameters.chanceNoDivide > 0 && UnityEngine.Random.Range(0, 1) < this._parameters.chanceNoDivide)
        {
            this._dividedPolygons.Add(pop);
            return true;
        }
        List<List<Vector3>> divided = PolygonUtil.subdividePolygon(pop, this._parameters.minArea);
        if (divided.Count > 0)
        {
            foreach (List<Vector3> poly in divided)
                this._dividedPolygons.Add(poly);
            return true;
        }

        return false;
    }

    public void findPolygons()
    {
        this._shrunkPolygons = new List<List<Vector3>>();
        this._dividedPolygons = new List<List<Vector3>>();
        List<List<Vector3>> polys = new List<List<Vector3>>();

        foreach (Node node in this._nodes)
        {
            if (node.adj.Count < 2)
                continue;
            foreach (Node nextNode in node.adj)
            {
                List<Node> nodes = new List<Node> { node, nextNode };
                List<Node> polygon = this.recursiveWalk(nodes);
                if (polygon != null && polygon.Count < this._parameters.maxLength)
                {
                    this.removePolygonAdjacencies(polygon);
                    List<Vector3> poly = new List<Vector3>();
                    foreach (Node n in polygon)
                    {
                        poly.Add(n._position);
                    }
                    polys.Add(poly);
                }
            }
        }

        this._polygons = this.filterPolygonsByWater(polys);
    }

    private List<List<Vector3>> filterPolygonsByWater(List<List<Vector3>> polys)
    {
        List<List<Vector3>> output = new List<List<Vector3>>();
        foreach (List<Vector3> p in polys)
        {
            Vector3 averagePoint = PolygonUtil.averagePoint(p);
            if (this._tensorField.onLand(averagePoint) && !this._tensorField.inParks(averagePoint))
                output.Add(p);
        }
        return output;
    }

    private void removePolygonAdjacencies(List<Node> polygon)
    {
        for (int i = 0; i < polygon.Count; i++)
        {
            Node current = polygon[i];
            Node next = polygon[(i + 1) % polygon.Count];

            int index = current.adj.IndexOf(next);
            if (index >= 0)
                current.adj.RemoveAt(index);
            else
                Debug.LogError("PolygonFinder - node not in adj");
        }
    }

    private List<Node> recursiveWalk(List<Node> nodes, int count = 0)
    {
        if (count >= this._parameters.maxLength)
            return null;

        Node nextNode = this.getRightmostNode(nodes[nodes.Count - 2], nodes[nodes.Count - 1]);
        if (nextNode == null)
            return null; // currently ignores polygon with dead end inside

        int index = nodes.IndexOf(nextNode);
        if (index >= 0)
        {
            nodes.RemoveAt(index);
            return nodes;
        }
        else
        {
            nodes.Add(nextNode);
            return this.recursiveWalk(nodes, count++);
        }
            
    }

    private Node getRightmostNode(Node nodeFrom, Node nodeTo)
    {
        // we want to turn right at every junction
        if (nodeTo.adj.Count == 0)
            return null;

        Vector3 backwardsDifferenceVector = nodeFrom._position - nodeTo._position;
        float transformAngle = Mathf.Atan2(backwardsDifferenceVector.z, backwardsDifferenceVector.x);

        Node rightmostNode = null;
        float smallestTheta = Mathf.PI * 2;

        foreach (Node nextNode in nodeTo.adj)
        {
            if (nextNode != nodeFrom)
            {
                Vector3 nextVector = nextNode._position - nodeTo._position;
                float nextAngle = Mathf.Atan2(nextVector.z, nextVector.x) - transformAngle;
                if (nextAngle < 0)
                {
                    nextAngle += Mathf.PI * 2;
                }

                if (nextAngle < smallestTheta)
                {
                    smallestTheta = nextAngle;
                    rightmostNode = nextNode;
                }
            }
        }

        return rightmostNode;
    }
}
