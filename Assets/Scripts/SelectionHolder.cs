using UnityEngine;
using UnityEngine.UI;

public class SelectionHolder : MonoBehaviour
{
    public bool isBook = true;
    public Photo photo;

    public void UpdatePhoto(Photo image)
    {
        photo = image;
        GetComponent<RawImage>().texture = (image is not null) ? DataBaseManager.Instance.LoadPictureFromPath(image.Path) : null;
        GetComponent<RawImage>().color = (image is null) ? new Color(0, 0, 0, 0) : Color.white;
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
