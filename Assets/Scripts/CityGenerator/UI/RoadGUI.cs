using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadGUI
{
    protected StreamlineGenerator streamlines;
    private List<RoadGUI> _existingStreamlines = new List<RoadGUI>();

    private bool streamlinesInProgress = false;
    private bool _animate = false;

    public float worldWidth = 0f;
    public float worldHeight = 0f;
    public Vector3 worldDims = Vector3.zero;

    public StreamlineParams _parameters;
    public Integrator _integrator;

    public RoadGUI(StreamlineParams parameters, Integrator integrator, bool animate = false)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint(Camera.main.transform.position);
        Vector3 origin = new Vector3(world.x, world.z);
        this.worldWidth = Screen.width / (10f / Camera.main.orthographicSize);
        this.worldHeight = Screen.height / (10f / Camera.main.orthographicSize);
        this.worldDims = new Vector3(worldWidth, worldHeight);
        this.streamlines = new StreamlineGenerator(integrator, origin, this.worldDims, parameters);

        // update path iterations based on window size
        this.setPathIterations();

        this._integrator = integrator;
        this._parameters = parameters;
    }

    public void Start()
    {
        this.generateRoads(this._animate);
        this.streamlines.joinDanglingStreamlines();
    }

    public void setAnimate(bool b)
    {
        this._animate = b;
    }

    public bool getAnimate()
    {
        return this._animate;
    }

    public List<List<Vector3>> getAllStreamlines()
    {
        return this.streamlines.allStreamlinesSimple;
    }

    public List<List<Vector3>> getRoads()
    {
        List<List<Vector3>> roads = new List<List<Vector3>>();
        for (int i = 0; i < this.streamlines.allStreamlinesSimple.Count; i++)
        {
            for (int j = 0; j < this.streamlines.allStreamlinesSimple[i].Count; j++)
            {
                Vector3 point = this.streamlines.allStreamlinesSimple[i][j];
                Vector3 screenPoint = new Vector3(point.x, point.y, point.z);
                roads[i][j] = Camera.main.WorldToScreenPoint(screenPoint);
            }
        }

        return roads;
    }

    public bool roadsEmpty()
    {
        return this.streamlines.allStreamlinesSimple.Count == 0;
    }

    public void setExistingStreamlines(List<RoadGUI> existingStreamlines)
    {
        this._existingStreamlines = existingStreamlines;
    }

    public void clearStreamlines()
    {
        this.streamlines.clearStreamlines();
    }

    public void generateRoads(bool animate)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint(Camera.main.transform.position);
        Vector3 origin = new Vector3(world.x, world.y, world.z);
        this.worldWidth = Screen.width / (10f / Camera.main.orthographicSize);
        this.worldHeight = Screen.height / (10f / Camera.main.orthographicSize);
        this.worldDims = new Vector3(worldWidth, worldHeight);
        Camera.main.orthographicSize = Camera.main.orthographicSize / UtilFunctions.DRAW_INFLATE_AMOUNT;
        this.streamlines = new StreamlineGenerator(this._integrator, origin, this.worldDims, this._parameters);
        Camera.main.orthographicSize = Camera.main.orthographicSize * UtilFunctions.DRAW_INFLATE_AMOUNT;

        foreach (RoadGUI s in this._existingStreamlines)
            this.streamlines.addExistingStreamlines(s.streamlines);

        this.streamlines.createAllStreamlines(animate);
    }

    // returns true if streamlines changes
    public bool Update()
    {
        return this.streamlines.update();
    }

    // sets path iterations so that a road can cover the screens
    private void setPathIterations()
    {
        float max = 1.5f * Mathf.Max(Screen.width, Screen.height);
        this._parameters.pathIterations = (int)(max / this._parameters.dstep);
    }
}
