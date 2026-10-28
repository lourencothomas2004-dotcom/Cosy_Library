using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;
using System;

public class Bookmovement : MonoBehaviour
{
    public static bool isEditMode = false;
    public static event Action<bool> OnEditModeChanged;
    public Image masterEditButtonImage;
    public GameObject masterEditButton;

    public Transform OriginalTrans;
    public Vector3 OriginalPos;
    public Vector3 OriginalDir;
    public Transform readingTransform;

    public Transform FrontTransform;
    public Vector3 OpenPos;
    public Vector3 OpenDir;
    
    private BoxCollider myCollider;
    public GameObject backPage;
    public GameObject frontPage;

    private bool isOnShelf = true;
    private bool isOpen = false;
    public bool isMainBook = false;

    private GameObject spawnedBackPage;
    private GameObject spawnedFrontPage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OriginalTrans = GetComponent<Transform>();
        OriginalPos = OriginalTrans.position;
        OriginalDir = OriginalTrans.eulerAngles;
        myCollider = GetComponent<BoxCollider>();
    }

    public void Move2Read()
    {
        myCollider.enabled = true;

        transform.eulerAngles = readingTransform.eulerAngles;
        transform.position = readingTransform.transform.position;

        if (!isOnShelf)
        {
            OpenBook();
        }

        isOnShelf = false;
    }

    public void Move2Shelf()
    {
        transform.eulerAngles = OriginalDir;
        transform.position = OriginalPos;

        if (isOpen)
        {
            CloseBook();
        }
        isOnShelf = true;
    }

    public void OpenBook()
    {
        isOpen = true;
        FrontTransform.localEulerAngles = OpenDir;
        FrontTransform.localPosition = OpenPos;

        masterEditButton.SetActive(true);

        spawnedBackPage = Instantiate(backPage, transform, false);
        spawnedFrontPage = Instantiate(frontPage, transform, false);
        spawnedFrontPage.transform.localPosition += new Vector3(0, 0.000035f, 0);

        spawnedFrontPage.GetComponent<PageScript>().pageNb = 0;
        spawnedBackPage.GetComponent<PageScript>().pageNb = 1;

        CinemachineCamera childCam = transform.GetComponentInChildren<CinemachineCamera>();
        
        // 1. Save the camera's exact WORLD position before the book moves
        Vector3 savedCamWorldPos = childCam.transform.position;

        // 2. Move the book on its Z axis
        transform.Translate(0, 0.07033f, 0, Space.Self);

        // 3. Snap the camera right back to its saved world position
        childCam.transform.position = savedCamWorldPos;
    }

    public void CloseBook()
    {
        isOpen = false;
        FrontTransform.localEulerAngles = Vector3.zero;
        FrontTransform.localPosition = Vector3.zero;

        masterEditButton.SetActive(false);
        
        CinemachineCamera childCam = transform.GetComponentInChildren<CinemachineCamera>();
        
        // 1. Save the camera's exact WORLD position again
        Vector3 savedCamWorldPos = childCam.transform.position;

        // 2. Move the book back
        transform.Translate(0, -0.07033f, 0, Space.Self);

        // 3. Snap the camera back so the room stays frozen
        childCam.transform.position = savedCamWorldPos;

        Destroy(spawnedBackPage);
        Destroy(spawnedFrontPage);
    }

    public void ToggleEditMode()
    {
        isEditMode = !isEditMode;
        
        // Broadcast the event to all photos currently in the scene
        OnEditModeChanged?.Invoke(isEditMode);

        // Optional: Change the master button color to visually show it is active
        if (masterEditButtonImage != null)
        {
            masterEditButtonImage.color = isEditMode ? Color.green : Color.white;
        }
    }
}
