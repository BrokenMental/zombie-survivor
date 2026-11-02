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
            var fast = AssetDatabase.LoadAssetAtPath<EnemySettings>("Assets/_Game/Data/FastEnemy.asset");
            if (fast == null)
            {
                fast = ScriptableObject.CreateInstance<EnemySettings>();
                fast.health = 20;
                fast.speed = 3.5f;
                fast.color = new Color(.85f, .35f, .15f);
                AssetDatabase.CreateAsset(fast, "Assets/_Game/Data/FastEnemy.asset");
            }
            var waves = AssetDatabase.LoadAssetAtPath<WaveSettings>("Assets/_Game/Data/PrototypeWaves.asset");
            if (waves == null)
            {
                waves = ScriptableObject.CreateInstance<WaveSettings>();
                AssetDatabase.CreateAsset(waves, "Assets/_Game/Data/PrototypeWaves.asset");
            }
            session.FastSettings = fast;
            session.Waves = waves;
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponSettings>("Assets/_Game/Data/Pistol.asset");
            if (pistol == null)
            {
                pistol = ScriptableObject.CreateInstance<WeaponSettings>();
                AssetDatabase.CreateAsset(pistol, "Assets/_Game/Data/Pistol.asset");
            }
            var shotgun = AssetDatabase.LoadAssetAtPath<WeaponSettings>("Assets/_Game/Data/Shotgun.asset");
            if (shotgun == null)
            {
                shotgun = ScriptableObject.CreateInstance<WeaponSettings>();
                shotgun.displayName = "Shotgun";
                shotgun.damage = 10;
                shotgun.interval = 1;
                shotgun.range = 8;
                shotgun.projectileSpeed = 20;
                shotgun.pellets = 5;
                shotgun.spread = 40;
                AssetDatabase.CreateAsset(shotgun, "Assets/_Game/Data/Shotgun.asset");
            }
            var bulletObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulletObject.name = "Bullet";
            bulletObject.transform.localScale = Vector3.one * .2f;
            Object.DestroyImmediate(bulletObject.GetComponent<Collider>());
            bulletObject.AddComponent<Projectile>();
            var bulletPrefab = PrefabUtility.SaveAsPrefabAsset(bulletObject, "Assets/_Game/Prefabs/Bullet.prefab");
            Object.DestroyImmediate(bulletObject);
            var gun = new GameObject("Aim");
            gun.transform.SetParent(player.transform, false);
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "Barrel";
            Object.DestroyImmediate(barrel.GetComponent<Collider>());
            barrel.transform.SetParent(gun.transform, false);
            barrel.transform.localPosition = new Vector3(0, .2f, .6f);
            barrel.transform.localScale = new Vector3(.18f, .18f, 1);
            var weapons = player.AddComponent<WeaponController>();
            weapons.Session = session;
            weapons.Pistol = pistol;
            weapons.Shotgun = shotgun;
            weapons.ProjectilePrefab = bulletPrefab.GetComponent<Projectile>();
            weapons.GunVisual = gun.transform;
            session.Weapons = weapons;
            var light = new GameObject("Sun", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Arena.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/_Game/Scenes/Arena.unity", true) };
            AssetDatabase.SaveAssets();
        }
    }
}
