using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Register multiple center points
// closest one to mouse click will be selected to drag
public class DragController : MonoBehaviour
{
    // How close to drag handle pointer needs to be
    private float MIN_DRAG_DISTANCE = 1f;

    public List<GameObject> draggables = new List<GameObject>();
    private GameObject currentlyDragging = null;
    private bool isDragging = true;
    private bool disabled = false;
    
    public void setDragDisabled(bool disable)
    {
        this.disabled = disable;
    }


    public Vector3 worldPosition;
    Plane plane = new Plane(Vector3.up, 0);
    void Update()
    {
        
        if (Input.GetButton("Fire1"))
        {
            if (this.currentlyDragging == null)
            {
                float distance;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (plane.Raycast(ray, out distance))
                {
                    worldPosition = ray.GetPoint(distance);
                }
                float closestDist = Mathf.Infinity;
                foreach (GameObject draggable in draggables)
                {
                    float d = Vector3.Distance(draggable.transform.position, worldPosition);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        this.currentlyDragging = draggable;
                    }
                }

                // Zoom screen size to world size for consistent drag distance while zoomed
                float scaledDragDistance = this.MIN_DRAG_DISTANCE;
                if (closestDist > scaledDragDistance)
                {
                    this.currentlyDragging = null;
                }
            }
            else
            {
                float distance;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (plane.Raycast(ray, out distance))
                {
                    worldPosition = ray.GetPoint(distance);
                }
                worldPosition.y += 0.5f;
                this.currentlyDragging.transform.position = worldPosition;
            }
        }
        else
        {
            this.isDragging = false;
            this.currentlyDragging = null;
        }
    }

    public bool getIsDragging()
    {
        return this.isDragging;
    }
}
