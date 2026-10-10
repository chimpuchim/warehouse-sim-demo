using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Warehouse.EditorTools
{
    /// <summary>
    /// One-shot project setup and command-line builds.
    ///   unity build --target WebGL --execute-method Warehouse.EditorTools.WarehouseBuild.BuildWebGL -o Build/WebGL
    ///   unity build --target StandaloneWindows64 --execute-method Warehouse.EditorTools.WarehouseBuild.BuildWindows -o Build/Windows/WarehouseSimDemo.exe
    /// </summary>
    public static class WarehouseBuild
    {
        const string ScenePath = "Assets/Scenes/Warehouse.unity";

        [MenuItem("Warehouse/Set Up Project")]
        public static void Setup()
        {
            CreateScene();
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Warehouse setup done.");
        }

        static void CreateScene()
        {
            if (File.Exists(ScenePath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Bootstrap", typeof(Bootstrap));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Vo Tran Quoc Cuong";
            PlayerSettings.productName = "Warehouse Sim Demo";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            PlayerSettings.WebGL.template = "PROJECT:Warehouse";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = Environment.GetEnvironmentVariable("WAREHOUSE_DEBUG") == "1"
                ? WebGLExceptionSupport.FullWithStacktrace
                : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
        }

        public static void BuildWebGL() => Build(BuildTarget.WebGL, "Build/WebGL");
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Build/Windows/WarehouseSimDemo.exe");

        static void Build(BuildTarget target, string defaultOutput)
        {
            Setup();
            var output = ArgAfter("-buildOutput") ?? defaultOutput;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = target,
                locationPathName = output,
                options = BuildOptions.None,
            });
            var summary = report.summary;
            Debug.Log($"Build {summary.result}: {summary.totalSize / (1024 * 1024)} MB -> {summary.outputPath} ({summary.totalErrors} errors)");
            if (summary.result != BuildResult.Succeeded)
                throw new Exception("Build failed with " + summary.totalErrors + " errors");
        }

        static string ArgAfter(string flag)
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, flag);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
