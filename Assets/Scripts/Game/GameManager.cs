using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sugoroku.Data;
using Sugoroku.Board;
using Sugoroku.Visual;
using Sugoroku.Network;

namespace Sugoroku.Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public static int           HumanCount     = 1;
        public static int           CpuCount       = 1;
        public static CharacterType HumanCharacter = CharacterType.Hobbyist;

        private PlayerData[]  _players;
        private int           _currentPlayerIndex;
        private int           _finishRankCounter = 1;
        private GameObject[]  _pieces;

        /// <summary>「先を越される」判定用: 既にIFを獲得済みのジャーナルマスindex。</summary>
        private readonly HashSet<int> _claimedJournalIndices = new();

        /// <summary>中間発表の周期判定用。個人ターンが進むたびに加算。</summary>
        private int _turnCounter;

        [Header("駒表示")]
        [SerializeField] private float _pieceTargetHeight = 1.0f;

        public bool IsInitialized => _players != null && _players.Length > 0;

        public event System.Action<PlayerData, string> OnSquareEffect;
        public event System.Action<PlayerData>         OnGameOver;
        public event System.Action                     OnAllFinished;
        public event System.Action<string>             OnLog;

        private bool _ending;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _ending = false;
            DontDestroyOnLoad(gameObject);
            if (NetworkSessionHost.Instance == null)
                gameObject.AddComponent<NetworkSessionHost>();
            StatChangeNotifier.OnChanged += OnPlayerStatChanged;
        }

        private IEnumerator Start()
        {
            HumanCount     = GameSession.HumanCount;
            CpuCount       = GameSession.CpuCount;
            HumanCharacter = GameSession.HumanCharacter;

            var op = SceneManager.LoadSceneAsync("GameUIScene", LoadSceneMode.Additive);
            yield return op;
            yield return null;
            OnAllFinished += HandleAllFinished;
            InitGame();
        }

        private void InitGame()
        {
            _claimedJournalIndices.Clear();
            _turnCounter = 0;
            BoardLayoutGenerator.Invalidate();
            int total = Mathf.Clamp(HumanCount + CpuCount, 1, GameConfig.MaxPlayers);

            _players = new PlayerData[total];
            GameSession.EnsureHumanCharacters();
            for (int i = 0; i < total; i++)
            {
                bool isCpu = i >= HumanCount;
                var charType = isCpu
                    ? (CharacterType)GameRng.Range(0, 4)
                    : GameSession.GetHumanCharacter(i);
                _players[i] = PlayerData.Create(i, charType, isCpu);
            }

            PlayerIdentity.Apply(_players);

            if (BoardManager.Instance != null)
            {
                BoardManager.Instance.GenerateDefaultBoard();
                RefreshWaypointCards();
            }

            _pieces = new GameObject[total];
            SpawnPieces();
            BoardEffectPresenter.EnsureSceneInstance();

            var boardCam = Object.FindFirstObjectByType<Board.BoardCameraController>();
            if (boardCam != null)
            {
                boardCam.FrameBoard();
                boardCam.StopFollowing();
            }

            _currentPlayerIndex = 0;
            BoardDicePlacement.PlaceNearCurrentPlayer();
            TurnManager.Instance?.StartTurn();
        }

        private static void RefreshWaypointCards()
        {
            var route = BoardManager.Instance?.Route;
            if (route == null) return;
            foreach (var wp in route.Waypoints)
            {
                if (wp != null) wp.RequestVisualUpdate();
            }
        }

        private void SpawnPieces()
        {
            Transform parent = BoardManager.Instance?.Route != null
                ? BoardManager.Instance.Route.transform
                : BoardManager.Instance != null
                    ? BoardManager.Instance.transform
                    : transform;

            Vector3 startPos = BoardManager.Instance != null
                ? BoardManager.Instance.GetPosition(0)
                : Vector3.zero;

            for (int i = 0; i < _players.Length; i++)
            {
                var player = _players[i];
                var go = new GameObject($"Piece_{player.Character.DisplayName()}");
                go.transform.SetParent(parent, false);
                go.transform.position   = startPos + new Vector3(i * 0.25f, 0.12f * i, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                Board.BoardVisualUtility.ApplySpriteRenderer(
                    sr, Board.BoardSortingLayers.Player, 10 + i);

                var sprite = OriginalcharAssets.GetSprite(player.Character);
                sr.sprite  = sprite != null ? sprite : Board.BoardVisualUtility.GetCircleSprite();
                sr.color   = player.PieceTint;
                go.transform.localScale = Vector3.one * ComputePieceScale(sr.sprite);
                PlayerPieceLabel.Attach(go.transform, player);
                _pieces[i] = go;
            }
        }

        /// <summary>「先を越される」: このジャーナルマスindexは既にIF加算済みか。</summary>
        public bool IsJournalClaimed(int boardIndex) => _claimedJournalIndices.Contains(boardIndex);

        /// <summary>このジャーナルマスindexを加算済みとして記録する。</summary>
        public void ClaimJournal(int boardIndex) => _claimedJournalIndices.Add(boardIndex);

        public PlayerData   GetCurrentPlayer()  => IsInitialized ? _players[_currentPlayerIndex] : null;
        public int          GetCurrentPlayerIndex() => _currentPlayerIndex;
        public PlayerData[] GetAllPlayers()      => _players ?? System.Array.Empty<PlayerData>();
        public GameObject   GetPiece(int index)  => index < _pieces?.Length ? _pieces[index] : null;

        private float ComputePieceScale(Sprite sprite)
        {
            if (sprite == null) return 0.5f;
            float h = sprite.bounds.size.y;
            return h > 0.01f ? _pieceTargetHeight / h : 0.5f;
        }

        public void OnDiceResult(int steps)    => TurnManager.Instance.OnDiceRolled(steps);

        public void AdvanceTurn()
        {
            if (_ending) return;
            if (IsAllFinished()) { OnAllFinished?.Invoke(); return; }
            do { _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Length; }
            while (_players[_currentPlayerIndex].IsFinished && !IsAllFinished());
            NetworkSessionHost.Instance?.NotifyTurnAdvanced(_currentPlayerIndex);

            _turnCounter++;
            int interval = Mathf.Max(1, _players.Length) * GameConfig.MidGameCheckInterval;
            if (_turnCounter % interval == 0)
                StartCoroutine(RunMidGameAnnouncementThenStartTurn());
            else
                TurnManager.Instance.StartTurn();
        }

        private IEnumerator RunMidGameAnnouncementThenStartTurn()
        {
            yield return StartCoroutine(MidGameAnnouncementRules.Resolve(_players));
            TurnManager.Instance.StartTurn();
        }

        public void OnPlayerReachGoal(PlayerData player)
        {
            player.Status     = PlayerStatus.Graduated;
            player.FinishRank = _finishRankCounter++;
            AddLog($"[修了] {player.Name} が修了！（{player.FinishRank}位）");
            if (IsAllFinished()) OnAllFinished?.Invoke();
            else TurnManager.Instance.EndTurn();
        }

        private void OnPlayerStatChanged(PlayerData player, int _, int __, int ___, int ____)
        {
            CheckGameOver(player);
        }

        public void CheckGameOver(PlayerData player)
        {
            if (player.IsFinished || _ending) return;
            var reason = PlayerStatRules.EvaluateDefeat(player);
            if (reason == GameOverReason.None) return;
            player.Status = PlayerStatus.Dropout;
            string label = reason switch
            {
                GameOverReason.Bankruptcy => "破産",
                GameOverReason.Missing    => "失踪",
                _                         => "研究不振"
            };
            AddLog($"[脱落] {player.Name} が{label}により脱落...");
            if (!player.IsCpu)
            {
                GameSession.LastGameOverReason = reason;
                GameSession.SavePlayers(_players);
                StartCoroutine(LoadEndScene("GameOverScene"));
            }
            OnGameOver?.Invoke(player);
        }

        private void HandleAllFinished()
        {
            if (_ending) return;
            GameSession.SavePlayers(_players);
            StartCoroutine(LoadEndScene("ResultScene"));
        }

        private IEnumerator LoadEndScene(string sceneName)
        {
            _ending = true;
            Time.timeScale = 1f;
            yield return new WaitForSeconds(0.5f);

            if (SceneManager.GetSceneByName("GameUIScene").isLoaded)
                yield return SceneManager.UnloadSceneAsync("GameUIScene");

            OnAllFinished -= HandleAllFinished;
            Instance = null;
            Destroy(gameObject);
            SceneManager.LoadScene(sceneName);
        }

        public void ShowSquareEffect(PlayerData player, SquareType type, bool ignored = false)
        {
            string label = SquareEffectLabels.Get(type);
            string msg = ignored
                ? $"[{player.Name}] {label}マス — イベント回避"
                : $"[{player.Name}] {label}マスに到達！";
            OnSquareEffect?.Invoke(player, msg);
            AddLog(msg);
        }

        public void UsePlayerSkill(PlayerData player)
        {
            if (player.SkillUsedThisTurn) return;
            player.SkillUsedThisTurn = true;
            switch (player.Character)
            {
                case CharacterType.Hobbyist:
                    player.IgnoreNextEvents++;
                    player.ApplyStatChange(0, 0, 10, 0);
                    AddLog($"[スキル] {player.Name}「一旦逃避」発動！");
                    break;
                case CharacterType.Serious:
                    player.IgnoreNextEvents++;
                    AddLog($"[スキル] {player.Name}「一点突破」発動！ 次の講義マス無効。");
                    break;
                case CharacterType.Athletic:
                    player.AthleticMentalImmunity = true;
                    AddLog($"[スキル] {player.Name}「根性」発動！ このターンメンタルダメージ0。");
                    break;
                case CharacterType.Rich:
                    if (player.Money >= GameConfig.RichSkillCost)
                    {
                        player.ApplyStatChange(-GameConfig.RichSkillCost, 0, 0, 0);
                        player.IgnoreNextEvents++;
                        AddLog($"[スキル] {player.Name}「課金解決」発動！");
                    }
                    break;
                case CharacterType.Genius:
                    player.GeniusBonusActive = true;
                    AddLog($"[スキル] {player.Name}「閃き」発動！ 次のジャーナルでボーナス。");
                    break;
            }
            DiceRoller.Instance.Roll();
        }

        private bool IsAllFinished() => _players.All(p => p.IsFinished);
        private void AddLog(string msg) => OnLog?.Invoke(msg);
        private void OnDestroy()
        {
            StatChangeNotifier.OnChanged -= OnPlayerStatChanged;
        }
    }
}
