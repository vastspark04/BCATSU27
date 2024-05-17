using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdAircraftEquipEditor : MonoBehaviour
{
	public delegate void ConfigSelectionDelegate(string[] equips);

	public class HardpointButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		public VTEdAircraftEquipEditor eqEditor;

		public int idx;

		public void OnClick()
		{
			eqEditor.EditHPButton(idx);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			eqEditor.HoverHP(idx);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			eqEditor.UnhoverHP(idx);
		}
	}

	public VTScenarioEditor editor;

	public GameObject hardpointTemplate;

	public ScrollRect hpScrollRect;

	public Text titleText;

	private string[] currentEquips;

	private string[] modifiedEquips;

	private Text[] hpDisplayTexts;

	private List<GameObject> hpObjs = new List<GameObject>();

	private Rect hpRect;

	private ConfigSelectionDelegate OnSelected;

	public RawImage orthoImage;

	private RenderTexture orthoRT;

	public Camera orthoCamera;

	public GameObject orthoHpTemplate;

	private List<HPEquippable> availableEquips;

	private List<HPEquippable> selectableEquips;

	private List<string> selectionOptions;

	private List<string> menuDisplayOptions;

	private int editingHPIdx;

	private List<Text> orthoHpObjs = new List<Text>();

	private void Awake()
	{
		hardpointTemplate.SetActive(value: false);
		hpRect = ((RectTransform)hardpointTemplate.transform).rect;
		orthoRT = new RenderTexture(512, 512, 16);
		orthoImage.texture = orthoRT;
		orthoCamera.enabled = false;
		orthoCamera.targetTexture = orthoRT;
	}

	private void OnDestroy()
	{
		if (orthoRT != null)
		{
			orthoRT.Release();
			Object.Destroy(orthoRT);
		}
	}

	public void Display(GameObject unitPrefab, string title, string[] currentConfigs, List<HPEquippable> availableEquips, ConfigSelectionDelegate onSelected)
	{
		base.gameObject.SetActive(value: true);
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("equipEditor");
		OnSelected = onSelected;
		titleText.text = title;
		this.availableEquips = availableEquips;
		foreach (GameObject hpObj in hpObjs)
		{
			Object.Destroy(hpObj);
		}
		hpObjs.Clear();
		currentEquips = new string[currentConfigs.Length];
		modifiedEquips = new string[currentConfigs.Length];
		hpDisplayTexts = new Text[currentConfigs.Length];
		int num = 0;
		int num2 = 0;
		int num3 = Mathf.CeilToInt((float)currentConfigs.Length / 3f);
		for (int i = 0; i < currentConfigs.Length; i++)
		{
			currentEquips[i] = currentConfigs[i];
			modifiedEquips[i] = currentConfigs[i];
			GameObject gameObject = Object.Instantiate(hardpointTemplate, hpScrollRect.content);
			gameObject.transform.localPosition = new Vector3((float)num * hpRect.width, (float)(-num2) * hpRect.height, 0f);
			num2++;
			if (num2 >= num3)
			{
				num2 = 0;
				num++;
			}
			gameObject.SetActive(value: true);
			gameObject.GetComponent<Text>().text = $"HP{i.ToString()}: ";
			HardpointButton hardpointButton = gameObject.AddComponent<HardpointButton>();
			hardpointButton.idx = i;
			hardpointButton.eqEditor = this;
			Button componentInChildren = gameObject.GetComponentInChildren<Button>();
			componentInChildren.onClick.AddListener(hardpointButton.OnClick);
			Text componentInChildren2 = componentInChildren.GetComponentInChildren<Text>();
			hpDisplayTexts[i] = componentInChildren2;
			if (!string.IsNullOrEmpty(currentConfigs[i]))
			{
				HPEquippable hPEquippable = null;
				foreach (HPEquippable availableEquip in availableEquips)
				{
					if (availableEquip.gameObject.name == currentConfigs[i])
					{
						hPEquippable = availableEquip;
						break;
					}
				}
				if ((bool)hPEquippable)
				{
					componentInChildren2.text = hPEquippable.fullName;
				}
				else
				{
					currentConfigs[i] = string.Empty;
					modifiedEquips[i] = string.Empty;
					componentInChildren2.text = "None";
				}
			}
			else
			{
				componentInChildren2.text = "None";
			}
			hpObjs.Add(gameObject);
		}
		hpScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num2 * hpRect.height);
		hpScrollRect.ClampVertical();
		CreateOrthoImage(unitPrefab);
	}

	private string GetHoverInfo(int idx)
	{
		if (idx == 0)
		{
			return string.Empty;
		}
		int index = idx - 1;
		HPEquippable hPEquippable = selectableEquips[index];
		return hPEquippable.fullName + "\n\n" + hPEquippable.GetLocalizedDescription();
	}

	public void HoverHP(int eqIdx)
	{
		orthoHpObjs[eqIdx].color = Color.green;
		orthoHpObjs[eqIdx].transform.localScale = orthoHpTemplate.transform.localScale * 1.5f;
		orthoHpObjs[eqIdx].transform.SetAsLastSibling();
	}

	public void UnhoverHP(int eqIdx)
	{
		orthoHpObjs[eqIdx].color = Color.white;
		orthoHpObjs[eqIdx].transform.localScale = orthoHpTemplate.transform.localScale;
	}

	public void EditHPButton(int idx)
	{
		selectionOptions = new List<string>();
		menuDisplayOptions = new List<string>();
		editingHPIdx = idx;
		int selected = 0;
		selectionOptions.Add(string.Empty);
		menuDisplayOptions.Add("None");
		selectableEquips = new List<HPEquippable>();
		int num = 1;
		foreach (HPEquippable availableEquip in availableEquips)
		{
			int compatibilityMask = LoadoutConfigurator.EquipCompatibilityMask(availableEquip);
			if (IsCompatibleWithHardpoint(compatibilityMask, idx))
			{
				selectionOptions.Add(availableEquip.gameObject.name);
				selectableEquips.Add(availableEquip);
				menuDisplayOptions.Add(availableEquip.fullName);
				if (modifiedEquips[idx] == availableEquip.gameObject.name)
				{
					selected = num;
				}
				num++;
			}
		}
		editor.optionSelector.Display("HP" + idx, menuDisplayOptions.ToArray(), selected, OnEquipSelected, GetHoverInfo);
	}

	private void OnEquipSelected(int selectionIdx)
	{
		modifiedEquips[editingHPIdx] = selectionOptions[selectionIdx];
		if (!string.IsNullOrEmpty(menuDisplayOptions[selectionIdx]))
		{
			hpDisplayTexts[editingHPIdx].text = menuDisplayOptions[selectionIdx];
		}
		else
		{
			hpDisplayTexts[editingHPIdx].text = "None";
		}
	}

	private bool IsCompatibleWithHardpoint(int compatibilityMask, int hpIdx)
	{
		int num = 1 << hpIdx;
		return (compatibilityMask & num) == num;
	}

	public void OkayButton()
	{
		Close();
		if (OnSelected != null)
		{
			OnSelected(modifiedEquips);
		}
	}

	public void CancelButton()
	{
		Close();
		if (OnSelected != null)
		{
			OnSelected(currentEquips);
		}
	}

	private void Close()
	{
		base.gameObject.SetActive(value: false);
		editor.editorCamera.inputLock.RemoveLock("equipEditor");
		editor.UnblockEditor(base.transform);
	}

	private void CreateOrthoImage(GameObject unitPrefab)
	{
		if (!unitPrefab)
		{
			orthoImage.enabled = false;
			return;
		}
		orthoImage.enabled = true;
		VTScenarioEditor.isLoadingPreviewThumbnails = true;
		GameObject gameObject = Object.Instantiate(unitPrefab.gameObject);
		gameObject.transform.position = Vector3.zero;
		gameObject.transform.rotation = Quaternion.identity;
		gameObject.SetActive(value: true);
		Bounds bounds = default(Bounds);
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in componentsInChildren)
		{
			if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer) && (renderer.gameObject.layer == 0 || renderer.gameObject.layer == 8) && !renderer.gameObject.name.ToLower().Contains("lod"))
			{
				renderer.gameObject.layer = 26;
				bounds.Encapsulate(renderer.bounds);
			}
		}
		LODGroup[] componentsInChildren2 = gameObject.GetComponentsInChildren<LODGroup>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].enabled = false;
		}
		Camera camera = orthoCamera;
		orthoCamera.orthographic = true;
		orthoCamera.orthographicSize = bounds.extents.x * 1.05f;
		camera.cullingMask = 67108864;
		camera.gameObject.SetActive(value: true);
		float z = bounds.size.z;
		float num = z / 2f;
		Vector3 vector2 = (camera.transform.position = new Vector3(0f, 0f, bounds.center.z + num + 1f));
		camera.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
		camera.nearClipPlane = 0.5f;
		camera.farClipPlane = z + 2f;
		RenderTexture renderTexture2 = (camera.targetTexture = orthoRT);
		camera.Render();
		camera.targetTexture = null;
		camera.gameObject.SetActive(value: false);
		foreach (Text orthoHpObj in orthoHpObjs)
		{
			Object.Destroy(orthoHpObj.gameObject);
		}
		orthoHpObjs.Clear();
		orthoHpTemplate.SetActive(value: false);
		WeaponManager component = gameObject.GetComponent<WeaponManager>();
		if ((bool)component)
		{
			Vector3 zero = Vector3.zero;
			int num2 = 0;
			Transform[] hardpointTransforms = component.hardpointTransforms;
			foreach (Transform transform in hardpointTransforms)
			{
				GameObject obj = Object.Instantiate(orthoHpTemplate, orthoHpTemplate.transform.parent);
				obj.SetActive(value: true);
				Vector3 vector3 = new Vector3(0f - transform.transform.position.x, transform.transform.position.y, 0f);
				vector3 = vector3 / orthoCamera.orthographicSize * (orthoImage.rectTransform.rect.width / 2f);
				obj.transform.localPosition = vector3;
				zero += vector3;
				Text component2 = obj.GetComponent<Text>();
				component2.text = num2.ToString();
				orthoHpObjs.Add(component2);
				num2++;
			}
			float y = 0f - (zero / num2).y;
			orthoImage.transform.localPosition = new Vector3(0f, y, 0f);
		}
		Object.DestroyImmediate(gameObject);
		VTScenarioEditor.isLoadingPreviewThumbnails = false;
	}
}
