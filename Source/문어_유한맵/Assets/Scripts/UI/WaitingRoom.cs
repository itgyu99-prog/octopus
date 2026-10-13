using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OctopusSurvivors
{
    /// <summary>
    /// 접속은 됐지만 아직 게임을 시작하지 않은 상태에서 보여 주는 대기실.
    /// 접속한 플레이어가 준비하면 호스트가 시작한다. 1인 테스트도 지원한다.
    /// </summary>
    public sealed class WaitingRoom : MonoBehaviour
    {
        public static WaitingRoom Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        public static bool IsVisible => Instance != null && Instance._root.activeSelf;

        GameObject _root;
        Text _title;
        Text _count;
        Text _hostRow;
        Text _guestRow;
        Text _hint;
        Button _readyButton;
        Text _readyLabel;
        Button _startButton;
        Text _startLabel;

        public static void Show()
        {
            Ensure()._root.SetActive(true);
        }

        public static void Hide()
        {
            if (Instance != null)
                Instance._root.SetActive(false);
        }

        static WaitingRoom Ensure()
        {
            if (Instance != null)
                return Instance;
            var canvas = UiUtil.CreateOverlay("WaitingRoomCanvas");
            canvas.sortingOrder = 120;
            var room = canvas.gameObject.AddComponent<WaitingRoom>();
            room.Build(canvas.transform);
            Instance = room;
            return room;
        }

        void Build(Transform canvas)
        {
            _root = UiUtil.Panel(canvas, Vector2.zero, new Vector2(860, 690), new Color(0.07f, 0.09f, 0.12f, 0.96f)).gameObject;
            var rt = _root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

            _title = UiUtil.Label(_root.transform, new Vector2(40, -28), new Vector2(780, 60), 40, Color.white);
            _title.text = "대기실";

            _count = UiUtil.Label(_root.transform, new Vector2(40, -92), new Vector2(780, 40), 26, new Color(0.95f, 0.85f, 0.4f));

            _hostRow = UiUtil.Label(_root.transform, new Vector2(40, -150), new Vector2(780, 44), 26, Color.white);
            _guestRow = UiUtil.Label(_root.transform, new Vector2(40, -196), new Vector2(780, 44), 26, Color.white);

            var choose = UiUtil.Label(_root.transform, new Vector2(40,-250), new Vector2(780,36),24,Color.white);
            choose.text = "시작 속성을 선택하세요";
            for (int i=0;i<GameplayRules.BasicElements.Length;i++)
            {
                ElementId element=GameplayRules.BasicElements[i];
                var button=UiUtil.Button(_root.transform,new Vector2((i-1.5f)*180f,30f),new Vector2(165,56),ElementRules.Name(element));
                button.onClick.AddListener(()=>GameSession.Instance?.SelectStartingElement(element));
                DisableKeyboardSubmit(button);
            }
            _readyButton = UiUtil.Button(_root.transform, new Vector2(0, -50), new Vector2(520, 66), "준비 (Space)");
            _readyButton.onClick.AddListener(ToggleReady);
            _readyLabel = _readyButton.GetComponentInChildren<Text>();
            DisableKeyboardSubmit(_readyButton);

            _startButton = UiUtil.Button(_root.transform, new Vector2(0, -130), new Vector2(520, 66), "게임 시작 (Enter)");
            _startButton.onClick.AddListener(RequestStart);
            _startLabel = _startButton.GetComponentInChildren<Text>();
            DisableKeyboardSubmit(_startButton);

            _hint = UiUtil.Label(_root.transform, new Vector2(40, -555), new Vector2(780, 110), 20, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft);

            _root.SetActive(false);
        }

        // Space/Enter를 직접 읽기 때문에, 버튼이 선택되어 같은 키로 두 번 눌리는 걸 막는다.
        static void DisableKeyboardSubmit(Button button)
        {
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        void ToggleReady()
        {
            var session = GameSession.Instance;
            if (session != null)
                session.ToggleLocalReady();
        }

        void RequestStart()
        {
            var session = GameSession.Instance;
            if (session != null)
                session.RequestStart();
        }

        void Update()
        {
            if (_root == null || !_root.activeSelf)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame)
                    ToggleReady();
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                    RequestStart();
                if (keyboard.escapeKey.wasPressedThisFrame)
                    GameBootstrap.LeaveToLobby();
            }

            var session = GameSession.Instance;
            if (session == null)
            {
                _count.text = "연결 중...";
                _readyButton.interactable = false;
                _startButton.gameObject.SetActive(false);
                return;
            }

            bool isHost = session.IsServer;
            int players = session.PlayerCount.Value;
            bool hostReady = session.HostReady.Value;
            bool guestReady = session.GuestReady.Value;
            bool guestHere = players >= 2;

            _count.text = $"접속 인원 {players} / {GameConfig.MaxPlayers}";
            _hostRow.text = $"호스트{(isHost ? " (나)" : "")}   {(hostReady ? "준비 완료" : "대기 중")}";
            _hostRow.color = hostReady ? new Color(0.45f, 0.9f, 0.55f) : Color.white;

            if (guestHere)
            {
                _guestRow.text = $"참가자{(isHost ? "" : " (나)")}   {(guestReady ? "준비 완료" : "대기 중")}";
                _guestRow.color = guestReady ? new Color(0.45f, 0.9f, 0.55f) : Color.white;
            }
            else
            {
                _guestRow.text = "참가자 없음 · 혼자 테스트 가능";
                _guestRow.color = new Color(0.6f, 0.65f, 0.7f);
            }

            bool myReady = isHost ? hostReady : guestReady;
            _readyButton.interactable = (isHost ? session.HostElement.Value : session.GuestElement.Value) != 0;
            _hostRow.text += "  / " + ElementRules.Name((ElementId)session.HostElement.Value);
            if(guestHere) _guestRow.text += "  / " + ElementRules.Name((ElementId)session.GuestElement.Value);
            _readyLabel.text = myReady ? "준비 취소 (Space)" : "준비 (Space)";

            _startButton.gameObject.SetActive(isHost);
            if (isHost)
            {
                bool canStart = session.CanStart();
                _startButton.interactable = canStart;
                _startLabel.text = canStart
                    ? (guestHere ? "게임 시작 (Enter)" : "혼자 시작 (Enter)")
                    : (guestHere ? "모두 속성 선택 후 준비하세요" : "속성 선택 후 준비하면 혼자 시작 가능");
            }

            _hint.text = isHost
                ? HostHint()
                : GuestHint();
        }

        static string HostHint()
        {
            if (RelaySession.UsingRelay && !string.IsNullOrEmpty(RelaySession.JoinCode))
                return $"참가 코드:  {RelaySession.JoinCode}   (클립보드에 복사됨)\n상대는 로비에서 이 코드를 넣고 [인터넷] 참가\nSpace 준비 / Enter 시작 / Esc 나가기";

            return $"같은 네트워크: {LanLobby.LocalIPv4()}   포트 {GameConfig.Port}\n다른 네트워크에서는 로비의 인터넷 방 만들기 / 참가 코드를 사용하세요.\nSpace 준비 / Enter 시작 / Esc 나가기";
        }

        static string GuestHint()
        {
            if (RelaySession.UsingRelay && !string.IsNullOrEmpty(RelaySession.JoinCode))
                return $"인터넷 참가 코드 {RelaySession.JoinCode} 로 들어왔습니다.\n호스트가 시작하면 같이 시작됩니다.\nSpace 준비 / Esc 나가기";

            return "호스트가 시작하면 같이 시작됩니다.\nSpace 준비 / Esc 나가기";
        }
    }
}
