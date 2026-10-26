using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

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

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            Vector3 inputPosition = Input.GetMouseButtonDown(0) ? Input.mousePosition : (Vector3)Input.GetTouch(0).position;
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