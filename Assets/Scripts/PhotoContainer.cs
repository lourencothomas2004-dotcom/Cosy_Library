using UnityEngine;
using System.Collections.Generic;

public class PhotoContainer : MonoBehaviour
{
    public GameObject container;
    int pageNb = 0;
    
    void setupPage()
    {
        List<Photo> photos = DataBaseManager.Instance.db.Table<Photo>().ToList();
        SelectionHolder[] holders = container.GetComponentsInChildren<SelectionHolder>();

        for (int i = 0; i < holders.Length; i++)
        {
            int j = pageNb * holders.Length + i;
            if (j < photos.Count)
            {
                holders[i].UpdatePhoto(photos[j]);
            }
            else if (i==0)
            {
                pageNb = (pageNb >= 1) ? pageNb-1 : 0;
            }
            else
            {
                holders[i].UpdatePhoto(null);
            }
        }
    }

    public void Initialize()
    {
        pageNb = 0;
        setupPage();
    }

    public void NextPage()
    {
        pageNb += 1;
        setupPage();
    }

    public void PreviousPage()
    {
        pageNb = (pageNb >= 1) ? pageNb-1 : 0;
        setupPage();
    }
}
