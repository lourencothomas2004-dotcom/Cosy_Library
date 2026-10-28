using UnityEngine;
using UnityEngine.UI;

public class PageScript : MonoBehaviour
{
    public Canvas canva;
    public GameObject Image;
    public int nbPhoto = 1;
    public int pageNb = 0;

    private bool inMainBook = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Bookmovement myBook = GetComponentInParent<Bookmovement>();

        if (myBook != null)
        {
            inMainBook = myBook.isMainBook;
        }

        if (inMainBook)
        {
            Debug.Log("0");
            GameObject image = Instantiate(Image, canva.transform);
            Photo photo = DataBaseManager.Instance.db.Find<Photo>(pageNb + 1);
            image.GetComponent<PhotoScript>().UpdatePhoto(photo);
        }
    }

    
}
