using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CampaignNotificationSystem : MonoBehaviour
{
	public class Notification
	{
		public string title;

		public string description;

		public Color color;
	}

	private List<Notification> notifs = new List<Notification>();

	private Coroutine notifCoroutine;

	public Color newMissionColor;

	public Color newTrainingColor;

	public Color newWeaponColor;

	public Transform notifTransform;

	public Text titleText;

	public Text descriptionText;

	public Image bgImage;

	public GameObject displayObject;

	public float marqueeWidth = 1380f;

	public float marqueeSpeed = 2f;

	public float marqueeLerpRate = 9f;

	public float marqueeStayTime = 0.75f;

	public float bgFadeSpeed = 1.5f;

	public float bgAlpha = 0.95f;

	public AudioSource audioSource;

	public AudioClip notificationSound;

	public VRPointInteractableCanvas iCanvas;

	private bool isShowingNotifs;

	private bool _skip;

	private float skipMult
	{
		get
		{
			if (!_skip)
			{
				return 1f;
			}
			return 2f;
		}
	}

	private void Awake()
	{
		bgImage.color = Color.clear;
		displayObject.SetActive(value: false);
	}

	public void AddNotification(string title, string description, Color color)
	{
		Notification notification = new Notification();
		notification.title = title;
		notification.description = description;
		notification.color = color;
		notifs.Add(notification);
	}

	public void ClearNotifications()
	{
		notifs = new List<Notification>();
		if (notifCoroutine != null)
		{
			StopCoroutine(notifCoroutine);
			notifCoroutine = null;
			displayObject.SetActive(value: false);
		}
		iCanvas.isInteractable = true;
		isShowingNotifs = false;
	}

	public void PlayNotifications()
	{
		if (notifs != null && notifs.Count >= 1)
		{
			notifCoroutine = StartCoroutine(NotificationRoutine());
		}
	}

	private IEnumerator NotificationRoutine()
	{
		iCanvas.isInteractable = false;
		isShowingNotifs = true;
		notifTransform.localPosition = new Vector3(marqueeWidth, 0f, 0f);
		displayObject.SetActive(value: true);
		bgImage.color = new Color(0f, 0f, 0f, 0f);
		float t5 = 0f;
		while (t5 < 1f)
		{
			yield return null;
			t5 = Mathf.MoveTowards(t5, 1f, bgFadeSpeed * Time.deltaTime * skipMult);
			bgImage.color = new Color(0f, 0f, 0f, t5 * bgAlpha);
		}
		foreach (Notification notif in notifs)
		{
			notifTransform.localPosition = new Vector3(marqueeWidth, 0f, 0f);
			titleText.text = notif.title;
			titleText.color = notif.color;
			descriptionText.text = notif.description;
			if ((bool)audioSource)
			{
				audioSource.Stop();
				audioSource.PlayOneShot(notificationSound);
			}
			t5 = 0f;
			Vector3 startPos = new Vector3(marqueeWidth, 0f, 0f);
			Vector3 tgtPos = startPos;
			while (t5 < 1f)
			{
				yield return null;
				t5 = Mathf.MoveTowards(t5, 1f, marqueeSpeed * Time.deltaTime * skipMult);
				tgtPos = Vector3.Lerp(startPos, Vector3.zero, t5);
				notifTransform.localPosition = Vector3.Lerp(notifTransform.localPosition, tgtPos, marqueeLerpRate * Time.deltaTime * skipMult);
			}
			t5 = Time.time;
			while (Time.time - t5 < marqueeStayTime && !_skip)
			{
				notifTransform.localPosition = Vector3.Lerp(notifTransform.localPosition, tgtPos, marqueeLerpRate * Time.deltaTime * skipMult);
				yield return null;
			}
			t5 = 0f;
			while (t5 < 1f)
			{
				yield return null;
				t5 = Mathf.MoveTowards(t5, 1f, marqueeSpeed * Time.deltaTime * skipMult);
				tgtPos = Vector3.Lerp(Vector3.zero, -startPos, t5);
				notifTransform.localPosition = Vector3.Lerp(notifTransform.localPosition, tgtPos, marqueeLerpRate * Time.deltaTime * skipMult);
			}
			while (notifTransform.localPosition.x > 0f - marqueeWidth + 1f)
			{
				notifTransform.localPosition = Vector3.Lerp(notifTransform.localPosition, tgtPos, marqueeLerpRate * Time.deltaTime * skipMult);
			}
		}
		t5 = 0f;
		while (t5 < 1f)
		{
			yield return null;
			t5 = Mathf.MoveTowards(t5, 1f, bgFadeSpeed * Time.deltaTime * skipMult);
			bgImage.color = new Color(0f, 0f, 0f, (1f - t5) * bgAlpha);
		}
		ClearNotifications();
	}

	private void Update()
	{
		if (!isShowingNotifs)
		{
			return;
		}
		_skip = false;
		for (int i = 0; i < VRHandController.controllers.Count; i++)
		{
			VRHandController vRHandController = VRHandController.controllers[i];
			if ((bool)vRHandController && vRHandController.triggerClicked)
			{
				_skip = true;
			}
		}
		audioSource.volume = (_skip ? 0.25f : 1f);
	}
}
