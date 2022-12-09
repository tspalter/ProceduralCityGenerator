using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplexNoise;
using System;

public struct NoiseParams
{
    public bool globalNoise;
    public float noiseSizePark;
    public float noiseAnglePark; // in degrees
    public float noiseSizeGlobal;
    public float noiseAngleGlobal;

    public void defaultParams()
    {
        this.globalNoise = false;
        this.noiseSizePark = 0f;
        this.noiseAnglePark = 0f;
        this.noiseAnglePark = 0f;
        this.noiseAnglePark = 0f;
    }
};

// Combines basis fields
// Noise added when sampling a point in a given parks
public class TensorField : MonoBehaviour
{
    // global vars
    public List<BasisField> basisFields;
    public Noise noise;
    private NoiseParams nParams;

    public DragController d;

    public List<List<Vector3>> parks = new List<List<Vector3>>();
    public List<Vector3> sea = new List<Vector3>();
    public List<Vector3> river = new List<Vector3>();
    public bool ignoreRiver = false;

    public bool smooth = false;

    public void Start()
    {
        this.basisFields = new List<BasisField>();
        this.noise = new Noise();
        this.nParams = new NoiseParams();

        this.parks = new List<List<Vector3>>();
        this.sea = new List<Vector3>();
        this.river = new List<Vector3>();
        this.ignoreRiver = false;
        this.smooth = false;

        // Instantiate(d);
    }

    public TensorField(NoiseParams noiseParams)
    {
        this.nParams = noiseParams;
    }

    public TensorField()
    {

        this.nParams.defaultParams();

    }

    // used to integrate coastline and river
    public void enableGlobalNoise(float angle, float size)
    {
        this.nParams.globalNoise = true;
        this.nParams.noiseAngleGlobal = angle;
        this.nParams.noiseSizeGlobal = size;
    }

    public void disableGlobalNoise()
    {
        this.nParams.globalNoise = false;
    }

    public void addGrid(Vector3 center, int size, float decay, float theta)
    {
        BasisField grid = new Grid(center, size, decay, theta);
        this.addField(grid);
    }

    public void addRadial(Vector3 center, int size, float decay)
    {
        BasisField radial = new Radial(center, size, decay);
        this.addField(radial);
    }

    public void addField(BasisField basisField)
    {
        this.basisFields.Add(basisField);
    }

    protected void removeField(BasisField basisField)
    {
        int index = this.basisFields.IndexOf(basisField);
        if (index > -1)
            this.basisFields.RemoveAt(index);
    }

    private void Reset()
    {
        this.basisFields = new List<BasisField>();
        this.parks = new List<List<Vector3>>();
        this.sea = new List<Vector3>();
        this.river = new List<Vector3>();
    }

    public List<Vector3> getCenterPoints()
    {
        List<Vector3> centerPoints = new List<Vector3>();
        foreach (BasisField b in this.basisFields)
        {
            centerPoints.Add(b._center);
        }

        return centerPoints;
    }

    public List<BasisField> getBasisFields()
    {
        return this.basisFields;
    }

    public Tensor samplePoint(Vector3 point)
    {
        if (!this.onLand(point))
        {
            // degenerate point
            return Tensor.zero();
        }
        // default field is a grid
        if (this.basisFields.Count == 0)
        {
            float[] arr = { 0, 0 };
            return new Tensor(1, arr);
        }

        Tensor tensorAcc = Tensor.zero();

        foreach (BasisField field in this.basisFields)
        {
            tensorAcc.add(field.getWeightedTensor(point, this.smooth), this.smooth);
        }

        // add rotational noise for parks, range of -pi/2 to pi/2
        foreach (List<Vector3> p in this.parks)
        {
            if (PolygonUtil.insidePolygon(point, p))
            {
                tensorAcc.rotate(getRotationalNoise(point, this.nParams.noiseSizePark, this.nParams.noiseAnglePark));
            }
        }

        if (this.nParams.globalNoise)
        {
            tensorAcc.rotate(getRotationalNoise(point, this.nParams.noiseSizeGlobal, this.nParams.noiseAngleGlobal));
        }

        return tensorAcc;

    }

    private float getRotationalNoise(Vector3 point, float noiseSize, float noiseAngle)
    {
        return this.noise.CalcPixel2D((int)point.x, (int)point.z, noiseSize) * noiseAngle * Mathf.PI / 180;
    }

    public bool onLand(Vector3 point)
    {
       bool inSea = PolygonUtil.insidePolygon(point, this.sea);
        if (this.ignoreRiver)
        {
            return !inSea;
        }

        return !inSea && !PolygonUtil.insidePolygon(point, this.river);
    }

    public bool inParks(Vector3 point)
    {
        foreach (List<Vector3> park in parks)
        {
            if (PolygonUtil.insidePolygon(point, park))
                return true;
        }

        return false;
    }
}
