using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct NoiseStreamlineParams
{
    public bool noiseEnabled;
    public float noiseSize;
    public float noiseAngle;
}

public class WaterParams : StreamlineParams
{
    public NoiseStreamlineParams coastNoise;
    public NoiseStreamlineParams riverNoise;
    public float riverBankSize;
    public float riverSize;
}

// Integrates polylines to create coastline and river, with controllable noise
public class WaterGenerator : StreamlineGenerator
{
    private int TRIES = 100;
    private bool coastlineMajor = true;
    private List<Vector3> _coastline = new List<Vector3>();
    private List<Vector3> _seaPolygon = new List<Vector3>();
    private List<Vector3> _riverPolygon = new List<Vector3>();
    private List<Vector3> _riverSecondaryRoad = new List<Vector3>();

    protected WaterParams _waterParameters = new WaterParams();
    protected TensorField _tensorField = new TensorField();
    
    public WaterGenerator(Integrator integrator, Vector3 origin, Vector3 worldDimensions, WaterParams parameters, TensorField tensorField) : base (integrator, origin, worldDimensions, parameters)
    {
        this._integrator = integrator;
        this._origin = origin;
        this._worldDimensions = worldDimensions;
        this._waterParameters = parameters;
        this._tensorField = tensorField;

    }

    public List<Vector3> getCoastline()
    {
        return this._coastline;
    }

    public List<Vector3> getSeaPolygon()
    {
        return this._seaPolygon;
    }

    public List<Vector3> getRiverPolyon()
    {
        return this._riverPolygon;
    }

    public List<Vector3> getRiverSecondaryRoad()
    {
        return this._riverSecondaryRoad;
    }

    public void createCoast()
    {
        List<Vector3> coastStreamline = new List<Vector3>();
        Vector3 seed;
        bool major = true;

        if (this._waterParameters.coastNoise.noiseEnabled)
            this._tensorField.enableGlobalNoise(this._waterParameters.coastNoise.noiseAngle, this._waterParameters.coastNoise.noiseSize);

        for (int i = 0; i < this.TRIES; i++)
        {
            major = UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f;
            seed = this.getSeed(major);
            coastStreamline = this.extendStreamline(this.integrateStreamline(seed, major));
            if (this.reachesEdges(coastStreamline))
                break;

        }

        this._tensorField.disableGlobalNoise();

        this._coastline = coastStreamline;
        this.coastlineMajor = major;

        List<Vector3> road = this.simplifyStreamline(coastStreamline);
        this._seaPolygon = this.getSeaPolygon(road);
        this.allStreamlinesSimple.Add(road);
        this._tensorField.sea = this._seaPolygon;

        // create intermediate samples
        List<Vector3> complex = this.complexifyStreamline(road);
        this.grid(major).addPolyline(complex);
        this.streamlines(major).Add(complex);
        this.allStreamlines.Add(complex);
    }

    public void createRiver()
    {
        List<Vector3> riverStreamline = new List<Vector3>();
        Vector3 seed;

        // Need to ignore sea when integrating for edge check
        List<Vector3> oldSea = this._tensorField.sea;
        this._tensorField.sea = new List<Vector3>();
        if (this._waterParameters.riverNoise.noiseEnabled)
            this._tensorField.enableGlobalNoise(this._waterParameters.riverNoise.noiseAngle, this._waterParameters.riverNoise.noiseSize);

        for (int i = 0; i < this.TRIES; i++)
        {
            seed = this.getSeed(!this.coastlineMajor);
            riverStreamline = this.extendStreamline(this.integrateStreamline(seed, !this.coastlineMajor));

            if (this.reachesEdges(riverStreamline))
                break;
            else if (i == this.TRIES - 1)
                Debug.LogError("Water Generator - Failed to find river reaching edge");

        }

        this._tensorField.sea = oldSea;
        this._tensorField.disableGlobalNoise();

        // Create river roads
        List<Vector3> expandedNoisy = this.complexifyStreamline(PolygonUtil.resizeGeometry(riverStreamline, this._waterParameters.riverSize));
        this._riverPolygon = PolygonUtil.resizeGeometry(riverStreamline, this._waterParameters.riverSize - this._waterParameters.riverBankSize);
        // make sure riverPolygon[0] is off screen
        Vector3 firstOff = FindFirstOff(expandedNoisy);
        int firstOffScreen = expandedNoisy.IndexOf(firstOff);
        for (int i = 0; i < firstOffScreen; i++)
        {
            expandedNoisy.Add(expandedNoisy[0]);
            expandedNoisy.RemoveAt(0);
        }

        // Create river roads
        List<Vector3> riverSplitPoly = this.getSeaPolygon(riverStreamline);
        List<Vector3> road1 = new List<Vector3>();
        foreach (Vector3 v in expandedNoisy)
        {
            if (!PolygonUtil.insidePolygon(v, this._seaPolygon) && !this.vectorOffScreen(v) && PolygonUtil.insidePolygon(v, riverSplitPoly))
                road1.Add(v);
        }
        List<Vector3> road1Simple = this.simplifyStreamline(road1);
        List<Vector3> road2 = new List<Vector3>();
        foreach (Vector3 v in expandedNoisy)
        {
            if (!PolygonUtil.insidePolygon(v, this._seaPolygon) && !this.vectorOffScreen(v) && !PolygonUtil.insidePolygon(v, riverSplitPoly))
                road2.Add(v);
        }
        List<Vector3> road2Simple = this.simplifyStreamline(road2);

        if (road1.Count == 0 || road2.Count == 0)
            return;

        if (Mathf.Pow(Vector3.Distance(road1[0], road2[0]), 2) < Mathf.Pow(Vector3.Distance(road1[0], road2[road2.Count - 1]), 2))
            road2Simple.Reverse();

        this._tensorField.river = road1Simple;
        foreach (Vector3 v in road2Simple)
            this._tensorField.river.Add(v);

        // Road 1
        this.allStreamlinesSimple.Add(road1Simple);
        this._riverSecondaryRoad = road2Simple;

        this.grid(!this.coastlineMajor).addPolyline(road1);
        this.grid(!this.coastlineMajor).addPolyline(road2);
        this.streamlines(!this.coastlineMajor).Add(road1);
        this.streamlines(!this.coastlineMajor).Add(road2);
        this.allStreamlines.Add(road1);
        this.allStreamlines.Add(road2);
    }

