using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class MainGUI : MonoBehaviour
{
    private int numBigParks = 2;
    private int numSmallParks = 0;
    private bool clusterBigParks = false;

    private List<Vector3> intersections = new List<Vector3>();
    private List<List<Vector3>> bigParks = new List<List<Vector3>>();
    private List<List<Vector3>> smallParks = new List<List<Vector3>>();
    private bool animate = true;
    private int animationSpeed = 30;

    public TensorFieldUI tensorFieldUI;
    public RoadGUI mainRoads;
    public RoadGUI majorRoads;
    public RoadGUI minorRoads;
    public WaterGUI coastline;
    public Buildings buildings;

    // params
    public WaterParams coastlineParams;
    public StreamlineParams mainParams;
    public StreamlineParams majorParams;
    public StreamlineParams minorParams;

    private bool redraw = true;

    public TensorField _tensorField;

    public GameObject radialOrigin = new GameObject();
    public GameObject gridOrigin = new GameObject();

    public void Start()
    {
        // initialize all class objects
        Instantiate(this._tensorField);
        this.tensorFieldUI = Instantiate(this.tensorFieldUI);
        this.minorParams = new StreamlineParams();
        this.majorParams = new StreamlineParams();
        this.mainParams = new StreamlineParams();
        Debug.Log("Instaniated parameter fields");
        //this.minorParams.dsep = 20f;
        //this.minorParams.dtest = 15f;
        //this.minorParams.dstep = 1f;
        //this.minorParams.dlookahead = 40f;
        //this.minorParams.dcirclejoin = 5f;
        //this.minorParams.joinangle = 0.1f; // approx 30 degrees
        //this.minorParams.pathIterations = 1000;
        //this.minorParams.seedTries = 300;
        //this.minorParams.simplifyTolerance = 0.5f;
        //this.minorParams.collideEarly = 0f;
        //Debug.Log("Initialized minor params");
        // this.coastline = new WaterGUI(this._tensorField, new WaterParams(), new RK4Integrator(this._tensorField, this.minorParams));
        this.buildings = new Buildings(this._tensorField, 0f, false);
        Debug.Log("Instaniated additional UI");
    }

    public MainGUI(TensorField tensorField)
    {
        this._tensorField = tensorField;
        Debug.Log("Tensor Field set");
        // Coastline Params
        this.coastlineParams.coastNoise = new NoiseStreamlineParams();
        this.coastlineParams.coastNoise.noiseEnabled = true;
        this.coastlineParams.coastNoise.noiseSize = 30f;
        this.coastlineParams.coastNoise.noiseAngle = 20f;

        this.coastlineParams.riverNoise = new NoiseStreamlineParams();
        this.coastlineParams.riverNoise.noiseEnabled = true;
        this.coastlineParams.riverNoise.noiseSize = 30f;
        this.coastlineParams.riverNoise.noiseAngle = 20f;

        this.coastlineParams.riverBankSize = 10f;
        this.coastlineParams.riverSize = 30f;
        // transfer this data to our minor road params
        this.minorParams = this.coastlineParams;
        this.coastlineParams.pathIterations = 10000;
        this.coastlineParams.simplifyTolerance = 10f;

        // major road params can be initialized to minor road and built from there
        this.majorParams = this.minorParams;
        this.majorParams.dsep = 100f;
        this.majorParams.dtest = 30f;
        this.majorParams.dlookahead = 200f;
        this.majorParams.collideEarly = 0f;

        // same with main road params
        this.mainParams = this.minorParams;
        this.mainParams.dsep = 400f;
        this.mainParams.dtest = 200f;
        this.mainParams.dlookahead = 500f;
        this.mainParams.collideEarly = 0f;
        Debug.Log("Remaining parameter values set");

        Integrator integrator = new RK4Integrator(tensorField, this.minorParams);
        Debug.Log("Instaniated Integrator");
        bool reDraw = this.redraw = true;

        this.coastline = new WaterGUI(tensorField, this.coastlineParams, integrator);
        this.mainRoads = new RoadGUI(this.mainParams, integrator, reDraw);
        this.majorRoads = new RoadGUI(this.majorParams, integrator, reDraw);
        this.minorRoads = new RoadGUI(this.minorParams, integrator, reDraw);
        Debug.Log("Instaniated GUI features");

        // park preset functions
        this.buildings.reset();
        this.addParks();
        this.redraw = true;

        // building preset functions
        this.buildings = new Buildings(tensorField, this.minorParams.dstep, this.animate);
        List<List<Vector3>> allStreamlines = new List<List<Vector3>>();
        foreach (List<Vector3> streamline in this.mainRoads.getAllStreamlines())
        {
            allStreamlines.Add(streamline);
        }
        foreach (List<Vector3> streamline in this.majorRoads.getAllStreamlines())
        {
            allStreamlines.Add(streamline);
        }
        foreach (List<Vector3> streamline in this.minorRoads.getAllStreamlines())
        {
            allStreamlines.Add(streamline);
        }
        foreach (List<Vector3> streamline in this.coastline.getStreamlinesWithSecondaryRoad())
        {
            allStreamlines.Add(streamline);
        }
        this.buildings.setAllStreamlines(allStreamlines);

        // set the existing streamlines
        this.minorRoads.setExistingStreamlines(new List<RoadGUI> { this.coastline, this.mainRoads, this.majorRoads });
        this.majorRoads.setExistingStreamlines(new List<RoadGUI> { this.coastline, this.mainRoads });
        this.mainRoads.setExistingStreamlines(new List<RoadGUI> { this.coastline });

        // coastline preset functions
        this.mainRoads.clearStreamlines();
        this.majorRoads.clearStreamlines();
        this.minorRoads.clearStreamlines();
        this.bigParks = new List<List<Vector3>>();
        this.smallParks = new List<List<Vector3>>();
        this.buildings.reset();
        tensorField.parks = new List<List<Vector3>>();
        tensorField.sea = new List<Vector3>();
        tensorField.river = new List<Vector3>();

        // main road presets
        this.majorRoads.clearStreamlines();
        this.minorRoads.clearStreamlines();
        this.bigParks = new List<List<Vector3>>();
        this.smallParks = new List<List<Vector3>>();
        this.buildings.reset();
        tensorField.parks = new List<List<Vector3>>();
        tensorField.ignoreRiver = true;

        // main road post set
        tensorField.ignoreRiver = false;

        // major road presets
        this.minorRoads.clearStreamlines();
        this.bigParks = new List<List<Vector3>>();
        this.smallParks = new List<List<Vector3>>();
        this.buildings.reset();
        tensorField.parks = new List<List<Vector3>>();
        tensorField.ignoreRiver = true;

        // major road post set
        tensorField.ignoreRiver = false;
        this.addParks();
        this.redraw = true;

        // minor road presets
        this.buildings.reset();
        this.smallParks = new List<List<Vector3>>();
        tensorField.parks = this.bigParks;

        // main road post set
        this.addParks();
    }

    public void toggleAnimation()
    {
        this.majorRoads.setAnimate(!this.majorRoads.getAnimate());
        this.minorRoads.setAnimate(!this.minorRoads.getAnimate());
        this.buildings.setAnimate(!this.buildings.getAnimate());
    }

    private void addParks()
    {
        List<List<Vector3>> allStreams = new List<List<Vector3>>();
        foreach (List<Vector3> streamline in this.majorRoads.getAllStreamlines())
            allStreams.Add(streamline);
        foreach (List<Vector3> streamline in this.mainRoads.getAllStreamlines())
            allStreams.Add(streamline);
        foreach (List<Vector3> streamline in this.minorRoads.getAllStreamlines())
            allStreams.Add(streamline);

        Graph g = new Graph(allStreams, this.minorParams.dstep);
        this.intersections = g._intersections;

        PolygonParams polyParams = new PolygonParams();
        polyParams.maxLength = 20;
        polyParams.minArea = 80f;
        polyParams.shrinkSpacing = 4f;
        polyParams.chanceNoDivide = 1f;
        PolygonFinder p = new PolygonFinder(g._nodes, polyParams, this._tensorField);

        p.findPolygons();
        List<List<Vector3>> polygons = p._polygons;

        if (this.minorRoads.getAllStreamlines().Count == 0)
        {
            // Big parks
            this.bigParks = new List<List<Vector3>>();
            this.smallParks = new List<List<Vector3>>();
            if (polygons.Count > this.numBigParks)
            {
                if (this.clusterBigParks)
                {
                    // Group in adjacent polygons
                    int parkIndex = Mathf.FloorToInt(UnityEngine.Random.value * (polygons.Count - this.numBigParks));
                    for (int i = parkIndex; i < parkIndex + numBigParks; i++)
                    {
                        this.bigParks.Add(polygons[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < this.numBigParks; i++)
                    {
                        int parkIndex = Mathf.FloorToInt(UnityEngine.Random.value * polygons.Count);
                        this.bigParks.Add(polygons[parkIndex]);
                    }
                }
            }
            else
            {
                foreach (List<Vector3> park in polygons)
                    this.bigParks.Add(park);
            }
        }
        else
        {
            // small parks
            this.smallParks = new List<List<Vector3>>();
            for (int i = 0; i < numSmallParks; i++)
            {
                int parkIndex = Mathf.FloorToInt(UnityEngine.Random.value * polygons.Count);
                this.smallParks.Add(polygons[parkIndex]);
            }
        }

        this._tensorField.parks = new List<List<Vector3>>();
        foreach (List<Vector3> park in this.bigParks)
            this._tensorField.parks.Add(park);
        foreach (List<Vector3> park in this.smallParks)
            this._tensorField.parks.Add(park);
    }

    public void generateEverything()
    {
        this.coastline.generateRoads();
        this.mainRoads.generateRoads(this.animate);
        this.majorRoads.generateRoads(this.animate);
        this.minorRoads.generateRoads(this.animate);
        this.redraw = true;
        this.buildings.generate(this.animate);
    }

    public void Update()
    {
        bool continueUpdate = true;
        float currTime = Time.deltaTime;
        while (continueUpdate && Time.deltaTime - currTime < this.animationSpeed)
        {
            bool minorChanged = this.minorRoads.Update();
            bool majorChanged = this.majorRoads.Update();
            bool mainChanged = this.mainRoads.Update();
            bool buildingsChanged = this.buildings.Update();
            continueUpdate = minorChanged || majorChanged || mainChanged || buildingsChanged;
        }

        this.redraw = this.redraw || continueUpdate;
    }

    public void tensorFieldReset()
    {
        tensorFieldUI.reset();
    }

    public void setRecommendedTensor()
    {
        tensorFieldUI.radialOrigin = this.radialOrigin;
        tensorFieldUI.gridOrigin = this.gridOrigin;
        tensorFieldUI.setRecommended();
    }

    public bool roadsEmpty()
    {
        return this.majorRoads.roadsEmpty() 
            && this.minorRoads.roadsEmpty() 
            && this.mainRoads.roadsEmpty() 
            && this.coastline.roadsEmpty();
    }
}
