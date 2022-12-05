using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct StreamlineIntegration
{
    public Vector3 seed;
    public Vector3 originalDir;
    public List<Vector3> streamline;
    public Vector3 previousDirection;
    public Vector3 previousPoint;
    public bool isValid;
}

public class StreamlineParams
{
    public string key;
    public float dsep; // streamline seed separating distance
    public float dtest; // streamline integration separating distance
    public float dstep; // step size
    public float dcirclejoin; // How far to look to join circles
    public float dlookahead; // how far to look ahead to join up dangling
    public float joinangle; // angle to join roads in radians
    public int pathIterations; // path integration iteration limit
    public int seedTries; // max failed seeds
    public float simplifyTolerance;
    public float collideEarly; // chance of early collision
}

// Creates polylines that make up the roads by integrating the tensor field
// See the paper 'Interactive Procedural Street Modeling' for a thorough explanation
public class StreamlineGenerator : MonoBehaviour
{
    bool SEED_AT_ENDPOINTS = false;
    int NEAR_EDGE = 3; // Sample near edge

    protected GridStorage majorGrid;
    protected GridStorage minorGrid;
    protected StreamlineParams paramsSq;

    // How many samples to skip when checking streamline collision with itself
    protected float nStreamlineStep;
    // How many samples to ignore backards when checking streamline collision with itself
    protected float nStreamlineLookBack;
    protected float dCollideSelfSq;

    protected List<Vector3> candidateSeedsMajor = new List<Vector3>();
    protected List<Vector3> candidateSeedsMinor = new List<Vector3>();

    protected bool streamlinesDone = true;
    protected bool lastStreamlineMajor = true;

    public List<List<Vector3>> allStreamlines = new List<List<Vector3>>();
    public List<List<Vector3>> streamlinesMajor = new List<List<Vector3>>();
    public List<List<Vector3>> streamlinesMinor = new List<List<Vector3>>();
    public List<List<Vector3>> allStreamlinesSimple = new List<List<Vector3>>(); // reduced vertex count

    protected StreamlineParams _parameters;
    protected Integrator _integrator;
    protected Vector3 _worldDimensions;
    protected Vector3 _origin;
    // Uses world-space coords
    public StreamlineGenerator(Integrator integrator, Vector3 origin, Vector3 worldDimensions, StreamlineParams parameters)
    {
        if (parameters.dstep > parameters.dsep)
        {
            Debug.LogError("STREAMLINE SAMPLE DISTANCE BIGGER THAN SEPARATION DISTANCE");
        }

        // Enforce test < sep
        parameters.dtest = Mathf.Min(parameters.dtest, parameters.dsep);

        // Needs to be less than circlejoin
        this.dCollideSelfSq = Mathf.Pow((parameters.dcirclejoin / 2), 2);
        this.nStreamlineStep = Mathf.Floor(parameters.dcirclejoin / parameters.dstep);
        this.nStreamlineLookBack = 2 * this.nStreamlineStep;

        this.majorGrid = new GridStorage(worldDimensions, origin, parameters.dsep);
        this.minorGrid = new GridStorage(worldDimensions, origin, parameters.dsep);

        this.setParamsSq();

        this._parameters = parameters;
        this._integrator = integrator;
        this._worldDimensions = worldDimensions;
        this._origin = origin;
    }

    public void clearStreamlines()
    {
        this.allStreamlinesSimple = new List<List<Vector3>>();
        this.streamlinesMajor = new List<List<Vector3>>();
        this.streamlinesMinor = new List<List<Vector3>>();
        this.allStreamlines = new List<List<Vector3>>();
    }

