using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdResourceBrowser : MonoBehaviour
{
	public class BrowserItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public int idx;

		public VTEdResourceBrowser browser;

		private float timeClicked = -1f;

		public void OnPointerClick(PointerEventData d)
		{
			browser.SelectIdx(idx);
			if (Time.unscaledTime - timeClicked < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				browser.OpenButton();
			}
			timeClicked = Time.unscaledTime;
		}
	}

	public VTScenarioEditor editor;

	public Text titleText;

	public GameObject fileItemTemplate;

	public GameObject directoryItemTemplate;

	public Transform selectionTransform;

	public Button openButton;

	public Button backButton;

	public ScrollRect scrollRect;

	public Text currPathText;

	public Text supportedExtsText;

	private string[] extFilters;

	private Action<string> OnSelectedFile;

	private string lastDir = string.Empty;

	private List<string> availablePaths = new List<string>();

	private List<GameObject> listObjects = new List<GameObject>();

	private float lineHeight;

	private int selectedIdx = -1;

	public void OpenBrowser(string title, Action<string> onSelected, string[] extensionFilters)
	{
		if ((bool)editor)
		{
			editor.BlockEditor(base.transform);
			editor.editorCamera.inputLock.AddLock("resourceBrowser");
		}
		fileItemTemplate.SetActive(value: false);
		directoryItemTemplate.SetActive(value: false);
		lineHeight = ((RectTransform)selectionTransform).rect.height;
		selectionTransform.gameObject.SetActive(value: false);
		base.gameObject.SetActive(value: true);
		titleText.text = title;
		openButton.interactable = false;
		OnSelectedFile = onSelected;
		extFilters = extensionFilters;
		EnsureResourceDir();
		if (!string.IsNullOrEmpty(lastDir))
		{
			OpenDir(lastDir);
		}
		else
		{
			OpenDir(VTResources.vtEditResourceDir);
			backButton.interactable = false;
		}
		if (extFilters == null)
		{
			supportedExtsText.text = ".png, .jpg, .ogg";
			return;
		}
		string text = string.Empty;
		for (int i = 0; i < extFilters.Length; i++)
		{
			text += extFilters[i];
			if (i < extFilters.Length - 1)
			{
				text += ", ";
			}
		}
		supportedExtsText.text = text;
	}

	private void OpenDir(string path)
	{
		if (!Directory.Exists(path))
		{
			OpenDir(VTResources.vtEditResourceDir);
			return;
		}
		lastDir = path;
		currPathText.text = lastDir.Substring(lastDir.IndexOf("EditorResources"));
		foreach (GameObject listObject in listObjects)
		{
			UnityEngine.Object.Destroy(listObject);
		}
		listObjects.Clear();
		availablePaths.Clear();
		int num = 0;
		string[] directories = Directory.GetDirectories(path);
		foreach (string text in directories)
		{
			availablePaths.Add(text);
			GameObject gameObject = UnityEngine.Object.Instantiate(directoryItemTemplate, scrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-num) * lineHeight, 0f);
			BrowserItem browserItem = gameObject.AddComponent<BrowserItem>();
			browserItem.idx = num;
			browserItem.browser = this;
			gameObject.GetComponent<Text>().text = Path.GetFileName(text);
			listObjects.Add(gameObject);
			num++;
		}
		directories = Directory.GetFiles(path);
		foreach (string text2 in directories)
		{
			string fileName = Path.GetFileName(text2);
			if (PassesFilter(fileName))
			{
				availablePaths.Add(text2);
				GameObject gameObject2 = UnityEngine.Object.Instantiate(fileItemTemplate, scrollRect.content);
				gameObject2.SetActive(value: true);
				gameObject2.transform.localPosition = new Vector3(0f, (float)(-num) * lineHeight, 0f);
				BrowserItem browserItem2 = gameObject2.AddComponent<BrowserItem>();
				browserItem2.idx = num;
				browserItem2.browser = this;
				gameObject2.GetComponent<Text>().text = fileName;
				listObjects.Add(gameObject2);
				num++;
			}
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num * lineHeight);
		selectionTransform.gameObject.SetActive(value: false);
		openButton.interactable = false;
	}

	private bool PassesFilter(string filename)
	{
		if (extFilters == null)
		{
			return true;
		}
		string text = Path.GetExtension(filename).ToLower();
		for (int i = 0; i < extFilters.Length; i++)
		{
			if (text == extFilters[i])
			{
				return true;
			}
		}
		return false;
	}

	private void EnsureResourceDir()
	{
		if (!Directory.Exists(VTResources.vtEditResourceDir))
		{
			Directory.CreateDirectory(VTResources.vtEditResourceDir);
		}
	}

	public void SelectIdx(int idx)
	{
		selectedIdx = idx;
		openButton.interactable = true;
		selectionTransform.gameObject.SetActive(value: true);
		selectionTransform.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
	}

	public void OpenButton()
	{
		string text = availablePaths[selectedIdx];
		if (IsDirectory(text))
		{
			OpenDir(text);
			backButton.interactable = true;
			return;
		}
		if (OnSelectedFile != null)
		{
			OnSelectedFile(text);
		}
		Close();
	}

	public void BackButton()
	{
		string fullName = Directory.GetParent(lastDir).FullName;
		if (fullName == VTResources.vtEditResourceDir)
		{
			backButton.interactable = false;
		}
		OpenDir(fullName);
	}

	public void CancelButton()
	{
		Close();
	}

	private void Close()
	{
		if ((bool)editor)
		{
			editor.UnblockEditor(base.transform);
			editor.editorCamera.inputLock.RemoveLock("resourceBrowser");
		}
		base.gameObject.SetActive(value: false);
	}

	private bool IsDirectory(string path)
	{
		return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
	}
}
