using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Nordo.Core.Events;
using Nordo.Progression;

// NOTE: EventBus calls below are fully qualified (Nordo.Core.EventBus<T>) after a CS0103 was reported
// against this file. Full qualification cannot fail name resolution as long as the Nordo.Core asmdef
// reference exists — which it does — making this file immune to using-directive state.

namespace Nordo.UI
{
    /// <summary>
    /// The in-game HUD and journal, drawn with IMGUI and styled in the cold station palette: the current
    /// objective, the inventory, the interaction prompt, transient feedback toasts, a modal note reader,
    /// and a toggleable field logbook (objectives + collected notes). It reads everything through events
    /// and the inventory/objective services, so it is fully decoupled and swappable for a diegetic
    /// world-space UI later without touching gameplay.
    /// <para>
    /// (IMGUI is used because the whole level is generated at runtime; this guarantees the UI renders
    /// with no scene-authored Canvas/fonts. It is deliberately themed, not default gray.)
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StationHUD : MonoBehaviour
    {
        private struct Toast { public string Text; public float Until; }
        private struct Note { public string Id; public string Title; public string Body; }

        private static readonly Color Ink = new Color(0.82f, 0.86f, 0.9f);
        private static readonly Color Accent = new Color(0.55f, 0.75f, 0.85f);
        private static readonly Color PanelBg = new Color(0.05f, 0.07f, 0.09f, 0.85f);

        private InventoryService _inventory;
        private ObjectiveTracker _objectives;

        private readonly List<ItemStack> _stacks = new();
        private readonly List<Toast> _toasts = new();
        private readonly List<Note> _notes = new();

        private string _objectiveTitle = string.Empty;
        private bool _allComplete;
        private string _promptText = string.Empty;
        private bool _promptVisible;

        private bool _logbookOpen;
        private Note? _openNote;
        private Vector2 _logScroll;

        private GUIStyle _panel, _header, _label, _prompt, _toast, _noteTitle, _noteBody;
        private Texture2D _panelTex;
        private bool _stylesReady;

        private void Awake()
        {
            _inventory = FindObjectOfType<InventoryService>();
            _objectives = FindObjectOfType<ObjectiveTracker>();
        }

        private void OnEnable()
        {
            Nordo.Core.EventBus<ObjectiveChangedEvent>.Subscribe(OnObjective);
            Nordo.Core.EventBus<GameMessageEvent>.Subscribe(OnMessage);
            Nordo.Core.EventBus<NoteReadEvent>.Subscribe(OnNote);
            Nordo.Core.EventBus<InteractionPromptEvent>.Subscribe(OnPrompt);
        }

        private void OnDisable()
        {
            Nordo.Core.EventBus<ObjectiveChangedEvent>.Unsubscribe(OnObjective);
            Nordo.Core.EventBus<GameMessageEvent>.Unsubscribe(OnMessage);
            Nordo.Core.EventBus<NoteReadEvent>.Unsubscribe(OnNote);
            Nordo.Core.EventBus<InteractionPromptEvent>.Unsubscribe(OnPrompt);
        }

        private void Update()
        {
            // Expire toasts.
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime >= _toasts[i].Until)
                {
                    _toasts.RemoveAt(i);
                }
            }

            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            // J toggles the logbook; Esc closes an open note (or the logbook).
            if (kb.jKey.wasPressedThisFrame)
            {
                _logbookOpen = !_logbookOpen;
            }

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (_openNote != null)
                {
                    _openNote = null;
                }
                else if (_logbookOpen)
                {
                    _logbookOpen = false;
                }
            }
        }

        // --- Event handlers ---------------------------------------------------------

        private void OnObjective(ObjectiveChangedEvent e)
        {
            _objectiveTitle = e.CurrentTitle;
            _allComplete = e.AllComplete;
        }

        private void OnMessage(GameMessageEvent e)
        {
            _toasts.Add(new Toast { Text = e.Text, Until = Time.unscaledTime + Mathf.Max(1f, e.Duration) });
        }

        private void OnNote(NoteReadEvent e)
        {
            if (!_notes.Exists(n => n.Id == e.NoteId))
            {
                _notes.Add(new Note { Id = e.NoteId, Title = e.Title, Body = e.Body });
            }

            _openNote = new Note { Id = e.NoteId, Title = e.Title, Body = e.Body };
        }

        private void OnPrompt(InteractionPromptEvent e)
        {
            _promptVisible = e.Visible;
            _promptText = e.Text;
        }

        // --- Rendering --------------------------------------------------------------

        private void OnGUI()
        {
            EnsureStyles();

            DrawObjective();
            DrawInventory();
            DrawPrompt();
            DrawToasts();

            if (_openNote != null)
            {
                DrawNoteModal(_openNote.Value);
            }
            else if (_logbookOpen)
            {
                DrawLogbook();
            }

            DrawHint();
        }

        private void DrawObjective()
        {
            string text = _allComplete ? "SECTION COMPLETE" : $"OBJECTIVE\n{_objectiveTitle}";
            var rect = new Rect(16, 16, 340, 60);
            GUI.Box(rect, GUIContent.none, _panel);
            GUI.Label(new Rect(rect.x + 12, rect.y + 8, rect.width - 20, rect.height - 12), text, _header);
        }

        private void DrawInventory()
        {
            if (_inventory == null)
            {
                return;
            }

            _inventory.GetStacks(_stacks);
            float h = 30 + _stacks.Count * 20;
            var rect = new Rect(16, Screen.height - h - 16, 240, h);
            GUI.Box(rect, GUIContent.none, _panel);
            GUI.Label(new Rect(rect.x + 12, rect.y + 6, rect.width - 20, 20), "INVENTORY", _header);

            for (int i = 0; i < _stacks.Count; i++)
            {
                string line = _stacks[i].Count > 1 ? $"{_stacks[i].Item.DisplayName}  ×{_stacks[i].Count}" : _stacks[i].Item.DisplayName;
                GUI.Label(new Rect(rect.x + 14, rect.y + 28 + i * 20, rect.width - 22, 20), "• " + line, _label);
            }

            if (_stacks.Count == 0)
            {
                GUI.Label(new Rect(rect.x + 14, rect.y + 28, rect.width - 22, 20), "(empty)", _label);
            }
        }

        private void DrawPrompt()
        {
            if (!_promptVisible || string.IsNullOrEmpty(_promptText))
            {
                return;
            }

            var size = _prompt.CalcSize(new GUIContent(_promptText));
            var rect = new Rect(Screen.width * 0.5f - size.x * 0.5f - 12, Screen.height * 0.62f, size.x + 24, size.y + 10);
            GUI.Box(rect, GUIContent.none, _panel);
            GUI.Label(rect, _promptText, _prompt);
        }

        private void DrawToasts()
        {
            float y = Screen.height - 120;
            for (int i = _toasts.Count - 1; i >= 0 && i >= _toasts.Count - 4; i--)
            {
                var content = new GUIContent(_toasts[i].Text);
                float w = Mathf.Min(520, _toast.CalcSize(content).x + 24);
                var rect = new Rect(Screen.width * 0.5f - w * 0.5f, y, w, 26);
                GUI.Box(rect, GUIContent.none, _panel);
                GUI.Label(rect, _toasts[i].Text, _toast);
                y -= 30;
            }
        }

        private void DrawNoteModal(Note note)
        {
            var rect = new Rect(Screen.width * 0.5f - 260, Screen.height * 0.5f - 180, 520, 360);
            GUI.Box(rect, GUIContent.none, _panel);
            GUI.Label(new Rect(rect.x + 20, rect.y + 16, rect.width - 40, 30), note.Title, _noteTitle);
            GUI.Label(new Rect(rect.x + 20, rect.y + 54, rect.width - 40, rect.height - 96), note.Body, _noteBody);
            GUI.Label(new Rect(rect.x + 20, rect.yMax - 34, rect.width - 40, 24), "[Esc] Close", _label);
        }

        private void DrawLogbook()
        {
            var rect = new Rect(Screen.width * 0.5f - 320, Screen.height * 0.5f - 220, 640, 440);
            GUI.Box(rect, GUIContent.none, _panel);
            GUI.Label(new Rect(rect.x + 20, rect.y + 14, rect.width - 40, 30), "FIELD LOGBOOK", _noteTitle);

            // Objectives (left column).
            GUI.Label(new Rect(rect.x + 20, rect.y + 52, 280, 20), "OBJECTIVES", _header);
            if (_objectives != null)
            {
                for (int i = 0; i < _objectives.Steps.Count; i++)
                {
                    bool done = i < _objectives.CurrentIndex;
                    bool current = i == _objectives.CurrentIndex;
                    string mark = done ? "✓" : current ? "▶" : "•";
                    var style = current ? _header : _label;
                    GUI.Label(new Rect(rect.x + 24, rect.y + 76 + i * 22, 280, 22), $"{mark} {_objectives.Steps[i].Title}", style);
                }
            }

            // Notes (right column, scrollable).
            GUI.Label(new Rect(rect.x + 320, rect.y + 52, 300, 20), "RECOVERED NOTES", _header);
            var viewRect = new Rect(rect.x + 320, rect.y + 76, 300, rect.height - 110);
            float contentH = 8;
            for (int i = 0; i < _notes.Count; i++)
            {
                contentH += 24 + _noteBody.CalcHeight(new GUIContent(_notes[i].Body), 280) + 10;
            }

            _logScroll = GUI.BeginScrollView(viewRect, _logScroll, new Rect(0, 0, 280, contentH));
            float ny = 4;
            for (int i = 0; i < _notes.Count; i++)
            {
                GUI.Label(new Rect(0, ny, 280, 22), _notes[i].Title, _header);
                ny += 24;
                float bh = _noteBody.CalcHeight(new GUIContent(_notes[i].Body), 280);
                GUI.Label(new Rect(0, ny, 280, bh), _notes[i].Body, _noteBody);
                ny += bh + 10;
            }
            if (_notes.Count == 0)
            {
                GUI.Label(new Rect(0, 4, 280, 22), "(none recovered)", _label);
            }
            GUI.EndScrollView();

            GUI.Label(new Rect(rect.x + 20, rect.yMax - 30, rect.width - 40, 24), "[J] Close   [Esc] Back", _label);
        }

        private void DrawHint()
        {
            if (!_logbookOpen && _openNote == null)
            {
                GUI.Label(new Rect(Screen.width - 150, 16, 140, 20), "[J] Logbook", _label);
            }
        }

        // --- Styling ----------------------------------------------------------------

        private void EnsureStyles()
        {
            if (_stylesReady)
            {
                return;
            }

            _panelTex = new Texture2D(1, 1);
            _panelTex.SetPixel(0, 0, PanelBg);
            _panelTex.Apply();

            _panel = new GUIStyle(GUI.skin.box) { border = new RectOffset(0, 0, 0, 0) };
            _panel.normal.background = _panelTex;

            _header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true };
            _header.normal.textColor = Accent;

            _label = new GUIStyle(GUI.skin.label) { wordWrap = true };
            _label.normal.textColor = Ink;

            _prompt = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _prompt.normal.textColor = Ink;

            _toast = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _toast.normal.textColor = Ink;

            _noteTitle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 16 };
            _noteTitle.normal.textColor = Accent;

            _noteBody = new GUIStyle(GUI.skin.label) { wordWrap = true };
            _noteBody.normal.textColor = Ink;

            _stylesReady = true;
        }
    }
}
