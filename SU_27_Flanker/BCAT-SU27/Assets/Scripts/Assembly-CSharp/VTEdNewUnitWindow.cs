using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdNewUnitWindow : MonoBehaviour
{
	public class CategoryItemButton : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public VTEdNewUnitWindow window;

		public int idx;

		public void OnPointerClick(PointerEventData eventData)
		{
			window.SelectCategory(idx);
		}
	}

	public class UnitItemButton : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public VTEdNewUnitWindow window;

		public int idx;

		public void OnPointerClick(PointerEventData eventData)
		{
			window.SelectUnit(idx);
		}
	}

	public VTScenarioEditor editor;

	[Header("Categories")]
	public GameObject categoryItemTemplate;

	public RectTransform categoryScrollTf;

	public Transform categorySelectCursor;

	[Header("Units")]
	public GameObject unitItemTemplate;

	public RectTransform unitScrollTf;

	private ScrollRect unitScrollRect;

	public Transform unitSelectCursor;

	private Dictionary<string, RenderTexture> unitImages = new Dictionary<string, RenderTexture>();

	[Header("Inputs")]
	public Button okayButton;

	public InputField searchField;

	[Header("Description")]
	public GameObject descriptionObj;

	public Text unitNameText;

	public Text unitDescriptionText;

	public RawImage unitDescriptionImage;

	[Header("Team Select")]
	public Transform alliedUnitsTf;

	public Transform enemyUnitsTf;

	public Transform selectedTeamTf;

	private Teams selectedTeam;

	private bool firstOpen = true;

	private float catLineHeight;

	private string[] categories;

	private int selectedCategoryIdx = -1;

	private List<GameObject> catButtons = new List<GameObject>();

	private float unitLineHeight;

	private string[] units;

	private int selectedUnitIdx = -1;

	private List<GameObject> unitButtons = new List<GameObject>();

	public Camera thumbCam;

	private float lastSelectUnitTime;

	private bool searchMode;

	private void Awake()
	{
		unitItemTemplate.SetActive(value: false);
		categoryItemTemplate.SetActive(value: false);
		unitScrollRect = unitScrollTf.GetComponentInParent<ScrollRect>();
		catLineHeight = ((RectTransform)categoryItemTemplate.transform).rect.height;
		SelectCategory(-1);
		unitLineHeight = ((RectTransform)unitItemTemplate.transform).rect.height;
		SelectUnit(-1);
		okayButton.interactable = false;
	}

	public void OpenWindow()
	{
		base.gameObject.SetActive(value: true);
		if (firstOpen)
		{
			StartCoroutine(FirstOpenRoutine());
			firstOpen = false;
		}
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("newUnit");
	}

	private IEnumerator FirstOpenRoutine()
	{
		UnitCatalogue.UpdateCatalogue();
		yield return null;
		yield return StartCoroutine(LoadUnitsRoutine());
		AlliedButton();
	}

	private IEnumerator LoadUnitsRoutine()
	{
		Transform dummyTf = new GameObject().transform;
		editor.BlockEditor(dummyTf);
		string msgId = "loadingUnits";
		editor.popupMessages.DisplayPersistentMessage("Loading Units...", Color.white, msgId);
		VTScenarioEditor.isLoadingPreviewThumbnails = true;
		Stopwatch sw = new Stopwatch();
		sw.Start();
		foreach (UnitCatalogue.UnitTeam value in UnitCatalogue.catalogue.Values)
		{
			foreach (UnitCatalogue.Unit allUnit in value.allUnits)
			{
				GenerateImage(allUnit);
				if (sw.ElapsedMilliseconds > 20)
				{
					yield return null;
					sw.Restart();
				}
			}
		}
		VTScenarioEditor.isLoadingPreviewThumbnails = false;
		editor.popupMessages.RemovePersistentMessage(msgId);
		editor.UnblockEditor(dummyTf);
		Object.Destroy(dummyTf.gameObject);
	}

	private void OnDestroy()
	{
		if (unitImages == null)
		{
			return;
		}
		foreach (RenderTexture value in unitImages.Values)
		{
			if ((bool)value)
			{
				value.Release();
				Object.Destroy(value);
			}
		}
	}

	private void GenerateImage(UnitCatalogue.Unit unit)
	{
		if (unitImages.ContainsKey(unit.prefabName))
		{
			return;
		}
		GameObject gameObject = Object.Instantiate(Resources.Load<GameObject>(unit.resourcePath));
		gameObject.transform.position = Vector3.zero;
		gameObject.transform.rotation = Quaternion.identity;
		gameObject.SetActive(value: true);
		Bounds bounds = default(Bounds);
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in componentsInChildren)
		{
			if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer) && (renderer.gameObject.layer == 0 || renderer.gameObject.layer == 8) && !renderer.gameObject.name.Contains("_lod"))
			{
				renderer.gameObject.layer = 26;
				bounds.Encapsulate(renderer.bounds);
			}
		}
		if (thumbCam == null)
		{
			thumbCam = new GameObject("UnitThumbnailCamera").AddComponent<Camera>();
			thumbCam.enabled = false;
			thumbCam.clearFlags = CameraClearFlags.Color;
			thumbCam.backgroundColor = Color.black;
			Light light = thumbCam.gameObject.AddComponent<Light>();
			light.type = LightType.Directional;
			light.cullingMask = 67108864;
		}
		thumbCam.cullingMask = 67108864;
		thumbCam.gameObject.SetActive(value: true);
		float num = bounds.size.x * 6f;
		Vector3 vector2 = (thumbCam.transform.position = new Vector3(0f - num, num, num));
		thumbCam.transform.LookAt(bounds.center);
		thumbCam.nearClipPlane = Mathf.Min(1f, num * 0.5f);
		thumbCam.farClipPlane = Mathf.Max(1000f, num * 2f);
		float a = Vector3.Angle(bounds.center + bounds.extents - vector2, bounds.center - bounds.extents - vector2);
		thumbCam.fieldOfView = Mathf.Min(a, 60f);
		RenderTexture renderTexture = new RenderTexture(128, 128, 8);
		thumbCam.targetTexture = renderTexture;
		thumbCam.Render();
		thumbCam.targetTexture = null;
		thumbCam.gameObject.SetActive(value: false);
		unitImages.Add(unit.prefabName, renderTexture);
		Object.DestroyImmediate(gameObject);
	}

	public void CloseWindow()
	{
		base.gameObject.SetActive(value: false);
		editor.UnblockEditor(base.transform);
		editor.editorCamera.inputLock.RemoveLock("newUnit");
	}

	public void SelectCategory(int idx)
	{
		selectedCategoryIdx = idx;
		if (idx >= 0)
		{
			categorySelectCursor.gameObject.SetActive(value: true);
			Vector3 localPosition = categorySelectCursor.localPosition;
			localPosition.y = (float)(-idx) * catLineHeight;
			categorySelectCursor.localPosition = localPosition;
			SetupUnitsList(UnitCatalogue.catalogue[selectedTeam].categories[categories[idx]].units.Values.Where((UnitCatalogue.Unit x) => !x.hideFromEditor).ToList());
		}
		else
		{
			categorySelectCursor.gameObject.SetActive(value: false);
		}
	}

	public void AlliedButton()
	{
		CancelSearch();
		selectedTeam = Teams.Allied;
		SetupCategories(Teams.Allied);
		SelectCategory(0);
		selectedTeamTf.gameObject.SetActive(value: true);
		selectedTeamTf.position = alliedUnitsTf.position;
	}

	public void EnemyButton()
	{
		CancelSearch();
		selectedTeam = Teams.Enemy;
		SetupCategories(Teams.Enemy);
		SelectCategory(0);
		selectedTeamTf.gameObject.SetActive(value: true);
		selectedTeamTf.position = enemyUnitsTf.position;
	}

	private void ClearCategoryList()
	{
		foreach (GameObject catButton in catButtons)
		{
			Object.Destroy(catButton);
		}
		catButtons = new List<GameObject>();
		SelectCategory(-1);
	}

	private void SetupCategories(Teams team)
	{
		ClearCategoryList();
		int num = 0;
		Dictionary<string, UnitCatalogue.UnitCategory>.KeyCollection keys = UnitCatalogue.catalogue[team].categories.Keys;
		categories = new string[keys.Count];
		foreach (string item in keys)
		{
			GameObject gameObject = Object.Instantiate(categoryItemTemplate, categoryScrollTf);
			CategoryItemButton categoryItemButton = gameObject.AddComponent<CategoryItemButton>();
			categoryItemButton.window = this;
			categoryItemButton.idx = num;
			gameObject.GetComponentInChildren<Text>().text = item;
			Vector3 localPosition = gameObject.transform.localPosition;
			localPosition.y = (float)(-num) * catLineHeight;
			gameObject.transform.localPosition = localPosition;
			categories[num] = item;
			catButtons.Add(gameObject);
			gameObject.SetActive(value: true);
			num++;
		}
		categoryScrollTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num * catLineHeight);
	}

	private void ClearUnitList()
	{
		foreach (GameObject unitButton in unitButtons)
		{
			Object.Destroy(unitButton);
		}
		unitButtons = new List<GameObject>();
		unitSelectCursor.gameObject.SetActive(value: false);
		SelectUnit(-1);
	}

	private void SetupUnitsList(ICollection<UnitCatalogue.Unit> unitList)
	{
		ClearUnitList();
		int num = 0;
		units = new string[unitList.Count];
		foreach (UnitCatalogue.Unit unit in unitList)
		{
			GameObject gameObject = Object.Instantiate(unitItemTemplate, unitScrollTf);
			VTEdImageListItem component = gameObject.GetComponent<VTEdImageListItem>();
			RenderTexture value = null;
			unitImages.TryGetValue(unit.prefabName, out value);
			component.Setup(num, unit.name, value, SelectUnit);
			Vector3 localPosition = gameObject.transform.localPosition;
			localPosition.y = (float)(-num) * unitLineHeight;
			gameObject.transform.localPosition = localPosition;
			units[num] = unit.prefabName;
			unitButtons.Add(gameObject);
			gameObject.SetActive(value: true);
			num++;
		}
		unitScrollTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num * unitLineHeight);
		unitScrollRect.ClampVertical();
	}

	private void SelectUnit(int idx)
	{
		if (idx >= 0 && idx == selectedUnitIdx && Time.time - lastSelectUnitTime < 0.4f)
		{
			OkayButton();
			return;
		}
		selectedUnitIdx = idx;
		if (idx >= 0)
		{
			lastSelectUnitTime = Time.time;
			unitSelectCursor.gameObject.SetActive(value: true);
			Vector3 localPosition = unitSelectCursor.localPosition;
			localPosition.y = (float)(-idx) * unitLineHeight;
			unitSelectCursor.localPosition = localPosition;
			okayButton.interactable = true;
			UnitCatalogue.Unit unit = UnitCatalogue.GetUnit(units[idx]);
			unitNameText.text = unit.name;
			unitDescriptionText.text = unit.description;
			if (unitImages.TryGetValue(unit.prefabName, out var value))
			{
				unitDescriptionImage.gameObject.SetActive(value: true);
				unitDescriptionImage.texture = value;
			}
			else
			{
				unitDescriptionImage.gameObject.SetActive(value: false);
			}
			descriptionObj.SetActive(value: true);
		}
		else
		{
			unitSelectCursor.gameObject.SetActive(value: false);
			okayButton.interactable = false;
			descriptionObj.SetActive(value: false);
		}
	}

	public void OkayButton()
	{
		UnitSpawner unitSpawner = editor.CreateNewUnit(units[selectedUnitIdx]);
		if ((bool)unitSpawner)
		{
			editor.unitsTab.RemoteOpenTab();
			editor.unitsTab.SelectUnitAndOpenOptions(unitSpawner);
		}
		CloseWindow();
	}

	public void OnEditSearch(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			if (!searchMode)
			{
				ClearCategoryList();
				GameObject gameObject = Object.Instantiate(categoryItemTemplate, categoryScrollTf);
				gameObject.GetComponentInChildren<Text>().text = "Search";
				catButtons.Add(gameObject);
				gameObject.SetActive(value: true);
				searchMode = true;
			}
			List<UnitCatalogue.Unit> allUnits = UnitCatalogue.catalogue[selectedTeam].allUnits;
			List<UnitCatalogue.Unit> list = new List<UnitCatalogue.Unit>();
			text = text.ToLower();
			foreach (UnitCatalogue.Unit item in allUnits)
			{
				if (!item.hideFromEditor && item.name.ToLower().Contains(text))
				{
					list.Add(item);
				}
			}
			SetupUnitsList(list);
		}
		else if (searchMode)
		{
			searchMode = false;
			if (selectedTeam == Teams.Allied)
			{
				AlliedButton();
			}
			else
			{
				EnemyButton();
			}
		}
	}

	private void CancelSearch()
	{
		searchField.text = string.Empty;
		searchMode = false;
	}
}
