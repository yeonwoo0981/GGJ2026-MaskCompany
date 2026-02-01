using System;
using UnityEngine;
using UnityEngine.InputSystem;
using CSILib.SoundManager.RunTime;

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