using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Work.Yeonwoo._01_Scripts.ETC
{
    public class LogoDOTween : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;

        public UnityEvent UIAppearance;
        
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            _spriteRenderer.DOFade(1f, 1.2f).SetEase(Ease.OutSine)
                .OnComplete(() =>
                    UIAppearance?.Invoke());
        }
    }
}