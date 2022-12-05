using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct Intersection
{
	public Vector3 vec;
	public bool flag;

	public Intersection(Vector3 v, bool f)
    {
		this.vec = v;
		this.flag = f;
    }
}
public class PolygonUtil : MonoBehaviour
{
    private static List<GameObject> geometryFactory = new List<GameObject>();
	const float _BUFFER = 0.0000000001f;

	// slices rectangles by line and returns the smallest polygon
	public static List<Vector3> sliceRectangle(Vector3 origin, Vector3 worldDims, Vector3 p1, Vector3 p2)
    {
        List<Vector3> rectangle = new List<Vector3>
        {
            new Vector3(origin.x, origin.y, origin.z),
            new Vector3(origin.x + worldDims.x, origin.y, origin.z),
            new Vector3(origin.x + worldDims.x, origin.y, origin.z + worldDims.z),
            new Vector3(origin.x, origin.y, origin.z + worldDims.z)
        };

        List<List<Vector3>> slicedRects = Slice(rectangle, p1, p2);
        float minArea = calcPolygonArea(slicedRects[0]);

		if (slicedRects.Count > 1 && calcPolygonArea(slicedRects[1]) < minArea)
			return slicedRects[1];

        return slicedRects[0];
    }

	// used to create sea-based polygons
	public static List<Vector3> lineRectanglePolygonIntersection(Vector3 origin, Vector3 worldDimensions, List<Vector3> line)
    {
		List<Vector3> bounds = new List<Vector3> {
			origin,
			new Vector3(origin.x + worldDimensions.x, origin.y, origin.z),
			new Vector3(origin.x + worldDimensions.x, origin.y, origin.z + worldDimensions.z),
			new Vector3(origin.x, origin.y, origin.z + worldDimensions.z),
			origin
		};
		List<Vector3> union = bounds;
		for (int i = 0; i < line.Count; i++)
        {
			if (!union.Contains(line[i]))
            {
				union.Add(line[i]);
            }
        }
		List<List<Vector3>> polygons = new List<List<Vector3>> { union };

		float smallestArea = Mathf.Infinity;
		List<Vector3> smallestPoly = new List<Vector3>();
		for (int i = 0; i < polygons.Count; i++)
		{
			float area = getArea(polygons[i]);
			if (area < smallestArea)
			{
				smallestArea = area;
				smallestPoly = polygons[i];
			}
		}

		if (smallestPoly.Count == 0)
			return new List<Vector3>{ Vector3.zero };

		return smallestPoly;
	}

    private static float getArea(List<Vector3> poly)
    {
		float area = 0.0f;

		for (int i = 0; i < poly.Count; i++)
        {
			if (i == poly.Count - 1)
            {
				area += (poly[i].x * poly[0].z) - (poly[i].z * poly[0].x);
            }
			else
            {
				area += (poly[i].x * poly[i+1].z) - (poly[i].z * poly[i+1].x);
			}
		}

		return Mathf.Abs(area / 2);
    }

    public static float calcPolygonArea(List<Vector3> poly)
    {
        float area = 0;
        for (int i = 0; i < poly.Count; i++)
        {
            float addX = poly[i].x;
            float addZ = poly[i == poly.Count - 1 ? 0 : i + 1].z;
            float subX = poly[i == poly.Count - 1 ? 0 : i + 1].x;
            float subZ = poly[i].z;

            area += (addX * addZ * 0.5f);
            area -= (subX * subZ * 0.5f);
        }

        return Mathf.Abs(area);
    }

