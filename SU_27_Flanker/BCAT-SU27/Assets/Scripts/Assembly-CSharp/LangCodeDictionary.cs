using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Localization/LangCodeDictionary")]
public class LangCodeDictionary : ScriptableObject
{
	public List<string> codes;

	public List<string> names;

	public List<string> nativeNames;

	private Dictionary<string, string> dict;

	private Dictionary<string, string> nativeDict;

	public string GetLanguageName(string langCode)
	{
		if (dict == null)
		{
			dict = new Dictionary<string, string>();
			for (int i = 0; i < codes.Count; i++)
			{
				dict.Add(codes[i], names[i]);
			}
		}
		if (dict.TryGetValue(langCode, out var value))
		{
			return value;
		}
		return langCode;
	}

	public string GetNativeLanguageName(string langCode)
	{
		if (nativeDict == null)
		{
			nativeDict = new Dictionary<string, string>();
			for (int i = 0; i < codes.Count; i++)
			{
				nativeDict.Add(codes[i], nativeNames[i]);
			}
		}
		if (nativeDict.TryGetValue(langCode, out var value))
		{
			if (string.IsNullOrEmpty(value))
			{
				return GetLanguageName(langCode);
			}
			return value;
		}
		return langCode;
	}
}
