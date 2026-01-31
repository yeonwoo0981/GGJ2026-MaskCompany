using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Work.Yeonwoo._01_Scripts.ETC.csiimnida.CSILib.SoundManager.RunTime;

namespace Work.Yeonwoo._01_Scripts
{
    public class Asdf : MonoBehaviour
    {
        private void Update()
        {
            if (Keyboard.current.nKey.wasPressedThisFrame)
                SoundManager.Instance.PlaySound("asdf");
            if (Keyboard.current.mKey.wasPressedThisFrame)
                SoundManager.Instance.StopSound("asdf");
        }
    }
}