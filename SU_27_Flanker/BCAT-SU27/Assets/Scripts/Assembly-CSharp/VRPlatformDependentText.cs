using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class VRPlatformDependentText : MonoBehaviour, ILocalizationUser
{
	[Serializable]
	public class PlatformText
	{
		public ControllerStyles platform;

		[TextArea(3, 10)]
		public string text;
	}

	[TextArea]
	[Tooltip("Replaces [menu] with the VR_CONTROLLER_STYLE appropriate string (menu, B or Y, B, etc)")]
	public string textString;

	[Header("Deprecated")]
	public PlatformText[] platformTexts;

	private bool localized;

	private void Awake()
	{
		ApplyLocalization();
	}

	private string FinalString()
	{
		string newValue;
		switch (GameSettings.VR_CONTROLLER_STYLE)
		{
		default:
			newValue = "Menu button";
			break;
		case ControllerStyles.RiftTouch:
		case ControllerStyles.WMRStick:
		case ControllerStyles.ViveCosmos:
			newValue = "B or Y";
			break;
		case ControllerStyles.Index:
			newValue = "B";
			break;
		}
		return textString.Replace("[menu]", newValue);
	}

	public void ApplyLocalization()
	{
		string key = "vpdt_" + base.gameObject.name;
		if (Application.isPlaying && !localized)
		{
			localized = true;
			textString = VTLocalizationManager.GetString(key, textString, string.Empty);
		}
		else
		{
			VTLocalizationManager.GetString(key, textString, "[xxx] tags will be replaced by platform specific controls. Keep them.");
		}
	}

	private void Start()
	{
		GetComponent<Text>().text = FinalString();
	}
}
