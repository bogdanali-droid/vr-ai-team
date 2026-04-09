#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using System.IO;

namespace VROffice.Editor
{
    /// <summary>
    /// Menu: VROffice > Setup Quest 3 Project
    ///
    /// Configureaza automat proiectul Unity pentru Meta Quest 3:
    ///   - Switch platform la Android
    ///   - Player Settings (package name, orientare, API level)
    ///   - XR Management (Oculus provider)
    ///   - Quality Settings (optimizat VR)
    ///   - AndroidManifest.xml cu permisiuni microfon + VR
    /// </summary>
    public static class QuestProjectSetup
    {
        private const string PackageName = "com.bogdan.vroffice";
        private const string AppName     = "VR Office";

        [MenuItem("VROffice/Setup Quest 3 Project")]
        public static void Setup()
        {
            EditorUtility.DisplayProgressBar("VROffice Setup", "Configurare proiect Quest 3...", 0f);

            try
            {
                SetupAndroidPlatform();
                EditorUtility.DisplayProgressBar("VROffice Setup", "Player Settings...", 0.3f);

                SetupPlayerSettings();
                EditorUtility.DisplayProgressBar("VROffice Setup", "Quality Settings...", 0.5f);

                SetupQualitySettings();
                EditorUtility.DisplayProgressBar("VROffice Setup", "AndroidManifest...", 0.7f);

                CreateAndroidManifest();
                EditorUtility.DisplayProgressBar("VROffice Setup", "Structura foldere...", 0.9f);

                CreateFolderStructure();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("VROffice — Quest 3 Setup",
                "Proiect configurat!\n\n" +
                "Urmatorii pasi:\n" +
                "1. Window → Package Manager → Add Meta XR SDK\n" +
                "2. Project Settings → XR Plug-in Management → bifezi Oculus\n" +
                "3. VROffice → Build Phase 1 Scene\n\n" +
                "Pachet Meta XR (scrie in Package Manager):\n" +
                "com.meta.xr.sdk.all",
                "OK");

            Debug.Log("[VROffice] Quest 3 setup complet!");
        }

        // ------------------------------------------------------------------ //

        private static void SetupAndroidPlatform()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[Setup] Switch la Android platform...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android, BuildTarget.Android);
            }
        }

        private static void SetupPlayerSettings()
        {
            // Identificare app
            PlayerSettings.applicationIdentifier = PackageName;
            PlayerSettings.productName            = AppName;
            PlayerSettings.companyName            = "VR AI Team";

            // Android — Quest 3 necesita API 32 minim
            PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;

            // Architectura ARM64 (Quest 3 e ARM64)
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // IL2CPP (performanta mai buna decat Mono pe Quest)
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,
                                               ScriptingImplementation.IL2CPP);

            // Orientare landscape (VR)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            // Stereo rendering (Single Pass Instanced = cel mai performant)
            PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;

            // Grafic: Vulkan + OpenGLES3 (Quest 3 suporta ambele)
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
            {
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3,
            });

            // Dezactiveaza auto-rotation (VR nu are nevoie)
            PlayerSettings.allowedAutorotateToPortrait           = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft      = true;
            PlayerSettings.allowedAutorotateToLandscapeRight     = false;

            Debug.Log("[Setup] Player Settings configurate.");
        }

        private static void SetupQualitySettings()
        {
            // Dezactiveaza vsync (VR isi gestioneaza singur frame rate-ul)
            QualitySettings.vSyncCount = 0;

            // Target frame rate 90Hz (Quest 3 maxim)
            Application.targetFrameRate = 90;

            // Antialiasing 4x (balanta calitate/performanta pe Quest 3)
            QualitySettings.antiAliasing = 4;

            // Shadow distance redus pentru performanta
            QualitySettings.shadowDistance = 15f;

            Debug.Log("[Setup] Quality Settings configurate.");
        }

        private static void CreateAndroidManifest()
        {
            string pluginsDir  = "Assets/Plugins/Android";
            string manifestPath = pluginsDir + "/AndroidManifest.xml";

            Directory.CreateDirectory(pluginsDir);

            string manifest = @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"">

    <!-- VR Mode -->
    <uses-feature android:name=""android.hardware.vr.headtracking"" android:required=""true"" android:version=""1"" />

    <!-- Microfon pentru voice pipeline -->
    <uses-permission android:name=""android.permission.RECORD_AUDIO"" />

    <!-- Internet pentru backend WebSocket + API-uri -->
    <uses-permission android:name=""android.permission.INTERNET"" />
    <uses-permission android:name=""android.permission.ACCESS_NETWORK_STATE"" />
    <uses-permission android:name=""android.permission.ACCESS_WIFI_STATE"" />

    <!-- Vibratii controllere -->
    <uses-permission android:name=""android.permission.VIBRATE"" />

    <application>
        <activity
            android:name=""com.unity3d.player.UnityPlayerGameActivity""
            android:theme=""@android:style/Theme.Black.NoTitleBar.Fullscreen""
            android:launchMode=""singleTask""
            android:screenOrientation=""landscape""
            android:configChanges=""density|keyboard|keyboardHidden|navigation|orientation|screenLayout|screenSize|uiMode"">

            <intent-filter>
                <action android:name=""android.intent.action.MAIN"" />
                <category android:name=""android.intent.category.LAUNCHER"" />
                <category android:name=""com.oculus.intent.category.VR"" />
            </intent-filter>

            <!-- Quest 3 VR -->
            <meta-data android:name=""com.oculus.supportedDevices"" android:value=""quest3"" />
            <meta-data android:name=""com.oculus.handtracking.version"" android:value=""V2"" />

        </activity>
    </application>

</manifest>";

            File.WriteAllText(manifestPath, manifest);
            Debug.Log("[Setup] AndroidManifest.xml creat la: " + manifestPath);
        }

        private static void CreateFolderStructure()
        {
            string[] folders =
            {
                "Assets/VROffice",
                "Assets/VROffice/Scenes",
                "Assets/VROffice/Scripts",
                "Assets/VROffice/Editor",
                "Assets/VROffice/Avatars",
                "Assets/VROffice/Avatars/Ana",
                "Assets/VROffice/Avatars/Alina",
                "Assets/VROffice/Avatars/Cosmin",
                "Assets/VROffice/Avatars/Ion",
                "Assets/VROffice/Avatars/Gogu",
                "Assets/VROffice/Avatars/Victor",
                "Assets/VROffice/Animations",
                "Assets/VROffice/Animators",
                "Assets/VROffice/Materials",
                "Assets/VROffice/Audio",
                "Assets/Plugins/Android",
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parts  = folder.Split('/');
                    var parent = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var full = parent + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(full))
                            AssetDatabase.CreateFolder(parent, parts[i]);
                        parent = full;
                    }
                }
            }

            Debug.Log("[Setup] Structura foldere creata.");
        }
    }
}
#endif
