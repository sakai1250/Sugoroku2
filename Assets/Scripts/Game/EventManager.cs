using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Network;

namespace Sugoroku.Game
{
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        private List<EventMaster> _allEvents = new();
        private List<EventMaster> _pool      = new();
        private List<EventMaster> _rarePool  = new();
        private Dictionary<string, EventMaster> _byId = new();

        public event System.Action<EventMaster, PlayerData> OnEventTriggered;
        public event System.Action<PlayerData, EventChoice> OnChoiceApplied;

        private string[] _lastEventTags;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadEvents();
        }

        private void LoadEvents()
        {
            var textAsset = Resources.Load<TextAsset>("EventMasters/events");
            if (textAsset == null)
            {
                Debug.LogError("events.json が Resources/EventMasters/ に見つかりません");
                return;
            }

            // JsonUtility は配列直接不可なのでラッパーで解析
            string json = "{\"Events\":" + textAsset.text + "}";
            var wrapper = JsonUtility.FromJson<EventMasterList>(json);
            _allEvents = wrapper?.ToList() ?? new List<EventMaster>();
            _byId.Clear();
            foreach (var ev in _allEvents)
            {
                if (ev != null && !string.IsNullOrEmpty(ev.EventId))
                    _byId[ev.EventId] = ev;
            }

            int broken = 0;
            foreach (var ev in _allEvents)
            {
                if (ev == null || ev.ChoiceCount == 0) broken++;
            }
            if (_allEvents.Count == 0)
                Debug.LogError("events.json にイベントが1件も読み込めませんでした。");
            else if (broken > 0)
                Debug.LogWarning($"events.json: 選択肢のないイベントが {broken} 件あります。JSON 形式を確認してください。");
            else
                Debug.Log($"events.json: {_allEvents.Count} 件ロード完了");

            ValidateRobustness();

            RefillPool();
            RefillRarePool();
        }

        private void RefillPool()
        {
            _pool = _allEvents.Where(e => e != null && !e.IsRare).ToList();
            Shuffle(_pool);
        }

        private void RefillRarePool()
        {
            _rarePool = _allEvents.Where(e => e != null && e.IsRare).ToList();
            Shuffle(_rarePool);
        }

        public EventMaster GetById(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return null;
            return _byId.TryGetValue(eventId, out var ev) ? ev : null;
        }

        public EventMaster DrawEvent() => DrawEventOnAuthority();

        /// <summary>§7.3 — プール抽選（StateAuthority からのみ呼ぶ）。抽選ごとに低確率で超レアプールから引く。</summary>
        public EventMaster DrawEventOnAuthority()
        {
            if (_rarePool.Count > 0 && GameRng.Value01() < GameConfig.RareEventChance)
            {
                var rare = DrawFromRarePool();
                if (rare != null) return rare;
            }

            if (_pool.Count == 0) RefillPool();
            if (_pool.Count == 0) return null;
            var ev = _pool[0];
            _pool.RemoveAt(0);
            return ev;
        }

        private EventMaster DrawFromRarePool()
        {
            if (_rarePool.Count == 0) RefillRarePool();
            if (_rarePool.Count == 0) return null;
            var ev = _rarePool[0];
            _rarePool.RemoveAt(0);
            return ev;
        }

        public void TriggerEvent(EventMaster ev, PlayerData player)
        {
            if (ev == null)
            {
                Debug.LogWarning("TriggerEvent: イベントが null です。");
                return;
            }

            _lastEventTags = ev.Tags;

            var modal = Object.FindFirstObjectByType<Sugoroku.UI.EventModalUI>(FindObjectsInactive.Include);

            bool hasListeners = OnEventTriggered != null;
            if (modal == null)
                Debug.LogError($"イベント「{ev.Title}」: UI が未接続です。EventModalUI を確認してください。");

            OnEventTriggered?.Invoke(ev, player);

            StartCoroutine(PresentEvent(ev, player, modal, hasListeners));
        }

        /// <summary>カットイン演出を先に再生してから、イベントモーダル（または直接処理）へ進む。</summary>
        private IEnumerator PresentEvent(EventMaster ev, PlayerData player, Sugoroku.UI.EventModalUI modal, bool hasListeners)
        {
            yield return Sugoroku.UI.EventIntroPresenter.Play(ev, player);

            if (modal != null)
            {
                if (player != null && player.IsCpu)
                    yield return RunCpuEvent(ev, player);
                else
                    modal.ShowEventFromManager(ev, player);
            }
            else if (!hasListeners)
            {
                Sugoroku.UI.EventModalUI.ShowEventDirect(ev, player);
            }
        }

        public IEnumerator ApplyChoiceSequence(PlayerData player, EventChoice choice)
        {
            if (player == null || choice == null) yield break;

            var (money, ifScore, mental, virtue) = CharacterTagAffinity.Apply(
                player.Character, _lastEventTags,
                choice.MoneyChange, choice.IfScoreChange, choice.MentalChange, choice.VirtueChange);

            (money, ifScore, mental, virtue) = ProfessorEffectRules.Apply(
                GameSession.SelectedProfessor, money, ifScore, mental, virtue);

            yield return StatChangeSequencer.Apply(player, money, ifScore, mental, virtue);

            if (!string.IsNullOrEmpty(choice.SetsBranchRoute))
                ApplyBranchRoute(player, choice.SetsBranchRoute);

            if (choice.DelayTurns > 0)
            {
                var pending = PendingEventResult.FromChoice(choice);
                player.PendingResults.Add(pending);
                Sugoroku.UI.GameStatusBanner.Show(
                    $"{PlayerIdentity.FormatHudLabel(player)} — {pending.Label}（{pending.TurnsRemaining}ターン後）");
            }

            OnChoiceApplied?.Invoke(player, choice);
        }

        private static void ApplyBranchRoute(PlayerData player, string route)
        {
            if (route == "Lab") player.ActiveBranch = BranchRoute.Lab;
            else if (route == "PartTime") player.ActiveBranch = BranchRoute.PartTime;
        }

        public void ApplyChoice(PlayerData player, EventChoice choice)
        {
            StartCoroutine(ApplyChoiceSequence(player, choice));
        }

        public void RunHumanChoiceResolution(PlayerData player, EventChoice choice, System.Action dismissUi)
        {
            StartCoroutine(HumanChoiceResolution(player, choice, dismissUi));
        }

        private IEnumerator HumanChoiceResolution(PlayerData player, EventChoice choice, System.Action dismissUi)
        {
            dismissUi?.Invoke();
            yield return ApplyChoiceSequence(player, choice);
            TurnManager.Instance.EndTurn();
            Object.FindFirstObjectByType<Sugoroku.UI.GameHUD>()?.RefreshAll();
        }

        private IEnumerator RunCpuEvent(EventMaster ev, PlayerData player)
        {
            yield return new WaitForSeconds(1.2f);
            if (ev == null || player == null) yield break;

            int idx = CpuController.Instance != null
                ? CpuController.Instance.PickChoice(player, ev)
                : EventRobustnessValidator.FirstSelectableIndex(ev, player);

            if (idx >= 0)
            {
                var choice = ev.GetChoice(idx);
                if (choice != null)
                    yield return ApplyChoiceSequence(player, choice);
            }
            else if (player.Virtue >= GameConfig.VirtueRescueThreshold)
            {
                yield return StatChangeSequencer.Apply(player, 0, 0, 10, -5);
            }
            else
            {
                int fallback = EventRobustnessValidator.FirstUnconditionalIndex(ev);
                if (fallback >= 0)
                {
                    var choice = ev.GetChoice(fallback);
                    if (choice != null)
                        yield return ApplyChoiceSequence(player, choice);
                }
            }

            TurnManager.Instance.EndTurn();
            Object.FindFirstObjectByType<Sugoroku.UI.GameHUD>()?.RefreshAll();
        }

        private void ValidateRobustness()
        {
            var issues = EventRobustnessValidator.ValidateAll(_allEvents);
            string report = EventRobustnessValidator.FormatReport(issues);
            if (issues.Count > 0)
                Debug.LogError(report);
            else
                Debug.Log(report);
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = GameRng.Range(0, i);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

    }
}
