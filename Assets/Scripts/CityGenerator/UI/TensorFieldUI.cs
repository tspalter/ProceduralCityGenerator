using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TensorFieldUI : TensorField
{
    public float TENSOR_LINE_DIAMETER = 20f;
    private float TENSOR_SPAWN_SCALE = 0.7f; // how much to shrink worldDims to find spawn point
    private float worldWidth = Screen.width / (10f / Camera.main.orthographicSize);
    private float worldHeight = Screen.height / (10f / Camera.main.orthographicSize);
    public Vector3 worldDims = Vector3.zero;

    public DragController d;
    public GameObject gridOrigin;
    public GameObject radialOrigin;

    public GameObject tensorFieldMenu;

    public TensorFieldUI(bool drawCenter, NoiseParams noiseParams) : base(noiseParams)
    {
    }

    public void Start()
    {
        this.worldWidth = Screen.width / (10f / Camera.main.orthographicSize);
        this.worldHeight = Screen.height / (10f / Camera.main.orthographicSize);
        setRecommended();
    }

    public void Update()
    {
        this.TENSOR_LINE_DIAMETER = 20f;
        this.worldWidth = Screen.width / (10f / Camera.main.orthographicSize);
        this.worldHeight = Screen.height / (10f / Camera.main.orthographicSize);
        this.worldDims = new Vector3(worldWidth, worldHeight);
        if (tensorFieldMenu.activeInHierarchy)
        {
            Draw();
        }
    }

    // 4 grids, one radial
    public void setRecommended()
    {
        this.reset();
        Vector3 size = this.worldDims * this.TENSOR_SPAWN_SCALE;
        Vector3 newOrigin = (this.worldDims * (1f - this.TENSOR_SPAWN_SCALE / 2f)) + Vector3.zero;
        Debug.Log("New origin is at " + newOrigin);
        this.addRadialRandom();
        this.addGridAtLocation(newOrigin);
        this.addGridAtLocation(newOrigin + size);
        this.addGridAtLocation(newOrigin + new Vector3(size.x, 0f, 0f));
        this.addGridAtLocation(newOrigin + new Vector3(0f, 0f, size.y));
    }

    private void addRadialRandom()
    {
        float width = this.worldDims.x;
        Vector3 location = this.randomLocation();
        this.addRadial(location, (int)UnityEngine.Random.Range(width / 10f, width / 5f), UnityEngine.Random.Range(0, 50f));
        GameObject rad = UnityEngine.Object.Instantiate(radialOrigin, new Vector3(location.x, location.y, location.z), Quaternion.identity);
        d.draggables.Add(rad);
    }

    public void addGridRandom()
    {
        this.addGridAtLocation(this.randomLocation());
    }

    private void addGridAtLocation(Vector3 newOrigin)
    {
        Debug.Log("Creating grid at " + newOrigin);
        float width = this.worldDims.x;
        this.addGrid(newOrigin, 
            (int)UnityEngine.Random.Range(width / 4, width), 
            UnityEngine.Random.Range(0, 50f), 
            UnityEngine.Random.Range(0f, Mathf.PI / 2));
        Debug.Log("Grid Added at " + newOrigin);
        GameObject grid = UnityEngine.Object.Instantiate(gridOrigin, new Vector3(newOrigin.x, newOrigin.y, newOrigin.z), Quaternion.identity);
        d.draggables.Add(grid);
    }

    private Vector3 randomLocation()
    {
        Vector3 size = this.worldDims * this.TENSOR_SPAWN_SCALE;
        Vector3 location = new Vector3(UnityEngine.Random.value, 0f, UnityEngine.Random.value);
        location.Scale(size);
        Vector3 newOrigin = this.worldDims * (1 - this.TENSOR_SPAWN_SCALE);
        return location + Vector3.zero + newOrigin;
    }

    public List<Vector3> getCrossLocations()
    {
        // Gets grid of points for vector field in world space
        float diameter = this.TENSOR_LINE_DIAMETER / (50f / Camera.main.orthographicSize);
        Vector3 worldDimensions = new Vector3(this.worldDims.x, this.worldDims.y, this.worldDims.z);
        int nHor = (int)(Mathf.Ceil(worldDimensions.x / diameter) + 1f); // prevent pop-in
        int nVert = (int)(Mathf.Ceil(worldDimensions.z / diameter) + 1f);
        float originX = diameter * Mathf.Floor(Camera.main.ScreenToWorldPoint(Camera.main.transform.position).x / diameter);
        float originY = diameter * Mathf.Floor(Camera.main.ScreenToWorldPoint(Camera.main.transform.position).y / diameter);
        float originZ = diameter * Mathf.Floor(Camera.main.ScreenToWorldPoint(Camera.main.transform.position).z / diameter);

        List<Vector3> output = new List<Vector3>();
        for (int i = 0; i <= nHor; i++)
        {
            for (int j = 0; j <= nVert; j++)
            {
                output.Add(new Vector3(originX + (i * diameter), originY, originZ + (j * diameter)));
            }
        }
        return output;
    }

    public List<Vector3> getTensorLine(Vector3 point, Vector3 tensorV)
    {
        Vector3 transformedPoint = Camera.main.WorldToScreenPoint(point);
        Vector3 diff = tensorV * (this.TENSOR_LINE_DIAMETER / 2); // assuming tensorV is normalized
        Vector3 start = transformedPoint - diff;
        Vector3 end = transformedPoint + diff;
        return new List<Vector3> { start, end };
    }

    public void Draw()
    {
        List<Vector3> tensorPoints = this.getCrossLocations();
        foreach (Vector3 p in tensorPoints)
        {
            Tensor t = this.samplePoint(new Vector3(p.x, p.y, p.z));
            Vector3 major = new Vector3(t.getMajor().x, t.getMajor().y, t.getMajor().z);
            Vector3 minor = new Vector3(t.getMinor().x, t.getMinor().y, t.getMinor().z);
            this.drawLine(this.getTensorLine(p, major));
            this.drawLine(this.getTensorLine(p, minor));
        }
    }

    private void drawLine(List<Vector3> line)
    {
        Color lineColor = Color.white;
        GameObject myLine = new GameObject();
        myLine.transform.position = line[0];
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Line Material"));
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.startWidth = 1f;
        lr.endWidth = 1f;
        lr.SetPosition(0, line[0]);
        lr.SetPosition(1, line[1]);
        GameObject.Destroy(myLine);
    }

    public void reset()
    {
        Debug.Log("Resetting");
        foreach (GameObject g in d.draggables)
            UnityEngine.Object.Destroy(g);

        d.draggables = new List<GameObject>();
    }
}
