using UnityEngine;

public class FrameScript : MonoBehaviour
{
    public int physicalFrameId;
    public MeshRenderer myRenderer;

    public Frame frame;
    public Photo photo;
    
    private Texture2D loadedPicture;
    private string loadedPicturePath;
    private Material frameMaterial;

    void Start() 
    {
        Frame existingFrame = DataBaseManager.Instance.db.Table<Frame>().Where(p => p.physicalFrameID == physicalFrameId).FirstOrDefault();
            
        if (existingFrame == null)
        {
            frame = new Frame { physicalFrameID = physicalFrameId };
            DataBaseManager.Instance.db.Insert(frame);
        }
        else
        {
            frame = existingFrame;
        }

        SetupFrame(DataBaseManager.Instance.db.Find<Photo>(frame.PhotoId));
    }

    private void OnDestroy()
    {
        if (frameMaterial != null)
        {
            frameMaterial.SetTexture("_BaseMap", null);
            Destroy(frameMaterial);
            frameMaterial = null;
        }

        if (!string.IsNullOrEmpty(loadedPicturePath) &&
            DataBaseManager.Instance != null)
        {
            DataBaseManager.Instance.ReleaseTexture(loadedPicturePath);
        }

        loadedPicture = null;
        loadedPicturePath = null;
        photo = null;
    }

    public void SetupFrame(Photo image)
    {
        if (image == null) return;

        if (myRenderer == null)
            myRenderer = GetComponent<MeshRenderer>();

        if (myRenderer == null)
        {
            Debug.LogError("[FRAME] Missing MeshRenderer.");
            return;
        }

        Texture2D newPicture =
            DataBaseManager.Instance.LoadPictureFromPath(image.Path);

        if (newPicture == null)
            return;

        if (frameMaterial == null)
            frameMaterial = myRenderer.material;

        if (frameMaterial == null)
        {
            DataBaseManager.Instance.ReleaseTexture(image.Path);
            return;
        }

        // Assign the new texture first.
        frameMaterial.SetTexture("_BaseMap", newPicture);

        string previousPath = loadedPicturePath;

        loadedPicture = newPicture;
        loadedPicturePath = image.Path;
        photo = image;

        if (!string.IsNullOrEmpty(previousPath))
        {
            DataBaseManager.Instance.ReleaseTexture(previousPath);
        }

        loadedPicture = newPicture;
        photo = image;

        frame.PhotoId = photo.Id;
        DataBaseManager.Instance.db.Update(frame);
    }
}