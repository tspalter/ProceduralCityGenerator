using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BuildingModel
{
    public float height;
    public List<Vector3> lotWorld; // In world space
    public List<Vector3> lotScreen; // in screen space
    public List<Vector3> roof; // in screen space
    public List<List<Vector3>> sides; // in screen space
}


public class BuildingModels
{
    private List<BuildingModel> _buildingModels = new List<BuildingModel>();

    public BuildingModels(List<List<Vector3>> lots) {
        foreach (List<Vector3> lot in lots)
        {
            BuildingModel b = new BuildingModel();
            b.height = UnityEngine.Random.Range(0f, 1f) * 20f + 20f;
            b.lotWorld = lot;
            b.lotScreen = new List<Vector3>();
            b.roof = new List<Vector3>();
            b.sides = new List<List<Vector3>>();
            this._buildingModels.Add(b);
        }

        this._buildingModels.Sort(delegate (BuildingModel a, BuildingModel b)
        {
            return a.height.CompareTo(b.height);
        });
    }

    public List<BuildingModel> GetBuildingModels()
    {
        return this._buildingModels;
    }

    // TODO:  Add in extrusion from base building poly
    //// recalculated when the camera moves
    //public void setBuildingProjections()
    //{
    //    float d = 1000f / (10f / Camera.main.orthographicSize);
    //    Vector3 cameraPos = Camera.main.transform.position;
    //    int i = 0;
    //    foreach (BuildingModel b in this._buildingModels)
    //    {
    //        b.lotScreen[i] = Camera.main.WorldToScreenPoint(b.lotWorld[i]);
    //        b.roof[i] = this.heightVectorToScreen(b.lotScreen[i], b.height, d, cameraPos);
    //        b.sides = this.getBuildingSides(b);
    //    }
    //}

    //private Vector3 heightVectorToScreen(Vector3 vector3, float height, float d, Vector3 cameraPos)
    //{
    //    throw new System.NotImplementedException();
    //}
}

// Finds building lots and optionally creates extrusions of those buildings
public class Buildings
{
    private PolygonFinder _polygonFinder;
    private List<List<Vector3>> allStreamlines = new List<List<Vector3>>();
    private BuildingModels _models = new BuildingModels(new List<List<Vector3>>());
    private List<List<Vector3>> _blocks = new List<List<Vector3>>();

    public PolygonParams buildingParams = new PolygonParams(20, 50f, 4f, 0.05f);
    private bool _animate = false;
    private TensorField _tensorField;
    private float _dstep = 0f;

    public Buildings(TensorField tensorField, float dstep, bool animate)
    {
        this._tensorField = tensorField;
        this._dstep = dstep;
        this._animate = animate;
        this._polygonFinder = new PolygonFinder(new List<Node>(), this.buildingParams, this._tensorField);
    }
    
    public void setAnimate(bool v)
    {
        this._animate = v;
    }

    public bool getAnimate()
    {
        return this._animate;
    }

    public List<List<Vector3>> getLots()
    {
        List<List<Vector3>> lots = new List<List<Vector3>>();
        foreach (List<Vector3> poly in this._polygonFinder._polygons)
        {
            lots.Add(poly);
        }
        return lots;
    }

    // Only used when creating the 3D model to 'fake' the roads
    public void getBlocks()
    {
        List<List<Vector3>> streams2D = new List<List<Vector3>>();
        foreach (List<Vector3> streamline in this.allStreamlines)
        {
            streams2D.Add(streamline);
        }
        Graph g = new Graph(streams2D, this._dstep, true);
        PolygonParams blockParams = this.buildingParams;
        blockParams.shrinkSpacing /= 2f;
        PolygonFinder polygonFinder = new PolygonFinder(g._nodes, blockParams, this._tensorField);
        polygonFinder.findPolygons();
        polygonFinder.shrink(false);
    }

    public List<BuildingModel> getModels()
    {
        // this._models.setBuildingProjections();
        return this._models.GetBuildingModels();
    }

    public void setAllStreamlines(List<List<Vector3>> s)
    {
        this.allStreamlines = s;
    }

    public void reset()
    {
        this._polygonFinder.Reset();
        this._models = new BuildingModels(new List<List<Vector3>>());
    }

    public bool Update()
    {
        return this._polygonFinder.update();
    }

    public void generate(bool animate)
    {
        this._models = new BuildingModels(new List<List<Vector3>>());
        List<List<Vector3>> streams2D = new List<List<Vector3>>();
        foreach (List<Vector3> streamline in this.allStreamlines)
        {
            streams2D.Add(streamline);
        }
        Graph g = new Graph(streams2D, this._dstep, true);

        this._polygonFinder = new PolygonFinder(g._nodes, this.buildingParams, this._tensorField);
        this._polygonFinder.findPolygons();

        List<List<Vector3>> polys = new List<List<Vector3>>();
        foreach (List<Vector3> poly in this._polygonFinder._polygons)
        {
            polys.Add(poly);
        }
        this._models = new BuildingModels(polys);
    }
}
