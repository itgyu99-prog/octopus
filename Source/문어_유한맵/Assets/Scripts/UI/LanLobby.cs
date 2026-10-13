using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

namespace OctopusSurvivors
{
    public sealed class LanLobby : MonoBehaviour
    {
        public static LanLobby Instance { get; private set; }
        public static bool IsVisible => Instance != null && Instance._root.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        GameObject _root;
        Text _status;
        InputField _ip;
        InputField _joinCode;

        public static void Show(string status = null)
        {
            Ensure().SetVisible(true, status);
        }

        public static void Hide()
        {
            if (Instance != null)
                Instance.SetVisible(false, null);
        }

        static LanLobby Ensure()
        {
            if (Instance != null)
                return Instance;
            var canvas = UiUtil.CreateOverlay("LobbyCanvas");
            var lobby = canvas.gameObject.AddComponent<LanLobby>();
            lobby.Build(canvas.transform);
            Instance = lobby;
            return lobby;
        }

        void Build(Transform canvas)
        {
            _root = UiUtil.Panel(canvas, Vector2.zero, new Vector2(900, 760), new Color(0.07f, 0.09f, 0.12f, 0.94f)).gameObject;
            var rt = _root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

            var title = UiUtil.Label(_root.transform, new Vector2(40, -22), new Vector2(820, 56), 40, Color.white, TextAnchor.MiddleLeft);
            title.text = "문어 뱀서라이크";

            var sub = UiUtil.Label(_root.transform, new Vector2(40, -78), new Vector2(820, 70), 20, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft);
            sub.text = "혼자 테스트: 아래 [혼자 테스트 / LAN 방 만들기] → 시작 속성 선택 → 준비 → 혼자 시작.\n2인 인터넷: 방 만들기 → 상대에게 참가 코드 전달 → 코드로 참가.";

            var netHost = UiUtil.Button(_root.transform, new Vector2(0, 150), new Vector2(560, 64), "[인터넷] 방 만들기");
            netHost.onClick.AddListener(GameBootstrap.StartRelayHost);

            _joinCode = UiUtil.Field(_root.transform, new Vector2(0, 78), new Vector2(560, 52), "참가 코드 6글자");
            _joinCode.characterLimit = 8;
            _joinCode.contentType = InputField.ContentType.Alphanumeric;
            _joinCode.characterValidation = InputField.CharacterValidation.None;

            var netJoin = UiUtil.Button(_root.transform, new Vector2(0, 12), new Vector2(560, 64), "[인터넷] 참가");
            netJoin.onClick.AddListener(() => GameBootstrap.StartRelayClient(_joinCode.text));

            var lanHint = UiUtil.Label(_root.transform, new Vector2(40, -430), new Vector2(820, 36), 20, new Color(0.65f, 0.72f, 0.8f), TextAnchor.MiddleLeft);
            lanHint.text = "LAN 테스트 (같은 네트워크 / 127.0.0.1)";

            var host = UiUtil.Button(_root.transform, new Vector2(0, -130), new Vector2(560, 56), "혼자 테스트 / LAN 방 만들기");
            host.onClick.AddListener(GameBootstrap.StartHost);

            _ip = UiUtil.Field(_root.transform, new Vector2(0, -196), new Vector2(560, 48), "상대 Host IP (예: 192.168.0.10)");
            _ip.text = "127.0.0.1";

            var join = UiUtil.Button(_root.transform, new Vector2(0, -258), new Vector2(560, 56), "Join (LAN 테스트)");
            join.onClick.AddListener(() => GameBootstrap.StartClient(_ip.text));

            _status = UiUtil.Label(_root.transform, new Vector2(40, -560), new Vector2(820, 180), 20, new Color(0.95f, 0.85f, 0.4f), TextAnchor.UpperLeft);
            _status.text = "방향키 이동  /  Left Ctrl 근접  /  E 속성  /  F 요소  /  Esc 로비";
        }

        void SetVisible(bool visible, string status)
        {
            _root.SetActive(visible);
            if (status != null && _status != null)
                _status.text = status;
        }

        public void SetStatus(string status)
        {
            if (_status != null)
                _status.text = status;
        }

        public static string LocalIPv4()
        {
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up)
                        continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;
                    foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr.Address))
                            return addr.Address.ToString();
                    }
                }
            }
            catch
            {
                // ignored
            }

            return "127.0.0.1";
        }
    }
}
