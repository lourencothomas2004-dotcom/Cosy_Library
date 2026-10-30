using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;
using System;
using TMPro;

public class Bookmovement : MonoBehaviour
{
    public static bool isEditMode = false;
    public static event Action<bool> OnEditModeChanged;
    public Image masterEditButtonImage;
    public GameObject masterEditButton;
    public GameObject NextButton;
    public GameObject PreviousButton;
    public GameObject RenameButton;

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

    public TMP_Text Title;
    public TMP_Text sideTitle;
    public TMP_InputField  newTitle;

    private bool isOnShelf = true;
    private bool isOpen = false;
    public bool isMainBook = false;

    private GameObject spawnedBackPage;
    private GameObject spawnedFrontPage;

    Album album = null;
    public int phisicalBookId;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OriginalTrans = GetComponent<Transform>();
        OriginalPos = OriginalTrans.position;
        OriginalDir = OriginalTrans.eulerAngles;
        myCollider = GetComponent<BoxCollider>();

        if (!isMainBook)
        {
            Title.text ="";
            sideTitle.text ="";
            // 1. Fetch the actual Album object (not just the ID)
            Album existingAlbum = DataBaseManager.Instance.db.Table<Album>().Where(p => p.phisicalBookID == phisicalBookId).FirstOrDefault();
            
            if (existingAlbum == null)
            {
                // 2. If nothing was found, create it and insert it (don't forget the semicolon!)
                album = new Album { phisicalBookID = phisicalBookId };
                DataBaseManager.Instance.db.Insert(album);
            }
            else
            {
                // 3. If it was found, just assign it directly! No need to query the database twice.
                album = existingAlbum;
                Title.text = (album.Title is not null) ? album.Title : "New Album";
                sideTitle.text = Title.text;
                newTitle.text = Title.text;
            }
        }
    }

    public void Move2Read()
    {
        if (isOpen) return;
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
        if (isOpen)
        {
            CloseBook();
        }
        
        transform.eulerAngles = OriginalDir;
        transform.position = OriginalPos;

        isOnShelf = true;
    }

    public void OpenBook()
    {
        if (isOpen) return;
        isOpen = true;
        FrontTransform.localEulerAngles = OpenDir;
        FrontTransform.localPosition = OpenPos;

        masterEditButton.SetActive(true);
        NextButton.SetActive(true);
        PreviousButton.SetActive(true);

        spawnedBackPage = Instantiate(backPage, transform, false);
        spawnedFrontPage = Instantiate(frontPage, transform, false);
        spawnedFrontPage.transform.localPosition += new Vector3(0, 0.000035f, 0);

        if (!isMainBook)
        {
            spawnedFrontPage.GetComponent<PageScript>().SetupPage(album, 0);
            spawnedBackPage.GetComponent<PageScript>().SetupPage(album, 1);
        }
        else
        {
            spawnedFrontPage.GetComponent<PageScript>().SetupPage(new Album{}, 0);
            spawnedBackPage.GetComponent<PageScript>().SetupPage(new Album{}, 1);
        }

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
        if (!isOpen) return;
        isOpen = false;
        FrontTransform.localEulerAngles = Vector3.zero;
        FrontTransform.localPosition = Vector3.zero;

        masterEditButton.SetActive(false);
        NextButton.SetActive(false);
        PreviousButton.SetActive(false);
        
        CinemachineCamera childCam = transform.GetComponentInChildren<CinemachineCamera>();
        
        // 1. Save the camera's exact WORLD position again
        Vector3 savedCamWorldPos = childCam.transform.position;

        // 2. Move the book back
        transform.position = readingTransform.transform.position;

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

        if (!isMainBook)
        {
            DataBaseManager.Instance.SaveAlbum(album);

            if (isEditMode && RenameButton is not null)
            {
                RenameButton.SetActive(true);
            }
            else if (RenameButton is not null)
            {
                RenameButton.SetActive(false);
            }
        }
    }

    public void TurnNextPage()
    {
        spawnedFrontPage.GetComponent<PageScript>().NextPage();
        spawnedBackPage.GetComponent<PageScript>().NextPage();
    }

    public void TurnPreviousPage()
    {
        spawnedFrontPage.GetComponent<PageScript>().PreviousPage();
        spawnedBackPage.GetComponent<PageScript>().PreviousPage();
    }

    public void Rename()
    {
        album.Title = newTitle.text;
        Title.text = newTitle.text;
        sideTitle.text = newTitle.text;
        DataBaseManager.Instance.SaveAlbum(album);
    }
}
