using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTEdEquipmentEditor : MonoBehaviour
{
	public delegate void ConfigSelectionDelegate(string[] equips);

	public class HardpointButton : MonoBehaviour
	{
		public VTEdEquipmentEditor eqEditor;

		public int idx;

		public void OnClick()
		{
			eqEditor.EditHPButton(idx);
		}
	}

	public VTScenarioEditor editor;

	public GameObject hardpointTemplate;

	public RectTransform hpContentTf;

	private ScrollRect hpScrollRect;

	public Text titleText;

	private ConfigSelectionDelegate OnSelected;

	private List<GameObject> hpObjs = new List<GameObject>();

	private string[] currentEquips;

	private string[] modifiedEquips;

	private Text[] hpDisplayTexts;

	private float hpHeight;

	private List<string> selectionOptions;

	private List<string> menuDisplayOptions;

	private int editingHPIdx;

	private List<HPEquippable> availableEquips;

	private List<HPEquippable> selectableEquips;

	private void Awake()
	{
		hardpointTemplate.SetActive(value: false);
		hpHeight = ((RectTransform)hardpointTemplate.transform).rect.height;
		hpScrollRect = hpContentTf.GetComponentInParent<ScrollRect>();
	}

	public void Display(string title, string[] currentConfigs, List<HPEquippable> availableEquips, ConfigSelectionDelegate onSelected)
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
		for (int i = 0; i < currentConfigs.Length; i++)
		{
			currentEquips[i] = currentConfigs[i];
			modifiedEquips[i] = currentConfigs[i];
			GameObject gameObject = Object.Instantiate(hardpointTemplate, hpContentTf);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * hpHeight, 0f);
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
		hpContentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)currentEquips.Length * hpHeight);
		hpScrollRect.ClampVertical();
	}

	private string GetHoverInfo(int idx)
	{
		if (idx == 0)
		{
			return string.Empty;
		}
		HPEquippable hPEquippable = selectableEquips[idx - 1];
		return hPEquippable.fullName + "\n\n" + hPEquippable.GetLocalizedDescription();
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
}
