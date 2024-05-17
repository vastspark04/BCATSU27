using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WingmanVoicesSettingsUI : MonoBehaviour
{
	public GameObject voiceTemplate;

	public ScrollRect scrollRect;

	public float padding = 2f;

	private List<GameObject> itemObjs = new List<GameObject>();

	private List<WingmanVoiceProfile> voices;

	public void OpenWindow()
	{
		base.gameObject.SetActive(value: true);
		foreach (GameObject itemObj in itemObjs)
		{
			Object.Destroy(itemObj);
		}
		itemObjs.Clear();
		voices = VTResources.GetWingmanVoiceProfiles();
		RectTransform obj = (RectTransform)voiceTemplate.transform;
		float num = obj.rect.width * voiceTemplate.transform.localScale.x + padding;
		float num2 = obj.rect.height * voiceTemplate.transform.localScale.y + padding;
		voiceTemplate.gameObject.SetActive(value: false);
		for (int i = 0; i < voices.Count; i++)
		{
			GameObject gameObject = Object.Instantiate(voiceTemplate, voiceTemplate.transform.parent);
			gameObject.SetActive(value: true);
			int num3 = i / 2;
			int num4 = i % 2;
			gameObject.transform.localPosition += new Vector3((float)num4 * num, (float)(-num3) * num2, 0f);
			WingmanVoicesSettingsItem component = gameObject.GetComponent<WingmanVoicesSettingsItem>();
			component.voiceProfile = voices[i];
			component.nameText.text = voices[i].name;
			component.checkObj.SetActive(voices[i].enabled);
			itemObjs.Add(gameObject);
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Ceil((float)voices.Count / 2f) * num2);
		scrollRect.verticalNormalizedPosition = 1f;
		GetComponentInParent<VRPointInteractableCanvas>().RefreshInteractables();
	}

	public void SaveAndClose()
	{
		if (GameSettings.enabledWingmanVoices == null)
		{
			GameSettings.enabledWingmanVoices = new List<string>();
		}
		else
		{
			GameSettings.enabledWingmanVoices.Clear();
		}
		foreach (WingmanVoiceProfile voice in voices)
		{
			if (voice.enabled)
			{
				GameSettings.enabledWingmanVoices.Add(voice.name);
			}
		}
		GameSettings.SaveGameSettings();
		base.gameObject.SetActive(value: false);
	}
}
