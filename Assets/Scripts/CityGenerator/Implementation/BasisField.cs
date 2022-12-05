using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum FIELD_TYPE
{
    RADIAL,
    GRID
};
public abstract class BasisField
{
    // global vars
    string FOLDER_NAME;
    public FIELD_TYPE fieldType;
    protected int folderNameIndex = 0;
    public Vector3 _center;
    public int _size;
    public float _decay;

    protected BasisField() { }

    BasisField(Vector3 center, int size, float decay)
    {
        this._center = center;
        this._size = size;
        this._decay = decay;
    }

    void setCenter(Vector3 center)
    {
        this._center = center;
    }

    Vector3 getCenter()
    {
        return this._center;
    }

    void setDecay(float decay)
    {
        this._decay = decay;
    }

    void setSize(int size)
    {
        this._size = size;
    }

    public Tensor getWeightedTensor(Vector3 point, bool smooth)
    {
        return this.getTensor(point).scale(this.getTensorWeight(point, smooth));
    }

    public abstract Tensor getTensor(Vector3 point);


    // interpolates between 0 and 1
    protected float getTensorWeight(Vector3 point, bool smooth)
    {
        Vector3 diff = Vector3.zero;
        diff.x = point.x - this._center.x;
        diff.z = point.z - this._center.z;
        float normalDistToCenter = diff.magnitude / this._size;
        if (smooth)
        {
            return Mathf.Pow(normalDistToCenter, -this._decay);
        }
        if (this._decay == 0 && normalDistToCenter >= 1)
        {
            return 0;
        }
        return Mathf.Pow(Mathf.Max(0, 1 - normalDistToCenter), this._decay);
    }
}

public class Grid : BasisField
{
    string FOLDER_NAME = "Grid ";
    FIELD_TYPE fieldType = FIELD_TYPE.GRID;
    public float _theta;
    public Vector3 center;
    public float size;
    public float decay;

    public Grid(Vector3 center, int size, float decay, float theta)
    {
        this.FOLDER_NAME += folderNameIndex++;
        this._theta = theta;
        this._center = center;
        this._size = size;
        this._decay = decay;
    }

    public override Tensor getTensor(Vector3 point)
    {
        float cos = Mathf.Cos(2 * this._theta);
        float sin = Mathf.Sin(2 * this._theta);
        float[] arr = { cos, sin };
        return new Tensor(1, arr);
    }

    void setTheta(float theta)
    {
        this._theta = theta;
    }

}

public class Radial : BasisField
{
    string FOLDER_NAME = "Radial ";
    FIELD_TYPE fieldType = FIELD_TYPE.RADIAL;

    public Radial(Vector3 center, int size, float decay)
    {
        this.FOLDER_NAME += folderNameIndex++;
        this._center = center;
        this._size = size;
        this._decay = decay;
    }

    public override Tensor getTensor(Vector3 point)
    {
        Vector3 t = Vector3.zero;
        t.x = point.x - this._center.x;
        t.z = point.z - this._center.z;
        float t1 = Mathf.Pow(t.z, 2) - Mathf.Pow(t.x, 2);
        float t2 = -2 * t.x * t.z;
        float[] arr = { t1, t2 };
        return new Tensor(1, arr);
    }
}