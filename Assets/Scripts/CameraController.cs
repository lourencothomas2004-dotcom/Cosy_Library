using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    public CinemachineCamera roomView;
    public CinemachineCamera mainCamera;
    
    [Header("Aspect Ratio FOV Scaling")]
    [SerializeField] private float referenceAspect = 4f / 3f;
    private float lastAspect;
    private Dictionary<CinemachineCamera, float> baseFOVs = new Dictionary<CinemachineCamera, float>();

    void Start()
    {
        RegisterCameraFOV(roomView);
        mainCamera = roomView;
        UpdateFOV();
    }

    void Update()
    {
        // Automatically recalculate FOV if the device is folded/unfolded or rotated
        if (Mathf.Abs(Camera.main.aspect - lastAspect) > 0.01f)
        {
            UpdateFOV();
        }

        // 1. Determine if the user is interacting this frame and grab the position
        bool hasInput = false;
        Vector3 inputPosition = Vector3.zero;
        bool isPointerOverUI = false;

        // Check Android touch first
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                hasInput = true;
                inputPosition = touch.position;
                // Crucial for Android: Pass the fingerId to check if touching UI
                isPointerOverUI = EventSystem.current.IsPointerOverGameObject(touch.fingerId);
            }
        }
        // Fallback to PC Mouse if no touch is detected
        else if (Input.GetMouseButtonDown(0))
        {
            hasInput = true;
            inputPosition = Input.mousePosition;
            // Standard check for PC mouse
            isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        }

        // 2. Process the interaction if valid
        if (hasInput)
        {
            // If the user clicked a UI button or Canvas, abort the 3D raycast immediately!
            if (isPointerOverUI) 
            {
                return; 
            }
            
            // Perform the 3D world raycast
            Ray ray = Camera.main.ScreenPointToRay(inputPosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CinemachineCamera clickedCamera = hit.transform.GetComponentInChildren<CinemachineCamera>();
                Bookmovement Book = hit.transform.GetComponent<Bookmovement>(); 

                if (clickedCamera != null && clickedCamera != mainCamera)
                {
                    changeCamera(clickedCamera);
                }

                if (Book is not null)
                {
                    Book.Move2Read();
                }
            }
        }
    }
    
    public void changeCamera(CinemachineCamera clickedCamera)
    {
        if (clickedCamera != null && clickedCamera != mainCamera)
        {
            BoxCollider collider = mainCamera.GetComponentInParent<BoxCollider>();
            if (collider != null)
            {
                collider.enabled = true;
            }
            
            mainCamera.Priority = 0;
            clickedCamera.Priority = 10;
            mainCamera = clickedCamera;
            
            RegisterCameraFOV(mainCamera);
            UpdateFOV();
            
            collider = mainCamera.GetComponentInParent<BoxCollider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    public async void Change2RoomCamera()
    {
        BoxCollider collider = mainCamera.GetComponentInParent<BoxCollider>();
        Bookmovement Book = mainCamera.GetComponentInParent<Bookmovement>(); 

        if (collider != null)
        {
            collider.enabled = true;
        }

        mainCamera.Priority = 0;
        roomView.Priority = 10;
        await Awaitable.WaitForSecondsAsync(0.99f);

        if (Book is not null)
        {
            Book.Move2Shelf();
        }

        mainCamera = roomView;
        
        UpdateFOV();
    }

    private void RegisterCameraFOV(CinemachineCamera cam)
    {
        if (cam != null && !baseFOVs.ContainsKey(cam))
        {
            baseFOVs.Add(cam, cam.Lens.FieldOfView);
        }
    }

    private void UpdateFOV()
    {
        float currentAspect = Camera.main.aspect;

        if (mainCamera != null && baseFOVs.ContainsKey(mainCamera))
        {
            float baseFOV = baseFOVs[mainCamera];

            if (currentAspect < referenceAspect)
            {
                // Portrait mode: widen the vertical FOV to keep horizontal bounds visible
                float horizontalFOV = Camera.VerticalToHorizontalFieldOfView(baseFOV, referenceAspect);
                mainCamera.Lens.FieldOfView = Camera.HorizontalToVerticalFieldOfView(horizontalFOV, currentAspect);
            }
            else
            {
                // Landscape mode: rely on the standard vertical FOV
                mainCamera.Lens.FieldOfView = baseFOV;
            }
        }

        lastAspect = currentAspect;
    }
}