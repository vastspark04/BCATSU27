using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTEdMultiSelector : MonoBehaviour
{
	public delegate void OnMultiSelected(int[] selectedIndices);

	public VTScenarioEditor editor;

	public GameObject itemTemplate;

	public RectTransform contentTf;

	private ScrollRect scrollRect;

	public Text titleText;

	private OnMultiSelected OnComplete;

	private List<int> preselectedIndices;

	private List<VTBoolProperty> boolItems = new List<VTBoolProperty>();

	private float lineHeight;

	private void Awake()
	{
		itemTemplate.SetActive(value: false);
		lineHeight = ((RectTransform)itemTemplate.transform).rect.height;
		scrollRect = contentTf.GetComponentInParent<ScrollRect>();
	}

	public void Display(string title, string[] options, List<int> selectedIndices, OnMultiSelected onComplete)
	{
		Open();
		OnComplete = onComplete;
		preselectedIndices = selectedIndices;
		titleText.text = title;
		foreach (VTBoolProperty boolItem in boolItems)
		{
			Object.Destroy(boolItem.gameObject);
		}
		boolItems = new List<VTBoolProperty>();
		for (int i = 0; i < options.Length; i++)
		{
			GameObject obj = Object.Instantiate(itemTemplate, contentTf);
			obj.SetActive(value: true);
			obj.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			VTBoolProperty component = obj.GetComponent<VTBoolProperty>();
			bool flag = selectedIndices.Contains(i);
			component.SetInitialValue(flag);
			component.SetLabel(options[i]);
			boolItems.Add(component);
		}
		contentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)options.Length * lineHeight);
		scrollRect.ClampVertical();
	}

	public void OkayButton()
	{
		Close();
		if (OnComplete == null)
		{
			return;
		}
		List<int> list = new List<int>();
		for (int i = 0; i < boolItems.Count; i++)
		{
			if ((bool)boolItems[i].GetValue())
			{
				list.Add(i);
			}
		}
		OnComplete(list.ToArray());
	}

	public void CancelButton()
	{
		Close();
		if (OnComplete != null)
		{
			OnComplete(preselectedIndices.ToArray());
		}
	}

	public void AllButton()
	{
		foreach (VTBoolProperty boolItem in boolItems)
		{
			boolItem.SetInitialValue(true);
		}
	}

	public void NoneButton()
	{
		foreach (VTBoolProperty boolItem in boolItems)
		{
			boolItem.SetInitialValue(false);
		}
	}

	private void Open()
	{
		base.gameObject.SetActive(value: true);
		if ((bool)editor)
		{
			editor.BlockEditor(base.transform);
			editor.editorCamera.inputLock.AddLock("multiSelector");
		}
	}

	private void Close()
	{
		base.gameObject.SetActive(value: false);
		if ((bool)editor)
		{
			editor.UnblockEditor(base.transform);
			editor.editorCamera.inputLock.RemoveLock("multiSelector");
		}
	}
}
