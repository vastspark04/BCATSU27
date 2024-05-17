using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using UnityEngine;
using UnityEngine.CrashReportHandler;

public static class VTLocalizationManager
{
	public class LocalizationInitBehaviour : MonoBehaviour
	{
		private void OnApplicationQuit()
		{
			VTLocalizationManager.OnApplicationQuit();
		}
	}

	public struct VTLanguage
	{
		public string language;

		public VTLanguage(string l)
		{
			language = l;
		}

		public override bool Equals(object obj)
		{
			if (obj is VTLanguage)
			{
				return ((VTLanguage)obj).language.Equals(language);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return language.GetHashCode();
		}
	}

	public class LocalizationData
	{
		public string key;

		public string description;

		public string english;

		public Dictionary<string, string> langDict = new Dictionary<string, string>();
	}

	private static string _currLang;

	public static string[] SupportedLanguages = new string[1] { "en" };

	private static bool localizationDirty = false;

	private static bool checkOverflow = false;

	private static bool hasInit = false;

	private static Dictionary<string, LocalizationData> localizationDictionary = new Dictionary<string, LocalizationData>();

	private const int ENGLISH_COL = 2;

	private static bool loadedLanguageNameDict = false;

	private static LangCodeDictionary langCodeDict;

	private static string csvTemplateResourceDir = "LocalizationTemplates/";

	private static SOText[] csvTemplates = null;

	public static string currentLanguage
	{
		get
		{
			if (!hasInit)
			{
				Init();
			}
			return _currLang;
		}
		set
		{
			_currLang = value;
		}
	}

	public static bool writeLocalizationDict { get; private set; }

	public static string localizationDir => Path.Combine(VTResources.gameRootDirectory, "Localization");

	public static string localizationFilepath => Path.Combine(localizationDir, "VTOLVR_Strings.csv");

	public static string localizationFileSeparatePrefix => "VTOLVR_Strings_";

	public static event Action<string> OnSetLangauge;

	public static Configuration GetCsvHelperConfig(string filepath)
	{
		return new Configuration
		{
			BadDataFound = delegate(ReadingContext context)
			{
				Debug.LogError($"Bad data found in file {filepath} (CurrentIndex = {context.CurrentIndex})");
			}
		};
	}

	public static void TryLoadSupportedLanguagesFromFile()
	{
		Debug.Log("VTLocalizationManager: trying to get supported languages from VTOLVR_Strings");
		if (!Directory.Exists(localizationDir))
		{
			Debug.Log(" - No localization directory.");
			return;
		}
		try
		{
			List<string> list = new List<string>();
			list.Add("en");
			if (File.Exists(localizationFilepath))
			{
				Debug.Log("- Found VTOLVR_Strings.csv");
				using CsvReader csvReader = new CsvReader(File.OpenText(localizationFilepath), GetCsvHelperConfig(localizationFilepath));
				csvReader.Read();
				csvReader.ReadHeader();
				bool flag = false;
				int num = 2;
				while (!flag)
				{
					if (csvReader.TryGetField(num, out string field) && !string.IsNullOrEmpty(field))
					{
						if (!list.Contains(field))
						{
							list.Add(field);
							Debug.LogFormat(" - {0}", field);
						}
						num++;
					}
					else
					{
						Debug.Log("- End of language headers at index: " + num);
						flag = true;
					}
				}
			}
			Debug.Log("Checking for other files");
			string[] files = Directory.GetFiles(localizationDir, localizationFileSeparatePrefix + "*", SearchOption.AllDirectories);
			foreach (string text in files)
			{
				if (text.EndsWith(".csv"))
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
					string text2 = fileNameWithoutExtension.Substring(localizationFileSeparatePrefix.Length, fileNameWithoutExtension.Length - localizationFileSeparatePrefix.Length);
					Debug.Log(" - Found a separate localization file with key: " + text2);
					if (list.Contains(text2))
					{
						Debug.Log(" - - already got this key from master file");
					}
					else
					{
						list.Add(text2);
					}
				}
			}
			if (list.Count > 0)
			{
				if (list[0] == "en")
				{
					SupportedLanguages = list.ToArray();
					Debug.Log(" - - SUCCESS: Loaded supported languages!");
				}
				else
				{
					Debug.Log(" - - FAILED to load supported languages (The first language must be English!!)");
				}
			}
			else
			{
				Debug.Log(" - - FAILED to load supported languages (No language headers found...");
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Exception thrown when trying to get supported languages from file: \n" + ex);
		}
	}

	public static void Init()
	{
		if (!hasInit)
		{
			hasInit = true;
			TryLoadSupportedLanguagesFromFile();
			GameSettings.TryGetGameSettingValue<bool>("checkLocalizationOverflow", out checkOverflow);
			bool val = false;
			if (GameSettings.TryGetGameSettingValue<bool>("writeLocalizationDict", out val))
			{
				writeLocalizationDict = val;
			}
			else
			{
				writeLocalizationDict = false;
			}
			if (writeLocalizationDict)
			{
				UnityEngine.Object.DontDestroyOnLoad(new GameObject("VTLocalizationManager").AddComponent<LocalizationInitBehaviour>());
			}
			currentLanguage = "en";
			LoadData(localizationFilepath);
		}
	}

