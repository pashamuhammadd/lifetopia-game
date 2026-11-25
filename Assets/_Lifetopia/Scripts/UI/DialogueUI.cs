using System;
using Lifetopia.Controls;
using Lifetopia.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>
    /// Simple modal dialogue box. While open it blocks gameplay input through the
    /// GameplayInputGate, so the player stands still and E/tap advances the text instead.
    /// Escape (Cancel) closes it immediately. Clicking/tapping the panel also advances.
    /// </summary>
    public sealed class DialogueUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text speakerLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Text continueHint;
        [SerializeField] private Button advanceButton;

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private GameplayInputGate gate;

        [SerializeField] private string continueText = "Press E or tap to continue";
        [SerializeField] private string closeText = "Press E or tap to close";

        private string[] _lines = Array.Empty<string>();
        private int _index;

        public event Action Opened;
        public event Action Closed;

        public bool IsOpen { get; private set; }

        /// <summary>Index of the line currently shown (for tests).</summary>
        public int LineIndex => _index;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (advanceButton != null) advanceButton.onClick.AddListener(Advance);
        }

        private void OnDestroy()
        {
            if (advanceButton != null) advanceButton.onClick.RemoveListener(Advance);
            if (IsOpen && gate != null) gate.Unblock(this);
        }

        public void Open(string speaker, string[] lines)
        {
            if (IsOpen || lines == null || lines.Length == 0) return;

            _lines = lines;
            _index = 0;
            IsOpen = true;
            if (panel != null) panel.SetActive(true);
            if (speakerLabel != null) speakerLabel.text = speaker;
            ShowLine();

            if (gate != null) gate.Block(this);
            if (input != null)
            {
                input.UiSubmitPerformed += Advance;
                input.CancelPerformed += Close;
            }
            Opened?.Invoke();
        }

        public void Advance()
        {
            if (!IsOpen) return;
            if (_index + 1 < _lines.Length)
            {
                _index++;
                ShowLine();
            }
            else
            {
                Close();
            }
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (panel != null) panel.SetActive(false);
            if (input != null)
            {
                input.UiSubmitPerformed -= Advance;
                input.CancelPerformed -= Close;
            }
            if (gate != null) gate.Unblock(this);
            Closed?.Invoke();
        }

        private void ShowLine()
        {
            if (bodyLabel != null) bodyLabel.text = _lines[_index];
            if (continueHint != null) continueHint.text = _index + 1 < _lines.Length ? continueText : closeText;
        }
    }
}
