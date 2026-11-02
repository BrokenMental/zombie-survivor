using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZombieSurvivor.Editor
{
    public static class CreatePrototype
    {
        [MenuItem("Zombie Survivor/Create Movement Prototype")]
        public static void Create()
        {
            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            System.IO.Directory.CreateDirectory("Assets/_Game/Data");
            const string pipelinePath = "Assets/_Game/Data/PrototypePipeline.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, "Assets/_Game/Data/PrototypeRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.companyName = "ZombieSurvivor";
            PlayerSettings.productName = "Zombie Survivor";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Arena";
            ground.transform.localScale = new Vector3(5, 1, 5);
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = Vector3.up;
            player.AddComponent<PlayerController>();
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().orthographic = true;
            camera.GetComponent<Camera>().orthographicSize = 12;
            camera.AddComponent<UniversalAdditionalCameraData>();
            camera.AddComponent<FollowCamera>().target = player.transform;
            var light = new GameObject("Sun", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Arena.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/_Game/Scenes/Arena.unity", true) };
            AssetDatabase.SaveAssets();
        }
    }
}