	public static void LoadData()
	{
		LoadData(localizationFilepath);
	}

	public static void SaveData()
	{
		SaveData(localizationFilepath);
	}

	public static string GetString(string key, VTLanguage language)
	{
		return GetString(key, key, string.Empty, language.language);
	}

	public static string GetString(string key, string fallback, string description, string language, bool forceAddData = false)
	{
		if (key.Equals("en") && !writeLocalizationDict)
		{
			return fallback;
		}
		if (localizationDictionary != null)
		{
			if (localizationDictionary.TryGetValue(key, out var value))
			{
				if (writeLocalizationDict)
				{
					if (value.langDict.TryGetValue("en", out var value2))
					{
						if (string.IsNullOrEmpty(value2) || !value2.Equals(fallback))
						{
							value.langDict["en"] = fallback;
							Debug.LogFormat("Replaced existing english localization entry with new fallback.  Key({2}), Old({0}) New({1})", value2, fallback, key);
							localizationDirty = true;
						}
					}
					else
					{
						value.langDict.Add("en", fallback);
						localizationDirty = true;
					}
				}
				if (value.langDict.TryGetValue(language, out var value3))
				{
					value3?.Trim();
					if (!string.IsNullOrEmpty(value3))
					{
						if (checkOverflow)
						{
							return $"-{value3}-";
						}
						return value3;
					}
				}
			}
			else if (writeLocalizationDict || forceAddData)
			{
				LocalizationData localizationData = new LocalizationData();
				localizationData.key = key;
				localizationData.english = fallback;
				localizationData.description = description;
				localizationData.langDict.Add("en", fallback);
				localizationDictionary.Add(key, localizationData);
				localizationDirty = true;
			}
		}
		return fallback;
	}

	public static string GetString(string key, string fallback, string description)
	{
		return GetString(key, fallback, description, currentLanguage);
	}

	public static string GetString(string key, string fallback)
	{
		return GetString(key, fallback, string.Empty);
	}

	public static string GetString(string key)
	{
		return GetString(key, key);
	}

	public static void OnApplicationQuit()
	{
		if (localizationDirty && writeLocalizationDict)
		{
			Debug.Log("App quitting.  Writing dirty localization file: " + localizationFilepath);
			SaveData(localizationFilepath);
		}
	}

	public static void SetLanguage(string language)
	{
		language = language.ToLower();
		if (!SupportedLanguages.Contains(language))
		{
			Debug.LogErrorFormat("Tried to change language to '{0}' but that language is not supported!", language);
		}
		else if (language != currentLanguage)
		{
			currentLanguage = language;
			CrashReportHandler.SetUserMetadata("language", currentLanguage);
			Debug.LogFormat("Setting language to {0}", currentLanguage);
			VTLStaticStrings.ApplyLocalization();
			if (VTLocalizationManager.OnSetLangauge != null)
			{
				VTLocalizationManager.OnSetLangauge(language);
			}
			VTResources.ReLocalizeScenarioObjectives();
		}
	}

	private static void LoadData(string filepath)
	{
		LoadDataCSVHelper(filepath);
	}

	public static string GetTutorialTextKey(string text, string campaignID, string scenarioID)
	{
		return $"{campaignID}:{scenarioID}:{GetDeterministicHashCode(text)}";
	}

	private static int GetDeterministicHashCode(string str)
	{
		int num = 352654597;
		int num2 = num;
		for (int i = 0; i < str.Length; i += 2)
		{
			num = ((num << 5) + num) ^ str[i];
			if (i == str.Length - 1)
			{
				break;
			}
			num2 = ((num2 << 5) + num2) ^ str[i + 1];
		}
		return num + num2 * 1566083941;
	}

