using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Build2048
{
    [MenuItem("2048/Build/Windows x64")]
    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64, Path.Combine("Windows", "2048.exe"));
    }

    [MenuItem("2048/Build/Android APK")]
    public static void BuildAndroid()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        // Local test identifier. Replace it with your own reverse-domain name before release.
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,
            "com.example.unity2048");
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,
            ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        Build(BuildTarget.Android, Path.Combine("Android", "2048.apk"));
    }

    private static void Build(BuildTarget target, string relativeOutput)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0)
            throw new InvalidOperationException("请先在 Build Settings 中勾选游戏场景。");

        string outputRoot = Environment.GetEnvironmentVariable("BUILD_OUTPUT_DIR");
        if (string.IsNullOrWhiteSpace(outputRoot))
            outputRoot = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");

        string output = Path.Combine(outputRoot, relativeOutput);
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("构建失败：" + report.summary.result + "，请查看 Unity Editor.log。");

        Debug.Log("2048 构建完成：" + output + " (" + report.summary.totalSize + " bytes)");
    }
}
