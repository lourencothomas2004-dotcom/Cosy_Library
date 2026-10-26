using UnityEngine;
using SQLite;
using System.IO;
public class DataBaseManager : MonoBehaviour
{
    private string dbPath;
    public SQLiteConnection db;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dbPath = "URI=file:" + Path.Combine(Application.persistentDataPath, "GalleryDatabase.sqlite");
        db = new SQLiteConnection(dbPath);

        db.CreateTable<Photo>();
        db.CreateTable<Frame>();
        db.CreateTable<Album>();
        db.CreateTable<AlbumPage>();
        db.CreateTable<PageSlot>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
