using UnityEngine;

public class FrameScript : MonoBehaviour
{
    public int physicalFrameId;
    public MeshRenderer myRenderer;

    public Frame frame;
    public Photo photo;
    
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

    public void SetupFrame(Photo image)
    {
        if (image is null) return;

        photo = image;

        frame.PhotoId = photo.Id;
        DataBaseManager.Instance.db.Update(frame);

        Texture2D newPicture = DataBaseManager.Instance.LoadPictureFromPath(image.Path);
        
        if (newPicture != null)
        {
            myRenderer.material.SetTexture("_BaseMap", newPicture);

            // Fix 3D frame texture orientation natively via material scale/offset if needed
            if (newPicture.height > newPicture.width)
            {
                // Portrait adjustment for 3D shaders
                myRenderer.material.mainTextureScale = new Vector2(1, 1);
            }
        }
    }
}