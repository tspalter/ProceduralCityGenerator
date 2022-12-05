using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SVS
{
    public class RoadHelper : MonoBehaviour
    {
        public GameObject roadStraight, roadCorner, road3Way, road4Way, roadEnd;
        Dictionary<Vector3Int, GameObject> roadDictionary = new Dictionary<Vector3Int, GameObject>();
        HashSet<Vector3Int> fixRoadCandidates = new HashSet<Vector3Int>();

        public List<Vector3Int> GetRoadPositions()
        {
            return roadDictionary.Keys.ToList();
        }

        public void PlaceStreetPositions(Vector3 startPosition, Vector3Int direction, int length)
        {
            var rotation = Quaternion.identity;
            if (direction.x == 0)
            {
                rotation = Quaternion.Euler(0, 90, 0);
            }
            for (int i = 0; i < length; i++)
            {
                var position = Vector3Int.RoundToInt(startPosition + direction * i);
                if (roadDictionary.ContainsKey(position))
                {
                    continue;
                }
                var road = Instantiate(roadStraight, position, rotation, transform);
                roadDictionary.Add(position, road);
                if (i == 0 || i == length - 1)
                {
                    fixRoadCandidates.Add(position);
                }
            }
        }

        public void FixRoad()
        {
            foreach (var position in fixRoadCandidates)
            {
                List<Direction> neighborDirections = PlacementHelper.FindNeighbor(position, roadDictionary.Keys);

                Quaternion rotation = Quaternion.identity;

                if (neighborDirections.Count == 1)
                {
                    Destroy(roadDictionary[position]);
                    if (neighborDirections.Contains(Direction.Down))
                    {
                        rotation = Quaternion.Euler(0, 90, 0);
                    }
                    else if (neighborDirections.Contains(Direction.Left))
                    {
                        rotation = Quaternion.Euler(0, 180, 0);
                    }
                    else if (neighborDirections.Contains(Direction.Up))
                    {
                        rotation = Quaternion.Euler(0, -90, 0);
                    }
                    roadDictionary[position] = Instantiate(roadEnd, position, rotation, transform);
                }
                else if (neighborDirections.Count == 2)
                {
                    if ((neighborDirections.Contains(Direction.Up) && neighborDirections.Contains(Direction.Down))
                        || (neighborDirections.Contains(Direction.Left) && neighborDirections.Contains(Direction.Right)))
                    {
                        continue;
                    }
                    Destroy(roadDictionary[position]);
                    if (neighborDirections.Contains(Direction.Up) && neighborDirections.Contains(Direction.Right))
                    {
                        rotation = Quaternion.Euler(0, 90, 0);
                    }
                    else if (neighborDirections.Contains(Direction.Right) && neighborDirections.Contains(Direction.Down))
                    {
                        rotation = Quaternion.Euler(0, 180, 0);
                    }
                    else if (neighborDirections.Contains(Direction.Down) && neighborDirections.Contains(Direction.Left))
                    {
                        rotation = Quaternion.Euler(0, -90, 0);
                    }
                    roadDictionary[position] = Instantiate(roadCorner, position, rotation, transform);
                }
                else if (neighborDirections.Count == 3)
                {
                    Destroy(roadDictionary[position]);
                    if (!neighborDirections.Contains(Direction.Up))
                    {
                        rotation = Quaternion.Euler(0, 90, 0);
                    }
                    else if (!neighborDirections.Contains(Direction.Right))
                    {
                        rotation = Quaternion.Euler(0, 180, 0);
                    }
                    else if (!neighborDirections.Contains(Direction.Down))
                    {
                        rotation = Quaternion.Euler(0, -90, 0);
                    }
                    roadDictionary[position] = Instantiate(road3Way, position, rotation, transform);
                }
                else
                {
                    Destroy(roadDictionary[position]);
                    roadDictionary[position] = Instantiate(road4Way, position, rotation, transform);
                }
            }
        }
    }
}