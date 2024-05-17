using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class PilotColorSetup : MonoBehaviour
{
	public struct ColorScheme
	{
		public Color suitColor;

		public Color vestColor;

		public Color strapsColor;

		public Color gSuitColor;

		public Color skinColor;

		public override string ToString()
		{
			return ConfigNodeUtils.WriteList(new List<Color> { suitColor, vestColor, strapsColor, gSuitColor, skinColor });
		}

		public static ColorScheme FromString(string s)
		{
			List<Color> list = ConfigNodeUtils.ParseList<Color>(s);
			if (list.Count == 5)
			{
				ColorScheme result = default(ColorScheme);
				result.suitColor = list[0];
				result.vestColor = list[1];
				result.strapsColor = list[2];
				result.gSuitColor = list[3];
				result.skinColor = list[4];
				return result;
			}
			Debug.LogError("ColorScheme could not parse.  It had an invalid number of colors: " + s);
			return default(ColorScheme);
		}
	}

	public Renderer[] pilotRenderers;

	public bool autoApplyLocal = true;

	public bool applyLocalInMP = true;

	private void Start()
	{
		if (autoApplyLocal && (applyLocalInMP || !VTOLMPUtils.IsMultiplayer()))
		{
			UpdatePropertiesLocal();
		}
	}

	public void UpdateProperties(ColorScheme colors)
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		int nameID = Shader.PropertyToID("_BaseColor");
		int nameID2 = Shader.PropertyToID("_ColorB");
		int nameID3 = Shader.PropertyToID("_ColorG");
		int nameID4 = Shader.PropertyToID("_ColorR");
		int nameID5 = Shader.PropertyToID("_SkinColor");
		materialPropertyBlock.SetColor(nameID, colors.suitColor);
		materialPropertyBlock.SetColor(nameID2, colors.vestColor);
		materialPropertyBlock.SetColor(nameID3, colors.strapsColor);
		materialPropertyBlock.SetColor(nameID4, colors.gSuitColor);
		materialPropertyBlock.SetColor(nameID5, colors.skinColor);
		for (int i = 0; i < pilotRenderers.Length; i++)
		{
			if ((bool)pilotRenderers[i])
			{
				pilotRenderers[i].SetPropertyBlock(materialPropertyBlock);
			}
		}
	}

	public void UpdatePropertiesLocal()
	{
		if (PilotSaveManager.current != null)
		{
			ColorScheme colorScheme = default(ColorScheme);
			colorScheme.suitColor = PilotSaveManager.current.suitColor;
			colorScheme.vestColor = PilotSaveManager.current.vestColor;
			colorScheme.strapsColor = PilotSaveManager.current.strapsColor;
			colorScheme.gSuitColor = PilotSaveManager.current.gSuitColor;
			colorScheme.skinColor = PilotSaveManager.current.skinColor;
			ColorScheme colors = colorScheme;
			UpdateProperties(colors);
		}
		else
		{
			Debug.LogError("Current pilot save is null! Unable to apply colors.");
		}
	}
}