	private static void LoadDataCSVHelper(string filepath)
	{
		TryLoadSupportedLanguagesFromFile();
		Debug.Log("VTLocalizationManager loading data from CSV");
		localizationDictionary = new Dictionary<string, LocalizationData>();
		using (StringReader reader = new StringReader(GetCSVTemplate("VTOLVR_Strings").text))
		{
			using CsvReader reader2 = new CsvReader(reader, GetCsvHelperConfig(filepath + " (template)"));
			int count = 0;
			ReadOverrideData("en", reader2, ref count, countOverwritesOnly: false);
			Debug.LogFormat("- Loaded {0} keys from VTOLVR_Strings template", count);
		}
		if (File.Exists(filepath))
		{
			for (int i = 0; i < SupportedLanguages.Length; i++)
			{
				using CsvReader reader3 = new CsvReader(File.OpenText(filepath), GetCsvHelperConfig(filepath));
				string text = SupportedLanguages[i];
				if (text != "en")
				{
					int count2 = 0;
					ReadOverrideData(text, reader3, ref count2, countOverwritesOnly: true);
					Debug.LogFormat("- Overwrite {0} keys for {1} from VTOLVR_Strings.csv", count2, text);
				}
			}
		}
		else if (writeLocalizationDict)
		{
			Debug.LogError("VTLocalizationManager tried to load data but localization file does not exist! (Creating a new one) : " + filepath);
			SaveData(filepath);
		}
		int num = 0;
		if (Directory.Exists(localizationDir))
		{
			string[] files = Directory.GetFiles(localizationDir, localizationFileSeparatePrefix + "*", SearchOption.AllDirectories);
			foreach (string text2 in files)
			{
				if (text2.EndsWith(".csv"))
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text2);
					string text3 = fileNameWithoutExtension.Substring(localizationFileSeparatePrefix.Length, fileNameWithoutExtension.Length - localizationFileSeparatePrefix.Length);
					Debug.Log(" - Found a separate localization file with langCode: " + text3);
					int count3 = 0;
					using CsvReader reader4 = new CsvReader(File.OpenText(text2), GetCsvHelperConfig(text2));
					ReadOverrideData(text3, reader4, ref count3, countOverwritesOnly: true);
					Debug.LogFormat(" - - Overwrote {0} keys from {1}", count3, text2);
					num += count3;
				}
			}
		}
		if (num > 0)
		{
			Debug.LogFormat("Separate language files overwrote {0} existing strings", num);
		}
	}

	private static void ReadOverrideData(string langCode, CsvReader reader, ref int count, bool countOverwritesOnly)
	{
		reader.Read();
		reader.ReadHeader();
		int fieldIndex = reader.GetFieldIndex(langCode, 0, isTryGet: true);
		if (fieldIndex <= 0)
		{
			return;
		}
		while (reader.Read())
		{
			string field = reader.GetField(0);
			if (localizationDictionary.TryGetValue(field, out var value))
			{
				string text = reader.GetField(fieldIndex);
				if (text != null)
				{
					text = text.Trim();
				}
				if (!string.IsNullOrEmpty(text))
				{
					if (value.langDict.ContainsKey(langCode))
					{
						value.langDict[langCode] = text;
					}
					else
					{
						value.langDict.Add(langCode, text);
					}
					count++;
				}
			}
			else
			{
				value = new LocalizationData();
				value.key = field;
				value.description = reader.GetField(1);
				value.english = reader.GetField(2);
				value.langDict.Add(langCode, reader.GetField(fieldIndex));
				localizationDictionary.Add(value.key, value);
				if (!countOverwritesOnly)
				{
					count++;
				}
			}
		}
	}

	public static string[] ParseCSVLine(string s)
	{
		string[] array = null;
		using StringReader reader = new StringReader(s);
		using CsvParser csvParser = new CsvParser(reader);
		return csvParser.Read();
	}

	private static void SaveDataCSVHelper(string filepath)
	{
		TryLoadSupportedLanguagesFromFile();
		if (!File.Exists(filepath))
		{
			string directoryName = Path.GetDirectoryName(filepath);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.Create(filepath).Dispose();
		}
		using (StreamWriter writer = new StreamWriter(filepath, append: false))
		{
			using CsvWriter csvWriter = new CsvWriter(writer);
			csvWriter.WriteField("Key");
			csvWriter.WriteField("Description");
			string[] supportedLanguages = SupportedLanguages;
			foreach (string field in supportedLanguages)
			{
				csvWriter.WriteField(field);
			}
			csvWriter.NextRecord();
			foreach (LocalizationData value2 in localizationDictionary.Values)
			{
				csvWriter.WriteField(value2.key);
				csvWriter.WriteField(value2.description);
				supportedLanguages = SupportedLanguages;
				foreach (string key in supportedLanguages)
				{
					string value = string.Empty;
					value2.langDict.TryGetValue(key, out value);
					csvWriter.WriteField(value);
				}
				csvWriter.NextRecord();
			}
		}
		Debug.Log("Saved localization file.");
	}

	private static void SaveData(string filepath)
	{
		SaveDataCSVHelper(filepath);
	}

	private static int CountQuotes(string line)
	{
		int num = 0;
		for (int i = 0; i < line.Length; i++)
		{
			if (line[i] == '"' && (i >= line.Length - 1 || line[i + 1] != '"') && (i <= 0 || line[i - 1] != '"'))
			{
				num++;
			}
		}
		return num;
	}

	private static void LoadLanguageNameDict()
	{
		if (!loadedLanguageNameDict)
		{
			langCodeDict = Resources.Load<LangCodeDictionary>("lang_codes");
			loadedLanguageNameDict = true;
		}
	}

	public static string GetFullLanguageName(string languageCode)
	{
		LoadLanguageNameDict();
		return langCodeDict.GetNativeLanguageName(languageCode);
	}

	public static List<string> GetKnownLanguageCodes()
	{
		return langCodeDict.codes.Copy();
	}

	public static SOText[] GetCSVTemplates()
	{
		if (csvTemplates == null)
		{
			csvTemplates = Resources.LoadAll<SOText>(csvTemplateResourceDir);
		}
		return csvTemplates;
	}

	public static SOText GetCSVTemplate(string filename)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
		GetCSVTemplates();
		for (int i = 0; i < csvTemplates.Length; i++)
		{
			if (csvTemplates[i].name == fileNameWithoutExtension)
			{
				return csvTemplates[i];
			}
		}
		return null;
	}
}