    // edits streamlines
    public void joinDanglingStreamlines()
    {
        foreach (List<Vector3> streamline in this.streamlinesMajor)
        {
            // ignore circles
            if (streamline[0].Equals(streamline[streamline.Count - 1]))
                continue;

            Vector3 newStart = this.getBestNextPoint(streamline[0], streamline[4], streamline);
            if (newStart != new Vector3(Mathf.Infinity, 0f, Mathf.Infinity))
            {
                foreach (Vector3 p in this.pointsBetween(streamline[0], newStart, this._parameters.dstep))
                {
                    streamline.Insert(0, p);
                    this.majorGrid.addSample(p);
                }
            }

            Vector3 newEnd = this.getBestNextPoint(streamline[streamline.Count - 1], streamline[streamline.Count - 4], streamline);
            if (newEnd != new Vector3(Mathf.Infinity, 0f, Mathf.Infinity))
            {
                foreach (Vector3 p in this.pointsBetween(streamline[0], newEnd, this._parameters.dstep))
                {
                    streamline.Add(p);
                    this.majorGrid.addSample(p);
                }
            }
        }

        // now do minor
        foreach (List<Vector3> streamline in this.streamlinesMinor)
        {
            // ignore circles
            if (streamline[0].Equals(streamline[streamline.Count - 1]))
                continue;

            Vector3 newStart = this.getBestNextPoint(streamline[0], streamline[4], streamline);
            if (newStart != null)
            {
                foreach (Vector3 p in this.pointsBetween(streamline[0], newStart, this._parameters.dstep))
                {
                    streamline.Insert(0, p);
                    this.minorGrid.addSample(p);
                }
            }

            Vector3 newEnd = this.getBestNextPoint(streamline[streamline.Count - 1], streamline[streamline.Count - 4], streamline);
            if (newEnd != null)
            {
                foreach (Vector3 p in this.pointsBetween(streamline[0], newEnd, this._parameters.dstep))
                {
                    streamline.Add(p);
                    this.minorGrid.addSample(p);
                }
            }
        }

        // reset simplified streamlines
        this.allStreamlinesSimple = new List<List<Vector3>>();
        foreach (List<Vector3> s in this.allStreamlines)
            this.allStreamlinesSimple.Add(this.simplifyStreamline(s));
    }

    // returns array of points from v1 to v2 such that they are separated by at most dsep
    // not including v1
    private List<Vector3> pointsBetween(Vector3 v1, Vector3 v2, float dstep)
    {
        float d = Vector3.Distance(v1, v2);
        int nPoints = Mathf.FloorToInt(d / dstep);
        if (nPoints == 0)
            return new List<Vector3>();

        Vector3 stepVector = v2 - v1;

        List<Vector3> outList = new List<Vector3>();
        int i = 1;
        Vector3 next = v1 + (stepVector * (i / nPoints));

        for (i = 1; i <= nPoints; i++)
        {
            if (this._integrator.integrate(next, true).sqrMagnitude > 0.001)
            {
                outList.Add(next);
            } else
            {
                return outList;
            }
            next = v1 + (stepVector * (i / nPoints));
        }

        return outList;
    }

    // Gets the next best point to join streamline
    // Returns null if there are no good candidates
    private Vector3 getBestNextPoint(Vector3 v1, Vector3 v2, List<Vector3> streamline)
    {
        List<Vector3> nearbyPoints = this.majorGrid.getNearbyPoints(v1, this._parameters.dlookahead);
        foreach (Vector3 point in this.minorGrid.getNearbyPoints(v1, this._parameters.dlookahead))
            nearbyPoints.Add(point);

        Vector3 direction = v1 - v2;
        Vector3 closestSample = new Vector3(Mathf.Infinity, 0f, Mathf.Infinity);
        float closestDistance = Mathf.Infinity;

        foreach (Vector3 sample in nearbyPoints) {
            if (!sample.Equals(v1) && !sample.Equals(v2))
            {
                Vector3 diffVector = sample - v1;
                if (Vector3.Dot(diffVector, direction) < 0)
                {
                    // backwards
                    continue;
                }

                // Acute angle between vectors (agnostic of CW, ACW)
                float distanceToSample = Vector3.Distance(v1, sample);
                if (distanceToSample < 2 * this.paramsSq.dstep)
                {
                    closestSample = sample;
                    break;
                }

                float angleBetween = Mathf.Abs(Vector3.Angle(direction, diffVector));

                // Filter by angle
                if (angleBetween < this._parameters.joinangle && distanceToSample < closestDistance)
                {
                    closestDistance = distanceToSample;
                    closestSample = sample;
                }
            }
        }

        if (closestSample != new Vector3(Mathf.Infinity, 0f, Mathf.Infinity))
            closestSample = closestSample + (direction.normalized * this._parameters.simplifyTolerance * 4);

        return closestSample;
    }

