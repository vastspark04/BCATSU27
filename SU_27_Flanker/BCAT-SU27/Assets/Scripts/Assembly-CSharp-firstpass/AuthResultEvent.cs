using System;
using UnityEngine.Events;

[Serializable]
public class AuthResultEvent : UnityEvent<bool, int, string>
{
}
