using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Work.Yeonwoo._01_Scripts.ETC
{
    public class UIAppearance : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        public UnityEvent OnPressStart;
        
        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        public void Appearance()
        {
            _text.DOFade(1.0f, 1.5f).SetEase(Ease.OutSine)
                .OnComplete(() =>
                    _text.DOFade(0f, 1f)
                        .SetLoops(-1, LoopType.Yoyo));
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                OnPressStart?.Invoke();
            }
        }
    }
}