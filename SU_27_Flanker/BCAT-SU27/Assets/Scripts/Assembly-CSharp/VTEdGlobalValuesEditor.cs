using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdGlobalValuesEditor : MonoBehaviour
{
	public delegate void GlobalValueDelegate(GlobalValue gv);

	private class GVListItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public ScenarioGlobalValues.GlobalValueData gv;

		public VTEdGlobalValuesEditor editor;

		private float clickTime;

		public void OnPointerClick(PointerEventData eventData)
		{
			editor.Select(gv);
			if (Time.unscaledTime - clickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				editor.AcceptSelect();
			}
			else
			{
				clickTime = Time.unscaledTime;
			}
		}
	}

	public VTScenarioEditor editor;

	public ScrollRect scrollRect;

	public GameObject selectorObj;

	public GameObject nonSelectorObj;

	public GameObject editorPanel;

	public GameObject itemTemplate;

	public Transform selTf;

	private float entryHeight;

	public Button[] itemDependentButtons;

	[Header("Editor")]
	public Text nameText;

	public InputField descriptionField;

	public VTFloatRangeProperty initialValProp;

	private bool isSelector;

	private int currSelected = -1;

	private List<GameObject> listObjs = new List<GameObject>();

	private List<ScenarioGlobalValues.GlobalValueData> globalValues;

	public event GlobalValueDelegate OnGlobalValueEdited;

	private event GlobalValueDelegate OnSelected;

	private void Awake()
	{
		initialValProp.OnPropertyValueChanged += InitialValProp_OnPropertyValueChanged;
		itemTemplate.SetActive(value: false);
		initialValProp.min = -99999f;
		initialValProp.max = 99999f;
	}

	private void InitialValProp_OnPropertyValueChanged(object arg0)
	{
		if (currSelected >= 0)
		{
			globalValues[currSelected].initialValue = Mathf.RoundToInt((float)arg0);
		}
	}

	public void OpenSelector(GlobalValueDelegate onSelected, GlobalValue currentSelected)
	{
		base.gameObject.SetActive(value: true);
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("GVEditor");
		entryHeight = ((RectTransform)itemTemplate.transform).rect.height;
		this.OnSelected = onSelected;
		isSelector = true;
		nonSelectorObj.SetActive(value: false);
		selectorObj.SetActive(value: true);
		PopulateList();
		Select(currentSelected.data);
	}

	public void OpenEditor()
	{
		base.gameObject.SetActive(value: true);
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("GVEditor");
		entryHeight = ((RectTransform)itemTemplate.transform).rect.height;
		isSelector = false;
		nonSelectorObj.SetActive(value: true);
		selectorObj.SetActive(value: false);
		PopulateList();
		Select(null);
	}

	public void AcceptSelect()
	{
		if (isSelector)
		{
			if (currSelected >= 0)
			{
				this.OnSelected?.Invoke(globalValues[currSelected].GetReference());
			}
			else
			{
				this.OnSelected?.Invoke(GlobalValue.none);
			}
			Close();
		}
	}

	public void DeleteButton()
	{
		editor.confirmDialogue.DisplayConfirmation("Delete?", "Are you sure you want to delete this value?  Any event or conditional that references it will no longer work properly!", FinallyDelete, null);
	}

	private void FinallyDelete()
	{
		GlobalValue reference = globalValues[currSelected].GetReference();
		editor.currentScenario.globalValues.DeleteValue(reference);
		PopulateList();
		Select(null);
		this.OnGlobalValueEdited?.Invoke(reference);
	}

	public void NewButton()
	{
		GlobalValue globalValue = editor.currentScenario.globalValues.CreateNewValue();
		PopulateList();
		Select(globalValue.data);
	}

	public void CancelSelect()
	{
		Close();
	}

	public void Close()
	{
		editor.UnblockEditor(base.transform);
		editor.editorCamera.inputLock.RemoveLock("GVEditor");
		base.gameObject.SetActive(value: false);
	}

	private void Select(ScenarioGlobalValues.GlobalValueData gv)
	{
		if (gv != null)
		{
			int num = globalValues.IndexOf(gv);
			selTf.gameObject.SetActive(value: true);
			selTf.transform.localPosition = new Vector3(0f, (float)(-num) * entryHeight, 0f);
			currSelected = num;
			SetupEditor(gv);
			itemDependentButtons.SetInteractable(interactable: true);
		}
		else
		{
			selTf.gameObject.SetActive(value: false);
			editorPanel.SetActive(value: false);
			currSelected = -1;
			itemDependentButtons.SetInteractable(interactable: false);
		}
	}

	private void SetupEditor(ScenarioGlobalValues.GlobalValueData gv)
	{
		editorPanel.SetActive(value: true);
		nameText.text = gv.name;
		descriptionField.text = gv.description;
		initialValProp.SetInitialValue((float)gv.initialValue);
	}

	public void EditNameButton()
	{
		editor.textInputWindow.Display("Name", "Enter a name for the value", globalValues[currSelected].name, 24, OnEditedName);
	}

	public void OnEditedDescription(string s)
	{
		if (currSelected >= 0)
		{
			globalValues[currSelected].description = s;
		}
	}

	private void OnEditedName(string n)
	{
		globalValues[currSelected].name = n;
		listObjs[currSelected].GetComponentInChildren<Text>().text = n;
		nameText.text = n;
		this.OnGlobalValueEdited?.Invoke(globalValues[currSelected].GetReference());
	}

	private void PopulateList()
	{
		foreach (GameObject listObj in listObjs)
		{
			Object.Destroy(listObj);
		}
		listObjs.Clear();
		globalValues = editor.currentScenario.globalValues.GetAllDatas();
		for (int i = 0; i < globalValues.Count; i++)
		{
			GameObject gameObject = Object.Instantiate(itemTemplate, scrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * entryHeight, 0f);
			gameObject.GetComponentInChildren<Text>().text = globalValues[i].name;
			GVListItem gVListItem = gameObject.AddComponent<GVListItem>();
			gVListItem.gv = globalValues[i];
			gVListItem.editor = this;
			listObjs.Add(gameObject);
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)globalValues.Count * entryHeight);
	}
}
