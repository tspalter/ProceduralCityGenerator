using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Cartesian grid accelerated data structure
// Grid of cells, each containing a list of Vectors
public class GridStorage : MonoBehaviour
{
    Vector3 _worldDimensions;
    Vector3 _gridDimensions;
    List<List<List<Vector3>>> _grid;
    float _dsepSq;
    Vector3 _origin;

    // worldDimensions assumes origin of 0,0
    // dsep - Separation distance between samples
    public GridStorage(Vector3 worldDimensions, Vector3 origin, float dsep)
    {
        this._worldDimensions = worldDimensions;
        this._dsepSq = dsep * dsep;
        this._gridDimensions = worldDimensions / dsep;
        this._grid = new List<List<List<Vector3>>>();
        this._origin = origin;
        for (int i = 0; i < this._gridDimensions.x; i++)
        {
            this._grid.Add(new List<List<Vector3>>());
            for (int j = 0; j < this._gridDimensions.z; j++)
            {
                this._grid[i].Add(new List<Vector3>());
            }
        }
    }

    // Add all samples from another grid to this one
    public void addAll(GridStorage gridStorage)
    {
        foreach (List<List<Vector3>> row in gridStorage._grid)
        {
            foreach (List<Vector3> cell in row)
            {
                foreach (Vector3 sample in cell)
                {
                    this.addSample(sample);
                }
            }
        }
    }

    public void addPolyline(List<Vector3> line)
    {
        foreach (Vector3 v in line)
        {
            this.addSample(v);
        }
    }

    // Does not enforce separation or clone
    public void addSample(Vector3 sample, Vector3 coords = default)
    {
        if (coords.Equals(default(Vector3)) || this.vectorOutOfBounds(coords, this._gridDimensions))
        {
            coords = this.getSampleCoords(sample);
        }

        this._grid[(int)coords.x][(int)coords.z].Add(sample);
    }

    // Tests whether vector is at least d distance away from samples
    // Performance important step - called at every integration step
    // could be dtest if we integrate a streamline
    public bool isValidSample(Vector3 vec, float dSq = default(float))
    {
        if (dSq == default(float))
            dSq = this._dsepSq;

        // Code duplication with this.getNearbyPoints but much slower when calling
        // this.getNearbyPoints due to array creation in that method

        Vector3 coords = this.getSampleCoords(vec);

        // Check samples in 9 cells in 3x3 grid
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                Vector3 cell = coords + new Vector3(i, 0f, j);
                if (!this.vectorOutOfBounds(cell, this._gridDimensions))
                {
                    if (!this.vectorFarFromVectors(vec, this._grid[(int)cell.x][(int)cell.z], dSq))
                        return false;
                }
            }
        }

        return true;
    }

    private bool vectorOutOfBounds(Vector3 cell, Vector3 gridDimensions)
    {
        return (cell.x < 0 || cell.z < 0 || cell.x >= gridDimensions.x || cell.z >= gridDimensions.z);
    }

    private bool vectorFarFromVectors(Vector3 vec, List<Vector3> vectors, float dSq)
    {
        foreach (Vector3 sample in vectors)
        {
            if (sample != vec)
            {
                float distanceSq = Mathf.Pow(Vector3.Distance(sample, vec), 2);
                if (distanceSq < dSq)
                    return false;
            }
        }

        return true;
    }

    public List<Vector3> getNearbyPoints(Vector3 vec, float distance)
    {
        int radius = (int)Mathf.Ceil((distance / Mathf.Sqrt(this._dsepSq)) - 0.5f);
        Vector3 coords = this.getSampleCoords(vec);
        List<Vector3> output = new List<Vector3>();
        for (int i = -1 * radius; i <= 1 * radius; i++)
        {
            for (int j = -1 * radius; j <= 1 * radius; j++)
            {
                Vector3 cell = coords + new Vector3(i, j);
                if (!this.vectorOutOfBounds(cell, this._gridDimensions))
                {
                    foreach (Vector3 vec2 in this._grid[(int)cell.x][(int)cell.z])
                        output.Add(vec2);
                }
            }
        }

        return output;
    }

    private Vector3 worldToGrid(Vector3 vec)
    {
        return vec - this._origin;
    }

    private Vector3 gridToWorld(Vector3 vec)
    {
        return vec + this._origin;
    }

    private Vector3 getSampleCoords(Vector3 sample)
    {
        Vector3 vec = this.worldToGrid(sample);
        if (this.vectorOutOfBounds(vec, this._worldDimensions))
        {
            return Vector3.zero;
        }

        return new Vector3(Mathf.Floor(vec.x / Mathf.Sqrt(this._dsepSq)), Mathf.Floor(vec.z / Mathf.Sqrt(this._dsepSq)));
    }
}
