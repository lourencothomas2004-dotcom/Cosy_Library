using UnityEngine;
using SQLite;
using System;
using System.IO;
using System.Linq;
using SFB;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

public class DataBaseManager : MonoBehaviour
{
    public static DataBaseManager Instance;

    private string dbPath;
    public SQLiteConnection db;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        dbPath = Path.Combine(
            Application.persistentDataPath,
            "GalleryDatabase.sqlite"
        );

        db = new SQLiteConnection(dbPath);

        db.CreateTable<Photo>();
        db.CreateTable<Frame>();
        db.CreateTable<Album>();
        db.CreateTable<AlbumPage>();
        db.CreateTable<PageSlot>();
    }

    public Photo loadPhoto(int id)
    {
        return db.Find<Photo>(id);
    }

    private void SavePhoto(Photo photo)
    {
        if (photo == null)
            return;

        if (photo.Id == 0)
            db.Insert(photo);
        else
            db.Update(photo);
    }

    private void SaveFrame(Frame frame)
    {
        if (frame == null)
            return;

        if (frame.Id == 0)
            db.Insert(frame);
        else
            db.Update(frame);
    }

    private void SaveSlot(PageSlot slot)
    {
        if (slot == null)
            return;

        if (slot.Id == 0)
            db.Insert(slot);
        else
            db.Update(slot);
    }

    private void SavePage(AlbumPage page)
    {
        if (page == null)
            return;

        if (page.Id == 0)
            db.Insert(page);
        else
            db.Update(page);
    }

    public void SaveAlbum(Album album)
    {
        if (album == null)
            return;

        if (album.Id == 0)
            db.Insert(album);
        else
            db.Update(album);
    }

    public void LoadPicture()
    {
#if UNITY_EDITOR
        ExtensionFilter[] extensions =
        {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Open File",
            "",
            extensions,
            true
        );

        if (paths == null || paths.Length == 0)
        {
            Debug.Log("Photo selection canceled in Editor.");
            return;
        }

        ProcessPhotos(paths);
#else
        string[] mimeTypes = { "image/png", "image/jpeg" };

        NativeFilePicker.PickMultipleFiles((paths) =>
        {
            if (paths == null || paths.Length == 0)
            {
                Debug.Log("Photo selection canceled on Android.");
                return;
            }

            ProcessPhotos(paths);
        }, mimeTypes);
#endif
    }

    private void ProcessPhotos(string[] paths)
    {
        if (paths == null || paths.Length == 0)
            return;

        foreach (string selectedPath in paths)
        {
            if (string.IsNullOrWhiteSpace(selectedPath))
                continue;

            string sourcePath = selectedPath;
            bool copiedSourceToPersistentData = false;
            bool importSucceeded = false;

#if !UNITY_EDITOR
            try
            {
                string uniqueFileName =
                    Guid.NewGuid().ToString("N") +
                    Path.GetExtension(selectedPath);

                sourcePath = Path.Combine(
                    Application.persistentDataPath,
                    uniqueFileName
                );

                File.Copy(selectedPath, sourcePath, false);
                copiedSourceToPersistentData = true;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[PHOTO IMPORT] Could not copy selected file: {e}"
                );
                continue;
            }
#endif

            Texture2D decoded = null;
            Texture2D upright = null;

            try
            {
                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException(
                        "The selected image file does not exist.",
                        sourcePath
                    );

                int orientation = 1;
                DateTime dateTaken = DateTime.MinValue;

                // Read EXIF orientation and original capture date when available.
                try
                {
                    var directories =
                        ImageMetadataReader.ReadMetadata(sourcePath);

                    ExifIfd0Directory ifd0Directory = directories
                        .OfType<ExifIfd0Directory>()
                        .FirstOrDefault();

                    if (ifd0Directory != null &&
                        ifd0Directory.TryGetInt32(
                            ExifDirectoryBase.TagOrientation,
                            out int exifOrientation
                        ) &&
                        exifOrientation >= 1 &&
                        exifOrientation <= 8)
                    {
                        orientation = exifOrientation;
                    }

                    ExifSubIfdDirectory subIfdDirectory = directories
                        .OfType<ExifSubIfdDirectory>()
                        .FirstOrDefault();

                    if (subIfdDirectory != null &&
                        subIfdDirectory.TryGetDateTime(
                            ExifDirectoryBase.TagDateTimeOriginal,
                            out DateTime extractedDate
                        ))
                    {
                        dateTaken = extractedDate;
                    }
                }
                catch (Exception e)
                {
                    // An image without readable EXIF is treated as orientation 1.
                    Debug.LogWarning(
                        $"[PHOTO IMPORT] EXIF metadata unavailable for " +
                        $"'{sourcePath}': {e.Message}"
                    );
                }

                // Decode the source image.
                decoded = new Texture2D(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false
                );

                byte[] imageBytes = File.ReadAllBytes(sourcePath);

                if (!decoded.LoadImage(imageBytes))
                    throw new Exception("Unity could not decode this image.");

                // Apply the EXIF transformation to the actual pixels.
                upright = ApplyExifOrientation(decoded, orientation);

                // Save a normalized PNG. Future loads no longer need EXIF handling.
                string normalizedPath = Path.Combine(
                    Application.persistentDataPath,
                    Guid.NewGuid().ToString("N") + ".png"
                );

                byte[] normalizedBytes = upright.EncodeToPNG();
                File.WriteAllBytes(normalizedPath, normalizedBytes);

                // Insert exactly one Photo record for this imported image.
                Photo importedPhoto = new Photo
                {
                    Path = normalizedPath,
                    AspectRatio = (float)upright.width / upright.height,
                    DateTaken = dateTaken
                };

                SavePhoto(importedPhoto);
                importSucceeded = true;

                Debug.Log(
                    $"[PHOTO IMPORT] Success. ID={importedPhoto.Id}, " +
                    $"EXIF orientation={orientation}, " +
                    $"size={upright.width}x{upright.height}, " +
                    $"path={normalizedPath}"
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[PHOTO IMPORT] Failed for '{sourcePath}': {e}"
                );
            }
            finally
            {
                if (upright != null && upright != decoded)
                    Destroy(upright);

                if (decoded != null)
                    Destroy(decoded);
            }

#if !UNITY_EDITOR
            // The copied original is only an intermediate file. The normalized
            // PNG is the permanent copy referenced by the database.
            if (importSucceeded &&
                copiedSourceToPersistentData &&
                File.Exists(sourcePath))
            {
                try
                {
                    File.Delete(sourcePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"[PHOTO IMPORT] Could not delete temporary copy: {e.Message}"
                    );
                }
            }
#endif
        }

        // Refresh pages that are currently instantiated in the scene.
        PageScript[] activePages =
            FindObjectsByType<PageScript>(FindObjectsSortMode.None);

        foreach (PageScript pageScript in activePages)
        {
            if (pageScript != null && pageScript.page != null)
            {
                pageScript.SetupPage(
                    pageScript.album,
                    pageScript.page.PageNumber
                );
            }
        }
    }

    public Texture2D LoadPictureFromPath(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Debug.LogError(
                    $"[PHOTO LOAD] File does not exist: {path}"
                );
                return null;
            }

            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false
            );

            if (!texture.LoadImage(fileData))
            {
                Debug.LogError(
                    $"[PHOTO LOAD] Unity could not decode: {path}"
                );
                Destroy(texture);
                return null;
            }

            return texture;
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[PHOTO LOAD] Failed to load '{path}': {e}"
            );
            return null;
        }
    }

    private Texture2D ApplyExifOrientation(Texture2D source, int orientation)
    {
        if (source == null)
            return null;

        if (orientation < 1 || orientation > 8)
            orientation = 1;

        if (orientation == 1)
            return source;

        int sourceWidth = source.width;
        int sourceHeight = source.height;

        bool swapDimensions = orientation >= 5;
        int destinationWidth = swapDimensions ? sourceHeight : sourceWidth;
        int destinationHeight = swapDimensions ? sourceWidth : sourceHeight;

        Color32[] sourcePixels = source.GetPixels32();
        Color32[] destinationPixels =
            new Color32[destinationWidth * destinationHeight];

        // EXIF coordinates are described from the top-left. Unity's pixel
        // arrays are indexed from the bottom-left, so convert both indices.
        for (int y = 0; y < sourceHeight; y++)
        {
            for (int x = 0; x < sourceWidth; x++)
            {
                int destinationX = x;
                int destinationY = y;

                switch (orientation)
                {
                    case 2: // Mirror horizontally
                        destinationX = sourceWidth - 1 - x;
                        break;

                    case 3: // Rotate 180 degrees
                        destinationX = sourceWidth - 1 - x;
                        destinationY = sourceHeight - 1 - y;
                        break;

                    case 4: // Mirror vertically
                        destinationY = sourceHeight - 1 - y;
                        break;

                    case 5: // Transpose
                        destinationX = y;
                        destinationY = x;
                        break;

                    case 6: // Rotate 90 degrees clockwise
                        destinationX = sourceHeight - 1 - y;
                        destinationY = x;
                        break;

                    case 7: // Transverse
                        destinationX = sourceHeight - 1 - y;
                        destinationY = sourceWidth - 1 - x;
                        break;

                    case 8: // Rotate 90 degrees counterclockwise
                        destinationX = y;
                        destinationY = sourceWidth - 1 - x;
                        break;
                }

                int sourceIndex =
                    x + (sourceHeight - 1 - y) * sourceWidth;

                int destinationIndex =
                    destinationX +
                    (destinationHeight - 1 - destinationY) * destinationWidth;

                destinationPixels[destinationIndex] = sourcePixels[sourceIndex];
            }
        }

        Texture2D result = new Texture2D(
            destinationWidth,
            destinationHeight,
            TextureFormat.RGBA32,
            false
        );

        result.SetPixels32(destinationPixels);
        result.Apply(false, false);

        return result;
    }
}
