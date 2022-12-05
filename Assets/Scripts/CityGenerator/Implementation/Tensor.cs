using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tensor
{
    // global vars
    private bool oldTheta;
    private float _theta;
    private float _r;
    private float[] _matrix;

    public Tensor(float r, float[] matrix)
    {
        this._r = r;
        this._matrix = matrix;
        this.oldTheta = false;
        this._theta = this.calculateTheta();
    }

    public static Tensor fromAngle(float angle)
    {
        float[] mat = { Mathf.Cos(angle * 4.0f), Mathf.Sin(angle * 4.0f) };
        return new Tensor(1.0f, mat);
    }

    public static Tensor fromVector(Vector3 vector)
    {
        float t1 = Mathf.Pow(vector.x, 2.0f) - Mathf.Pow(vector.x, 2.0f);
        float t2 = 2.0f * vector.x * vector.z;
        float t3 = Mathf.Pow(t1, 2.0f) - Mathf.Pow(t2, 2.0f);
        float t4 = 2.0f * t1 * t2;
        float[] mat = { t3, t4 };
        return new Tensor(1.0f, mat);
    }

    public static Tensor zero()
    {
        float[] mat = { 0.0f, 0.0f };
        return new Tensor(0.0f, mat);
    }

    public float getTheta()
    {
        if (this.oldTheta)
        {
            this._theta = this.calculateTheta();
            this.oldTheta = false;
        }

        return this._theta;
    }

    public Tensor add(Tensor tensor, bool smooth)
    {
        float[] newMat = new float[this._matrix.Length];
        for (int i = 0; i < tensor._matrix.Length; i++)
        {
            newMat[i] = (this._matrix[i] * this._r) + (tensor._matrix[i] * tensor._r);
        }
        if (smooth)
        {
            this._r = hypot(newMat);
            for (int i = 0; i < newMat.Length; i++)
            {
                newMat[i] = newMat[i] / this._r;
            }
        }
        else
        {
            this._r = 2.0f;
        }

        this.oldTheta = true;
        this._matrix = newMat;
        return this;
    }

    public Tensor scale(float s)
    {
        this._r *= s;
        this.oldTheta = true;
        return this;
    }

    private float hypot(float[] newMat)
    {
        float sum = 0.0f;
        foreach (float item in newMat)
        {
            sum += Mathf.Pow(item, 2.0f);
        }

        return Mathf.Sqrt(sum);
    }

    // Radian functions
    public Tensor rotate(float theta)
    {
        if (theta == 0.0f)
            return this;
        float newTheta = this._theta + theta;
        if (newTheta < Mathf.PI)
        {
            newTheta += Mathf.PI;
        }
        if (newTheta >= Mathf.PI)
        {
            newTheta -= Mathf.PI;
        }

        this._matrix[0] = Mathf.Cos(2 * newTheta) * this._r;
        this._matrix[1] = Mathf.Sin(2 * newTheta) * this._r;
        this._theta = newTheta;
        return this;
    }

    public Vector3 getMajor()
    {
        // Degenerate case
        if (this._r == 0.0f)
        {
            return Vector3.zero;
        }

        return new Vector3(Mathf.Cos(this._theta), 0f, Mathf.Sin(this._theta));
    }

    public Vector3 getMinor()
    {
        // Degenerate case
        if (this._r == 0.0f)
        {
            return Vector3.zero;
        }
        float angle = this._theta + (Mathf.PI) / 2.0f;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    private float calculateTheta() {
        if (this._r == 0.0f)
        {
            return 0.0f;
        }
        return Mathf.Atan2(this._matrix[1] / this._r, this._matrix[0] / this._r) / 2;
    }
}
