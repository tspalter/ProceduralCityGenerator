using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraDrag : MonoBehaviour
{
    public float dragSpeed = 2f;
    private Vector3 dragOrigin = Vector3.zero;

    float minFov = 15f;
    float maxFov = 200f;
    float sensitivity= 10f;

    void Update()
    {
        float fov = Camera.main.orthographicSize;
        fov -= Input.GetAxis("Mouse ScrollWheel") * sensitivity;
        fov = Mathf.Clamp(fov, minFov, maxFov);
        Camera.main.orthographicSize = fov;

        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
            return;
        }

        if (!Input.GetMouseButton(0)) return;

        Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
        Vector3 move = new Vector3(Mathf.Clamp(pos.x * dragSpeed, -27f, 27f), 0, Mathf.Clamp(pos.y * dragSpeed, -40f, 40f));

        transform.Translate(move, Space.World);
        
    }
}
