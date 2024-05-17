using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdStaticObjectBrowser : MonoBehaviour
{
	private class StaticObjItem
	{
		public int idx;

		public VTStaticObject prefab;
	}

	public class CategoryButton : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public VTEdStaticObjectBrowser browser;

		public string category;

		public void OnPointerClick(PointerEventData eventData)
		{
			browser.SelectCategory(category);
		}
	}

	public VTScenarioEditor editor;

	public VTStaticObjectsWindow objectsWindow;

	public Button okayButton;

	[Header("Category")]
	public ScrollRect catScrollRect;

	public GameObject catItemTemplate;

	public Transform catSelectCursor;

	private float catlineHeight;

	[Header("Objects")]
	public ScrollRect objScrollRect;

	public GameObject objItemTemplate;

	public Transform objSelectCursor;

	private float objLineHeight;

	private List<GameObject> objListObjects = new List<GameObject>();

	[Header("Description")]
	public GameObject descriptionDisplayObj;

	public Text descriptionName;

	public Text descriptionText;

	public RawImage descriptionImage;

	private Dictionary<string, List<StaticObjItem>> categories;

	private List<string> categoryList;

	private Dictionary<string, RenderTexture> objectImages = new Dictionary<string, RenderTexture>();

	private bool hasSetup;

	public Camera thumbCam;

	private List<StaticObjItem> displayedObjects;

	private float objClickTime;

	private int selectedObjIdx;

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("staticObjectBrowser");
		if (!hasSetup)
		{
			StartCoroutine(InitialSetupRoutine());
		}
	}

	private IEnumerator InitialSetupRoutine()
	{
		catlineHeight = ((RectTransform)catItemTemplate.transform).rect.height;
		objLineHeight = ((RectTransform)objItemTemplate.transform).rect.height;
		Transform dummyTf = new GameObject().transform;
		editor.BlockEditor(dummyTf);
		string msgId = "loadingObjects";
		editor.popupMessages.DisplayPersistentMessage("Loading objects...", Color.white, msgId);
		yield return null;
		List<VTStaticObject> allStaticObjects = VTResources.GetAllStaticObjectPrefabs();
		categories = new Dictionary<string, List<StaticObjItem>>();
		categoryList = new List<string>();
		Stopwatch sw = new Stopwatch();
		sw.Start();
		for (int i = 0; i < allStaticObjects.Count; i++)
		{
			if (!allStaticObjects[i].editorOnly || VTResources.isEditorOrDevTools)
			{
				StaticObjItem staticObjItem = new StaticObjItem
				{
					idx = i,
					prefab = allStaticObjects[i]
				};
				string text = staticObjItem.prefab.category;
				if (string.IsNullOrEmpty(text))
				{
					text = "Other";
				}
				if (categories.TryGetValue(text, out var value))
				{
					value.Add(staticObjItem);
				}
				else
				{
					value = new List<StaticObjItem>();
					value.Add(staticObjItem);
					categories.Add(text, value);
					categoryList.Add(text);
				}
				GenerateImage(staticObjItem.prefab);
				if (sw.ElapsedMilliseconds > 20)
				{
					yield return null;
					sw.Restart();
				}
			}
		}
		categoryList.Sort();
		SetupCategories();
		SelectCategory(categoryList[0]);
		editor.popupMessages.RemovePersistentMessage(msgId);
		editor.UnblockEditor(dummyTf);
		Object.Destroy(dummyTf.gameObject);
		hasSetup = true;
	}

	private void GenerateImage(VTStaticObject prefab)
	{
		if (objectImages.ContainsKey(prefab.gameObject.name))
		{
			return;
		}
		GameObject gameObject = Object.Instantiate(prefab.gameObject);
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
		LODGroup[] componentsInChildren2 = gameObject.GetComponentsInChildren<LODGroup>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].enabled = false;
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
		objectImages.Add(prefab.gameObject.name, renderTexture);
		Object.DestroyImmediate(gameObject);
	}

	private void SetupCategories()
	{
		for (int i = 0; i < categoryList.Count; i++)
		{
			GameObject obj = Object.Instantiate(catItemTemplate, catScrollRect.content);
			obj.GetComponentInChildren<Text>().text = categoryList[i];
			obj.transform.localPosition = new Vector3(0f, (float)(-i) * catlineHeight, 0f);
			obj.SetActive(value: true);
			CategoryButton categoryButton = obj.AddComponent<CategoryButton>();
			categoryButton.browser = this;
			categoryButton.category = categoryList[i];
		}
		catScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)categoryList.Count * catlineHeight);
		catScrollRect.verticalNormalizedPosition = 1f;
		catItemTemplate.SetActive(value: false);
	}

	public void SelectCategory(string category)
	{
		int num = categoryList.IndexOf(category);
		catSelectCursor.transform.localPosition = new Vector3(0f, (float)(-num) * catlineHeight, 0f);
		SetupObjects(categories[category]);
	}

	private void SetupObjects(List<StaticObjItem> objectPrefabs)
	{
		displayedObjects = objectPrefabs;
		foreach (GameObject objListObject in objListObjects)
		{
			Object.Destroy(objListObject);
		}
		objListObjects.Clear();
		objItemTemplate.SetActive(value: false);
		for (int i = 0; i < objectPrefabs.Count; i++)
		{
			GameObject gameObject = Object.Instantiate(objItemTemplate, objScrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * objLineHeight, 0f);
			VTEdImageListItem component = gameObject.GetComponent<VTEdImageListItem>();
			RenderTexture value = null;
			objectImages.TryGetValue(objectPrefabs[i].prefab.gameObject.name, out value);
			component.Setup(i, objectPrefabs[i].prefab.objectName, value, SelectObject);
			objListObjects.Add(gameObject);
		}
		objScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)objectPrefabs.Count * objLineHeight);
		SelectObject(-1);
	}

	private void SelectObject(int objIdx)
	{
		objSelectCursor.transform.localPosition = new Vector3(0f, (float)(-objIdx) * objLineHeight, 0f);
		if (objIdx >= 0 && objIdx == selectedObjIdx && Time.time - objClickTime < 0.4f)
		{
			OkayButton();
			return;
		}
		selectedObjIdx = objIdx;
		if (objIdx >= 0)
		{
			objClickTime = Time.time;
			StaticObjItem staticObjItem = displayedObjects[objIdx];
			descriptionDisplayObj.SetActive(value: true);
			descriptionName.text = staticObjItem.prefab.objectName;
			descriptionText.text = staticObjItem.prefab.description;
			if (objectImages.TryGetValue(staticObjItem.prefab.gameObject.name, out var value))
			{
				descriptionImage.texture = value;
			}
			okayButton.interactable = true;
		}
		else
		{
			descriptionDisplayObj.SetActive(value: false);
			okayButton.interactable = false;
		}
	}

	public void OkayButton()
	{
		if (selectedObjIdx >= 0)
		{
			VTStaticObject prefab = displayedObjects[selectedObjIdx].prefab;
			VTStaticObject vTStaticObject = editor.currentScenario.staticObjects.CreateObject(prefab.gameObject);
			vTStaticObject.transform.position = editor.editorCamera.focusTransform.position;
			vTStaticObject.transform.rotation = editor.editorCamera.focusTransform.rotation;
			UnitSpawner.PlacementValidityInfo placementValidityInfo = vTStaticObject.TryPlaceInEditor(editor.editorCamera.cursorLocation);
			if (placementValidityInfo.isValid)
			{
				objectsWindow.SetupList();
				objectsWindow.SelectLast();
			}
			else
			{
				editor.currentScenario.staticObjects.RemoveObject(vTStaticObject.id);
				editor.confirmDialogue.DisplayConfirmation("Invalid Placement", placementValidityInfo.reason, null, null);
			}
		}
		Close();
	}

	public void CancelButton()
	{
		Close();
	}

	private void Close()
	{
		editor.UnblockEditor(base.transform);
		editor.editorCamera.inputLock.RemoveLock("staticObjectBrowser");
		base.gameObject.SetActive(value: false);
	}

	private void OnDestroy()
	{
		if (objectImages == null)
		{
			return;
		}
		foreach (RenderTexture value in objectImages.Values)
		{
			value.Release();
			Object.Destroy(value);
		}
	}
}
