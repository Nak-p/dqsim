using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DQSim
{
    public class NotificationPanel : MonoBehaviour
    {
        private TextMeshProUGUI _messageText;

        private readonly Queue<string> _queue = new Queue<string>();
        private float _dismissTimer;
        private const float DisplayDuration = 4f;

        private void Awake()
        {
            var go = new GameObject("Text");
            go.transform.SetParent(transform, false);
            _messageText           = go.AddComponent<TextMeshProUGUI>();
            _messageText.fontSize  = 14f;
            _messageText.color     = Color.white;
            _messageText.alignment = TextAlignmentOptions.Center;
            UIBuilder.Stretch(go.GetComponent<RectTransform>(), 6, 6, 4, 4);

            gameObject.SetActive(false);
        }

        public void Show(string message)
        {
            _queue.Enqueue(message);
            if (!gameObject.activeSelf) ShowNext();
        }

        private void Update()
        {
            if (_queue.Count == 0 && _dismissTimer <= 0f) return;

            _dismissTimer -= Time.deltaTime;
            if (_dismissTimer <= 0f)
            {
                if (_queue.Count > 0) ShowNext();
                else gameObject.SetActive(false);
            }
        }

        private void ShowNext()
        {
            if (_queue.Count == 0) return;
            _messageText.text = _queue.Dequeue();
            gameObject.SetActive(true);
            _dismissTimer = DisplayDuration;
        }
    }
}