    // Assumes s has already been generated
    public void addExistingStreamlines(StreamlineGenerator s)
    {
        this.majorGrid.addAll(s.majorGrid);
        this.minorGrid.addAll(s.minorGrid);
    }

    public void setGrid(StreamlineGenerator s)
    {
        this.majorGrid = s.majorGrid;
        this.minorGrid = s.minorGrid;
    }

    // returns true if the state updates
    public bool update()
    {
        if (!this.streamlinesDone)
        {
            this.lastStreamlineMajor = !this.lastStreamlineMajor;
            if (!this.createStreamline(this.lastStreamlineMajor))
            {
                this.streamlinesDone = true;
            }
            return true;
        }

        return false;
    }

    // all at once - will freeze if dsep small

    public void createAllStreamlines(bool animate = false)
    {
        this.streamlinesDone = false;

        if (!animate)
        {
            bool major = true;
            while (this.createStreamline(major))
            {
                major = !major;
            }
        }

        this.joinDanglingStreamlines();
    }
    

    public List<Vector3> simplifyStreamline(List<Vector3> s)
    {
        List<Vector3> simplified = new List<Vector3>();
        foreach (Vector3 point in simplify(s, this._parameters.simplifyTolerance))
            simplified.Add(point);

        return simplified;
    }

    // simplify algorithms
    private List<Vector3> simplify(List<Vector3> s, float simplifyTolerance, bool highestQuality = true)
    {
        if (s.Count <= 2)
            return s;

        float sqTolerance = simplifyTolerance * simplifyTolerance;

        // use the Ramer-Douglas-Peucker algorithm
        int last = s.Count - 1;

        List<Vector3> simplified = new List<Vector3>();
        simplified.Add(s[0]);
        simplifyDPSStep(s, 0, last, sqTolerance, simplified);
        simplified.Add(s[last]);

        return simplified;
    }

    // recursive step of the RDP algorithm
    private void simplifyDPSStep(List<Vector3> s, int first, int last, float sqTolerance, List<Vector3> simplified)
    {
        float maxSqDist = sqTolerance;
        int index = 0;

        for (int i = first + 1; i < last; i++)
        {
            float sqDist = getSqSegDist(s[i], s[first], s[last]);

            if (sqDist > maxSqDist)
            {
                index = i;
                maxSqDist = sqDist;
            }
        }

        if (maxSqDist > sqTolerance)
        {
            if (index - first > 1)
                simplifyDPSStep(s, first, index, sqTolerance, simplified);
            simplified.Add(s[index]);
            if (last - index > 1)
                simplifyDPSStep(s, index, last, sqTolerance, simplified);
        }
    }

    private float getSqSegDist(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float x = p2.x;
        float z = p2.z;
        float dx = p3.x - x;
        float dz = p3.z - z;

        if (dx != 0 || dz != 0)
        {

            float t = ((p1.x - x) * dx + (p1.z - z) * dz) / (dx * dx + dz * dz);

            if (t > 1)
            {
                x = p3.x;
                z = p3.z;

            }
            else if (t > 0)
            {
                x += dx * t;
                z += dz * t;
            }
        }

        dx = p1.x - x;
        dz = p1.z - z;

        return dx * dx + dz * dz;
    }

