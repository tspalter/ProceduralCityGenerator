using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterGUI : RoadGUI
{
    protected WaterGenerator streamlines;

    private TensorField _tensorField;

    public WaterGUI(TensorField tensorField, WaterParams parameters, Integrator integrator) : base(parameters, integrator)
    {
        this._tensorField = tensorField;
        Vector3 world = Camera.main.ScreenToWorldPoint(Camera.main.transform.position);
        Vector3 origin = new Vector3(world.x, world.y, world.z);
        this.streamlines = new WaterGenerator(this._integrator, origin, this.worldDims, (WaterParams)this._parameters, this._tensorField);
    }

    public void generateRoads()
    {
        Vector3 world = Camera.main.ScreenToWorldPoint(Camera.main.transform.position);
        Vector3 origin = new Vector3(world.x, world.y, world.z);
        Camera.main.orthographicSize /= UtilFunctions.DRAW_INFLATE_AMOUNT;
        this.streamlines = new WaterGenerator(this._integrator, origin, this.worldDims, (WaterParams)this._parameters, this._tensorField);
        Camera.main.orthographicSize *= UtilFunctions.DRAW_INFLATE_AMOUNT;

        this.streamlines.createCoast();
        this.streamlines.createRiver();
    }

    // Secondary road runs along other side of river
    public List<List<Vector3>> getStreamlinesWithSecondaryRoad()
    {
        List<List<Vector3>> secondaryRoad = new List<List<Vector3>>();
        foreach (List<Vector3> streamline in this.streamlines.allStreamlinesSimple)
        {

            secondaryRoad.Add(streamline);
        }

        List<Vector3> sec = new List<Vector3>();
        foreach (Vector3 p in this.streamlines.getRiverSecondaryRoad())
            secondaryRoad.Add(sec);

        return secondaryRoad;
    }

    public List<Vector3> getRiver()
    {
        List<Vector3> river = new List<Vector3>();
        foreach (Vector3 pt in this.streamlines.getRiverPolyon())
        {
            river.Add(Camera.main.WorldToScreenPoint(pt));
        }

        return river;
    }

    public List<Vector3> getSecondaryRiver()
    {
        List<Vector3> secRiver = new List<Vector3>();
        foreach (Vector3 pt in this.streamlines.getRiverSecondaryRoad())
        {
            secRiver.Add(Camera.main.WorldToScreenPoint(pt));
        }
            

        return secRiver;
    }

    public List<Vector3> getCoastline()
    {
        List<Vector3> coastline = new List<Vector3>();
        foreach (Vector3 pt in this.streamlines.getCoastline())
        {
            coastline.Add(Camera.main.WorldToScreenPoint(pt));
        }


        return coastline;
    }

    public List<Vector3> getSeaPolygon()
    {
        List<Vector3> sea = new List<Vector3>();
        foreach (Vector3 pt in this.streamlines.getSeaPolygon())
        {
            Vector3 point = new Vector3(pt.x, pt.y, pt.z);
            sea.Add(Camera.main.WorldToScreenPoint(point));
        }


        return sea;
    }
}
