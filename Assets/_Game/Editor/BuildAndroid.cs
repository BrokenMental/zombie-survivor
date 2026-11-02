using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ZombieSurvivor.Editor
{
    public static class BuildAndroid
    {
        public static void DevelopmentAndVerify()
        {
            Development();
            VerifyMovement.Run();
        }

        public static void Development()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.zombiesurvivor.prototype");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            System.IO.Directory.CreateDirectory("Builds/Android");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Game/Scenes/Arena.unity" },
                target = BuildTarget.Android,
                locationPathName = "Builds/Android/ZombieSurvivor-development.apk",
                options = BuildOptions.Development
            });
            Debug.Log($"ANDROID BUILD {result.summary.result} bytes={result.summary.totalSize}");
            if (result.summary.result != BuildResult.Succeeded) throw new System.Exception("Android build failed");
        }
    }
}
