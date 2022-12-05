using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class MapFeaturesDropDown : MonoBehaviour
{
    public TextMeshProUGUI output;
    public enum MapFeature
    {
        WATER,
        MAIN,
        MAJOR,
        MINOR,
        PARKS,
        BUILDINGS
    };
    public MapFeature currentFeature;

    // different menu objects
    public GameObject water;
    public GameObject main;
    public GameObject major;
    public GameObject minor;
    public GameObject parks;
    public GameObject buildings;

    public void Awake()
    {
        currentFeature = MapFeature.WATER;
    }

    public void Update()
    {
        switch (currentFeature)
        {
            case MapFeature.WATER:
                water.SetActive(true);
                main.SetActive(false);
                major.SetActive(false);
                minor.SetActive(false);
                parks.SetActive(false);
                buildings.SetActive(false);
                break;
            case MapFeature.MAIN:
                water.SetActive(false);
                main.SetActive(true);
                major.SetActive(false);
                minor.SetActive(false);
                parks.SetActive(false);
                buildings.SetActive(false);
                break;
            case MapFeature.MAJOR:
                water.SetActive(false);
                main.SetActive(false);
                major.SetActive(true);
                minor.SetActive(false);
                parks.SetActive(false);
                buildings.SetActive(false);
                break;
            case MapFeature.MINOR:
                water.SetActive(false);
                main.SetActive(false);
                major.SetActive(false);
                minor.SetActive(true);
                parks.SetActive(false);
                buildings.SetActive(false);
                break;
            case MapFeature.PARKS:
                water.SetActive(false);
                main.SetActive(false);
                major.SetActive(false);
                minor.SetActive(false);
                parks.SetActive(true);
                buildings.SetActive(false);
                break;
            case MapFeature.BUILDINGS:
                water.SetActive(false);
                main.SetActive(false);
                major.SetActive(false);
                minor.SetActive(false);
                parks.SetActive(false);
                buildings.SetActive(true);
                break;
        }
    }

    public void ValueChange(int val)
    {

        if (val == 0)
        {
            currentFeature = MapFeature.WATER;
            Debug.Log("Water");
        }
        if (val == 1)
        {
            currentFeature = MapFeature.MAIN;
            Debug.Log("Main");
        }
        if (val == 2)
        {
            currentFeature = MapFeature.MAJOR;
            Debug.Log("Major");
        }
        if (val == 3)
        {
            currentFeature = MapFeature.MINOR;
            Debug.Log("Minor");
        }
        if (val == 4)
        {
            currentFeature = MapFeature.PARKS;
            Debug.Log("Parks");
        }
        if (val == 5)
        {
            currentFeature = MapFeature.BUILDINGS;
            Debug.Log("Buildings");
        }
    }
}
