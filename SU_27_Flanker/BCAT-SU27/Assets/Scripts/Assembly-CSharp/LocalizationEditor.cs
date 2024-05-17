using System.Collections;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LocalizationEditor : MonoBehaviour
{
	public struct CSVLine
	{
		public string key;

		public string description;

		public string en;

		public string lang;
	}

	public LocalizationEditorMainMenu mainMenu;

	public Text[] langNameTexts;

	public Text filenameText;

	public ScrollRect entryScroll;

	public GameObject entryTemplate;

	public float entryMargin = 5f;

	public Text pageNumText;

	private float lineHeight;

	private Dictionary<string, InputField> keyToInputField;

	private string currFilepath;

	private string currLangCode;

	private int currPage;

	public int maxEntriesPerPage;

	private int numPages;

	private List<GameObject> entryObjs = new List<GameObject>();

	public InputField searchBar;

	public GameObject clearSearchButton;

	private string currSearch = string.Empty;

	private bool doNextSel;

	private Selectable nextSel;

	public void ClearSearch()
	{
		searchBar.text = string.Empty;
		Save(currFilepath);
		SetupForFile(currFilepath, currLangCode);
	}

	public void OnSearched(string s)
	{
		Save(currFilepath);
		SetupForFile(currFilepath, currLangCode, 0, selectFirst: true, s);
	}

	public void SetupForFile(string filepath, string langCode, int page = 0, bool selectFirst = true, string search = "")
	{
		bool flag = string.IsNullOrEmpty(search);
		if (flag)
		{
			clearSearchButton.SetActive(value: false);
		}
		else
		{
			search = search.ToLower();
			clearSearchButton.SetActive(value: true);
		}
		currSearch = search;
		foreach (GameObject entryObj in entryObjs)
		{
			Object.Destroy(entryObj);
		}
		entryObjs.Clear();
		currFilepath = filepath;
		currLangCode = langCode;
		currPage = page;
		keyToInputField = new Dictionary<string, InputField>();
		Text[] array = langNameTexts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].text = $"{VTLocalizationManager.GetFullLanguageName(langCode)} ({langCode})";
		}
		filenameText.text = Path.GetFileName(filepath);
		entryTemplate.gameObject.SetActive(value: false);
		lineHeight = entryMargin + ((RectTransform)entryTemplate.transform).rect.height * entryTemplate.transform.localScale.y;
		using CsvReader csvReader = new CsvReader(File.OpenText(filepath), VTLocalizationManager.GetCsvHelperConfig(filepath));
		csvReader.Read();
		csvReader.ReadHeader();
		int fieldIndex = csvReader.GetFieldIndex(langCode, 0, isTryGet: true);
		int num = 0;
		int num2 = 0;
		int num3 = page * maxEntriesPerPage;
		int num4 = (page + 1) * maxEntriesPerPage;
		LocalizationEditorEntry localizationEditorEntry = null;
		string field = string.Empty;
		string field2 = string.Empty;
		string text = string.Empty;
		string field3 = string.Empty;
		while (csvReader.Read())
		{
			bool flag2 = true;
			if (!flag)
			{
				text = csvReader.GetField(0);
				field = string.Empty;
				csvReader.TryGetField(1, out field);
				field2 = string.Empty;
				csvReader.TryGetField(2, out field2);
				field3 = string.Empty;
				if (fieldIndex > 0)
				{
					csvReader.TryGetField(langCode, out field3);
				}
				flag2 = field.ToLower().Contains(search) || field2.ToLower().Contains(search) || text.ToLower().Contains(search) || field3.ToLower().Contains(search);
			}
			if (num >= num3 && num < num4)
			{
				if (flag)
				{
					text = csvReader.GetField(0);
				}
				if (!keyToInputField.ContainsKey(text))
				{
					if (flag)
					{
						field = string.Empty;
						csvReader.TryGetField(1, out field);
						field2 = string.Empty;
						csvReader.TryGetField(2, out field2);
					}
					if (flag2)
					{
						flag2 = true;
						if (flag)
						{
							field3 = string.Empty;
							if (fieldIndex > 0)
							{
								csvReader.TryGetField(langCode, out field3);
							}
						}
						GameObject gameObject = Object.Instantiate(entryTemplate, entryScroll.content);
						entryObjs.Add(gameObject);
						LocalizationEditorEntry component = gameObject.GetComponent<LocalizationEditorEntry>();
						gameObject.SetActive(value: true);
						component.Setup(text, field, field2, field3);
						gameObject.transform.localPosition = new Vector3(0f, (float)(-num2) * lineHeight, 0f);
						keyToInputField.Add(text, component.langInput);
						if ((bool)localizationEditorEntry)
						{
							InputField langInput = localizationEditorEntry.langInput;
							Navigation navigation = default(Navigation);
							navigation.mode = Navigation.Mode.Explicit;
							navigation.selectOnUp = langInput.navigation.selectOnUp;
							navigation.selectOnDown = component.langInput;
							langInput.navigation = navigation;
							Navigation navigation2 = default(Navigation);
							navigation2.mode = Navigation.Mode.Explicit;
							navigation2.selectOnUp = langInput;
						}
						localizationEditorEntry = component;
						if (num2 == 0 && selectFirst)
						{
							component.langInput.Select();
							StartCoroutine(DelayedCaretPosToEnd(component.langInput));
						}
						num2++;
					}
				}
			}
			if (flag2)
			{
				num++;
			}
		}
		if (!selectFirst)
		{
			localizationEditorEntry.langInput.Select();
			StartCoroutine(DelayedCaretPosToEnd(localizationEditorEntry.langInput));
		}
		numPages = Mathf.Max(1, Mathf.CeilToInt((float)num / (float)maxEntriesPerPage));
		pageNumText.text = $"{currPage + 1}/{numPages}";
		entryScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineHeight * (float)num2);
		entryScroll.verticalNormalizedPosition = 1f;
	}

	private void Save(string filepath)
	{
		List<CSVLine> list = new List<CSVLine>();
		using (CsvReader csvReader = new CsvReader(File.OpenText(filepath), VTLocalizationManager.GetCsvHelperConfig(filepath)))
		{
			csvReader.Read();
			csvReader.ReadHeader();
			int fieldIndex = csvReader.GetFieldIndex(currLangCode, 0, isTryGet: true);
			while (csvReader.Read())
			{
				string field = csvReader.GetField(0);
				string field2 = string.Empty;
				csvReader.TryGetField(1, out field2);
				string field3 = string.Empty;
				csvReader.TryGetField(2, out field3);
				string field4 = string.Empty;
				if (keyToInputField.TryGetValue(field, out var value))
				{
					field4 = value.text;
				}
				else if (fieldIndex > 0)
				{
					csvReader.TryGetField(fieldIndex, out field4);
				}
				list.Add(new CSVLine
				{
					key = field,
					description = field2,
					en = field3,
					lang = field4
				});
			}
		}
		using StreamWriter writer = new StreamWriter(filepath, append: false);
		using CsvWriter csvWriter = new CsvWriter(writer);
		csvWriter.WriteField("Key");
		csvWriter.WriteField("Description");
		csvWriter.WriteField("en");
		csvWriter.WriteField(currLangCode);
		csvWriter.NextRecord();
		foreach (CSVLine item in list)
		{
			csvWriter.WriteField(item.key);
			csvWriter.WriteField(item.description);
			csvWriter.WriteField(item.en);
			csvWriter.WriteField(item.lang);
			csvWriter.NextRecord();
		}
	}

	private void Update()
	{
		if (doNextSel)
		{
			if ((bool)nextSel)
			{
				nextSel.Select();
				float num = lineHeight * (((float)maxEntriesPerPage + 1f) / (float)maxEntriesPerPage) / entryScroll.content.rect.height;
				entryScroll.verticalNormalizedPosition -= num;
				InputField field = (InputField)nextSel;
				StartCoroutine(DelayedCaretPosToEnd(field));
			}
			else
			{
				NextPage();
			}
			doNextSel = false;
		}
	}

	private void OnGUI()
	{
		if (!Event.current.isKey || Event.current.type != EventType.KeyDown || !EventSystem.current.currentSelectedGameObject)
		{
			return;
		}
		InputField component = EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
		if ((bool)component)
		{
			bool num = Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter;
			bool flag = !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift);
			if (num && flag)
			{
				Event.current.Use();
				nextSel = component.FindSelectableOnDown();
				doNextSel = true;
				EventSystem.current.SetSelectedGameObject(null);
			}
		}
	}

	public void NextPage()
	{
		if (currPage < numPages - 1)
		{
			Save(currFilepath);
			SetupForFile(currFilepath, currLangCode, currPage + 1, selectFirst: true, currSearch);
		}
	}

	public void PrevPage()
	{
		if (currPage > 0)
		{
			Save(currFilepath);
			SetupForFile(currFilepath, currLangCode, currPage - 1, selectFirst: false, currSearch);
			entryScroll.verticalNormalizedPosition = 0f;
		}
	}

	public void SaveButton()
	{
		Save(currFilepath);
	}

	public void SaveAndExitButton()
	{
		SaveButton();
		VTLocalizationManager.LoadData();
		mainMenu.BackToLang(currLangCode);
	}

	private IEnumerator DelayedCaretPosToEnd(InputField field)
	{
		yield return null;
		field.MoveTextEnd(shift: false);
	}
}
