using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class VTTextFont : ScriptableObject
{
	[Serializable]
	public struct VTChar
	{
		public string character;

		public Vector2 uvPos;

		public Vector2 uvSize;
	}

	public Texture2D fontTexture;

	public Texture2D emissionTexture;

	public float spaceWidth;

	public VTChar[] chars;

	public Dictionary<char, VTChar> charsDict;

	[HideInInspector]
	public int chrCount;

	public void SetupCharsDict()
	{
		if (charsDict != null && (chars == null || chrCount == chars.Length))
		{
			return;
		}
		charsDict = new Dictionary<char, VTChar>();
		if (chars != null)
		{
			VTChar[] array = chars;
			for (int i = 0; i < array.Length; i++)
			{
				VTChar value = array[i];
				charsDict.Add(value.character.ToUpper()[0], value);
			}
		}
	}
}
