using System.Collections.Generic;
using UnityEngine;

public class SerializableDictionary<K, V> : ISerializationCallbackReceiver
{
	[SerializeField]
	private K[] keys;

	[SerializeField]
	private V[] values;

	public Dictionary<K, V> dictionary;

	public static T New<T>() where T : SerializableDictionary<K, V>, new()
	{
		return new T
		{
			dictionary = new Dictionary<K, V>()
		};
	}

	public void OnAfterDeserialize()
	{
		if (keys != null && values != null)
		{
			int num = keys.Length;
			dictionary = new Dictionary<K, V>(num);
			for (int i = 0; i < num; i++)
			{
				dictionary[keys[i]] = values[i];
			}
			keys = null;
			values = null;
		}
		else if (dictionary == null)
		{
			dictionary = new Dictionary<K, V>();
			keys = null;
			values = null;
		}
	}

	public void OnBeforeSerialize()
	{
		int count = dictionary.Count;
		keys = new K[count];
		values = new V[count];
		int num = 0;
		foreach (KeyValuePair<K, V> item in dictionary)
		{
			keys[num] = item.Key;
			values[num] = item.Value;
			num++;
		}
	}
}