    // Finds seed and creates a streamline from that point
    // Pushes new candidate seeds to queue
    private bool createStreamline(bool major)
    {
        Vector3 seed = this.getSeed(major);
        if (seed == new Vector3(Mathf.Infinity, 0f, Mathf.Infinity))
            return false;

        List<Vector3> streamline = this.integrateStreamline(seed, major);
        if (this.validStreamline(streamline))
        {
            this.grid(major).addPolyline(streamline);
            this.streamlines(major).Add(streamline);
            this.allStreamlines.Add(streamline);

            this.allStreamlinesSimple.Add(this.simplifyStreamline(streamline));

            // Add candidate seeds
            if (!streamline[0].Equals(streamline[streamline.Count - 1]))
            {
                this.candidateSeeds(!major).Add(streamline[0]);
                this.candidateSeeds(!major).Add(streamline[streamline.Count - 1]);
            }
        }

        return true;
    }

    private bool validStreamline(List<Vector3> streamline)
    {
        return streamline.Count > 5;
    }

    private void setParamsSq()
    {
        this.paramsSq = this._parameters;
        // square each value
        this.paramsSq.dsep *= this.paramsSq.dsep;
        this.paramsSq.dstep *= this.paramsSq.dstep;
        this.paramsSq.dtest *= this.paramsSq.dtest;
        this.paramsSq.dcirclejoin *= this.paramsSq.dcirclejoin;
        this.paramsSq.dlookahead *= this.paramsSq.dlookahead;
        this.paramsSq.joinangle *= this.paramsSq.joinangle;
        this.paramsSq.pathIterations *= this.paramsSq.pathIterations;
        this.paramsSq.seedTries *= this.paramsSq.seedTries;
        this.paramsSq.simplifyTolerance *= this.paramsSq.simplifyTolerance;
        this.paramsSq.collideEarly *= this.paramsSq.collideEarly;
    }

    public Vector3 samplePoint()
    {
        return new Vector3(UnityEngine.Random.Range(0, 1) * this._worldDimensions.x, this._worldDimensions.y, UnityEngine.Random.Range(0, 1) * this._worldDimensions.z) + this._origin;
    }

    // tries this.candidateSeeds first, then samples using this.samplePoint
    public Vector3 getSeed(bool major)
    {
        // candidate seeds first
        if (this.SEED_AT_ENDPOINTS && this.candidateSeedsMajor.Count > 0)
        {
            while (this.candidateSeeds(major).Count > 0)
            {
                int last = this.candidateSeeds(major).Count - 1;
                Vector3 s = this.candidateSeeds(major)[last];
                if (this.isValidSample(major, s, this.paramsSq.dsep))
                    return s;

                this.candidateSeeds(major).RemoveAt(last);
            }
        }

        Vector3 seed = this.samplePoint();
        int i = 0;
        while (!this.isValidSample(major, seed, this.paramsSq.dsep))
        {
            if (i >= this._parameters.seedTries)
            {
                return new Vector3(Mathf.Infinity, 0f, Mathf.Infinity);
            }
            seed = this.samplePoint();
        }
        return seed;
    }

    private bool isValidSample(bool major, Vector3 seed, float dsep, bool bothGrids = false)
    {
        bool gridValid = this.grid(major).isValidSample(seed, dsep);
        if (bothGrids)
            gridValid = gridValid && this.grid(!major).isValidSample(seed, dsep);

        return this._integrator.onLand(seed) && gridValid;
    }

    public List<Vector3> candidateSeeds(bool major)
    {
        return major ? this.candidateSeedsMajor : this.candidateSeedsMinor;
    }

    public List<List<Vector3>> streamlines(bool major)
    {
        return major ? this.streamlinesMajor : this.streamlinesMinor;
    }

    public GridStorage grid(bool major)
    {
        return major ? this.majorGrid : this.minorGrid;
    }

    public bool pointInBounds(Vector3 v)
    {
        return (v.x >= this._origin.x
            && v.z >= this._origin.z
            && v.x < this._worldDimensions.x + this._origin.x
            && v.z < this._worldDimensions.z + this._origin.z
        );
    }

