using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTMapEdTerrainPanel : VTEdUITab
{
	public enum TerrainEditModes
	{
		None,
		HeightPaint,
		CityPaint
	}

	public enum HeightTools
	{
		Paint,
		Flatten,
		Level,
		Erosion
	}

	public class PaintRectClickHandler : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
	{
		public VTMapEdTerrainPanel terrainPanel;

		public void OnPointerDown(PointerEventData eventData)
		{
			terrainPanel.PaintPointerDown();
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			terrainPanel.PaintPointerUp();
		}
	}

	public VTMapEditor editor;

	private TerrainEditModes mode;

	private HeightTools heightTool;

	[Header("Main Menu")]
	public GameObject mainMenuObject;

	public VTMapEdCursorTerrainCircle cursorCircle;

	public VTMapEdCursorTerrainCircle shapeCircle;

	[Header("Height Paint")]
	public RectTransform paintingRect;

	public GameObject heightPaintMenuObject;

	public VTFloatRangeProperty radiusProp;

	public VTFloatRangeProperty strengthProp;

	public VTFloatRangeProperty softnessProp;

	public VTFloatRangeProperty levelHeightProp;

	public VTEnumProperty heightToolProp;

	private float heightPaintRadius = 1000f;

	private float heightPaintSoftness = 1f;

	private float heightPaintStrength = 100f;

	private float levelHeightTarget = 100f;

	private List<Vector3> _tempHeightPaintVerts = new List<Vector3>();

	public Button[] toolButtons;

	public Color inactiveButtonColor;

	public Color activeButtonColor;

	public GameObject[] toolDisplayObjects;

	[Header("City Paint")]
	public GameObject cityPaintMenuObject;

	public VTFloatRangeProperty cityRadiusProp;

	public VTFloatRangeProperty cityStrengthProp;

	public VTFloatRangeProperty citySoftnessProp;

	public VTMinMaxProperty cityLevelRangeProp;

	public Text cityPaintLevelText;

	private float cityPaintRadius = 100f;

	private float cityPaintSoftness = 1f;

	private float cityPaintStrength = 500f;

	private int minCityPaintLevel;

	private int maxCityPaintLevel;

	private const float CITY_DOT_THRES = 0.98f;

	[Header("Controls")]
	public float radiusScrollRate = 5f;

	public float softnessScrollRate = 5f;

	private const float MIN_RADIUS = 100f;

	private const float MAX_RADIUS = 10000f;

	private const float MIN_STRENGTH = 1f;

	private const float MAX_STRENGTH = 5000f;

	private const float MIN_SOFTNESS = 0.01f;

	private const float MAX_SOFTNESS = 2f;

	private const float MIN_CITY_RADIUS = 50f;

	private const float MAX_CITY_RADIUS = 2500f;

	private const float MIN_CITY_STRENGTH = 100f;

	private const float MAX_CITY_STRENGTH = 5000f;

	private bool finalCityPreviewed;

	private float lastCityPreviewTime;

	private MeshTerrainErosion mte;

	private bool mousePainting;

	private List<IntVector2> chunksToRecalc = new List<IntVector2>();

	private float pointerUpTime;

	public GameObject generatingIndicatorObject;

	public Transform generatingIndicatorBar;

	private bool recalculating;

	private bool paintPointer;

	private void Awake()
	{
		mainMenuObject.SetActive(value: true);
		heightPaintMenuObject.SetActive(value: false);
		radiusProp.min = 100f;
		radiusProp.max = 10000f;
		radiusProp.SetInitialValue(heightPaintRadius);
		radiusProp.OnPropertyValueChanged += RadiusProp_OnPropertyValueChanged;
		strengthProp.min = 1f;
		strengthProp.max = 5000f;
		strengthProp.SetInitialValue(heightPaintStrength);
		strengthProp.OnPropertyValueChanged += StrengthProp_OnPropertyValueChanged;
		softnessProp.min = 0.01f;
		softnessProp.max = 2f;
		softnessProp.SetInitialValue(heightPaintSoftness);
		softnessProp.OnPropertyValueChanged += SoftnessProp_OnPropertyValueChanged;
		levelHeightProp.min = -80f;
		levelHeightProp.max = 6000f;
		levelHeightProp.SetInitialValue(levelHeightTarget);
		levelHeightProp.OnPropertyValueChanged += LevelHeightProp_OnPropertyValueChanged;
		levelHeightProp.gameObject.SetActive(value: false);
		heightToolProp.SetInitialValue(heightTool);
		heightToolProp.OnPropertyValueChanged += HeightToolProp_OnPropertyValueChanged;
		cityRadiusProp.min = 50f;
		cityRadiusProp.max = 2500f;
		cityRadiusProp.SetInitialValue(cityPaintRadius);
		cityRadiusProp.OnPropertyValueChanged += CityRadiusProp_OnPropertyValueChanged;
		cityStrengthProp.min = 100f;
		cityStrengthProp.max = 2500f;
		cityStrengthProp.SetInitialValue(cityPaintStrength);
		cityStrengthProp.OnPropertyValueChanged += CityStrengthProp_OnPropertyValueChanged;
		citySoftnessProp.min = 0.01f;
		citySoftnessProp.max = 2f;
		citySoftnessProp.SetInitialValue(cityPaintSoftness);
		citySoftnessProp.OnPropertyValueChanged += CitySoftnessProp_OnPropertyValueChanged;
		cityLevelRangeProp.minLimit = (minCityPaintLevel = 0);
		cityLevelRangeProp.maxLimit = (maxCityPaintLevel = 5);
		cityLevelRangeProp.rangeType = UnitSpawnAttributeRange.RangeTypes.Int;
		cityLevelRangeProp.SetInitialValue(new MinMax(0f, 5f));
		cityLevelRangeProp.OnPropertyValueChanged += CityLevelRangeProp_OnPropertyValueChanged;
		UpdateCityPaintLevelText();
		paintingRect.gameObject.AddComponent<PaintRectClickHandler>().terrainPanel = this;
		shapeCircle.transform.localPosition = Vector3.zero;
		RadiusProp_OnPropertyValueChanged(heightPaintRadius);
		SetHeightTool(0);
	}

	private void CityLevelRangeProp_OnPropertyValueChanged(object arg0)
	{
		MinMax minMax = (MinMax)arg0;
		minCityPaintLevel = Mathf.RoundToInt(minMax.min);
		maxCityPaintLevel = Mathf.RoundToInt(minMax.max);
		UpdateCityPaintLevelText();
	}

	private void UpdateCityPaintLevelText()
	{
		string arg = CityLevelToString(minCityPaintLevel);
		string arg2 = CityLevelToString(maxCityPaintLevel);
		cityPaintLevelText.text = $"{arg} to {arg2}";
	}

	private string CityLevelToString(int level)
	{
		return level switch
		{
			0 => "Empty", 
			1 => "Rural", 
			2 => "Suburb", 
			3 => "Midtown", 
			4 => "Downtown I", 
			5 => "Downtown II", 
			_ => "?", 
		};
	}

	private void CitySoftnessProp_OnPropertyValueChanged(object arg0)
	{
		cityPaintSoftness = (float)arg0;
		float t = 1f - Mathf.InverseLerp(citySoftnessProp.min, citySoftnessProp.max, cityPaintSoftness);
		shapeCircle.SetRadius(Mathf.Lerp(cityRadiusProp.min, cityPaintRadius, t));
	}

	private void CityStrengthProp_OnPropertyValueChanged(object arg0)
	{
		cityPaintStrength = (float)arg0;
	}

	private void CityRadiusProp_OnPropertyValueChanged(object arg0)
	{
		float num = (cityPaintRadius = (float)arg0);
		cursorCircle.SetRadius(num);
		cursorCircle.SetThickness(num / 30f);
		shapeCircle.SetThickness(num / 30f);
		CitySoftnessProp_OnPropertyValueChanged(heightPaintSoftness);
	}

	private void LevelHeightProp_OnPropertyValueChanged(object arg0)
	{
		levelHeightTarget = (float)arg0;
	}

	private void HeightToolProp_OnPropertyValueChanged(object arg0)
	{
		SetHeightTool((HeightTools)arg0);
	}

	public void SetHeightTool(HeightTools tool)
	{
		heightTool = tool;
		levelHeightProp.gameObject.SetActive(tool == HeightTools.Level);
		softnessProp.gameObject.SetActive(tool != HeightTools.Erosion);
		for (int i = 0; i < toolButtons.Length; i++)
		{
			toolButtons[i].image.color = ((tool == (HeightTools)i) ? activeButtonColor : inactiveButtonColor);
			if ((bool)toolDisplayObjects[i])
			{
				toolDisplayObjects[i].SetActive(tool == (HeightTools)i);
			}
		}
	}

	public void SetHeightTool(int tool)
	{
		SetHeightTool((HeightTools)tool);
	}

	public override void OnOpenedTab()
	{
		base.OnOpenedTab();
	}

	public override void OnClosedTab()
	{
		DisableAllTools();
		paintingRect.gameObject.SetActive(value: false);
		base.OnClosedTab();
	}

	private void SoftnessProp_OnPropertyValueChanged(object arg0)
	{
		heightPaintSoftness = (float)arg0;
		float t = 1f - Mathf.InverseLerp(softnessProp.min, softnessProp.max, heightPaintSoftness);
		shapeCircle.SetRadius(Mathf.Lerp(radiusProp.min, heightPaintRadius, t));
	}

	private void StrengthProp_OnPropertyValueChanged(object arg0)
	{
		float num = (heightPaintStrength = (float)arg0);
	}

	private void RadiusProp_OnPropertyValueChanged(object arg0)
	{
		float num = (heightPaintRadius = (float)arg0);
		cursorCircle.SetRadius(num);
		cursorCircle.SetThickness(num / 30f);
		shapeCircle.SetThickness(num / 30f);
		SoftnessProp_OnPropertyValueChanged(heightPaintSoftness);
	}

	private void Update()
	{
		switch (mode)
		{
		case TerrainEditModes.HeightPaint:
			UpdateHeightPaint();
			break;
		case TerrainEditModes.CityPaint:
			UpdateCityPaint();
			break;
		}
	}

	private void DisableAllTools()
	{
		switch (mode)
		{
		case TerrainEditModes.HeightPaint:
			DisableHeightPaint();
			break;
		case TerrainEditModes.CityPaint:
			DisableCityPaint();
			break;
		}
	}

	private void UpdateCityPaint()
	{
		if (VTMapGenerator.fetch.IsGenerating() || recalculating)
		{
			return;
		}
		bool flag = paintPointer && Input.GetMouseButton(0) && !GetCtrlKey();
		if (mousePainting)
		{
			pointerUpTime = Time.time;
		}
		mousePainting = flag;
		if (GetMouseOnTerrainPos(out var hit))
		{
			cursorCircle.gameObject.SetActive(value: true);
			cursorCircle.transform.position = hit.point;
			if (mousePainting)
			{
				List<VTMapGenerator.VTTerrainChunk> affectedTerrainChunks = GetAffectedTerrainChunks(hit.point, cityPaintRadius);
				for (int i = 0; i < affectedTerrainChunks.Count; i++)
				{
					if (Input.GetKey(KeyCode.LeftShift))
					{
						PaintTriMat(affectedTerrainChunks[i], hit.point, 1, 0);
					}
					else
					{
						PaintTriMat(affectedTerrainChunks[i], hit.point, 0, 1);
					}
				}
			}
		}
		if (!mousePainting && Time.time - pointerUpTime > 2f)
		{
			RecalcChunks();
		}
		else if (mousePainting)
		{
			if (chunksToRecalc.Count > 0 && Time.time - lastCityPreviewTime > 0.2f)
			{
				lastCityPreviewTime = Time.time;
				foreach (IntVector2 item in chunksToRecalc)
				{
					VTMapCities.instance.UpdatePreview(VTMapGenerator.fetch.GetTerrainChunk(item));
				}
			}
			finalCityPreviewed = false;
		}
		else
		{
			if (finalCityPreviewed)
			{
				return;
			}
			lastCityPreviewTime = Time.time;
			foreach (IntVector2 item2 in chunksToRecalc)
			{
				VTMapCities.instance.UpdatePreview(VTMapGenerator.fetch.GetTerrainChunk(item2));
			}
		}
	}

	public void EnableCityPaint()
	{
		mode = TerrainEditModes.CityPaint;
		mainMenuObject.SetActive(value: false);
		cityPaintMenuObject.SetActive(value: true);
		paintingRect.gameObject.SetActive(value: true);
		cursorCircle.gameObject.SetActive(value: true);
		cityLevelRangeProp.rangeType = UnitSpawnAttributeRange.RangeTypes.Int;
		CityRadiusProp_OnPropertyValueChanged(cityPaintRadius);
		StartCoroutine(DelayedCityLevelStepFix());
	}

	private IEnumerator DelayedCityLevelStepFix()
	{
		yield return null;
		cityLevelRangeProp.rangeType = UnitSpawnAttributeRange.RangeTypes.Int;
	}

	public void DisableCityPaint()
	{
		mode = TerrainEditModes.None;
		cursorCircle.gameObject.SetActive(value: false);
		paintingRect.gameObject.SetActive(value: false);
		cityPaintMenuObject.SetActive(value: false);
		mainMenuObject.SetActive(value: true);
		RecalcChunks();
		mousePainting = false;
		paintPointer = false;
	}

	public void EnableHeightPaint()
	{
		mode = TerrainEditModes.HeightPaint;
		heightPaintMenuObject.SetActive(value: true);
		paintingRect.gameObject.SetActive(value: true);
		mainMenuObject.SetActive(value: false);
		cursorCircle.gameObject.SetActive(value: true);
		RadiusProp_OnPropertyValueChanged(heightPaintRadius);
	}

	public void DisableHeightPaint()
	{
		heightPaintMenuObject.SetActive(value: false);
		mode = TerrainEditModes.None;
		cursorCircle.gameObject.SetActive(value: false);
		paintingRect.gameObject.SetActive(value: false);
		mainMenuObject.SetActive(value: true);
		RecalcChunks();
		mousePainting = false;
		paintPointer = false;
	}

	private void DoErosion(Vector3 brushWorldPos)
	{
		if (mte == null)
		{
			mte = new MeshTerrainErosion();
			mte.lineDrawer = editor.editorCamera.cam.GetComponent<VTLineDrawer>();
		}
		mte.maxHeight = VTMapGenerator.fetch.hm_maxHeight - VTMapGenerator.fetch.hm_minHeight;
		mte.initialSpeed = Mathf.Lerp(0.1f, 2f, heightPaintStrength / 5000f);
		bool key = Input.GetKey(KeyCode.RightShift);
		float num = heightPaintRadius;
		int num2 = Mathf.CeilToInt(0.0005086263f * num * 2f);
		if (key)
		{
			num2 = 1;
			num = 1f;
		}
		for (int i = 0; i < num2; i++)
		{
			mte.RunDroplet(brushWorldPos, heightPaintRadius, chunksToRecalc, key);
		}
		foreach (IntVector2 item in chunksToRecalc)
		{
			MeshCollider collider = VTMapGenerator.fetch.GetTerrainChunk(item).collider;
			collider.enabled = false;
			collider.enabled = true;
		}
	}

	private void UpdateHeightPaint()
	{
		if (VTMapGenerator.fetch.IsGenerating() || recalculating)
		{
			return;
		}
		bool flag = paintPointer && Input.GetMouseButton(0);
		bool flag2 = flag && !GetCtrlKey();
		if (mousePainting)
		{
			pointerUpTime = Time.time;
		}
		mousePainting = flag2;
		if (GetCtrlKey())
		{
			float y = Input.mouseScrollDelta.y;
			if (Mathf.Abs(y) > 0.001f)
			{
				float num = Mathf.Clamp(heightPaintRadius * (1f + y * radiusScrollRate * Time.deltaTime), 100f, 10000f);
				RadiusProp_OnPropertyValueChanged(num);
				radiusProp.SetInitialValue(num);
			}
		}
		else if (GetAltKey())
		{
			float y2 = Input.mouseScrollDelta.y;
			if (Mathf.Abs(y2) > 0.001f)
			{
				float num2 = Mathf.Clamp(heightPaintSoftness + y2 * softnessScrollRate * Time.deltaTime, 0.01f, 2f);
				SoftnessProp_OnPropertyValueChanged(num2);
				softnessProp.SetInitialValue(num2);
			}
		}
		if (GetMouseOnTerrainPos(out var hit))
		{
			cursorCircle.gameObject.SetActive(value: true);
			cursorCircle.transform.position = hit.point;
			if (heightTool == HeightTools.Level && GetCtrlKey() && flag)
			{
				levelHeightTarget = hit.point.y - WaterPhysics.instance.height - 5f;
				levelHeightProp.SetInitialValue(levelHeightTarget);
			}
			if (mousePainting)
			{
				if (heightTool == HeightTools.Erosion)
				{
					DoErosion(hit.point);
				}
				else
				{
					List<VTMapGenerator.VTTerrainChunk> affectedTerrainChunks = GetAffectedTerrainChunks(hit.point, heightPaintRadius);
					float avgHeight = 0f;
					if (heightTool == HeightTools.Flatten)
					{
						avgHeight = GetAvgHeight(affectedTerrainChunks, hit.point);
					}
					for (int i = 0; i < affectedTerrainChunks.Count; i++)
					{
						affectedTerrainChunks[i].sharedMeshes[0].GetVertices(_tempHeightPaintVerts);
						Vector3 localMousePos = affectedTerrainChunks[i].lodObjects[0].transform.InverseTransformPoint(hit.point);
						switch (heightTool)
						{
						case HeightTools.Paint:
							HeightPaintVerts(_tempHeightPaintVerts, localMousePos);
							break;
						case HeightTools.Flatten:
							FlattenVerts(_tempHeightPaintVerts, localMousePos, avgHeight);
							break;
						case HeightTools.Level:
							LevelVerts(_tempHeightPaintVerts, localMousePos);
							break;
						}
						affectedTerrainChunks[i].sharedMeshes[0].SetVertices(_tempHeightPaintVerts);
						affectedTerrainChunks[i].sharedMeshes[0].RecalculateNormals();
						if (heightPaintRadius < 3000f)
						{
							MeshCollider component = affectedTerrainChunks[i].lodObjects[0].GetComponent<MeshCollider>();
							component.enabled = false;
							component.enabled = true;
						}
						if (!chunksToRecalc.Contains(affectedTerrainChunks[i].grid))
						{
							chunksToRecalc.Add(affectedTerrainChunks[i].grid);
						}
					}
				}
				if ((bool)WaterDepthCamera.instance)
				{
					WaterDepthCamera.instance.Render();
				}
			}
		}
		else
		{
			cursorCircle.gameObject.SetActive(value: false);
		}
		if (!mousePainting && Time.time - pointerUpTime > 2f)
		{
			RecalcChunks();
		}
	}

	private void PaintTriMat(VTMapGenerator.VTTerrainChunk chunk, Vector3 mousePos, int fromMat, int toMat)
	{
		Vector3 vector = chunk.lodObjects[0].transform.InverseTransformPoint(mousePos);
		vector.z = 0f;
		chunk.sharedMeshes[0].GetVertices(_tempHeightPaintVerts);
		List<int> list = chunk.terrainMeshes[0].subMeshTriangles[fromMat];
		List<Vector3> verts = chunk.terrainMeshes[0].verts;
		List<Vector3> normals = chunk.terrainMeshes[0].normals;
		int num = 0;
		bool flag = true;
		while (flag)
		{
			flag = false;
			for (int i = num; i < list.Count - 2; i += 3)
			{
				if (flag)
				{
					break;
				}
				int index = list[i];
				int index2 = list[i + 1];
				int index3 = list[i + 2];
				Vector3 vector2 = verts[index];
				Vector3 vector3 = verts[index2];
				Vector3 vector4 = verts[index3];
				Vector3 vector5 = (vector2 + vector3 + vector4) / 3f;
				vector5.z = 0f;
				if (!((vector5 - vector).sqrMagnitude < cityPaintRadius * cityPaintRadius) || !CheckVertForCity(vector2, normals[index]) || !CheckVertForCity(vector3, normals[index2]) || !CheckVertForCity(vector4, normals[index3]))
				{
					continue;
				}
				float num2 = float.MaxValue;
				int num3 = -1;
				Vector3 vector6 = Vector3.zero;
				for (int j = 0; j < list.Count - 2; j += 3)
				{
					if (j != i)
					{
						Vector3 vector7 = verts[list[j]];
						Vector3 vector8 = verts[list[j + 1]];
						Vector3 vector9 = verts[list[j + 2]];
						Vector3 vector10 = (vector7 + vector8 + vector9) / 3f;
						vector10.z = 0f;
						float sqrMagnitude = (vector5 - vector10).sqrMagnitude;
						if (sqrMagnitude < num2)
						{
							num2 = sqrMagnitude;
							num3 = j;
							vector6 = vector10;
						}
					}
				}
				if (num3 < 0)
				{
					continue;
				}
				vector5 = (vector6 + vector5) / 2f;
				if (toMat != 0 || !(chunk.terrainMeshes[0].colors[index].g >= 0.2f))
				{
					if (num3 < i)
					{
						num = num3;
						chunk.terrainMeshes[0].SetTriangleMaterial(i, fromMat, toMat);
						chunk.terrainMeshes[0].SetTriangleMaterial(num3, fromMat, toMat);
					}
					else
					{
						num = i;
						chunk.terrainMeshes[0].SetTriangleMaterial(num3, fromMat, toMat);
						chunk.terrainMeshes[0].SetTriangleMaterial(i, fromMat, toMat);
					}
					flag = true;
				}
			}
		}
		int count = verts.Count;
		float num4 = Time.deltaTime * cityPaintStrength / 5000f;
		for (int k = 0; k < count; k++)
		{
			Vector3 vector11 = verts[k] - vector;
			vector11.z = 0f;
			float num5 = (vector11.magnitude - 260f) / cityPaintRadius;
			if (!(num5 <= 1f) || (toMat != 0 && !chunk.terrainMeshes[0].subMeshTriangles[1].Contains(k)))
			{
				continue;
			}
			float num6 = num4 * Mathf.Pow(1f - num5, cityPaintSoftness);
			Color value = chunk.terrainMeshes[0].colors[k];
			if (toMat == 1)
			{
				value.g += num6;
				value.g = Mathf.Clamp(value.g, Mathf.Max(0.201f, (float)minCityPaintLevel * 0.195f), (float)maxCityPaintLevel * 0.195f);
			}
			else
			{
				value.g -= num6;
				if (value.g < Mathf.Max(0.2f * (float)minCityPaintLevel, 0.2f))
				{
					value.g = 0f;
				}
			}
			chunk.terrainMeshes[0].colors[k] = value;
			IntVector2 coord = VTTerrainTextureConverter.VertToPixel(verts[k], chunk.grid.x, chunk.grid.y, chunk.generator.chunkSize, chunk.generator.gridSize, 20);
			BDColor pixel = VTMapGenerator.fetch.hmBdt.GetPixel(coord);
			pixel.g = value.g;
			VTMapGenerator.fetch.hmBdt.SetPixel(coord.x, coord.y, pixel);
		}
		chunk.terrainMeshes[0].ApplyToMesh(chunk.sharedMeshes[0]);
		if (!chunksToRecalc.Contains(chunk.grid))
		{
			chunksToRecalc.Add(chunk.grid);
		}
	}

	private bool CheckVertForCity(Vector3 vert, Vector3 normal)
	{
		if (vert.z > 10f)
		{
			return Vector3.Dot(normal, Vector3.forward) > 0.98f;
		}
		return false;
	}

	private IEnumerator PreventPaintWhileGenerating()
	{
		generatingIndicatorObject.SetActive(value: true);
		paintingRect.gameObject.SetActive(value: false);
		generatingIndicatorBar.localScale = new Vector3(0f, 1f, 1f);
		while (!VTMapGenerator.fetch.IsGenerating() || VTMapGenerator.fetch.GetJobsRemaining() < 1)
		{
			yield return null;
		}
		int numJobs = VTMapGenerator.fetch.GetJobsRemaining();
		while (VTMapGenerator.fetch.IsGenerating())
		{
			float x = (float)(numJobs - VTMapGenerator.fetch.GetJobsRemaining()) / (float)numJobs;
			generatingIndicatorBar.localScale = new Vector3(x, 1f, 1f);
			yield return null;
		}
		generatingIndicatorObject.SetActive(value: false);
		if (mode != 0)
		{
			paintingRect.gameObject.SetActive(value: true);
		}
		recalculating = false;
	}

	private void RecalcChunks()
	{
		if (chunksToRecalc.Count <= 0)
		{
			return;
		}
		recalculating = true;
		StartCoroutine(PreventPaintWhileGenerating());
		VTMapGenerator mapGenerator = VTCustomMapManager.instance.mapGenerator;
		foreach (IntVector2 item in chunksToRecalc)
		{
			VTMapGenerator.VTTerrainChunk terrainChunk = mapGenerator.GetTerrainChunk(item);
			terrainChunk.sharedMeshes[0].GetVertices(_tempHeightPaintVerts);
			for (int i = 0; i < terrainChunk.sharedMeshes[0].vertexCount; i++)
			{
				Vector3 vert = _tempHeightPaintVerts[i];
				Vector2 vector = terrainChunk.terrainMeshes[0].uvs[i];
				if (!(vector.x > 0.97f) && !(vector.y > 0.97f) && !(vector.x < 0f) && !(vector.y < 0f))
				{
					float num = (vert.z - terrainChunk.terrainMeshes[0].verts[i].z) / (mapGenerator.hm_maxHeight - mapGenerator.hm_minHeight);
					IntVector2 intVector = VTTerrainTextureConverter.VertToPixel(vert, item.x, item.y, mapGenerator.chunkSize, mapGenerator.gridSize, 20);
					intVector.x = Mathf.Clamp(intVector.x, 0, mapGenerator.hmBdt.width - 1);
					intVector.y = Mathf.Clamp(intVector.y, 0, mapGenerator.hmBdt.height - 1);
					BDColor pixel = mapGenerator.hmBdt.GetPixel(intVector.x, intVector.y);
					pixel.r += num;
					float g = terrainChunk.terrainMeshes[0].colors[i].g;
					if (g > 0.1f)
					{
						pixel.g = g;
					}
					else
					{
						pixel.g = 0f;
					}
					mapGenerator.hmBdt.SetPixel(intVector.x, intVector.y, pixel);
				}
			}
		}
		foreach (IntVector2 item2 in chunksToRecalc)
		{
			mapGenerator.RecalculateGrid(item2);
		}
		chunksToRecalc.Clear();
	}

	private void HeightPaintVerts(List<Vector3> verts, Vector3 localMousePos)
	{
		float hm_maxHeight = VTCustomMapManager.instance.mapGenerator.hm_maxHeight;
		int num = 1;
		if (Input.GetKey(KeyCode.LeftShift))
		{
			num = -1;
		}
		float num2 = (float)num * heightPaintStrength * Time.deltaTime;
		int count = verts.Count;
		for (int i = 0; i < count; i++)
		{
			Vector3 vector = verts[i] - localMousePos;
			vector.z = 0f;
			float num3 = vector.magnitude / heightPaintRadius;
			if (num3 <= 1f)
			{
				float num4 = num2 * Mathf.Pow(1f - num3, heightPaintSoftness);
				Vector3 value = verts[i];
				value.z += num4;
				value.z = Mathf.Clamp(value.z, -200f, hm_maxHeight);
				verts[i] = value;
			}
		}
	}

	private float GetAvgHeight(List<VTMapGenerator.VTTerrainChunk> chunks, Vector3 worldMousePos)
	{
		float num = 0f;
		int num2 = 0;
		for (int i = 0; i < chunks.Count; i++)
		{
			VTMapGenerator.VTTerrainChunk vTTerrainChunk = chunks[i];
			chunks[i].sharedMeshes[0].GetVertices(_tempHeightPaintVerts);
			Vector3 vector = chunks[i].lodObjects[0].transform.InverseTransformPoint(worldMousePos);
			vTTerrainChunk.sharedMeshes[0].GetVertices(_tempHeightPaintVerts);
			for (int j = 0; j < vTTerrainChunk.sharedMeshes[0].vertexCount; j++)
			{
				Vector3 vector2 = _tempHeightPaintVerts[j] - vector;
				vector2.z = 0f;
				if (vector2.magnitude / heightPaintRadius <= 1f)
				{
					num += _tempHeightPaintVerts[j].z;
					num2++;
				}
			}
		}
		return num / (float)num2;
	}

	private void FlattenVerts(List<Vector3> verts, Vector3 localMousePos, float avgHeight)
	{
		int count = verts.Count;
		for (int i = 0; i < count; i++)
		{
			Vector3 vector = verts[i] - localMousePos;
			vector.z = 0f;
			float num = vector.magnitude / heightPaintRadius;
			if (num <= 1f)
			{
				float num2 = Mathf.Pow(1f - num, heightPaintSoftness);
				float num3 = avgHeight - verts[i].z;
				verts[i] += new Vector3(0f, 0f, 4f * Time.deltaTime * num3 * num2 * heightPaintStrength / 5000f);
			}
		}
	}

	private void LevelVerts(List<Vector3> verts, Vector3 localMousePos)
	{
		int count = verts.Count;
		for (int i = 0; i < count; i++)
		{
			Vector3 vector = verts[i] - localMousePos;
			vector.z = 0f;
			float num = vector.magnitude / heightPaintRadius;
			if (num <= 1f)
			{
				float num2 = Mathf.Pow(1f - num, heightPaintSoftness);
				float num3 = levelHeightTarget - verts[i].z;
				verts[i] += new Vector3(0f, 0f, 4f * Time.deltaTime * num3 * num2 * heightPaintStrength / 5000f);
			}
		}
	}

	private bool GetMouseOnTerrainPos(out RaycastHit hit)
	{
		return Physics.Raycast(editor.editorCamera.cam.ScreenPointToRay(Input.mousePosition), out hit, 100000f, 1);
	}

	private List<VTMapGenerator.VTTerrainChunk> GetAffectedTerrainChunks(Vector3 worldPos, float radius)
	{
		List<VTMapGenerator.VTTerrainChunk> list = new List<VTMapGenerator.VTTerrainChunk>();
		VTMapGenerator mapGenerator = VTCustomMapManager.instance.mapGenerator;
		IntVector2 intVector = mapGenerator.ChunkGridAtPos(worldPos);
		int num = Mathf.CeilToInt(radius / mapGenerator.chunkSize);
		for (int i = intVector.x - num; i <= intVector.x + num; i++)
		{
			for (int j = intVector.y - num; j <= intVector.y + num; j++)
			{
				if (Mathf.Min(i, j) >= 0 && Mathf.Max(i, j) < mapGenerator.gridSize)
				{
					list.Add(mapGenerator.GetTerrainChunk(i, j));
				}
			}
		}
		return list;
	}

	public void PaintPointerDown()
	{
		paintPointer = true;
	}

	public void PaintPointerUp()
	{
		paintPointer = false;
	}

	private bool GetCtrlKey()
	{
		if (!Input.GetKey(KeyCode.LeftControl))
		{
			return Input.GetKey(KeyCode.RightControl);
		}
		return true;
	}

	private bool GetAltKey()
	{
		if (!Input.GetKey(KeyCode.LeftAlt))
		{
			return Input.GetKey(KeyCode.RightAlt);
		}
		return true;
	}
}
