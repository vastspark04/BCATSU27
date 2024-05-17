using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdOptionSelector : MonoBehaviour
{
	public delegate void OptionSelectionDelegate(int selected);

	public delegate void OptionSelectionObjectDelegate(object selected);

	public delegate string OptionHoverInfoDelegate(int itemIdx);

	public class VTEdOptionSelectorItem : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		public VTEdOptionSelector selector;

		public int idx;

		public OptionHoverInfoDelegate onHover;

		public void OnClick()
		{
			selector.SelectItem(idx);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (onHover != null)
			{
				string text = onHover(idx);
				if (!string.IsNullOrEmpty(text))
				{
					selector.DisplayInfo(text);
				}
				else
				{
					selector.HideInfo();
				}
			}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			selector.HideInfo();
		}
	}

	public VTScenarioEditor editor;

	public Text titleText;

	public GameObject itemTemplate;

	public RectTransform contentTf;

	private ScrollRect scrollRect;

	public GameObject infoPanelObj;

	public Text infoPanelText;

	public Color defaultColor;

	public Color selectedColor;

	private OptionSelectionDelegate OnSelected;

	private OptionSelectionObjectDelegate OnSelectedObject;

	private OptionHoverInfoDelegate OnItemHovered;

	private bool objectMode;

	private List<GameObject> listObjects = new List<GameObject>();

	private object[] returnValues;

	private int currentValue;

	private float itemHeight;

	private void Awake()
	{
		itemHeight = ((RectTransform)itemTemplate.transform).rect.height;
		itemTemplate.SetActive(value: false);
		scrollRect = contentTf.GetComponentInParent<ScrollRect>();
		HideInfo();
	}

	public void Display(string title, string[] options, int selected, OptionSelectionDelegate onSelected, OptionHoverInfoDelegate onItemHovered = null)
	{
		titleText.text = title;
		OnSelected = onSelected;
		OnItemHovered = onItemHovered;
		objectMode = false;
		currentValue = selected;
		Open();
		SetupUI(options, selected);
	}

	public void Display(string title, string[] options, object[] returnValues, int selected, OptionSelectionObjectDelegate onSelected, OptionHoverInfoDelegate onItemHovered = null)
	{
		titleText.text = title;
		OnSelectedObject = onSelected;
		OnItemHovered = onItemHovered;
		objectMode = true;
		currentValue = selected;
		Open();
		this.returnValues = new object[returnValues.Length];
		for (int i = 0; i < returnValues.Length; i++)
		{
			this.returnValues[i] = returnValues[i];
		}
		SetupUI(options, selected);
	}

	private void SetupUI(string[] options, int selected)
	{
		foreach (GameObject listObject in listObjects)
		{
			Object.Destroy(listObject.gameObject);
		}
		listObjects = new List<GameObject>();
		for (int i = 0; i < options.Length; i++)
		{
			GameObject gameObject = Object.Instantiate(itemTemplate, contentTf);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * itemHeight, 0f);
			listObjects.Add(gameObject);
			VTEdOptionSelectorItem vTEdOptionSelectorItem = gameObject.AddComponent<VTEdOptionSelectorItem>();
			vTEdOptionSelectorItem.idx = i;
			vTEdOptionSelectorItem.selector = this;
			vTEdOptionSelectorItem.onHover = OnItemHovered;
			gameObject.GetComponent<Button>().onClick.AddListener(vTEdOptionSelectorItem.OnClick);
			gameObject.GetComponentInChildren<Text>().text = options[i];
			gameObject.GetComponent<Image>().color = ((selected == i) ? selectedColor : defaultColor);
		}
		contentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)listObjects.Count * itemHeight);
		scrollRect.ClampVertical();
	}

	public void SelectItem(int idx)
	{
		Close();
		if (objectMode)
		{
			if (idx >= 0)
			{
				OnSelectedObject(returnValues[idx]);
			}
			else
			{
				OnSelectedObject(null);
			}
		}
		else if (OnSelected != null)
		{
			OnSelected(idx);
		}
	}

	private void Open()
	{
		HideInfo();
		base.gameObject.SetActive(value: true);
		if ((bool)editor)
		{
			editor.BlockEditor(base.transform);
			editor.editorCamera.inputLock.AddLock("optionSelector");
		}
	}

	private void Close()
	{
		base.gameObject.SetActive(value: false);
		if ((bool)editor)
		{
			editor.UnblockEditor(base.transform);
			editor.editorCamera.inputLock.RemoveLock("optionSelector");
		}
	}

	public void Cancel()
	{
		SelectItem(currentValue);
	}

	public void DisplayInfo(string info)
	{
		infoPanelObj.SetActive(value: true);
		infoPanelText.text = info;
	}

	public void HideInfo()
	{
		infoPanelObj.SetActive(value: false);
	}
}
