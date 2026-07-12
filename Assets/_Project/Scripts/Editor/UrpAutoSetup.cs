using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nordo.EditorTools
{
    /// <summary>
    /// One-time project bootstrap: if no render pipeline is assigned, creates a URP renderer +
    /// pipeline asset under <c>Assets/_Project/Settings/URP/</c> and assigns it as the project's
    /// default. This is what activates the locked <c>Nordo/PSX</c> art-direction shader — without a
    /// URP asset the game silently falls back to Built-in rendering and the PSX look never appears.
    /// <para>
    /// Idempotent and safe: it only acts when <see cref="GraphicsSettings.defaultRenderPipeline"/>
    /// is null, reuses the assets if they already exist, and never runs in Play mode. Delete the
    /// created assets and clear Project Settings → Graphics to intentionally revert to Built-in.
    /// </para>
    /// </summary>
    [InitializeOnLoad]
    public static class UrpAutoSetup
    {
        private const string SettingsFolder = "Assets/_Project/Settings";
        private const string UrpFolder = SettingsFolder + "/URP";
        private const string RendererPath = UrpFolder + "/Nordo_URP_Renderer.asset";
        private const string PipelinePath = UrpFolder + "/Nordo_URP_Pipeline.asset";

        static UrpAutoSetup()
        {
            // Defer until the editor is fully initialized (asset database ready).
            EditorApplication.delayCall += EnsurePipelineAssigned;
        }

        private static void EnsurePipelineAssigned()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            // Already on a pipeline (URP or otherwise) — nothing to do.
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                return;
            }

            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                if (!AssetDatabase.IsValidFolder(SettingsFolder))
                {
                    AssetDatabase.CreateFolder("Assets/_Project", "Settings");
                }

                if (!AssetDatabase.IsValidFolder(UrpFolder))
                {
                    AssetDatabase.CreateFolder(SettingsFolder, "URP");
                }

                // The pipeline asset needs a saved renderer-data asset to reference.
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);

                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssetDatabase.SaveAssets();

            Debug.Log("[Nordo] URP pipeline created & assigned (one-time setup) — the Nordo/PSX art-direction shader is now active. " +
                      "Commit ProjectSettings/GraphicsSettings.asset and Assets/_Project/Settings/URP/ to keep it.");
        }
    }
}
