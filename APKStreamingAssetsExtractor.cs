using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

//  Add these below Manifest tag inside AndroidManifest
//		<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE"/> (optional)
//      <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE"/>

public class APKStreamingAssetsExtractor : MonoBehaviour 
{
#if UNITY_EDITOR && UNITY_ANDROID
    static FileExtracter()
    {
        EditorApplication.hierarchyChanged += CheckSplitAPK;
    }
    static void CheckSplitAPK()
    {
        if (PlayerSettings.Android.splitApplicationBinary)
            Debug.LogError("APKStreamingAssetsExtractor.cs detected split APK option has been enabled. Make sure the path points to the correct file: https://docs.unity3d.com/2023.1/Documentation/ScriptReference/Application-dataPath.html");
    }
#endif

#if UNITY_ANDROID
    static string targetPath = "";
    static int versionCode = 1; // You can force a redownload of data on app update by changing the version
    static string versionFileFolder = "assets"; // folder to place the version file
    static bool useCoroutine = false; // Do extraction in coroutine
    static bool deleteSourceFile = false; // Delete files from APK after extraction. NOT TESTED!
    static string[] foldersToCopy = { "Folder", "Stuff", "Things/FolderName" }; //Which folders to copy inside the StreamingAssets

    public static Action<float> OnProgress;

    //download files before scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void FileSetup()
    {
        //check if files already exist
        if (!useCoroutine && CheckIfExtractionIsRequired())
        {
            //extract all files, otherwise wait for awake to extract them asynchronously
            ExtractFileSync();
        }
    }

    private void Awake()
    {
        if (useCoroutine && CheckIfExtractionIsRequired())
        {
            StartCoroutine(ExtractFile());
        }
    }

    static bool CheckIfExtractionIsRequired()
    {
        //Check for previously created version file
        string versionFilePath = Path.Combine(Application.persistentDataPath, versionFileFolder, "V" + versionCode + ".txt");
        if (File.Exists(versionFilePath))
        {
            Debug.Log("Correct version is already extracted.");
            return false;
        }

        foreach(string s in foldersToCopy)
        {
            DeleteDirectoryIfExists(s);
        }

        return true;
    }

    static void DeleteDirectoryIfExists(string directoryName)
    {
        string dirPath = Path.Combine(Application.persistentDataPath, directoryName);
        if (Directory.Exists(dirPath))
        {
            Directory.Delete(dirPath, true);
        }
    }

    static IEnumerator ExtractFile()
    {
        DoExtract(Application.dataPath, Path.Combine(Application.persistentDataPath, targetPath));
        yield return null; 
    }

    static void ExtractFileSync()
    {
        DoExtract(Application.dataPath, Path.Combine(Application.persistentDataPath, targetPath));
    }

    static void DoExtract(string in_Path, string targetPath)
    {
        // create missing directories
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }

        if (File.Exists(in_Path))
        {
            using (ZipArchive archive = ZipFile.OpenRead(in_Path))
            {
                long totalExtracted = 0;
                long totalSize = 0;
                //extraction
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (IsTargetDirectory(entry.FullName))
                    {
                        string destinationPath = Path.Combine(targetPath, entry.FullName);
                        //totalSize += entry.Length; //uncomment for progress updates
                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            string directoryPath = Path.GetDirectoryName(destinationPath);
                            if (!Directory.Exists(directoryPath))
                            {
                                Directory.CreateDirectory(directoryPath);
                            }

                            entry.ExtractToFile(destinationPath, true);

                            totalExtracted += entry.Length;
                            //float progress = (float)totalExtracted / totalSize;
                            //Debug.Log(progress);
                            //OnProgress?.Invoke(progress);
                        }
                    }
                }
            }

            if (deleteSourceFile)
            {
                File.Delete(in_Path);
            }

            // Create version file after successful extraction
            File.WriteAllText(Path.Combine(Application.persistentDataPath, versionFileFolder, "V" + versionCode + ".txt"), "Extracted");
        }
    }

    static bool IsTargetDirectory(string entryPath)
    {
        foreach (string s in foldersToCopy)
        {
            if (entryPath.StartsWith(s))
            {
                return true;
            }
        }
        return false;
    }
#endif
    //#if UNITY_EDITOR
    //private class BuildPreprocessor : IPreprocessBuildWithReport
    //    {
    //        public int callbackOrder { get { return 0; } }

    //        public void OnPreprocessBuild(BuildReport report)
    //        {
    //        bundleVersion = PlayerSettings.Android.bundleVersionCode -1;
    //        Debug.Log(bundleVersion);
    //        }
    //    }
    //#endif
}