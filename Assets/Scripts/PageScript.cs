using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PageScript : MonoBehaviour
{
    public Canvas canva;
    public GameObject Image;
    public int nbPhoto = 1;
    public AlbumPage page;
    public Album album;

    private bool inMainBook = false;


    public void SetupPage(Album alb, int nb)
    {
        Bookmovement myBook = GetComponentInParent<Bookmovement>();

        if (myBook != null)
        {
            inMainBook = myBook.isMainBook;
        }

        if (inMainBook)
        {
            List<Photo> photos = DataBaseManager.Instance.db.Table<Photo>().ToList();
            GameObject image = (nb < photos.Count) ? Instantiate(Image, canva.transform) : null;
            if (nb < photos.Count)
            {
                image.GetComponent<PhotoScript>().UpdatePhoto(new PageSlot{PhotoId = photos[nb].Id});
            }
        }
        else
        {
            album = alb;
            int albID = alb.Id;
            page = DataBaseManager.Instance.db.Table<AlbumPage>().Where(p => p.PageNumber == nb && p.AlbumId == albID).FirstOrDefault();

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
    }

    
}
