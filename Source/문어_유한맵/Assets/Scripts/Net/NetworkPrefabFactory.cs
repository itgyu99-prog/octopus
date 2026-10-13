using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace OctopusSurvivors
{
    public static class NetworkPrefabFactory
    {
        public static GameObject PlayerPrefab { get; private set; }
        public static GameObject SessionPrefab { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            PlayerPrefab = null;
            SessionPrefab = null;
        }

        public static void Ensure()
        {
            if (PlayerPrefab == null)
                PlayerPrefab = BuildPlayer();
            if (SessionPrefab == null)
                SessionPrefab = BuildSession();
            RefreshHashes();
        }

        public static void RefreshHashes()
        {
            if (PlayerPrefab != null)
                AssignHash(PlayerPrefab.GetComponent<NetworkObject>(), NetHashes.Player);
            if (SessionPrefab != null)
                AssignHash(SessionPrefab.GetComponent<NetworkObject>(), NetHashes.Session);
        }

        static GameObject BuildPlayer()
        {
            var root = new GameObject("OctopusPlayerPrefab");
            Object.DontDestroyOnLoad(root);
            root.SetActive(false);

            var col = root.AddComponent<CircleCollider2D>();
            col.radius = GameConfig.PlayerRadius;

            var no = root.AddComponent<NetworkObject>();
            AssignHash(no, NetHashes.Player);
            root.AddComponent<OctopusPlayer>();
            return root;
        }

        static GameObject BuildSession()
        {
            var root = new GameObject("GameSessionPrefab");
            Object.DontDestroyOnLoad(root);
            root.SetActive(false);
            var no = root.AddComponent<NetworkObject>();
            AssignHash(no, NetHashes.Session);
            root.AddComponent<GameSession>();
            return root;
        }

        public static void AssignHash(NetworkObject networkObject, uint hash)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = typeof(NetworkObject);
            SetField(type, networkObject, flags, hash, "GlobalObjectIdHash", "m_GlobalObjectIdHash");
            SetField(type, networkObject, flags, hash, "PrefabGlobalObjectIdHash", "m_PrefabGlobalObjectIdHash");

            var sceneField = type.GetField("m_IsSceneObject", flags);
            if (sceneField != null)
                sceneField.SetValue(networkObject, (bool?)false);
        }

        static void SetField(System.Type type, object target, BindingFlags flags, uint hash, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var field = type.GetField(names[i], flags);
                if (field == null)
                    continue;
                field.SetValue(target, hash);
                return;
            }
        }

        public static void RegisterHandlers(NetworkManager manager)
        {
            if (manager == null || manager.PrefabHandler == null)
                return;
            manager.PrefabHandler.RemoveHandler(PlayerPrefab);
            manager.PrefabHandler.RemoveHandler(SessionPrefab);
            manager.PrefabHandler.AddHandler(PlayerPrefab, new ActivateHandler(PlayerPrefab, NetHashes.Player));
            manager.PrefabHandler.AddHandler(SessionPrefab, new ActivateHandler(SessionPrefab, NetHashes.Session));
        }

        sealed class ActivateHandler : INetworkPrefabInstanceHandler
        {
            readonly GameObject _prefab;
            readonly uint _hash;

            public ActivateHandler(GameObject prefab, uint hash)
            {
                _prefab = prefab;
                _hash = hash;
            }

            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                var go = Object.Instantiate(_prefab, position, rotation);
                go.hideFlags = HideFlags.None;
                go.SetActive(true);
                var no = go.GetComponent<NetworkObject>();
                AssignHash(no, _hash);
                return no;
            }

            public void Destroy(NetworkObject networkObject)
            {
                if (networkObject != null)
                    Object.Destroy(networkObject.gameObject);
            }
        }
    }
}
