using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class BasisFieldDropdown : MonoBehaviour
{
    public List<BasisField> basisFields = new List<BasisField>();
    public TextMeshProUGUI output;
    public enum FieldState
    {
        FIELD_RADIAL,
        FIELD_GRID
    };
    public FieldState currentField;

    // different menu objects
    public GameObject radialField;
    public GameObject gridField;

    public void Awake()
    {
        currentField = getFieldType(0);
    }

    private FieldState getFieldType(int v)
    {
        if (basisFields.Count > 0)
        {
            if (basisFields[v].fieldType == FIELD_TYPE.RADIAL)
                return FieldState.FIELD_RADIAL;
            else if (basisFields[v].fieldType == FIELD_TYPE.GRID)
                return FieldState.FIELD_GRID;
        }

        return FieldState.FIELD_GRID;
    }

    public void Update()
    {
        switch (currentField)
        {
            case FieldState.FIELD_RADIAL:
                radialField.SetActive(true);
                gridField.SetActive(false);
                break;
            case FieldState.FIELD_GRID:
                gridField.SetActive(true);
                radialField.SetActive(false);
                break;
        }
    }

    public void ValueChange(int val)
    {

        if (val == 0)
        {
            currentField = FieldState.FIELD_GRID;
            Debug.Log("Grid");
        }
        if (val == 1)
        {
            currentField = FieldState.FIELD_GRID;
            Debug.Log("Grid");
        }
        if (val == 2)
        {
            currentField = FieldState.FIELD_GRID;
            Debug.Log("Grid");
        }
        if (val == 3)
        {
            currentField = FieldState.FIELD_GRID;
            Debug.Log("Grid");
        }
        if (val == 4)
        {
            currentField = FieldState.FIELD_RADIAL;
            Debug.Log("Radial");
        }
    }
}
