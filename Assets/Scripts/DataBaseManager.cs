using UnityEngine;
using SQLite;
using System.IO;
using SFB;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Png;
using System.Linq;
using System;

public class DataBaseManager : MonoBehaviour
{
    public static DataBaseManager Instance;

    private string dbPath;
    public SQLiteConnection db;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Prevents duplicates if you reload the scene
        }

        dbPath = Path.Combine(Application.persistentDataPath, "GalleryDatabase.sqlite");
        db = new SQLiteConnection(dbPath);

        db.CreateTable<Photo>();
        db.CreateTable<Frame>();
        db.CreateTable<Album>();
        db.CreateTable<AlbumPage>();
        db.CreateTable<PageSlot>();
    }

    public Photo loadPhoto(int Id) 
    {
        return db.Find<Photo>(Id);
    }

    void SavePhoto(Photo photo)
    {
        if (photo.Id == 0)
        {
            db.Insert(photo);
        }
        else
        {
            db.Update(photo);
        }
    }

    void SaveFrame(Frame frame)
    {
        if (frame.Id == 0)
        {
            db.Insert(frame);
        }
        else
        {
            db.Update(frame);
        }
    }

    void SaveSlot(PageSlot slot)
    {
        if (slot.Id == 0)
        {
            db.Insert(slot);
        }
        else
        {
            db.Update(slot);
        }
    }

    void SavePage(AlbumPage page)
    {
        if (page.Id == 0)
        {
            db.Insert(page);
        }
        else
        {
            db.Update(page);
        }
    }

    public void SaveAlbum(Album album)
    {
        if (album.Id == 0)
        {
            db.Insert(album);
        }
        else
        {
            db.Update(album);
        }
    }

    public void LoadPicture()
    {
        var extensions = new [] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg" )
        };
        
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true);
        foreach (string path in paths)
        {
            int ID = 0;
            Photo photo = new Photo 
            {
                Id = ID,
                Path = path
            };

            // Get The date
            var directories = ImageMetadataReader.ReadMetadata(path);
            var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfdDirectory is not null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime dateTaken))
            {
               photo.DateTaken = dateTaken;
            }

            // Get the aspect ratio
            var jpegDirectory = directories.OfType<JpegDirectory>().FirstOrDefault();
            var PngDirectory = directories.OfType<PngDirectory>().FirstOrDefault();
            if (jpegDirectory is not null && jpegDirectory.TryGetInt32(JpegDirectory.TagImageWidth, out int width) && jpegDirectory.TryGetInt32(JpegDirectory.TagImageHeight, out int height))
            {
                photo.AspectRatio = (float)width/(float)height;
            }
            else if (PngDirectory is not null && PngDirectory.TryGetInt32(PngDirectory.TagImageWidth, out int pngwidth) && PngDirectory.TryGetInt32(PngDirectory.TagImageHeight, out int pngheight))
            {
                photo.AspectRatio = (float)pngwidth/(float)pngheight;
            }

            SavePhoto(photo);
        }
    }

    public Texture2D LoadPictureFromPath(string path)
    {
        byte[] fileData = System.IO.File.ReadAllBytes(path);
        Texture2D myTexture = new Texture2D(2, 2);
        myTexture.LoadImage(fileData);
        return myTexture;
    }
}
