using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class PageScript : MonoBehaviour
{
    public Canvas canva;
    public GameObject Image;
    public AlbumPage page;
    public Album album;

    private bool inMainBook = false;


    public void SetupPage(Album alb, int nb)
    {
        // Destroy any previous Child
        for (int i = canva.gameObject.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(canva.gameObject.transform.GetChild(i).gameObject);
        }

        Bookmovement myBook = GetComponentInParent<Bookmovement>();

        if (myBook != null)
        {
            inMainBook = myBook.isMainBook;
        }

        album = alb;

        if (inMainBook)
        {
            List<Photo> photos = DataBaseManager.Instance.db.Table<Photo>().ToList();
            page = new AlbumPage{PageNumber = nb};
            GameObject image = (nb < photos.Count) ? Instantiate(Image, canva.transform) : null;
            if (nb < photos.Count)
            {
                image.GetComponent<PhotoScript>().UpdatePhoto(new PageSlot{PhotoId = photos[page.PageNumber].Id});
            }
        }
        else
        {
            int? albID = alb.Id;
            page = DataBaseManager.Instance.db.Table<AlbumPage>().Where(p => p.PageNumber == nb && p.AlbumId == albID).FirstOrDefault();

            if (page is not null)
            {
                int pageID = page.Id;
                List<PageSlot> photos = DataBaseManager.Instance.db.Table<PageSlot>().Where(p => p.AlbumPageId == pageID).ToList();
    
                foreach (PageSlot photo in photos)
                {
                    GameObject image = Instantiate(Image, canva.transform);
                    image.transform.localPosition = new Vector3(photo.xPos, photo.yPos, 0);
                    image.transform.localEulerAngles = new Vector3(0, 0, photo.rotation);
                    image.transform.localScale = Vector3.one * photo.scale;
                    
                    image.GetComponent<PhotoScript>().UpdatePhoto(photo);
                }
            }
            else
            {
                page = new AlbumPage{PageNumber = nb};
            }
        }
    }

    public void NextPage()
    {
        SetupPage(album, page.PageNumber + 2);
    }

    public void PreviousPage()
    {
        int? nb = (page.PageNumber>1) ? page.PageNumber - 2 : null;
        if (nb is not null)
        {
            SetupPage(album, (int)nb);
        }
        else
        {
            GetComponentInParent<Bookmovement>().CloseBook();
        }
    }
}
