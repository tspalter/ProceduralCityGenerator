using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TensorFieldUI : TensorField
{
    public float TENSOR_LINE_DIAMETER = 10f;
    private float TENSOR_SPAWN_SCALE = 0.7f; // how much to shrink worldDims to find spawn point
    private float worldWidth = Screen.width / (10f / Camera.main.orthographicSize);
    private float worldHeight = Screen.height / (10f / Camera.main.orthographicSize);
    public Vector3 worldDims = Vector3.zero;

    public GameObject gridOrigin;
    public GameObject radialOrigin;

    public GameObject tensorFieldMenu;

    public GameObject tensorPrefab;

    public TensorFieldUI(bool drawCenter, NoiseParams noiseParams) : base(noiseParams)
    {
    }

    public void Start()
    {
        this.basisFields = new List<BasisField>();
        Instantiate(d);

        this.TENSOR_LINE_DIAMETER = 30f;
        this.TENSOR_SPAWN_SCALE = 0.7f;
        this.worldWidth = Screen.width / (10f / Camera.main.orthographicSize);
        this.worldHeight = Screen.height / (10f / Camera.main.orthographicSize);
        this.worldDims = new Vector3(worldWidth / 10f, 0f, worldHeight / 10f);

        this.parks = new List<List<Vector3>>();
        this.sea = new List<Vector3>();
        this.river = new List<Vector3>();
        this.ignoreRiver = false;
        this.smooth = false;
        setRecommended();

        Draw();
    }

    public void Update()
    {
        this.TENSOR_LINE_DIAMETER = 20f;
        this.worldWidth = Screen.width / (10f / Camera.main.orthographicSize);
        this.worldHeight = Screen.height / (10f / Camera.main.orthographicSize);
        this.worldDims = new Vector3(worldWidth / 10f, 0f, worldHeight / 10f);

        // this.UpdateDraw();
    }

    // 4 grids, one radial
    public void setRecommended()
    {
        this.reset();
        Vector3 size = this.worldDims * this.TENSOR_SPAWN_SCALE;
        Vector3 newOrigin = (this.worldDims * (1f - this.TENSOR_SPAWN_SCALE / 2f) / 50f) + Vector3.zero;
        this.addGridAtLocation(newOrigin);
        this.addGridAtLocation(newOrigin + size);
        this.addGridAtLocation(newOrigin + new Vector3(size.x, 0f, 0f));
        this.addGridAtLocation(newOrigin + new Vector3(0f, 0f, size.z));
        this.addRadialRandom();
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
        float width = this.worldDims.x;
        this.addGrid(newOrigin, 
            (int)UnityEngine.Random.Range(width / 4, width), 
            UnityEngine.Random.Range(0, 50f), 
            UnityEngine.Random.Range(0f, Mathf.PI / 2));
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
        float originX = diameter * Mathf.Floor(Camera.main.ScreenToWorldPoint(Camera.main.transform.position).x / diameter) / 50f;
        float originY = 0f;
        float originZ = diameter * Mathf.Floor(Camera.main.ScreenToWorldPoint(Camera.main.transform.position).z / diameter) / 50f;

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
        Debug.Log("Starting point for line: " + point);
        Vector3 transformedPoint = Camera.main.WorldToScreenPoint(point);
        Debug.Log("Transformed point: " + transformedPoint);
        Vector3 diff = tensorV * (this.TENSOR_LINE_DIAMETER / 2); // assuming tensorV is normalized
        Debug.Log("Diff Vector: " + diff);
        Vector3 start = transformedPoint - diff;
        Debug.Log("Starting point: " + start);
        Vector3 end = transformedPoint + diff;
        Debug.Log("Ending point: " + end);
        return new List<Vector3> { start, end };
    }

    public void Draw()
    {
        List<Vector3> tensorPoints = this.getCrossLocations();
        foreach (Vector3 p in tensorPoints)
        {
            Tensor t = this.samplePoint(p);
            Vector3 major = t.getMajor();
            Vector3 minor = t.getMinor();
            //List<Vector3> majorLine = this.getTensorLine(p, major);
            //Vector3 diffVec = majorLine[1] - majorLine[0];
            tensorPrefab.transform.position = p;
            tensorPrefab.transform.rotation = Quaternion.Euler(major.x, major.y, major.z);
            Instantiate(tensorPrefab);
        }
    }

    public void UpdateDraw()
    {
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("Tensor");
        foreach (GameObject g in gameObjects)
        {
            Vector3 p = g.transform.position;
            Tensor t = this.samplePoint(p);
            Vector3 major = new Vector3(t.getMajor().x, t.getMajor().y, t.getMajor().z);
            List<Vector3> majorLine = this.getTensorLine(p, major);
            Vector3 diffVec = majorLine[1] - majorLine[0];
            g.transform.rotation = Quaternion.Euler(diffVec.y, diffVec.x, diffVec.z);
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
        this.basisFields = new List<BasisField>();

        d.draggables = new List<GameObject>();
    }
}
