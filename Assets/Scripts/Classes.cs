using UnityEngine;
using SQLite;
using System;

[UnityEngine.Scripting.Preserve]
public class Photo
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Path { get; set; }
    public float AspectRatio { get; set; }
    public DateTime DateTaken { get; set; }
}

[UnityEngine.Scripting.Preserve]
public class Frame
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int PhotoId { get; set; } // Points to a Photo.Id
    public int physicalFrameID { get; set; }
}

[UnityEngine.Scripting.Preserve]
public class Album
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int phisicalBookID { get; set; }
    public string Title { get; set; }
    
    public string HexColor { get; set; }

    [Ignore]
    public Color CoverColor
    {
        get
        {
            if (ColorUtility.TryParseHtmlString(HexColor, out Color color))
                return color;
            return Color.white;
        }
        set
        {
            HexColor = "#" + ColorUtility.ToHtmlStringRGBA(value);
        }
    }
}

[UnityEngine.Scripting.Preserve]
public class AlbumPage
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int AlbumId { get; set; } // Points to an Album.Id
    public int PageNumber { get; set; }
}

[UnityEngine.Scripting.Preserve]
public class PageSlot
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int AlbumPageId { get; set; } // Points to AlbumPage.Id
    public int PhotoId { get; set; } // Points to Photo.Id
    
    // Determines exactly where the photo sits in the chosen LayoutType
    public float xPos { get; set; } 
    public float yPos { get; set; } 
    public float rotation { get; set; } 
    public float scale { get; set; } 
    public int layerOrder { get; set; } 
}