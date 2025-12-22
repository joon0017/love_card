using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PointingHandler : MonoBehaviour
{
    [SerializeField] private InputAction press, screenPosition, rightClick;

    private Vector3 currentScreenPosition;
    public bool isDragging = false;
    public bool rotated = false;
    private Vector3 WorldPosition{
        get
        {
            float z = camera.WorldToScreenPoint(transform.position).z;
            return camera.ScreenToWorldPoint(currentScreenPosition + new Vector3(0, 0, z));
        }
    }

    private Camera camera;
    private bool isClickedOn
    {
        get
        {
            Ray ray = camera.ScreenPointToRay(currentScreenPosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                return hit.transform == this.transform;
            }
            return false;
        }
    }


    private void Awake() {
        camera = Camera.main;
        screenPosition.Enable();
        press.Enable();
        rightClick.Enable();

        screenPosition.performed += context => {
            currentScreenPosition = context.ReadValue<Vector2>();
        };

        press.performed += _ => {
            if(isClickedOn) StartCoroutine(Drag());
        };

        rightClick.performed += _ => {
           if(isClickedOn) StartCoroutine(RightClick());
        };

        press.canceled += _ => {
            isDragging = false;
        }; //no longer dragging
    }

    private IEnumerator Drag(){
        Debug.Log("Started Dragging " + gameObject.name);
        isDragging = true;
        Vector3 offset = transform.position - WorldPosition;

        while(isDragging){
            transform.position = WorldPosition + offset;
            yield return null;
        }
        //drop
    }
    
    private IEnumerator RightClick(){
        //right click action
        if(rotated)
            transform.Rotate(0,0,-90);
        else
            transform.Rotate(0,0,90);
        rotated = !rotated;
        yield return null;
    }


}
