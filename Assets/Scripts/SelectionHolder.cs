using UnityEngine;
using UnityEngine.UI;

public class SelectionHolder : MonoBehaviour
{
    public bool isBook = true;
    public Photo photo;

    private RawImage rawImage;
    private Texture2D displayedTexture;
    private string displayedTexturePath;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    private void OnDisable()
    {
        ReleaseDisplayedTexture();
    }

    private void OnDestroy()
    {
        ReleaseDisplayedTexture();
    }

    private void ReleaseDisplayedTexture()
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();

        // Remove the reference from the UI first.
        if (rawImage != null && rawImage.texture == displayedTexture)
            rawImage.texture = null;

        // Release the native Unity texture.
        if (displayedTexture != null)
        {
            if (!string.IsNullOrEmpty(displayedTexturePath) && DataBaseManager.Instance != null)
            {
                DataBaseManager.Instance.ReleaseTexture(displayedTexturePath);
            }

            displayedTexturePath = null;
            displayedTexture = null;
            photo = null;
        }
    }

    public void UpdatePhoto(Photo image)
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();

        // Release the previous photograph before loading another.
        ReleaseDisplayedTexture();

        photo = image;

        if (rawImage == null)
        {
            Debug.LogError("[SELECTOR] Missing RawImage component.");
            return;
        }

        if (image == null)
        {
            rawImage.color = new Color(0, 0, 0, 0);
            return;
        }

        displayedTexture = DataBaseManager.Instance.LoadPictureFromPath(image.Path);
        displayedTexturePath = displayedTexture != null ? image.Path : null;

        rawImage.texture = displayedTexture;

        rawImage.color = (displayedTexture != null) ? Color.white : new Color(0, 0, 0, 0);
    }

    public void pictureChosen()
    {
        if(photo is null) return;

        if(isBook)
        {
            int side = (int)transform.parent.parent.Find("Slider").GetComponent<Slider>().value;
            GameObject page;
            if (side==0)
            {
                page = GetComponentInParent<Bookmovement>().GetFrontPage();
            }
            else
            {
                page = GetComponentInParent<Bookmovement>().GetBackPage();
            }

            if (page is not null)
            {
                PageScript albumPage = page.GetComponent<PageScript>();

                if (albumPage.page.Id == 0)
                {
                    DataBaseManager.Instance.db.Insert(albumPage.page);
                }

                PageSlot image = new PageSlot{AlbumPageId = albumPage.page.Id, PhotoId = photo.Id, xPos = 0, yPos = 0, rotation = 0, scale = 1};

                DataBaseManager.Instance.db.Insert(image);
        
                albumPage.SetupPage(albumPage.album, albumPage.page.PageNumber);
            }
            else
            {
                Debug.Log("Page is null");
            }

            transform.parent.parent.parent.gameObject.SetActive(false);
        }
        else
        {
            transform.parent.parent.parent.parent.gameObject.GetComponent<FrameScript>().SetupFrame(photo);
            transform.parent.parent.parent.gameObject.SetActive(false);
        }
    }
}
