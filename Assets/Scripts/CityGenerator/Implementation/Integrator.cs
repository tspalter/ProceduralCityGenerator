using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Integrator
{
    protected TensorField _field;

    public Integrator() { }

    Integrator(TensorField field)
    {
        this._field = field;
    }

    private void Start()
    {
        this._field = new TensorField();
    }

    public abstract Vector3 integrate(Vector3 point, bool major);

    protected Vector3 sampleFieldVector(Vector3 point, bool major)
    {
        Tensor tensor = this._field.samplePoint(point);
        if (major)
            return tensor.getMajor();

        return tensor.getMinor();
    }

    public bool onLand(Vector3 point)
    {
        return this._field.onLand(point);
    }
}

public class EulerIntegrator : Integrator
{
    StreamlineParams _params;
    public EulerIntegrator(TensorField field, StreamlineParams parameters)
    {
        this._field = field;
        this._params = parameters;
    }

    public override Vector3 integrate(Vector3 point, bool major)
    {
        return this.sampleFieldVector(point, major) * this._params.dstep;
    }
}

public class RK4Integrator : Integrator
{
    StreamlineParams _params;
    public RK4Integrator(TensorField field, StreamlineParams parameters)
    {
        this._field = field;
        this._params = parameters;
    }

    public override Vector3 integrate(Vector3 point, bool major)
    {
        Vector3 k1 = this.sampleFieldVector(point, major);
        Vector3 k23 = this.sampleFieldVector(new Vector3(point.x + (this._params.dstep / 2), point.y, point.z + (this._params.dstep / 2)), major);
        Vector3 k4 = this.sampleFieldVector(new Vector3(point.x + this._params.dstep, point.y, point.z + this._params.dstep), major);

        return k1 + (k23 * 4) + k4 * (this._params.dstep / 6);
    }
}