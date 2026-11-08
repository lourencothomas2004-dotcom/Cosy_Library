using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

public class PhotoScript : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    private Bookmovement manager;

    public Button rotateButton;
    public Button DeleteButton;
    public bool canRotate = false;

    // Offset between the pointer and the photo's anchored position.
    private Vector2 clickOffset;

    public Photo photo;
    public PageSlot pageSlot;

    private bool inMainBook = false;

    // Two-finger pinch state.
    private readonly Dictionary<int, Vector2> activePointers =
        new Dictionary<int, Vector2>();

    private float initialPinchDistance;
    private Vector3 initialScale;
    private bool isPinching = false;
    [SerializeField] private float minPhotoScale = 0.2f;
    [SerializeField] private float maxPhotoScale = 3f;

    // Each displayed photo owns the texture loaded for it.
    private Texture2D loadedTexture;
    private string loadedTexturePath;
    private Sprite generatedSprite;

    private void Start()
    {
        RefreshOwningBook();
        UpdateEditModeUI(Bookmovement.isEditMode);
    }

    private void OnEnable()
    {
        Bookmovement.OnEditModeChanged += UpdateEditModeUI;
    }

    private void OnDisable()
    {
        Bookmovement.OnEditModeChanged -= UpdateEditModeUI;
        activePointers.Clear();
        isPinching = false;
    }

    private void OnDestroy()
    {
        if (generatedSprite != null)
        {
            Destroy(generatedSprite);
            generatedSprite = null;
        }

        if (loadedTexturePath != null &&
            DataBaseManager.Instance != null)
        {
            DataBaseManager.Instance.ReleaseTexture(loadedTexturePath);
        }

        loadedTexture = null;
        loadedTexturePath = null;
        photo = null;
    }

    private void RefreshOwningBook()
    {
        manager = GetComponentInParent<Bookmovement>();
        inMainBook = manager != null && manager.isMainBook;
    }

    private void UpdateEditModeUI(bool editModeActive)
    {
        // Rotation remains disabled in the main book.
        if (rotateButton != null)
            rotateButton.gameObject.SetActive(editModeActive && !inMainBook);

        // Deletion is allowed in BOTH the main book and albums.
        if (DeleteButton != null)
            DeleteButton.gameObject.SetActive(editModeActive);

        if (!editModeActive && canRotate)
        {
            canRotate = false;

            if (rotateButton != null)
            {
                Image buttonImage = rotateButton.GetComponent<Image>();
                if (buttonImage != null)
                    buttonImage.color = Color.white;
            }
        }
    }

    private float GetPinchDistance()
    {
        if (activePointers.Count < 2)
            return 0f;

        using (Dictionary<int, Vector2>.ValueCollection.Enumerator enumerator =
               activePointers.Values.GetEnumerator())
        {
            if (!enumerator.MoveNext())
                return 0f;

            Vector2 first = enumerator.Current;

            if (!enumerator.MoveNext())
                return 0f;

            Vector2 second = enumerator.Current;
            return Vector2.Distance(first, second);
        }
    }

    // Converts a screen pointer position into the same coordinate space as
    // RectTransform.anchoredPosition. This also works with top-left anchors.
    private bool TryGetPointerAnchoredPosition(
        PointerEventData eventData,
        out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        RectTransform parentRect = transform.parent as RectTransform;
        RectTransform photoRect = GetComponent<RectTransform>();

        if (parentRect == null || photoRect == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            return false;
        }

        Vector2 anchor = photoRect.anchorMin + Vector2.Scale(
            photoRect.anchorMax - photoRect.anchorMin,
            photoRect.pivot
        );

        Vector2 anchorReference = parentRect.rect.min + Vector2.Scale(
            parentRect.rect.size,
            anchor
        );

        anchoredPosition = localPoint - anchorReference;
        return true;
    }

    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Bookmovement.isEditMode)
            return;

        transform.SetAsLastSibling();

        // Register this finger.
        activePointers[eventData.pointerId] = eventData.position;

        // Two fingers: initialize pinch scaling.
        if (activePointers.Count >= 2)
        {
            isPinching = true;
            initialPinchDistance = GetPinchDistance();
            initialScale = GetComponent<RectTransform>().localScale;
            return;
        }

        // In the main book, single-finger movement is disabled.
        // We still register the first finger because a second
        // finger may start a pinch gesture.
        if (inMainBook)
            return;

        if (!TryGetPointerAnchoredPosition(
            eventData, out Vector2 pointerPosition))
            return;

        RectTransform rect = GetComponent<RectTransform>();

        clickOffset = rect.anchoredPosition - pointerPosition;
    }

    
    public void OnDrag(PointerEventData eventData)
    {
        if (!Bookmovement.isEditMode)
            return;

        // Update the moving finger.
        if (activePointers.ContainsKey(eventData.pointerId))
            activePointers[eventData.pointerId] = eventData.position;

        RectTransform rect = GetComponent<RectTransform>();

        // Two-finger pinch scaling.
        if (activePointers.Count >= 2)
        {
            float currentDistance = GetPinchDistance();

            if (initialPinchDistance > 0.01f)
            {
                float scaleFactor =
                    currentDistance / initialPinchDistance;

                float baseScale = Mathf.Max(
                    Mathf.Abs(initialScale.x),
                    0.0001f
                );

                float targetScale = Mathf.Clamp(
                    baseScale * scaleFactor,
                    minPhotoScale,
                    maxPhotoScale
                );

                float appliedFactor = targetScale / baseScale;

                rect.localScale = initialScale * appliedFactor;
            }

            return;
        }

        // Prevent movement after a pinch until both fingers
        // have been lifted.
        if (isPinching || inMainBook)
            return;

        // Single-finger movement or rotation in albums.
        if (!canRotate)
        {
            if (!TryGetPointerAnchoredPosition(
                eventData, out Vector2 pointerPosition))
                return;

            rect.anchoredPosition =
                pointerPosition + clickOffset;
        }
        else
        {
            rect.localEulerAngles +=
                new Vector3(0f, 0f, -eventData.delta.x);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Always unregister the pointer, even if edit mode was switched off
        // while the finger was still touching the screen.
        bool wasTracked = activePointers.Remove(eventData.pointerId);

        if (activePointers.Count == 0)
            isPinching = false;

        if (!wasTracked)
            return;

        if (!Bookmovement.isEditMode || inMainBook || pageSlot == null)
            return;

        SaveCurrentPageLayout();
    }

    private void SaveCurrentPageLayout()
    {
        if (DataBaseManager.Instance == null ||
            DataBaseManager.Instance.db == null ||
            transform.parent == null)
        {
            Debug.LogError("[PHOTO LAYOUT] Cannot save: database or parent is missing.");
            return;
        }

        // Save every photo on this page because SetAsLastSibling changes
        // the relative drawing order of the photos.
        Transform pageParent = transform.parent;

        for (int i = 0; i < pageParent.childCount; i++)
        {
            Transform child = pageParent.GetChild(i);
            PhotoScript siblingPhoto = child.GetComponent<PhotoScript>();

            if (siblingPhoto == null ||
                siblingPhoto.inMainBook ||
                siblingPhoto.pageSlot == null)
            {
                continue;
            }

            RectTransform rect = child.GetComponent<RectTransform>();
            if (rect == null)
                continue;

            PageSlot slot = siblingPhoto.pageSlot;
            slot.xPos = rect.anchoredPosition.x;
            slot.yPos = rect.anchoredPosition.y;
            slot.rotation = rect.localEulerAngles.z;
            slot.scale = rect.localScale.x;
            slot.layerOrder = i;

            if (slot.Id == 0)
                DataBaseManager.Instance.db.Insert(slot);
            else
                DataBaseManager.Instance.db.Update(slot);
        }

        Debug.Log(
            $"[PHOTO LAYOUT] Saved page layout. " +
            $"Position=({pageSlot.xPos}, {pageSlot.yPos})"
        );
    }

    public void BoolRotate()
    {
        canRotate = !canRotate;

        if (rotateButton != null)
        {
            Image buttonImage = rotateButton.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = canRotate ? Color.green : Color.white;
        }
    }

    public void UpdatePhoto(PageSlot slot)
    {
        if (slot == null)
            return;

        pageSlot = slot;
        RefreshOwningBook();

        if (slot.PhotoId <= 0)
            return;

        if (DataBaseManager.Instance == null || DataBaseManager.Instance.db == null)
        {
            Debug.LogError("[PHOTO DISPLAY] DataBaseManager or its database is not ready.");
            return;
        }

        string previousTexturePath = loadedTexturePath;
        photo = DataBaseManager.Instance.db.Find<Photo>(slot.PhotoId);

        if (photo == null)
        {
            Debug.LogError(
                $"[PHOTO DISPLAY] No Photo record found for ID {slot.PhotoId}."
            );
            return;
        }

        Texture2D newTexture =
            DataBaseManager.Instance.LoadPictureFromPath(photo.Path);

        if (newTexture == null)
        {
            Debug.LogError(
                $"[PHOTO DISPLAY] Could not load photo ID {photo.Id}: {photo.Path}"
            );
            return;
        }

        RawImage rawImage = GetComponent<RawImage>();
        Image uiImage = GetComponent<Image>();

        // Keep the existing RawImage prefab setup. RawImage is preferred.
        if (rawImage != null)
        {
            rawImage.texture = newTexture;
        }
        else if (uiImage != null)
        {
            Sprite newSprite = Sprite.Create(
                newTexture,
                new Rect(0, 0, newTexture.width, newTexture.height),
                new Vector2(0.5f, 0.5f)
            );

            if (generatedSprite != null)
                Destroy(generatedSprite);

            generatedSprite = newSprite;
            uiImage.sprite = generatedSprite;
        }
        else
        {
            DataBaseManager.Instance.ReleaseTexture(photo.Path);
            Debug.LogError(
                $"[PHOTO DISPLAY] '{name}' needs a RawImage component " +
                "on the same GameObject as PhotoScript."
            );
            return;
        }

        // Release the previous texture only after the new one was loaded
        // and assigned successfully.
        if (generatedSprite != null && rawImage != null)
        {
            Destroy(generatedSprite);
            generatedSprite = null;
        }

        // This PhotoScript now owns a reference to the new texture.
        loadedTexture = newTexture;
        loadedTexturePath = photo.Path;

        // Release the previous texture reference.
        // It will only be destroyed if nobody else uses it.
        if (!string.IsNullOrEmpty(previousTexturePath))
        {
            DataBaseManager.Instance.ReleaseTexture(previousTexturePath);
        }
        
        AspectRatioFitter fitter = GetComponent<AspectRatioFitter>();

        if (fitter != null)
        {
            fitter.aspectRatio =
                (float)newTexture.width / newTexture.height;
        }

        // Main-book images keep the position/rotation/scale configured on
        // the photo prefab. Main-book PageSlots are temporary records.
        if (inMainBook)
            return;

        // Album images restore their saved layout in anchored coordinates.
        RectTransform rectTransform = GetComponent<RectTransform>();

        rectTransform.anchoredPosition =
            new Vector2(slot.xPos, slot.yPos);

        float safeScale = slot.scale > 0f ? slot.scale : 1f;

        rectTransform.localScale =
            new Vector3(safeScale, safeScale, 1f);

        rectTransform.localEulerAngles =
            new Vector3(0f, 0f, slot.rotation);
    }

    public void DeletePicture()
    {
        if (photo == null)
        {
            Debug.LogWarning("[PHOTO DELETE] No photo is assigned to this object.");
            return;
        }

        if (DataBaseManager.Instance == null ||
            DataBaseManager.Instance.db == null)
        {
            Debug.LogError("[PHOTO DELETE] Database is unavailable.");
            return;
        }

        if (!inMainBook)
        {
            if (pageSlot != null && pageSlot.Id > 0)
                DataBaseManager.Instance.db.Delete<PageSlot>(pageSlot.Id);
        }
        else
        {
            int photoId = photo.Id;

            List<PageSlot> slots = DataBaseManager.Instance.db
                .Table<PageSlot>()
                .Where(p => p.PhotoId == photoId)
                .ToList();

            foreach (PageSlot slot in slots)
                DataBaseManager.Instance.db.Delete<PageSlot>(slot.Id);

            DataBaseManager.Instance.db.Delete<Photo>(photoId);
        }

        Destroy(gameObject);
    }
}
