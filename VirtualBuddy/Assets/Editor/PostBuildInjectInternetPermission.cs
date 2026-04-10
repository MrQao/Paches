#if UNITY_ANDROID
using System.IO;
using UnityEditor;
using UnityEditor.Android;

public class PostBuildInjectInternetPermission : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        // Path to final AndroidManifest.xml in the Gradle project
        var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");

        if (!File.Exists(manifestPath))
        {
            UnityEngine.Debug.LogError("AndroidManifest.xml not found: " + manifestPath);
            return;
        }

        string manifest = File.ReadAllText(manifestPath);

        string permissionLine = "<uses-permission android:name=\"android.permission.INTERNET\" />";
        if (!manifest.Contains(permissionLine))
        {
            int insertIndex = manifest.IndexOf("<application");
            if (insertIndex != -1)
            {
                manifest = manifest.Insert(insertIndex, permissionLine + "\n    ");
                File.WriteAllText(manifestPath, manifest);
                UnityEngine.Debug.Log("[PostBuild] Injected INTERNET permission into AndroidManifest.xml");
            }
            else
            {
                UnityEngine.Debug.LogError("Failed to inject INTERNET permission: <application> tag not found.");
            }
        }
    }
}
#endif