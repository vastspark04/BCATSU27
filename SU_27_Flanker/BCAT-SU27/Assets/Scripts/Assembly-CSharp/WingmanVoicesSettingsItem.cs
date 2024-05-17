using UnityEngine;
using UnityEngine.UI;

public class WingmanVoicesSettingsItem : MonoBehaviour
{
	public Text nameText;

	public GameObject checkObj;

	public WingmanVoiceProfile voiceProfile;

	public void ClickToggleButton()
	{
		voiceProfile.enabled = !voiceProfile.enabled;
		checkObj.SetActive(voiceProfile.enabled);
	}

	public void ClickSampleButton()
	{
		voiceProfile.PlayRandomMessage();
	}
}
