using UnityEngine;
using Unity.Cinemachine;

public class Bookmovement : MonoBehaviour
{
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

        GameObject back = Instantiate(backPage, transform, false);
        GameObject front = Instantiate(frontPage, transform, false);
        front.transform.localPosition += new Vector3(0, 0.000035f, 0);

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
        
        CinemachineCamera childCam = transform.GetComponentInChildren<CinemachineCamera>();
        
        // 1. Save the camera's exact WORLD position again
        Vector3 savedCamWorldPos = childCam.transform.position;

        // 2. Move the book back
        transform.Translate(0, -0.07033f, 0, Space.Self);

        // 3. Snap the camera back so the room stays frozen
        childCam.transform.position = savedCamWorldPos;

        Destroy(transform.GetChild(3).gameObject);
        Destroy(transform.GetChild(2).gameObject);
    }
}
