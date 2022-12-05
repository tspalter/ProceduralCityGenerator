using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilFunctions : MonoBehaviour
{
    // image related string values
    public const string CANVAS_ID = "canvas-id";
    public const string IMG_CANVAS_ID = "img-canvas-id";
    public const string SVG_ID = "svg-id";

    // dictates how far to integrate the streamlines beyond the screen, so that buildings
    // reach the edge of the screen
    public static float DRAW_INFLATE_AMOUNT = 1.2f;
    
    public static float RandomRange(float max, float min = 0f)
    {
        return Random.Range(min, max);
    }
}