	// recursively divide a polygon by its longest side until the minArea condition is met
	public static List<List<Vector3>> subdividePolygon(List<Vector3> poly, float minArea)
    {
		float area = calcPolygonArea(poly);
		if (area < 0.5 * minArea)
        {
			return new List<List<Vector3>> { poly };
        }

		List<List<Vector3>> dividedPolys = new List<List<Vector3>>();

		float longestSideLength = 0;
		List<Vector3> longestSide = new List<Vector3> { poly[0], poly[1] };

		float perimeter = 0;

		for (int i = 0; i < poly.Count; i++)
        {
			int sideIndex = (i + 1) % poly.Count;
			Vector3 sub = poly[i] - poly[sideIndex];
			float sideLength = Vector3.Magnitude(sub);
			perimeter += sideLength;
			if (sideLength > longestSideLength)
            {
				longestSideLength = sideLength;
				longestSide = new List<Vector3> { poly[i], poly[sideIndex] };
            }
        }

		// Shape index
		// using rectangle ratio of 1:4 as limit
		if (area / (perimeter * perimeter) < 0.04f)
        {
			return new List<List<Vector3>> { poly };
		}

		if (area < 2 * minArea)
        {
			return new List<List<Vector3>> { poly };
        }

		// between 0.4 and 0.6
		float deviation = (UnityEngine.Random.value * 0.2f) + 0.4f;

		Vector3 averagePoint = (longestSide[0] + longestSide[1]) * deviation;
		Vector3 diffVector = longestSide[0] - longestSide[1];
		Vector3 perpVector = new Vector3(diffVector.z, diffVector.y, -1 * diffVector.x);
		perpVector.Normalize();
		perpVector *= 100.0f;

		List<Vector3> bisect = new List<Vector3> { averagePoint + perpVector, averagePoint - perpVector };

		// array of polys
		try
        {
			List <List<Vector3>> sliced = Slice(poly, bisect[0], bisect[1]);
			// recursive call
			foreach (List<Vector3> slice in sliced)
            {
				List<List<Vector3>> subdivided = subdividePolygon(slice, minArea);
				foreach (List<Vector3> s in subdivided)
					dividedPolys.Add(s);
			}
			return dividedPolys;
        } catch
        {
			// Debug.Log("Failed to slice polys further");
			return new List<List<Vector3>> { poly };
		}
    }

	// function to shrink/expand polygon
	public static List<Vector3> resizeGeometry(List<Vector3> geometry, float spacing)
    {
		try
		{
			List<Vector3> resized = new List<Vector3>();
			foreach (Vector3 point in geometry)
            {
				Vector3 p = point;
				p *= spacing;
				resized.Add(p);
            }
			
			return resized;
		}
		catch
		{
			Debug.LogError("Geometry could not be resized");
			return new List<Vector3>();
		}
	}

	public static Vector3 averagePoint(List<Vector3> poly)
    {
		if (poly.Count == 0)
			return Vector3.zero;
		Vector3 sum = Vector3.zero;
		foreach (Vector3 p in poly)
			sum += p;

		return sum / (float)poly.Count;
    }

	public static bool insidePolygon(Vector3 point, List<Vector3> poly)
    {
		if (poly.Count == 0)
			return false;

		bool inside = false;
		for (int i = 0; i < poly.Count; i++)
        {
			float xi = poly[i].x;
			float zi = poly[i].z;
			float xj = poly[i + 1 % poly.Count].x;
			float zj = poly[i + 1 % poly.Count].z;

			bool intersect = ((zi > point.z) != (zj > point.z)) 
				&& (point.x < (xj - xi) * (point.z - zi) / (zj - zi) + xi);
			if (intersect)
				inside = !inside;
		}

		return inside;
    }

	public static bool pointInRectangle(Vector3 point, Vector3 origin, Vector3 dimensions)
    {
		return point.x >= origin.x && point.z >= origin.z && point.x <= dimensions.x && point.z <= dimensions.z;
	}

    private static List<List<Vector3>> Slice(List<Vector3> rectangle, Vector3 p1, Vector3 p2)
    {
		// if the point lies inside, return empty list
		if (containsPoint(rectangle, p1) || containsPoint(rectangle, p2)) 
			return new List<List<Vector3>> { rectangle };

		List<Intersection> intersections = new List<Intersection>();  // intersections
		List<Vector3> points = new List<Vector3>();    // points
		for (int i = 0; i < rectangle.Count; i++)
			points.Add(rectangle[i]);

		for (int i = 0; i < points.Count; i++)
		{
			Intersection intersection = new Intersection(Vector3.zero, false);
			intersection = getLineIntersection(p1, p2, points[i], points[(i + 1) % points.Count], intersection);
			Intersection firstIntersection = intersections[0];
			Intersection lastIntersection = intersections[intersections.Count - 1];
			if (intersection.vec != new Vector3(Mathf.Infinity, 0f, Mathf.Infinity)
				&& (firstIntersection.vec == null || Vector3.Distance(intersection.vec, firstIntersection.vec) > _BUFFER) 
				&& (lastIntersection.vec == null || Vector3.Distance(intersection.vec, lastIntersection.vec) > _BUFFER))
			{
				intersection.flag = true;
				intersections.Add(intersection);
				points.Insert(i + 1, intersection.vec);
				i++;
			}
		}

		if (intersections.Count < 2)
			return new List<List<Vector3>> { rectangle };

		intersections.Sort((v1, v2) => Vector3.Distance(v1.vec, Vector3.zero).CompareTo(Vector3.Distance(v2.vec, Vector3.zero)));

		List<List<Vector3>> polygons = new List<List<Vector3>>();
		int dir = 0;
		while (intersections.Count > 0)
		{
			int n = points.Count;
			Intersection int0 = intersections[0];
			Intersection int1 = intersections[1];
			int ind0 = points.IndexOf(int0.vec);
			int ind1 = points.IndexOf(int1.vec);
			bool solved = false;

			if (firstWithFlag(intersections, ind0) == ind1)
				solved = true;
			else
			{
				int0 = intersections[1];
				int1 = intersections[0];
				ind0 = points.IndexOf(int0.vec);
				ind1 = points.IndexOf(int1.vec);
				if (firstWithFlag(intersections, ind0) == ind1) 
					solved = true;
			}
			if (solved)
			{
				dir--;
				List<Vector3> polygon = getPoints(points, ind0, ind1);
				polygons.Add(polygon);
				points = getPoints(points, ind1, ind0);
				int0.flag = false;
				int1.flag = false;
				intersections.RemoveAt(0);
				intersections.RemoveAt(1);
				if (intersections.Count == 0)
					polygons.Add(points);
			}
			else 
			{ 
				dir++;
				intersections.Reverse(); 
			}
			if (dir > 1)
				break;
		}
		return polygons;
	}

