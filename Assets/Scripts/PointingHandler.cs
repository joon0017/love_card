using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PointingHandler : MonoBehaviour
{
    [SerializeField] private InputAction press, screenPosition, rightClick;
    private Vector3 currentScreenPosition;
    public bool isDragging = false;
    public bool rotated = false;

    private Camera mainCamera;
    private Card card;
    private FieldRaycastHandler hoveredField;
    private FieldRaycastHandler occupiedField;

    private Vector3 WorldPosition{
        get
        {
            if(mainCamera == null) return transform.position;

            float z = mainCamera.WorldToScreenPoint(transform.position).z;
            return mainCamera.ScreenToWorldPoint(currentScreenPosition + new Vector3(0, 0, z));
        }
    }

    private bool isClickedOn
    {
        get
        {
            if(mainCamera == null) return false;

            Ray ray = mainCamera.ScreenPointToRay(currentScreenPosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                return hit.transform == transform || hit.transform.IsChildOf(transform);
            }
            return false;
        }
    }

    private GameObject hitObject; // when inactive (not dragging) set transform to this object. null means hand
    public void SetHitObject(GameObject hitObject){
        this.hitObject = hitObject;
        hoveredField = hitObject != null ? hitObject.GetComponent<FieldRaycastHandler>() : null;
    }

    private void Awake() {
        mainCamera = Camera.main;
        card = GetComponent<Card>();

        if(screenPosition != null) screenPosition.performed += OnScreenPositionPerformed;
        if(press != null)
        {
            press.performed += OnPressPerformed;
            press.canceled += OnPressCanceled;
        }
        if(rightClick != null) rightClick.performed += OnRightClickPerformed;
    }

    private void OnEnable() {
        screenPosition?.Enable();
        press?.Enable();
        rightClick?.Enable();
    }

    private void OnDisable() {
        screenPosition?.Disable();
        press?.Disable();
        rightClick?.Disable();
    }

    private void OnDestroy() {
        if(screenPosition != null) screenPosition.performed -= OnScreenPositionPerformed;
        if(press != null)
        {
            press.performed -= OnPressPerformed;
            press.canceled -= OnPressCanceled;
        }
        if(rightClick != null) rightClick.performed -= OnRightClickPerformed;
    }

    private void OnScreenPositionPerformed(InputAction.CallbackContext context) {
        currentScreenPosition = context.ReadValue<Vector2>();
    }

    private void OnPressPerformed(InputAction.CallbackContext context) {
        if(isClickedOn) StartCoroutine(Drag());
    }

    private void OnPressCanceled(InputAction.CallbackContext context) {
        EndDrag();
    }

    private void OnRightClickPerformed(InputAction.CallbackContext context) {
        if(isClickedOn) RotateCard();
    }

    private void Update() {
        if(isDragging){
            hoveredField = FindFieldUnderCard();
            hitObject = hoveredField != null ? hoveredField.gameObject : null;
        }
    }

    private IEnumerator Drag(){
        if(isDragging) yield break;

        Debug.Log("Started Dragging " + gameObject.name);
        isDragging = true;
        ReleaseOccupiedField();
        HandManager.instance?.RemoveCardFromList(gameObject);

        Vector3 offset = transform.position - WorldPosition;

        while(isDragging){
            transform.position = WorldPosition + offset;
            yield return null;
        }
        //drop
    }

    private void EndDrag(){
        if(!isDragging) return;

        isDragging = false;
        PlaceOnFieldOrReturnToHand();
        HandManager.instance?.UpdateCardPositions();
    }

    private void PlaceOnFieldOrReturnToHand(){
        FieldRaycastHandler targetField = hoveredField != null ? hoveredField : FindFieldUnderCard();

        if(targetField != null && targetField.CanAcceptCard(card)){
            targetField.Occupy(card);
            occupiedField = targetField;
            hitObject = targetField.gameObject;
            transform.position = new Vector3(targetField.transform.position.x, targetField.transform.position.y, 0);
            HandManager.instance?.RemoveCardFromList(gameObject);
            return;
        }

        hitObject = null;
        card?.AddToHand();
    }

    private void ReleaseOccupiedField(){
        if(occupiedField == null) return;

        occupiedField.Clear(card);
        occupiedField = null;
    }

    private FieldRaycastHandler FindFieldUnderCard(){
        Ray ray = new Ray(transform.position + Vector3.back * 10f, Vector3.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, 20f);
        FieldRaycastHandler nearestField = null;
        float nearestDistance = float.MaxValue;

        foreach(RaycastHit hit in hits){
            FieldRaycastHandler field = hit.collider.GetComponent<FieldRaycastHandler>();
            if(field == null || hit.distance >= nearestDistance) continue;

            nearestField = field;
            nearestDistance = hit.distance;
        }

        return nearestField;
    }

    private void RotateCard(){
        if(rotated)
            transform.Rotate(0,0,-90);
        else
            transform.Rotate(0,0,90);
        rotated = !rotated;
    }


}
