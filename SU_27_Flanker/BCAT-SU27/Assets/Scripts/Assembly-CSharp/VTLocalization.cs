using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using UnityEngine;

public class VTLocalization : MonoBehaviour
{
	private class ELocData
	{
		public string key;

		public string description;

		public Dictionary<string, string> texts;
	}

	private struct TranslationData
	{
		public string key;

		public string text;
	}

	public string filename;

	[Header("Regeneration")]
	public bool regenerateWithExistingKeys;

	[Header("New Generation")]
	public bool uiText;

	public bool vtText;

	public bool vrInteractable;

	public bool textMeshPro;

	private bool checkOverflow;

	public string setToLanguage;

	private const int ENGLISH_COL = 2;

	private Dictionary<string, List<VTLocalizationComponentKey>> locKeys = new Dictionary<string, List<VTLocalizationComponentKey>>();

	private char[] letters = "abcdefghijklmnopqrstuvqxyz".ToCharArray();

	private void Awake()
	{
		GameSettings.TryGetGameSettingValue<bool>("checkLocalizationOverflow", out checkOverflow);
		VTLocalizationManager.OnSetLangauge += SetToLanguage;
		if (VTLocalizationManager.currentLanguage != "en")
		{
			SetToLanguage(VTLocalizationManager.currentLanguage);
		}
	}

	private void OnDestroy()
	{
		VTLocalizationManager.OnSetLangauge -= SetToLanguage;
	}

	private void ApplyTextWithReader(CsvReader reader, string language)
	{
		reader.Read();
		reader.ReadHeader();
		int fieldIndex = reader.GetFieldIndex(language, 0, isTryGet: true);
		if (fieldIndex == -1)
		{
			Debug.LogErrorFormat(base.gameObject, "VTLocalization tried to set language to {0}, but that language is not found in the base localization file.", language);
			return;
		}
		while (reader.Read())
		{
			if (!reader.TryGetField(fieldIndex, out string field))
			{
				field = reader.GetField(2);
			}
			TranslationData translationData = default(TranslationData);
			translationData.key = reader.GetField(0);
			translationData.text = field;
			TranslationData data = translationData;
			ApplyText(data);
		}
	}

	public void SetToLanguage()
	{
		SetToLanguage(setToLanguage);
	}

	public void SetToLanguage(string language)
	{
		if (string.IsNullOrEmpty(language) || !Directory.Exists(VTLocalizationManager.localizationDir))
		{
			return;
		}
		locKeys = new Dictionary<string, List<VTLocalizationComponentKey>>();
		VTLocalizationComponentKey[] componentsInChildren = GetComponentsInChildren<VTLocalizationComponentKey>(includeInactive: true);
		foreach (VTLocalizationComponentKey vTLocalizationComponentKey in componentsInChildren)
		{
			if (!locKeys.TryGetValue(vTLocalizationComponentKey.key, out var value))
			{
				value = new List<VTLocalizationComponentKey>();
				locKeys.Add(vTLocalizationComponentKey.key, value);
			}
			value.Add(vTLocalizationComponentKey);
		}
		string text = Path.Combine(VTLocalizationManager.localizationDir, filename);
		if (File.Exists(text))
		{
			using CsvReader reader = new CsvReader(File.OpenText(text), VTLocalizationManager.GetCsvHelperConfig(text));
			ApplyTextWithReader(reader, language);
		}
		else
		{
			Debug.LogErrorFormat("Base (en) localization file does not exist! ({0}) loading from template.", text);
			using StringReader reader2 = new StringReader(VTLocalizationManager.GetCSVTemplate(filename).text);
			using CsvReader reader3 = new CsvReader(reader2);
			ApplyTextWithReader(reader3, language);
		}
		string searchPattern = Path.GetFileNameWithoutExtension(filename) + "_" + language + ".csv";
		string text2 = string.Empty;
		DateTime dateTime = DateTime.MinValue;
		string[] files = Directory.GetFiles(VTLocalizationManager.localizationDir, searchPattern, SearchOption.AllDirectories);
		foreach (string text3 in files)
		{
			DateTime dateTime2 = VTResources.SafelyGetLastWriteTime(text3);
			if (dateTime2 > dateTime)
			{
				dateTime = dateTime2;
				text2 = text3;
			}
		}
		if (string.IsNullOrEmpty(text2) || !File.Exists(text2))
		{
			return;
		}
		Debug.Log("- Found separate language file!");
		using CsvReader csvReader = new CsvReader(File.OpenText(text2), VTLocalizationManager.GetCsvHelperConfig(text2));
		csvReader.Read();
		csvReader.ReadHeader();
		int fieldIndex = csvReader.GetFieldIndex(language, 0, isTryGet: true);
		if (fieldIndex == -1)
		{
			Debug.LogErrorFormat(base.gameObject, "VTLocalization tried to set language to {0}, but that language is not found in the separate localization file.", language);
			return;
		}
		while (csvReader.Read())
		{
			if (csvReader.TryGetField(fieldIndex, out string field))
			{
				TranslationData translationData = default(TranslationData);
				translationData.key = csvReader.GetField(0);
				translationData.text = field;
				TranslationData data = translationData;
				ApplyText(data);
			}
		}
	}

	private void ApplyText(TranslationData data)
	{
		if (!locKeys.TryGetValue(data.key, out var value))
		{
			return;
		}
		string text = data.text;
		if (checkOverflow)
		{
			text = $"{text}$";
		}
		foreach (VTLocalizationComponentKey item in value)
		{
			item.SetText(text);
		}
	}

	private bool DoesStringContainLetters(string s, int count = 1)
	{
		int num = 0;
		char[] array = s.ToLower().ToCharArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (letters.Contains(array[i]))
			{
				num++;
				if (num >= count)
				{
					return true;
				}
			}
		}
		return false;
	}
}
