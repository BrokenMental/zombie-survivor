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
            player.AddComponent<Health>();
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().orthographic = true;
            camera.GetComponent<Camera>().orthographicSize = 12;
            camera.AddComponent<UniversalAdditionalCameraData>();
            camera.AddComponent<FollowCamera>().target = player.transform;
            System.IO.Directory.CreateDirectory("Assets/_Game/Prefabs");
            const string settingsPath = "Assets/_Game/Data/NormalEnemy.asset";
            var enemySettings = AssetDatabase.LoadAssetAtPath<EnemySettings>(settingsPath);
            if (enemySettings == null)
            {
                enemySettings = ScriptableObject.CreateInstance<EnemySettings>();
                AssetDatabase.CreateAsset(enemySettings, settingsPath);
            }
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = "NormalZombie";
            var enemy = enemyObject.AddComponent<EnemyController>();
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Data/ZombieMaterial.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = new Color(.25f, .55f, .2f);
                AssetDatabase.CreateAsset(material, "Assets/_Game/Data/ZombieMaterial.mat");
            }
            enemyObject.GetComponent<Renderer>().sharedMaterial = material;
            var prefab = PrefabUtility.SaveAsPrefabAsset(enemyObject, "Assets/_Game/Prefabs/NormalZombie.prefab");
            Object.DestroyImmediate(enemyObject);
            var session = new GameObject("Encounter").AddComponent<EncounterSession>();
            session.Player = player.GetComponent<PlayerController>();
            session.ArenaCamera = camera.GetComponent<Camera>();
            session.EnemyPrefab = prefab.GetComponent<EnemyController>();
            session.Settings = enemySettings;
            var light = new GameObject("Sun", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Arena.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/_Game/Scenes/Arena.unity", true) };
            AssetDatabase.SaveAssets();
        }
    }
}
