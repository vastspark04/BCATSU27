using System;
using UnityEngine;

public class OneTimeSettingNotification : MonoBehaviour
{
	public string notificationName;

	public string settingName;

	public bool alwaysOpen;

	public event Action OnDismissed;

	private void OnEnable()
	{
		bool val = false;
		if (!alwaysOpen && GameSettings.TryGetGameSettingValue<bool>(notificationName, out val) && val)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public void Accept()
	{
		if (!string.IsNullOrEmpty(settingName))
		{
			GameSettings.SetGameSettingValue(settingName, 1f);
		}
		GameSettings.SetGameSettingValue(notificationName, val: true);
		base.gameObject.SetActive(value: false);
		this.OnDismissed?.Invoke();
	}

	public void Deny()
	{
		if (!string.IsNullOrEmpty(settingName))
		{
			GameSettings.SetGameSettingValue(settingName, -1f);
		}
		GameSettings.SetGameSettingValue(notificationName, val: true);
		base.gameObject.SetActive(value: false);
		this.OnDismissed?.Invoke();
	}
}
