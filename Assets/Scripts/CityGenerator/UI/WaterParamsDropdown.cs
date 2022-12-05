using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WaterParamsDropdown : MonoBehaviour
{
    public TextMeshProUGUI output;
    public enum WaterParams
    {
        COAST_PARAMS,
        RIVER_PARAMS,
        STREAMLINE_PARAMS
    };
    public WaterParams currentMenu;

    // different menu objects
    public GameObject coastParams;
    public GameObject riverParams;
    public GameObject streamlineParams;

    public void Awake()
    {
        currentMenu = WaterParams.COAST_PARAMS;
    }

    public void Update()
    {
        switch (currentMenu)
        {
            case WaterParams.COAST_PARAMS:
                coastParams.SetActive(true);
                riverParams.SetActive(false);
                streamlineParams.SetActive(false);
                break;
            case WaterParams.RIVER_PARAMS:
                coastParams.SetActive(false);
                riverParams.SetActive(true);
                streamlineParams.SetActive(false);
                break;
            case WaterParams.STREAMLINE_PARAMS:
                coastParams.SetActive(false);
                riverParams.SetActive(false);
                streamlineParams.SetActive(true);
                break;
        }
    }

    public void ValueChange(int val)
    {

        if (val == 0)
        {
            // switch to Tensor Field menu
            currentMenu = WaterParams.COAST_PARAMS;
            Debug.Log("Coastline");
        }
        if (val == 1)
        {
            // switch to Map menu
            currentMenu = WaterParams.RIVER_PARAMS;
            Debug.Log("River");
        }
        if (val == 2)
        {
            // switch to Map menu
            currentMenu = WaterParams.STREAMLINE_PARAMS;
            Debug.Log("Streamline");
        }
    }
}
