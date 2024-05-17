using UnityEngine;
using UnityEngine.UI;

public class MFDHardpointInfo : MonoBehaviour, ILocalizationUser
{
	public Text itemNameText;

	public Text countText;

	public GameObject jettisonObject;

	public GameObject armedObject;

	public HPEquippable equip;

	private string s_NONE;

	public void ApplyLocalization()
	{
		s_NONE = VTLocalizationManager.GetString("NONE");
	}

	private void Awake()
	{
		ApplyLocalization();
	}

	public void UpdateDisplay()
	{
		if ((bool)equip)
		{
			itemNameText.text = equip.shortName;
			countText.gameObject.SetActive(value: true);
			countText.text = "x" + equip.GetCount();
			jettisonObject.SetActive(equip.markedForJettison);
			armedObject.SetActive(equip.armed);
		}
		else
		{
			itemNameText.text = s_NONE;
			countText.gameObject.SetActive(value: false);
			jettisonObject.SetActive(value: false);
			armedObject.SetActive(value: false);
		}
	}
}
