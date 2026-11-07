using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PageScript : MonoBehaviour
{
    public Canvas canva;
    public GameObject Image;
    public AlbumPage page;
    public Album album;

    private bool inMainBook = false;

    public void SetupPage(Album alb, int pageNumber)
    {
        if (canva == null)
        {
            Debug.LogError($"[PAGE] Canvas is not assigned on '{name}'.");
            return;
        }

        if (Image == null)
        {
            Debug.LogError($"[PAGE] Photo prefab (Image) is not assigned on '{name}'.");
            return;
        }

        // Remove the photographs currently displayed on this page.
        for (int i = canva.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(canva.transform.GetChild(i).gameObject);
        }

        Bookmovement owningBook = GetComponentInParent<Bookmovement>();
        inMainBook = owningBook != null && owningBook.isMainBook;
        album = alb;

        if (DataBaseManager.Instance == null || DataBaseManager.Instance.db == null)
        {
            Debug.LogError("[PAGE] DataBaseManager or its database is not ready.");
            return;
        }

        if (inMainBook)
        {
            // The main book is a view over the complete photo collection.
            // Its PageSlots are temporary: do not load or save album layouts.
            List<Photo> allPhotos = DataBaseManager.Instance.db
                .Table<Photo>()
                .OrderBy(p => p.Id)
                .ToList();

            page = new AlbumPage { PageNumber = pageNumber };

            Debug.Log(
                $"[MAIN BOOK] Page={pageNumber}, " +
                $"photo count={allPhotos.Count}"
            );

            if (pageNumber < 0 || pageNumber >= allPhotos.Count)
            {
                Debug.Log(
                    $"[MAIN BOOK] No photo assigned to page {pageNumber}."
                );
                return;
            }

            GameObject photoObject = Instantiate(
                Image,
                canva.transform,
                false
            );

            PhotoScript photoScript =
                photoObject.GetComponent<PhotoScript>();

            if (photoScript == null)
            {
                Debug.LogError(
                    $"[MAIN BOOK] Prefab '{Image.name}' has no PhotoScript " +
                    "component on its root GameObject."
                );
                Destroy(photoObject);
                return;
            }

            Photo selectedPhoto = allPhotos[pageNumber];

            Debug.Log(
                $"[MAIN BOOK] Loading photo ID={selectedPhoto.Id}, " +
                $"path exists={System.IO.File.Exists(selectedPhoto.Path)}, " +
                $"path={selectedPhoto.Path}"
            );

            // Do not provide album layout coordinates here. PhotoScript knows
            // this belongs to the main book and preserves the prefab position.
            photoScript.UpdatePhoto(
                new PageSlot { PhotoId = selectedPhoto.Id }
            );

            return;
        }

        // The remaining branch is for normal albums.
        if (album == null || album.Id <= 0)
        {
            Debug.LogError(
                $"[ALBUM PAGE] Invalid album supplied to page '{name}'."
            );
            page = new AlbumPage { PageNumber = pageNumber };
            return;
        }

        page = DataBaseManager.Instance.db
            .Table<AlbumPage>()
            .Where(p => p.PageNumber == pageNumber && p.AlbumId == album.Id)
            .FirstOrDefault();

        if (page == null)
        {
            // The page is created in the database when its first photo is added.
            page = new AlbumPage
            {
                PageNumber = pageNumber,
                AlbumId = album.Id
            };
            return;
        }

        List<PageSlot> savedSlots = DataBaseManager.Instance.db
            .Table<PageSlot>()
            .Where(p => p.AlbumPageId == page.Id)
            .OrderBy(p => p.layerOrder)
            .ThenBy(p => p.Id)
            .ToList();

        foreach (PageSlot slot in savedSlots)
        {
            GameObject photoObject = Instantiate(
                Image,
                canva.transform,
                false
            );

            PhotoScript photoScript =
                photoObject.GetComponent<PhotoScript>();

            if (photoScript == null)
            {
                Debug.LogError(
                    $"[ALBUM PAGE] Prefab '{Image.name}' has no PhotoScript " +
                    "component on its root GameObject."
                );
                Destroy(photoObject);
                continue;
            }

            // PhotoScript restores anchored position, rotation and scale.
            photoScript.UpdatePhoto(slot);
        }
    }

    public void NextPage()
    {
        if (page == null)
            return;

        SetupPage(album, page.PageNumber + 2);
    }

    public void PreviousPage()
    {
        if (page == null)
            return;

        if (page.PageNumber > 1)
        {
            SetupPage(album, page.PageNumber - 2);
        }
        else
        {
            Bookmovement owningBook = GetComponentInParent<Bookmovement>();
            if (owningBook != null)
                owningBook.CloseBook();
        }
    }
}
