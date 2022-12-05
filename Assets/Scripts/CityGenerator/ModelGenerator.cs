using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// enum for states of the model generator
enum ModelGeneratorStates
{
    WAITING,
    SUBTRACT_OCEAN,
    ADD_COASTLINE,
    SUBTRACT_RIVER,
    ADD_ROADS,
    ADD_BLOCKS,
    ADD_BUILDINGS,
    CREATE_ZIP
};

public class ModelGenerator : MonoBehaviour
{
    private const int groundLevel = 20; // thickness of ground mesh

    private ModelGeneratorStates state = ModelGeneratorStates.WAITING;
    private Mesh groundMesh;
    private Vector2[][] polygonsToProcess;
    private GameObject roadsGeometry = new GameObject();
    private GameObject blockGeometry = new GameObject();
    private GameObject buildingGeometry = new GameObject();
}
