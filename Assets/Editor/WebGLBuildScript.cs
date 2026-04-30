using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class WebGLBuildScript
{
    public static void Build()
    {
        // GitHub Pages は Brotli 非対応のため圧縮を無効化
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        string buildPath = Environment.GetEnvironmentVariable("BUILD_PATH")
                           ?? "build/WebGL/DQSim";

        var scenePaths = Array.ConvertAll(
            EditorBuildSettings.scenes,
            s => s.path
        );

        var options = new BuildPlayerOptions
        {
            scenes           = scenePaths,
            locationPathName = buildPath,
            target           = BuildTarget.WebGL,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
