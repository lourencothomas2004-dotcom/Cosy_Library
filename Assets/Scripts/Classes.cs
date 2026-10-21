using UnityEngine;
using SQLite;

// 1. The Master Gallery (Every single picture your app knows about)
public class Photo
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Path { get; set; }
}

// 2. The 3D Frames (Showcasing a highlight)
public class Frame
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int PhotoId { get; set; } // Points to a Photo.Id
    public float AspectRatio { get; set; }
}

// 3. The Albums (The physical books on the shelf)
public class Album
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
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

// 4. The Pages inside the Albums
public class AlbumPage
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int AlbumId { get; set; } // Points to an Album.Id
    public int PhotoId { get; set; } // Points to a Photo.Id
    public int PageNumber { get; set; }
}