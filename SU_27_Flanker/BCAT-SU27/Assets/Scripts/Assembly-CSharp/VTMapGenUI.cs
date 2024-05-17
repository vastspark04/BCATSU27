using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class VTMapGenUI : MonoBehaviour
{
	public Transform previewCamTf;

	public VTMapGenSaveUI saveUI;

	public ScrollRect propertiesScrollRect;

	public VTEdPropertyFieldTemplates propTemplates;

	public VTMapGenerator previewGenerator;

	public VTEnumProperty mapTypeField;

	public InputField noiseSeedField;

	public VTEnumProperty biomeProp;

	private MapGenBiome.Biomes biome;

	public VTMapEditorMenu editorMenu;

	[Header("SizeEdit")]
	public VTFloatRangeProperty sizeRangeField;

	public Text sizeDisplayText;

	public int minSize = 8;

	public int maxSize = 64;

	private int finalMapSize;

	private int newGridSize;

	[Header("Saving Heightmap")]
	public VTTSaveHeightmapProgressWindow hmSaveProgressWindow;

	[Header("Loading Heightmap")]
	public VTEdResourceBrowser resourceBrowser;

	public GameObject loadHeightmapObject;

	public Text loadedHeightmapText;

	public VTEnumProperty edgeModeProp;

	public VTEnumProperty coastSideProp;

	private CardinalDirections coastSide = CardinalDirections.West;

	private VTMapGenerator.EdgeModes edgeMode;

	public VTFloatRangeProperty minHeightProp;

	public VTFloatRangeProperty maxHeightProp;

	private float minHeight = -80f;

	private float maxHeight = 6000f;

	private string noiseSeed;

	private VTMapGenerator.VTMapTypes mapType;

	private ConfigNode terrainSettings;

	private Dictionary<string, VTPropertyField> propertyFields = new Dictionary<string, VTPropertyField>();

	public Texture2D savedHeightmap;

	private VTMapCustom.AsyncSaveOp heightmapSaveOp;

	private Coroutine heightSaveRoutine;

	private bool mapDirty;

	private float mapDirtyTime;

	private Texture2D origHMTex;

	private void Awake()
	{
		loadHeightmapObject.SetActive(value: false);
		loadedHeightmapText.text = string.Empty;
	}

	private void Start()
	{
		mapTypeField.OnPropertyValueChanged += OnSetMapType;
		sizeRangeField.OnPropertyValueChanged += OnSetMapSize;
		edgeModeProp.OnPropertyValueChanged += EdgeModeProp_OnPropertyValueChanged;
		coastSideProp.OnPropertyValueChanged += CoastSideProp_OnPropertyValueChanged;
		coastSideProp.SetInitialValue(coastSide);
		minHeightProp.OnPropertyValueChanged += MinHeightProp_OnPropertyValueChanged;
		maxHeightProp.OnPropertyValueChanged += MaxHeightProp_OnPropertyValueChanged;
		biomeProp.OnPropertyValueChanged += BiomeProp_OnPropertyValueChanged;
		noiseSeedField.onEndEdit.AddListener(SetMapDirty);
		newGridSize = previewGenerator.gridSize;
		SetupDefaultSettings();
		GeneratePreview();
	}

	private void CoastSideProp_OnPropertyValueChanged(object arg0)
	{
		coastSide = (CardinalDirections)arg0;
	}

	private void BiomeProp_OnPropertyValueChanged(object arg0)
	{
		biome = (MapGenBiome.Biomes)arg0;
	}

	private void MaxHeightProp_OnPropertyValueChanged(object arg0)
	{
		maxHeight = (float)arg0;
		if (maxHeight < minHeight)
		{
			maxHeight = minHeight;
			maxHeightProp.SetInitialValue(maxHeight);
		}
		SetMapDirty(null);
	}

	private void MinHeightProp_OnPropertyValueChanged(object arg0)
	{
		minHeight = (float)arg0;
		if (minHeight > maxHeight)
		{
			minHeight = maxHeight;
			minHeightProp.SetInitialValue(minHeight);
		}
		SetMapDirty(null);
	}

	private void EdgeModeProp_OnPropertyValueChanged(object arg0)
	{
		VTMapGenerator.EdgeModes edgeModes = (edgeMode = (VTMapGenerator.EdgeModes)arg0);
		coastSideProp.gameObject.SetActive(edgeMode == VTMapGenerator.EdgeModes.Coast);
		SetMapDirty(null);
	}

	public void GeneratePreview()
	{
		if (mapType == VTMapGenerator.VTMapTypes.HeightMap && !previewGenerator.heightMap)
		{
			previewGenerator.ClearMap();
			return;
		}
		Vector3 position = previewCamTf.position;
		position.x = (position.z = 0.5f * previewGenerator.chunkSize * (float)previewGenerator.gridSize);
		previewCamTf.position = position;
		GetSettingsFromUI();
		previewGenerator.SetSeed(noiseSeed);
		previewGenerator.ApplySettingsForPreview(mapType, edgeMode, terrainSettings);
		previewGenerator.Generate();
		mapDirty = false;
	}

	private void GetSettingsFromUI()
	{
		noiseSeed = noiseSeedField.text;
		mapType = (VTMapGenerator.VTMapTypes)mapTypeField.GetValue();
		if (terrainSettings == null)
		{
			terrainSettings = new ConfigNode("TerrainSettings");
		}
		foreach (string key in propertyFields.Keys)
		{
			terrainSettings.SetValue(key, ConfigNodeUtils.WriteObject(propertyFields[key].GetValue()));
		}
	}

	private void SetupDefaultSettings()
	{
		mapTypeField.SetInitialValue(mapType);
		if (VTResources.isEditorOrDevTools)
		{
			biomeProp.SetInitialValue(biome);
		}
		else
		{
			biomeProp.SetInitialValueLimited(biome, new object[3]
			{
				MapGenBiome.Biomes.Boreal,
				MapGenBiome.Biomes.Desert,
				MapGenBiome.Biomes.Arctic
			});
		}
		sizeRangeField.min = minSize;
		sizeRangeField.max = maxSize;
		finalMapSize = maxSize;
		sizeRangeField.SetInitialValue((float)maxSize);
		edgeModeProp.gameObject.SetActive(mapType == VTMapGenerator.VTMapTypes.HeightMap);
		edgeModeProp.SetInitialValue(VTMapGenerator.EdgeModes.Water);
		coastSide = CardinalDirections.West;
		coastSideProp.SetInitialValue(coastSide);
		coastSideProp.gameObject.SetActive(edgeMode == VTMapGenerator.EdgeModes.Coast);
		minHeightProp.min = (maxHeightProp.min = -80f);
		minHeightProp.max = (maxHeightProp.max = 6000f);
		minHeightProp.SetInitialValue(minHeight);
		maxHeightProp.SetInitialValue(maxHeight);
		SetupDefaultPropertiesList();
	}

	private void OnSetMapSize(object o)
	{
		float num = (float)o;
		float num2 = Mathf.Floor(3072 * (finalMapSize = Mathf.RoundToInt(num)) / 1000);
		sizeDisplayText.text = string.Format("{0}km x {0}km", num2);
		int num3 = (newGridSize = Mathf.RoundToInt(1.5f * (num / 4f)));
		SetMapDirty(null);
	}

	private void SetupDefaultPropertiesList()
	{
		foreach (VTPropertyField value2 in propertyFields.Values)
		{
			UnityEngine.Object.Destroy(value2.gameObject);
		}
		propertyFields.Clear();
		terrainSettings = new ConfigNode("TerrainSettings");
		float num = 0f;
		Type jobType = VTMapGenerator.GetJobType(mapType);
		object obj = Activator.CreateInstance(jobType);
		FieldInfo[] fields = jobType.GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(VTTerrainAttribute), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				VTTerrainAttribute vTTerrainAttribute = (VTTerrainAttribute)customAttributes[j];
				GameObject propertyFieldForType = propTemplates.GetPropertyFieldForType(fieldInfo.FieldType, propertiesScrollRect.content);
				propertyFieldForType.transform.localPosition = new Vector3(0f, 0f - num, 0f);
				num += ((RectTransform)propertyFieldForType.transform).rect.height;
				VTPropertyField componentImplementing = propertyFieldForType.GetComponentImplementing<VTPropertyField>();
				componentImplementing.SetLabel(vTTerrainAttribute.displayName);
				if (componentImplementing is VTFloatRangeProperty)
				{
					VTFloatRangeProperty obj2 = (VTFloatRangeProperty)componentImplementing;
					obj2.min = vTTerrainAttribute.min;
					obj2.max = vTTerrainAttribute.max;
				}
				object value = fieldInfo.GetValue(obj);
				componentImplementing.SetInitialValue(value);
				componentImplementing.OnPropertyValueChanged += SetMapDirty;
				propertyFields.Add(fieldInfo.Name, componentImplementing);
				terrainSettings.SetValue(fieldInfo.Name, ConfigNodeUtils.WriteObject(value));
			}
		}
		propertiesScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		propertiesScrollRect.ClampVertical();
	}

	public void RandomizeSeed()
	{
		string text = "";
		int num = UnityEngine.Random.Range(1, 20);
		for (int i = 0; i < num; i++)
		{
			text += GetRandomChar();
		}
		noiseSeedField.text = text;
		noiseSeed = text;
		GeneratePreview();
	}

	private string GetRandomChar()
	{
		string text = ((char)UnityEngine.Random.Range(65, 90)).ToString();
		if (UnityEngine.Random.Range(0f, 100f) > 50f)
		{
			text = text.ToLower();
		}
		return text;
	}

	private void OnSetMapType(object t)
	{
		mapType = (VTMapGenerator.VTMapTypes)t;
		SetupDefaultPropertiesList();
		GeneratePreview();
		noiseSeedField.gameObject.SetActive(mapType != VTMapGenerator.VTMapTypes.HeightMap);
		loadHeightmapObject.SetActive(mapType == VTMapGenerator.VTMapTypes.HeightMap);
		edgeModeProp.gameObject.SetActive(mapType == VTMapGenerator.VTMapTypes.HeightMap);
		switch (mapType)
		{
		case VTMapGenerator.VTMapTypes.Archipelago:
			EdgeModeProp_OnPropertyValueChanged(VTMapGenerator.EdgeModes.Water);
			break;
		case VTMapGenerator.VTMapTypes.Mountain_Lakes:
			EdgeModeProp_OnPropertyValueChanged(VTMapGenerator.EdgeModes.Hills);
			break;
		case VTMapGenerator.VTMapTypes.HeightMap:
			break;
		}
	}

	public void SaveMap()
	{
		GetSettingsFromUI();
		VTMapCustom vTMapCustom = ScriptableObject.CreateInstance<VTMapCustom>();
		vTMapCustom.terrainSettings = terrainSettings;
		vTMapCustom.seed = noiseSeed;
		vTMapCustom.mapType = mapType;
		vTMapCustom.edgeMode = edgeMode;
		vTMapCustom.mapSize = Mathf.RoundToInt((float)sizeRangeField.GetValue());
		vTMapCustom.biome = biome;
		vTMapCustom.coastSide = coastSide;
		if (mapType != VTMapGenerator.VTMapTypes.HeightMap)
		{
			heightmapSaveOp = vTMapCustom.SaveToHeightMap();
			heightSaveRoutine = StartCoroutine(SaveHeightmapRoutine());
			vTMapCustom.mapType = VTMapGenerator.VTMapTypes.HeightMap;
		}
		else
		{
			int num = vTMapCustom.mapSize * 20 + 1;
			Texture2D texture2D = new Texture2D(num, num, TextureFormat.RGBA32, mipChain: false, linear: true);
			for (int i = 0; i < num; i++)
			{
				for (int j = 0; j < num; j++)
				{
					Vector2 vector = new Vector2((float)i / (float)num, (float)j / (float)num);
					Color pixelBilinear = previewGenerator.heightMap.GetPixelBilinear(vector.x, vector.y);
					pixelBilinear.g = (pixelBilinear.b = 0f);
					texture2D.SetPixel(i, j, pixelBilinear);
				}
			}
			texture2D.Apply();
			heightmapSaveOp = new VTMapCustom.AsyncSaveOp(null);
			heightmapSaveOp.done = true;
			heightmapSaveOp.progress = 1f;
			heightmapSaveOp.texture = texture2D;
		}
		saveUI.Open(vTMapCustom, OnSaved, OnCancelledSave);
	}

	private void OnCancelledSave()
	{
		if (heightSaveRoutine != null)
		{
			StopCoroutine(heightSaveRoutine);
		}
		if (heightmapSaveOp != null)
		{
			heightmapSaveOp.Cancel();
			heightmapSaveOp = null;
		}
		hmSaveProgressWindow.gameObject.SetActive(value: false);
	}

	private IEnumerator SaveHeightmapRoutine()
	{
		hmSaveProgressWindow.op = heightmapSaveOp;
		hmSaveProgressWindow.gameObject.SetActive(value: true);
		while (!heightmapSaveOp.done)
		{
			yield return null;
		}
		hmSaveProgressWindow.gameObject.SetActive(value: false);
		if (heightmapSaveOp.texture != null)
		{
			savedHeightmap = heightmapSaveOp.texture;
		}
	}

	private void OnSaved(string mapID)
	{
		Debug.Log("OnSaved callback with: " + mapID);
		StartCoroutine(WaitForHeightmapAfterSaveRoutine(mapID));
	}

	private IEnumerator WaitForHeightmapAfterSaveRoutine(string mapID)
	{
		while (!heightmapSaveOp.done && !heightmapSaveOp.texture)
		{
			yield return null;
		}
		byte[] bytes = heightmapSaveOp.texture.EncodeToPNG();
		File.WriteAllBytes(Path.Combine(VTResources.GetMapDirectoryPath(mapID), "height.png"), bytes);
		ReloadMapsAndLaunch(mapID);
	}

	private void ReloadMapsAndLaunch(string mapID)
	{
		VTResources.LoadMaps();
		VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.MapEditor;
		VTResources.LaunchMap(mapID);
	}

	private void SetMapDirty(object o)
	{
		mapDirty = true;
		mapDirtyTime = Time.time;
	}

	private void Update()
	{
		if (mapDirty && Time.time - mapDirtyTime > 0.25f && !previewGenerator.IsGenerating())
		{
			previewGenerator.gridSize = newGridSize;
			if (mapType == VTMapGenerator.VTMapTypes.HeightMap && origHMTex != null)
			{
				ApplyAdjustedHeightmap(previewGenerator.heightMap);
				previewGenerator.SetNewHeightmap(previewGenerator.heightMap);
			}
			GeneratePreview();
		}
	}

	public void BackButton()
	{
		editorMenu.gameObject.SetActive(value: true);
		base.gameObject.SetActive(value: false);
	}

	public void SelectHeightmapButton()
	{
		resourceBrowser.OpenBrowser("Select Heightmap", OnSelectedHeightmap, VTResources.supportedImageExtensions);
	}

	private void OnSelectedHeightmap(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			Texture2D texture = VTResources.GetTexture(s, mipmaps: false, linear: true);
			origHMTex = CopyTexture(texture);
			ApplyAdjustedHeightmap(texture);
			previewGenerator.SetNewHeightmap(texture);
			GeneratePreview();
			loadedHeightmapText.text = Path.GetFileName(s);
		}
	}

	private void AutoIslandBorder(Texture2D hMap)
	{
		Vector2 vector = new Vector2(hMap.width, hMap.height) / 2f;
		float num = (float)hMap.width / 2f;
		for (int i = 0; i < hMap.width; i++)
		{
			for (int j = 0; j < hMap.height; j++)
			{
				Vector2 vector2 = new Vector2(i, j) - vector;
				float f = Mathf.Max(Mathf.Abs(vector2.x), Mathf.Abs(vector2.y)) / num;
				float r = hMap.GetPixel(i, j).r;
				float t = Mathf.Pow(f, 6f * r);
				r = Mathf.Lerp(r, 0f, t);
				hMap.SetPixel(i, j, new Color(r, 0f, 0f, 1f));
			}
		}
		hMap.Apply();
	}

	private void AutoTerrainBorder(Texture2D hMap, VTMapGenerator.VTOOBTerrainProfile oobProfile)
	{
		float num = 1f / (float)(3072 * oobProfile.repeatScale);
		for (int i = 0; i < hMap.width; i++)
		{
			for (int j = 0; j < hMap.height; j++)
			{
				float hmHeight = hMap.GetPixel(i, j).r * 6000f;
				Vector2 vector = 153.6f * new Vector2(i, j);
				Vector2 vector2 = num * vector;
				float oobHeight = oobProfile.heightMap.GetPixelBilinear(vector2.x, vector2.y).r * 6000f;
				float blendedHeight = VTTHeightMap.GetBlendedHeight(hmHeight, oobHeight, finalMapSize, 3072f, 6000f, vector);
				hMap.SetPixel(i, j, new Color(blendedHeight / 6000f, 0f, 0f, 1f));
			}
		}
		hMap.Apply();
	}

	private void AutoCoastBorder(Texture2D hMap, VTMapGenerator.VTOOBTerrainProfile oobProfile)
	{
		float num = 1f / (float)(3072 * oobProfile.repeatScale);
		for (int i = 0; i < hMap.width; i++)
		{
			for (int j = 0; j < hMap.height; j++)
			{
				float hmHeight = hMap.GetPixel(i, j).r * 6000f;
				Vector2 vector = 153.6f * new Vector2(i, j);
				Vector2 vector2 = num * vector;
				float oobHeight = oobProfile.heightMap.GetPixelBilinear(vector2.x, vector2.y).r * 6000f;
				float blendedHeight = VTTHeightMap.GetBlendedHeight(hmHeight, oobHeight, finalMapSize, 3072f, 6000f, vector, coastal: true, coastSide);
				hMap.SetPixel(i, j, new Color(blendedHeight / 6000f, 0f, 0f, 1f));
			}
		}
		hMap.Apply();
	}

	private void ApplyAdjustedHeightmap(Texture2D toHMap)
	{
		for (int i = 0; i < toHMap.width; i++)
		{
			for (int j = 0; j < toHMap.height; j++)
			{
				Color pixel = origHMTex.GetPixel(i, j);
				float value = Mathf.Lerp(minHeight, maxHeight, pixel.r);
				float b = Mathf.InverseLerp(-80f, 6000f, value);
				pixel.r = (pixel.g = (pixel.b = b));
				toHMap.SetPixel(i, j, pixel);
			}
		}
		toHMap.Apply();
	}

	private Texture2D CopyTexture(Texture2D tex)
	{
		Texture2D texture2D = new Texture2D(tex.width, tex.height, tex.format, mipChain: false);
		texture2D.SetPixels(tex.GetPixels());
		return texture2D;
	}
}