    // tests whether streamline has turned through greater than 180 degrees
    public bool streamlineTurned(Vector3 seed, Vector3 originalDir, Vector3 point, Vector3 direction)
    {
        if (Vector3.Dot(originalDir, direction) < 0)
        {
            Vector3 perpendicularVector = new Vector3(originalDir.y, -originalDir.x);
            bool isLeft = Vector3.Dot(point - seed, perpendicularVector) < 0;
            bool directionUp = Vector3.Dot(direction, perpendicularVector) > 0;

            return isLeft == directionUp;
        }

        return false;
    }

    // one step of the streamline process
    public void streamlineIntegrationStep(StreamlineIntegration parameters, bool major, bool collideBoth)
    {
        if (parameters.isValid)
        {
            parameters.streamline.Add(parameters.previousPoint);
            Vector3 nextDirection = this._integrator.integrate(parameters.previousPoint, major);

            // stop at degenerate point
            if (Vector3.SqrMagnitude(nextDirection) < 0.01f)
            {
                parameters.isValid = false;
                return;
            }

            // Make sure that we travel in the same direction
            if (Vector3.Dot(nextDirection, parameters.previousDirection) < 0)
            {
                nextDirection *= -1;
            }

            Vector3 nextPoint = parameters.previousPoint + nextDirection;

            if (this.pointInBounds(nextPoint) && this.isValidSample(major, nextPoint, this.paramsSq.dtest, collideBoth)
                    && !this.streamlineTurned(parameters.seed, parameters.originalDir, nextPoint, nextDirection)) {
                parameters.previousPoint = nextPoint;
                parameters.previousDirection = nextDirection;
            }
            else
            {
                // one more step
                parameters.streamline.Add(nextPoint);
                parameters.isValid = false;
            }
        }
    }

    // By simultaneously integrating in both directions we reduce the impact of circles not joining
    // up as the error matches at the join
    public List<Vector3> integrateStreamline(Vector3 seed, bool major)
    {
        int count = 0;
        bool pointsEscaped = false; // true once two integration fronts have moved dlookahead away

        // Whether or not to test validity using both grid storages
        // (Collide with both major and minor)
        bool collideBoth = UnityEngine.Random.Range(0, 1) < this._parameters.collideEarly;

        Vector3 d = this._integrator.integrate(seed, major);
        StreamlineIntegration forwardParams = new StreamlineIntegration();
        forwardParams.seed = seed;
        forwardParams.originalDir = d;
        forwardParams.streamline = new List<Vector3> { seed };
        forwardParams.previousDirection = d;
        forwardParams.previousPoint = seed + d;
        forwardParams.isValid = this.pointInBounds(forwardParams.previousPoint);

        Vector3 negD = d * -1;
        StreamlineIntegration backwardsParams = new StreamlineIntegration();
        backwardsParams.seed = seed;
        backwardsParams.originalDir = negD;
        backwardsParams.streamline = new List<Vector3>();
        backwardsParams.previousDirection = negD;
        backwardsParams.previousPoint = seed + negD;
        backwardsParams.isValid = this.pointInBounds(backwardsParams.previousPoint);

        while (count < this._parameters.pathIterations && (forwardParams.isValid || backwardsParams.isValid))
        {
            this.streamlineIntegrationStep(forwardParams, major, collideBoth);
            this.streamlineIntegrationStep(backwardsParams, major, collideBoth);

            // Join up circles
            float sqDistanceBetweenPoints = Mathf.Pow(Vector3.Distance(forwardParams.previousPoint, backwardsParams.previousPoint), 2);

            if (!pointsEscaped && sqDistanceBetweenPoints > this.paramsSq.dcirclejoin)
                pointsEscaped = true;

            if (pointsEscaped && sqDistanceBetweenPoints <= this.paramsSq.dcirclejoin)
            {
                forwardParams.streamline.Add(forwardParams.previousPoint);
                forwardParams.streamline.Add(backwardsParams.previousPoint);
                backwardsParams.streamline.Add(backwardsParams.previousPoint);
                break;
            }
        }

        backwardsParams.streamline.Reverse();
        foreach (Vector3 v in forwardParams.streamline)
            backwardsParams.streamline.Add(v);
        return backwardsParams.streamline;
    }
}
