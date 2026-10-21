using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    public CinemachineCamera roomView;


    public CinemachineCamera mainCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = roomView;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            Vector3 inputPosition;
            if (Input.GetMouseButtonDown(0))
            {
                inputPosition = Input.mousePosition;
            }
            else
            {
                inputPosition = Input.GetTouch(0).position;
            }

            Ray ray = Camera.main.ScreenPointToRay(inputPosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                CinemachineCamera clickedCamera = hit.transform.GetComponentInChildren<CinemachineCamera>();

                if (clickedCamera != null && clickedCamera != mainCamera)
                {
                    changeCamera(clickedCamera);
                }
            }
        }
        
    }
    
    public void changeCamera(CinemachineCamera clickedCamera)
    {
        if (clickedCamera != null && clickedCamera != mainCamera)
        {
            mainCamera.Priority = 0;
            clickedCamera.Priority = 10;
            mainCamera = clickedCamera;
        }
    }

    public void Change2RoomCamera()
    {
        mainCamera.Priority = 0;
        roomView.Priority = 10;
        mainCamera = roomView;
    }
}
