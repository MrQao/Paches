#if UNITY_ANDROID
using System.IO;
using UnityEditor;
using UnityEditor.Android;

public class PostBuildInjectOverlayKeyboard : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1001;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");

        if (!File.Exists(manifestPath))
        {
            UnityEngine.Debug.LogError("AndroidManifest.xml not found: " + manifestPath);
            return;
        }

        string manifest = File.ReadAllText(manifestPath);

        string featureLine = "<uses-feature android:name=\"oculus.software.overlay_keyboard\" />";
        if (!manifest.Contains(featureLine))
        {
            int insertIndex = manifest.IndexOf("<application");
            if (insertIndex != -1)
            {
                manifest = manifest.Insert(insertIndex, featureLine + "\n    ");
                File.WriteAllText(manifestPath, manifest);
                UnityEngine.Debug.Log("[PostBuild] Injected Oculus overlay keyboard feature");
            }
        }
    }
}
#endif