    private Vector3 FindFirstOff(List<Vector3> expandedNoisy)
    {
        foreach (Vector3 v in expandedNoisy)
        {
            if (this.vectorOffScreen(v))
                return v;
        }

        return new Vector3(Mathf.Infinity, 0f, Mathf.Infinity);
    }

    // Assumes simplified
    // Used for Adding river roads
    private void manuallyAddStreamline(List<Vector3> s, bool major)
    {
        this.allStreamlinesSimple.Add(s);
        // create intermediate samples
        List<Vector3> complex = this.complexifyStreamline(s);
        this.grid(major).addPolyline(complex);
        this.streamlines(major).Add(complex);
        this.allStreamlines.Add(complex);
    }

    // might reverse input array?
    private List<Vector3> getSeaPolygon(List<Vector3> road)
    {
        return PolygonUtil.lineRectanglePolygonIntersection(this._origin, this._worldDimensions, road);
    }

    // insert samples in streamline until separated by dstep
    private List<Vector3> complexifyStreamline(List<Vector3> road)
    {
        List<Vector3> output = new List<Vector3>();
        for (int i = 0; i < road.Count; i++)
        {
            List<Vector3> recursiveStreamline = this.complexifyStreamlineRecursive(road[i], road[i + 1]);
            foreach (Vector3 v in recursiveStreamline)
                output.Add(v);
        }

        return output;
    }

    private List<Vector3> complexifyStreamlineRecursive(Vector3 v1, Vector3 v2)
    {
        if (Mathf.Pow(Vector3.Distance(v1, v2), 2) <= this.paramsSq.dstep)
            return new List<Vector3> { v1, v2 };

        Vector3 d = v2 - v1;
        Vector3 halfway = v1 + (d * 0.5f);
        List<Vector3> complex = this.complexifyStreamlineRecursive(v1, halfway);
        List<Vector3> secondHalf = this.complexifyStreamlineRecursive(halfway, v2);
        foreach (Vector3 v in secondHalf)
            complex.Add(v);

        return complex;
    }

    private List<Vector3> extendStreamline(List<Vector3> streamline)
    {
        streamline.Insert(0, streamline[0] + streamline[0] - (streamline[1].normalized * this._waterParameters.dstep * 5));
        streamline.Add(streamline[streamline.Count - 1] + streamline[streamline.Count - 1] - (streamline[streamline.Count - 2].normalized * this._waterParameters.dstep * 5));

        return streamline;
    }

    private bool reachesEdges(List<Vector3> streamline)
    {
        return this.vectorOffScreen(streamline[0]) && this.vectorOffScreen(streamline[streamline.Count - 1]);
    }

    private bool vectorOffScreen(Vector3 v)
    {
        Vector3 toOrigin = v - this._origin;
        return toOrigin.x <= 0 || toOrigin.z <= 0 ||
            toOrigin.x >= this._worldDimensions.x || toOrigin.z >= this._worldDimensions.z;
    }
}
