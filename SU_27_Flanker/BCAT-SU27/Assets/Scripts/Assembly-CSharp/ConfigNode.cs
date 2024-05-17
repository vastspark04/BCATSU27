using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ConfigNode
{
	public struct ConfigValue
	{
		public string name;

		public string value;

		public ConfigValue(string name, string value)
		{
			this.name = name;
			this.value = value;
		}
	}

	public string name;

	private Dictionary<string, string> values;

	private List<ConfigNode> nodes;

	private const string SPECIAL_CHARS = "={}<>\"";

	private static int SPECIAL_CHARS_COUNT = "={}<>\"".Length;

	public ConfigNode()
	{
		name = "NODE";
		values = new Dictionary<string, string>();
		nodes = new List<ConfigNode>();
	}

	public ConfigNode(string name)
	{
		this.name = name;
		values = new Dictionary<string, string>();
		nodes = new List<ConfigNode>();
	}

	public bool HasNode(string name)
	{
		foreach (ConfigNode node in nodes)
		{
			if (node.name == name)
			{
				return true;
			}
		}
		return false;
	}

	public void RemoveNodes(string nodeName)
	{
		nodes.RemoveAll((ConfigNode x) => x.name == nodeName);
	}

	public ConfigNode GetNode(string name)
	{
		for (int i = 0; i < nodes.Count; i++)
		{
			if (nodes[i].name == name)
			{
				return nodes[i];
			}
		}
		return null;
	}

	public List<ConfigNode> GetNodes()
	{
		return nodes.Copy();
	}

	public List<ConfigNode> GetNodes(string name)
	{
		List<ConfigNode> list = new List<ConfigNode>();
		for (int i = 0; i < nodes.Count; i++)
		{
			if (nodes[i].name == name)
			{
				list.Add(nodes[i]);
			}
		}
		return list;
	}

	public bool HasValue(string name)
	{
		return values.ContainsKey(name);
	}

	public string GetValue(string name)
	{
		return values[name];
	}

	public bool TryGetValue<T>(string name, out T value)
	{
		if (values.TryGetValue(name, out var value2))
		{
			value = ConfigNodeUtils.ParseObject<T>(value2);
			return true;
		}
		value = default(T);
		return false;
	}

	public T GetValue<T>(string name)
	{
		return ConfigNodeUtils.ParseObject<T>(GetValue(name));
	}

	public List<ConfigValue> GetValues()
	{
		List<ConfigValue> list = new List<ConfigValue>();
		foreach (string key in values.Keys)
		{
			list.Add(new ConfigValue(key, values[key]));
		}
		return list;
	}

	private void SetValueString(string name, string value)
	{
		if (values.ContainsKey(name))
		{
			values[name] = value;
		}
		else
		{
			values.Add(name, value);
		}
	}

	public void SetValue<T>(string name, T value)
	{
		string value2 = ConfigNodeUtils.WriteObject(typeof(T), value);
		SetValueString(name, value2);
	}

	public void AddNode(ConfigNode node)
	{
		nodes.Add(node);
	}

	public ConfigNode AddNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		nodes.Add(configNode);
		return configNode;
	}

	public ConfigNode AddOrGetNode(string nodeName)
	{
		ConfigNode configNode = GetNode(nodeName);
		if (configNode == null)
		{
			configNode = AddNode(nodeName);
		}
		return configNode;
	}

	public void ClearNode()
	{
		if (values == null)
		{
			values = new Dictionary<string, string>();
		}
		else
		{
			values.Clear();
		}
		if (nodes == null)
		{
			nodes = new List<ConfigNode>();
		}
		else
		{
			nodes.Clear();
		}
	}

	public void SaveToFile(string filePath)
	{
		File.WriteAllLines(filePath, new string[1] { WriteNode(this, 0) });
	}

	public static ConfigNode LoadFromFile(string filePath, bool logErrors = true)
	{
		filePath = filePath.Replace('\\', '/');
		try
		{
			return ParseNode(File.ReadAllText(filePath));
		}
		catch (Exception message)
		{
			if (logErrors)
			{
				Debug.LogError(message);
			}
			return null;
		}
	}

	public static string WriteNode(ConfigNode rootNode, int indent)
	{
		string empty = string.Empty;
		empty += GetIndent(indent);
		empty += rootNode.name;
		empty = empty + "\n" + GetIndent(indent) + "{";
		foreach (ConfigValue value in rootNode.GetValues())
		{
			empty = empty + "\n" + GetIndent(indent + 1);
			string text = value.name;
			string text2 = value.value;
			if (!string.IsNullOrEmpty(text2))
			{
				text2 = text2.Replace("\n", "///n");
				if (HasSpecialChars(text) || HasSpecialChars(text2))
				{
					text = "{" + text + "}";
					text2 = "{" + text2 + "}";
				}
			}
			empty = empty + text + " = " + text2;
		}
		empty += "\n";
		foreach (ConfigNode node in rootNode.nodes)
		{
			empty += WriteNode(node, indent + 1);
		}
		empty = empty + "\n" + GetIndent(indent) + "}\n";
		return empty.Replace("\n\n", "\n");
	}

	public static ConfigNode ParseNode(string nodeString)
	{
		ConfigNode configNode = new ConfigNode();
		string[] array = nodeString.Split(new string[1] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
		configNode.name = array[0].Replace(" ", string.Empty).Replace("\t", string.Empty);
		int num = 0;
		for (int i = 2; i < array.Length; i++)
		{
			string text = array[i];
			if (text.Contains("="))
			{
				if (text.Contains("{"))
				{
					int num2 = 0;
					int num3 = 0;
					int num4 = 0;
					int num5 = 0;
					int num6 = 0;
					bool flag = false;
					for (int j = 0; j < text.Length; j++)
					{
						if (text[j] == '{')
						{
							if (num6 == 0)
							{
								if (flag)
								{
									num4 = j + 1;
								}
								else
								{
									num2 = j + 1;
								}
							}
							num6++;
						}
						else
						{
							if (text[j] != '}')
							{
								continue;
							}
							num6--;
							if (num6 == 0)
							{
								if (flag)
								{
									num5 = j;
									continue;
								}
								num3 = j;
								flag = true;
							}
						}
					}
					string text2 = text.Substring(num2, num3 - num2);
					string text3 = text.Substring(num4, num5 - num4);
					text3 = text3.Replace("///n", "\n");
					configNode.SetValueString(text2, text3);
				}
				else
				{
					text = text.Replace("= ", "=");
					string[] array2 = text.Split('=');
					string text4 = array2[0].Replace(" ", string.Empty).Replace("\t", string.Empty);
					string text5 = array2[1];
					text5 = text5.Replace("///n", "\n");
					configNode.SetValueString(text4, text5);
				}
				continue;
			}
			num = i;
			break;
		}
		if (array[num].Contains("}"))
		{
			return configNode;
		}
		int num7 = 0;
		int num8 = -1;
		for (int k = num; k < array.Length; k++)
		{
			if (array[k].Contains("="))
			{
				continue;
			}
			if (array[k].Contains("{"))
			{
				num7++;
				if (num7 == 1)
				{
					num8 = k - 1;
				}
			}
			else if (array[k].Contains("}"))
			{
				if (num7 == 1 && num8 >= 0)
				{
					int num9 = k + 1;
					string nodeString2 = string.Join("\n", array, num8, num9 - num8);
					configNode.AddNode(ParseNode(nodeString2));
					num8 = -1;
				}
				num7--;
			}
		}
		return configNode;
	}

	private static string GetIndent(int indent)
	{
		string text = string.Empty;
		for (int i = 0; i < indent; i++)
		{
			text += "\t";
		}
		return text;
	}

	private static bool HasSpecialChars(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return false;
		}
		int length = s.Length;
		for (int i = 0; i < length; i++)
		{
			for (int j = 0; j < SPECIAL_CHARS_COUNT; j++)
			{
				if (s[i] == "={}<>\""[j])
				{
					return true;
				}
			}
		}
		return false;
	}
}
