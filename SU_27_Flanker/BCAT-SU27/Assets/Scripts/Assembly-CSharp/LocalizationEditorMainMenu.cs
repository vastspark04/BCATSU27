using System.Collections.Generic;
using System.IO;
using CsvHelper;
using UnityEngine;
using UnityEngine.UI;

public class LocalizationEditorMainMenu : MonoBehaviour
{
	public LocalizationEditor editor;

	public GameObject displayObj;

	public GameObject editLangTemplate;

	public ScrollRect editLangScroll;

	public GameObject editFileTemplate;

	public ScrollRect editFileScroll;

	public Text selectedLanguageText;

	public LocalizationEditorNewLang newLangMenu;

	private List<GameObject> editLangObjs = new List<GameObject>();

	private List<GameObject> editFileObjs = new List<GameObject>();

	private void Awake()
	{
		editLangTemplate.SetActive(value: false);
		editFileTemplate.SetActive(value: false);
	}

	private void Start()
	{
		SetupLanguages();
	}

	private void SetupLanguages(bool clearFiles = true)
	{
		VTLocalizationManager.TryLoadSupportedLanguagesFromFile();
		if (clearFiles)
		{
			ClearFiles();
		}
		foreach (GameObject editLangObj in editLangObjs)
		{
			Object.Destroy(editLangObj);
		}
		editLangObjs.Clear();
		float num = ((RectTransform)editLangTemplate.transform).rect.height * editLangTemplate.transform.localScale.y;
		int i;
		for (i = 0; i < VTLocalizationManager.SupportedLanguages.Length - 1; i++)
		{
			string existLang = VTLocalizationManager.SupportedLanguages[i + 1];
			string fullLanguageName = VTLocalizationManager.GetFullLanguageName(existLang);
			GameObject gameObject = Object.Instantiate(editLangTemplate, editLangScroll.content);
			gameObject.SetActive(value: true);
			editLangObjs.Add(gameObject);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * num, 0f);
			gameObject.GetComponentInChildren<Text>().text = fullLanguageName;
			gameObject.GetComponentInChildren<Button>().onClick.AddListener(delegate
			{
				ClickedEdit(existLang);
			});
		}
		editLangScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)i * num);
		editLangScroll.verticalNormalizedPosition = 1f;
	}

	private void ClearFiles()
	{
		foreach (GameObject editFileObj in editFileObjs)
		{
			Object.Destroy(editFileObj);
		}
		editFileObjs.Clear();
		selectedLanguageText.text = string.Empty;
	}

	private void ClickedEdit(string langCode)
	{
		string lc = langCode;
		ClearFiles();
		selectedLanguageText.text = $"{VTLocalizationManager.GetFullLanguageName(langCode)} ({langCode})";
		SOText[] cSVTemplates = VTLocalizationManager.GetCSVTemplates();
		int num = 0;
		float num2 = ((RectTransform)editFileTemplate.transform).rect.height * editFileTemplate.transform.localScale.y;
		SOText[] array = cSVTemplates;
		foreach (SOText sOText in array)
		{
			string text = sOText.name + "_" + langCode + ".csv";
			string text2 = Path.Combine(VTLocalizationManager.localizationDir, langCode);
			if (!Directory.Exists(text2))
			{
				Directory.CreateDirectory(text2);
			}
			string filepath = Path.Combine(text2, text);
			if (!File.Exists(filepath))
			{
				File.WriteAllText(filepath, sOText.text);
			}
			UpdateFile(filepath, sOText, langCode);
			GameObject gameObject = Object.Instantiate(editFileTemplate, editFileScroll.content);
			editFileObjs.Add(gameObject);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-num) * num2, 0f);
			gameObject.SetActive(value: true);
			gameObject.GetComponentInChildren<Text>().text = text;
			gameObject.GetComponentInChildren<Button>().onClick.AddListener(delegate
			{
				ClickedEditFile(filepath, lc);
			});
			num++;
		}
		editFileScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num * num2);
	}

	private void ClickedEditFile(string filepath, string langCode)
	{
		displayObj.SetActive(value: false);
		editor.gameObject.SetActive(value: true);
		editor.SetupForFile(filepath, langCode);
	}

	public void ReturnToMain()
	{
		displayObj.SetActive(value: true);
		SetupLanguages();
	}

	public void BackToLang(string langCode)
	{
		displayObj.SetActive(value: true);
		editor.gameObject.SetActive(value: false);
		SetupLanguages();
		ClickedEdit(langCode);
	}

	private void UpdateFile(string filepath, SOText template, string langCode)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		using (CsvReader csvReader = new CsvReader(File.OpenText(filepath), VTLocalizationManager.GetCsvHelperConfig(filepath)))
		{
			csvReader.Read();
			csvReader.ReadHeader();
			int fieldIndex = csvReader.GetFieldIndex(langCode, 0, isTryGet: true);
			while (csvReader.Read())
			{
				string field = csvReader.GetField(0);
				string field2 = string.Empty;
				if (fieldIndex > 0)
				{
					csvReader.TryGetField(fieldIndex, out field2);
				}
				if (!dictionary.ContainsKey(field))
				{
					dictionary.Add(field, field2);
				}
			}
		}
		List<LocalizationEditor.CSVLine> list = new List<LocalizationEditor.CSVLine>();
		using (CsvReader csvReader2 = new CsvReader(new StringReader(template.text), VTLocalizationManager.GetCsvHelperConfig(filepath + " (template)")))
		{
			csvReader2.Read();
			csvReader2.ReadHeader();
			csvReader2.GetFieldIndex(langCode, 0, isTryGet: true);
			while (csvReader2.Read())
			{
				string field3 = csvReader2.GetField(0);
				string field4 = string.Empty;
				csvReader2.TryGetField(1, out field4);
				string field5 = string.Empty;
				csvReader2.TryGetField(2, out field5);
				string lang = string.Empty;
				if (dictionary.TryGetValue(field3, out var value))
				{
					lang = value;
				}
				list.Add(new LocalizationEditor.CSVLine
				{
					key = field3,
					description = field4,
					en = field5,
					lang = lang
				});
			}
		}
		using StreamWriter writer = new StreamWriter(filepath, append: false);
		using CsvWriter csvWriter = new CsvWriter(writer);
		csvWriter.WriteField("Key");
		csvWriter.WriteField("Description");
		csvWriter.WriteField("en");
		csvWriter.WriteField(langCode);
		csvWriter.NextRecord();
		foreach (LocalizationEditor.CSVLine item in list)
		{
			csvWriter.WriteField(item.key);
			csvWriter.WriteField(item.description);
			csvWriter.WriteField(item.en);
			csvWriter.WriteField(item.lang);
			csvWriter.NextRecord();
		}
	}

	public void NewLanguageButton()
	{
		newLangMenu.Open(OnSelNewLang);
		displayObj.SetActive(value: false);
	}

	private void OnSelNewLang(string langCode)
	{
		displayObj.SetActive(value: true);
		if (!string.IsNullOrEmpty(langCode))
		{
			ClickedEdit(langCode);
			SetupLanguages(clearFiles: false);
		}
		else
		{
			SetupLanguages();
		}
	}

	public void ReturnToGame()
	{
		LoadingSceneController.LoadSceneImmediate(1);
	}
}
