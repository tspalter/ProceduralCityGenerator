using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DropDownMenu : MonoBehaviour
{

    public TextMeshProUGUI output;
    public enum MenuState { 
        MENU_TENSOR_FIELD, 
        MENU_MAP 
    };
    public MenuState currentMenu;

    // different menu objects
    public GameObject tensorFieldMenu;
    public GameObject mapMenu;

    public void Awake()
    {
        currentMenu = MenuState.MENU_TENSOR_FIELD;
    }

    public void Update()
    {
        switch (currentMenu)
        {
            case MenuState.MENU_TENSOR_FIELD:
                tensorFieldMenu.SetActive(true);
                mapMenu.SetActive(false);
                break;
            case MenuState.MENU_MAP:
                mapMenu.SetActive(true);
                tensorFieldMenu.SetActive(false);
                break;
        }
    }

    public void ValueChange(int val)
    {

        if (val == 0)
        {
            // switch to Tensor Field menu
            currentMenu = MenuState.MENU_TENSOR_FIELD;
            Debug.Log("Opening Tensor Field");
        }
        if (val == 1)
        {
            // switch to Map menu
            currentMenu = MenuState.MENU_MAP;
            Debug.Log("Opening Map");
        }
        if (val == 2)
            Debug.Log("Options");
    }

    
}
