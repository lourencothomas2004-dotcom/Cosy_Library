using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

public class PhotoScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    Bookmovement manager;
    public Button rotateButton;
    public Button DeleteButton;
    public bool canRotate = false;

    // Store the distance between the mouse and the center of the photo
    private Vector2 clickOffset;
    public Photo photo;
    public PageSlot pageSlot;

    private bool inMainBook = false;

    void Start()
    {
        Bookmovement myBook = GetComponentInParent<Bookmovement>();

        if (myBook != null)
        {
            inMainBook = myBook.isMainBook;
        }

        // When instantiated, immediately set the button visibility to match the current global state
        UpdateEditModeUI(Bookmovement.isEditMode);
    }

    void OnEnable()
    {
        // Subscribe to the master event
        Bookmovement.OnEditModeChanged += UpdateEditModeUI;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks when the photo is destroyed
        Bookmovement.OnEditModeChanged -= UpdateEditModeUI;
    }

    private void UpdateEditModeUI(bool editModeActive)
    {
        // Activate or deactivate the rotate button GameObject
        if (rotateButton != null)
        {
            rotateButton.gameObject.SetActive(editModeActive);
            DeleteButton.gameObject.SetActive(editModeActive);
        }

        // If we exit edit mode while rotating, turn off the rotation state
        if (!editModeActive && canRotate)
        {
            BoolRotate(); 
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Bookmovement.isEditMode || !inMainBook) return;
        transform.SetAsLastSibling();

        // 1. Find exactly where the mouse is on the page when you click
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localMousePos
        );

        // 2. Calculate the difference between the photo's position and the mouse
        clickOffset = GetComponent<RectTransform>().anchoredPosition - localMousePos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!Bookmovement.isEditMode || !inMainBook) return;
        if (!canRotate)
        {
            // 1. Find where the mouse is right now
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform, 
                eventData.position,                
                eventData.pressEventCamera,        
                out Vector2 localMousePos             
            );
    
            // 2. Move the photo to the mouse, but add the offset back so it doesn't jump!
            GetComponent<RectTransform>().anchoredPosition = localMousePos + clickOffset;
        }
        else
        {
            GetComponent<RectTransform>().localEulerAngles += new Vector3(0, 0, -eventData.delta.x);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!Bookmovement.isEditMode || !inMainBook) return;
        if (!canRotate)
        {
            Debug.Log(transform.localPosition);
        }
        else
        {
            Debug.Log(GetComponent<RectTransform>().localEulerAngles.z);
        }
    }

    public void BoolRotate()
    {
        canRotate = !canRotate;

        rotateButton.GetComponent<Image>().color = canRotate ? Color.green : Color.white;
    }

    public void UpdatePhoto(PageSlot picture)
    {
        pageSlot = picture;
        photo = DataBaseManager.Instance.db.Find<Photo>(picture.PhotoId);
        GetComponent<AspectRatioFitter>().aspectRatio = photo.AspectRatio;
        GetComponent<RawImage>().texture = DataBaseManager.Instance.LoadPictureFromPath(photo.Path);
    }

    public void DeletePicture()
    {
        if (!inMainBook)
        {
            int photoID = photo.Id;
            int id = pageSlot.Id;

            DataBaseManager.Instance.db.Delete<PageSlot>(id);
        }
        else
        {
            int photoID = photo.Id;
            DataBaseManager.Instance.db.Delete<Photo>(photoID);

            List<PageSlot> slots = DataBaseManager.Instance.db.Table<PageSlot>().Where(p => p.PhotoId == photoID).ToList();
            foreach (PageSlot slot in slots)
            {
                DataBaseManager.Instance.db.Delete<PageSlot>(slot.Id);
            }
        }
        Destroy(gameObject);
    }
}
