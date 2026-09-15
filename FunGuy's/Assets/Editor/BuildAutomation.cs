using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildAutomation
{
    public static void AndroidDevelopment() => BuildAndroid(true);
    public static void AndroidReleaseCheck() => BuildAndroid(false);

    private static void BuildAndroid(bool development)
    {
        var data = new GameData();
        data.LoadAll(); // Invalid content fails the build before packaging.
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length != 7 || scenes[0] != "Assets/Scenes/Boot.unity" || scenes.Any(p => !File.Exists(p)))
            throw new InvalidOperationException("Expected seven existing gameplay scenes with Boot first.");
        var output = Environment.GetEnvironmentVariable("FUNGUY_BUILD_OUTPUT");
        if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("Set FUNGUY_BUILD_OUTPUT to the output APK path.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        EditorUserBuildSettings.buildAppBundle = false;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes, target = BuildTarget.Android, locationPathName = output,
            options = development ? BuildOptions.Development : BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Android build failed: {report.summary.result}, {report.summary.totalErrors} errors.");
        File.WriteAllText(output + ".summary.txt", $"Unity {UnityEngine.Application.unityVersion}\nDevelopment: {development}\nAPKBytes: {new FileInfo(output).Length}\nBuildReportBytes: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\n");
    }
}