    private static List<Vector3> getPoints(List<Vector3> points, int ind0, int ind1)
    {
		int n = points.Count;
		List<Vector3> newPoints = new List<Vector3>();
		if (ind1 < ind0)
			ind1 += n;
		for (int i = ind0; i <= ind1; i++)
        {
			newPoints.Add(points[i % n]);
        }

		return newPoints;
    }

    private static int firstWithFlag(List<Intersection> points, int ind0)
    {
		int count = points.Count;
		while (true)
        {
			ind0 = (ind0 + 1) % count;
			if (points[ind0].flag)
				return ind0;
        }
    }

	private static Intersection getLineIntersection(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, Intersection intersection)
    {
		float dAX = a1.x - a2.x;
		float dBX = b1.x - b2.x;
		float dAZ = a1.z - a2.z;
		float dBZ = b1.z - b2.z;

		float cross = (dAX * dBZ) - (dAZ * dBX);
		if (cross == 0)
			return new Intersection(new Vector3(Mathf.Infinity, 0f, Mathf.Infinity), intersection.flag);

		float A = (a1.x * a2.z) - (a1.z * a2.x);
		float B = (b1.x * b2.z) - (b1.z * b2.x);

		Vector3 I = intersection.vec;
		I.x = (A * dBX - dAX * B) / cross;
		I.z = (A * dBZ - dAZ * B) / cross;

		if (inRect(I, a1, a2) && inRect(I, b1, b2))
			return new Intersection(I, intersection.flag);

		return new Intersection(new Vector3(Mathf.Infinity, 0f, Mathf.Infinity), intersection.flag);
    }

    private static bool inRect(Vector3 i, Vector3 a1, Vector3 a2)
    {
		float minX = Mathf.Min(a1.x, a2.x);
		float maxX = Mathf.Max(a1.x, a2.x);
		float minZ = Math.Min(a1.z, a2.z);
		float maxZ = Math.Max(a1.z, a2.z);

		if (minX == maxX) 
			return (minZ <= i.y && i.y <= maxZ);
		if (minZ == maxZ) 
			return (minX <= i.x && i.x <= maxX);

		return minX <= i.x + _BUFFER && i.x - _BUFFER <= maxX && minZ <= i.y + _BUFFER && i.y - _BUFFER <= maxZ;
	}

    public static bool containsPoint(List<Vector3> rectangle, Vector3 p)
	{
		int polygonLength = rectangle.Count;
		bool isInside = false;
		// x, y for tested point.
		float pointX = p.x, pointZ = p.z;
		// start / end point for the current polygon segment.
		float startX, startZ, endX, endZ;
		Vector3 endPoint = rectangle[polygonLength - 1];
		endX = endPoint.x;
		endZ = endPoint.z;
		int i = 0;
		while (i < polygonLength)
		{
			startX = endX; startZ = endZ;
			endPoint = rectangle[i++];
			endX = endPoint.x; endZ = endPoint.z;
			//
			isInside ^= (endZ > pointZ ^ startZ > pointZ) /* ? pointY inside [startY;endY] segment ? */
					  && /* if so, test if it is under the segment */
					  ((pointX - endX) < (pointZ - endZ) * (startX - endX) / (startZ - endZ));
		}
		return isInside;
	}
}
