using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PointingHandler : MonoBehaviour
{
    [SerializeField] private InputAction press, screenPosition;

    private Vector3 currentScreenPosition;
    Camera camera;
    public bool isDragging = false;
    private Vector3 WorldPosition{
        get
        {
            float z = camera.WorldToScreenPoint(transform.position).z;
            return camera.ScreenToWorldPoint(currentScreenPosition + new Vector3(0, 0, z));
        }
    }


    public bool isClickedOn
    {
        get
        {
            Ray ray = camera.ScreenPointToRay(currentScreenPosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Hit: " + hit.transform.name);
                return hit.transform == this.transform;
            }
            return false;
        }
    }
    private void Awake() {
        camera = Camera.main;
        screenPosition.Enable();
        press.Enable();

        screenPosition.performed += context => {
            currentScreenPosition = context.ReadValue<Vector2>();
        };

        press.performed += _ => {
                if(isClickedOn) StartCoroutine(Drag());
        };

        press.canceled += _ => {
            isDragging = false;
        }; //no longer dragging
    }

    private IEnumerator Drag(){
        isDragging = true;
        Vector3 offset = transform.position - WorldPosition;

        while(isDragging){
            transform.position = WorldPosition + offset;
            yield return null;
        }
        //drop
    }
}
