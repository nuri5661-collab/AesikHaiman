using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AesikHaiman.EditorTools
{
    /// <summary>
    /// itch.io 업로드용 WebGL 빌드. itch.io는 Content-Encoding 헤더를 제대로 안 내려주는 경우가 있어
    /// Gzip + DecompressionFallback을 켜서 서버 설정과 무관하게 브라우저에서 풀리도록 한다.
    /// </summary>
    public static class WebGLBuildTool
    {
        private const string OutputPath = "Builds/WebGL";

        public static void BuildWebGL()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            Debug.Log($"[WebGLBuildTool] Building {scenes.Length} scenes to {OutputPath}: {string.Join(", ", scenes)}");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[WebGLBuildTool] Result: {report.summary.result}, size: {report.summary.totalSize} bytes, time: {report.summary.totalTime}, errors: {report.summary.totalErrors}");

            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
