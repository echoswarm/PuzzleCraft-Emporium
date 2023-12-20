using UnityEngine;
using System.IO;

public class ProjectSetupManager : MonoBehaviour
{
    private string[] folders = {
        "01_Art",
        "02_Prefabs",
        "03_Animation",
        "04_Sound",
        "05_Text",
        "06_Scenes",
        "07_Scripts",
        "08_ScriptableObjects",
        "09_Materials",
        "10_StoreAssets"
    };

    void Start()
    {
        foreach (string folder in folders)
        {
            CreateFolderIfNotExist(folder);
        }
    }

    private void CreateFolderIfNotExist(string folderName)
    {
        string folderPath = Path.Combine(Application.dataPath, folderName);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log("Created folder: " + folderName);
        }
        else
        {
            Debug.Log("Folder already exists: " + folderName);
        }
    }
}
