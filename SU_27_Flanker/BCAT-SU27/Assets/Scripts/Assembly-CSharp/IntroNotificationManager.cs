using System.Collections;
using UnityEngine;

public class IntroNotificationManager : MonoBehaviour
{
	public OneTimeSettingNotification[] notifications;

	private void Start()
	{
		StartCoroutine(SetupRoutine());
	}

	private IEnumerator SetupRoutine()
	{
		Debug.Log("intro start");
		yield return null;
		Debug.Log("intro next frame");
		bool flag = false;
		for (int i = 0; i < notifications.Length; i++)
		{
			Debug.Log("notif " + i);
			if (!notifications[i].gameObject.activeSelf)
			{
				continue;
			}
			Debug.Log(" - is active");
			if (i < notifications.Length - 1)
			{
				int nextIdx = i + 1;
				notifications[i].OnDismissed += delegate
				{
					notifications[nextIdx].gameObject.SetActive(value: true);
				};
			}
			if (!flag)
			{
				flag = true;
			}
			else
			{
				notifications[i].gameObject.SetActive(value: false);
			}
		}
	}
}
