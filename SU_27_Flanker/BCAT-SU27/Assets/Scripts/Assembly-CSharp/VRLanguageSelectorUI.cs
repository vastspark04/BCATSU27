using UnityEngine;
using UnityEngine.UI;

public class VRLanguageSelectorUI : MonoBehaviour
{
	public GameObject dropDownObj;

	public Text selectLanguageText;

	public ScrollRect langSelectorScroll;

	public GameObject languageTemplate;

	public GameObject quitButtonObj;

	private void Start()
	{
		Setup();
	}

	private void Setup()
	{
		selectLanguageText.text = VTLocalizationManager.GetFullLanguageName(VTLocalizationManager.currentLanguage);
		float num = ((RectTransform)languageTemplate.transform).rect.height * languageTemplate.transform.localScale.y;
		languageTemplate.SetActive(value: false);
		int num2 = 0;
		string[] supportedLanguages = VTLocalizationManager.SupportedLanguages;
		foreach (string text in supportedLanguages)
		{
			GameObject obj = Object.Instantiate(languageTemplate, langSelectorScroll.content);
			string _langCode = text;
			string fullLanguageName = VTLocalizationManager.GetFullLanguageName(text);
			obj.GetComponentInChildren<Text>().text = fullLanguageName;
			VRInteractable componentInChildren = obj.GetComponentInChildren<VRInteractable>();
			componentInChildren.interactableName = fullLanguageName;
			componentInChildren.OnInteract.AddListener(delegate
			{
				Dropdown_SelectLanguage(_langCode);
			});
			obj.SetActive(value: true);
			obj.transform.localPosition = new Vector3(0f, (0f - num) * (float)num2, 0f);
			num2++;
		}
		langSelectorScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num2 * num);
		langSelectorScroll.verticalNormalizedPosition = 1f;
		dropDownObj.SetActive(value: false);
	}

	public void SelectLanguageButton()
	{
		dropDownObj.SetActive(!dropDownObj.activeSelf);
		quitButtonObj.SetActive(!dropDownObj.activeSelf);
	}

	private void Dropdown_SelectLanguage(string langCode)
	{
		dropDownObj.SetActive(value: false);
		VTLocalizationManager.SetLanguage(langCode);
		GameSettings.SetGameSettingValue("LANGUAGE", langCode);
		selectLanguageText.text = VTLocalizationManager.GetFullLanguageName(VTLocalizationManager.currentLanguage);
		quitButtonObj.SetActive(value: true);
	}
}
