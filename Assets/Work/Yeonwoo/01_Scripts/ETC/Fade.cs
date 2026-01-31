using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Work.Yeonwoo._01_Scripts.ETC
{
    public class Fade : MonoBehaviour
    {
        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        public void Next()
        {
            _image.DOFade(1f, 1f).OnComplete(() => SceneManager.LoadScene(1));
        }
    }
}