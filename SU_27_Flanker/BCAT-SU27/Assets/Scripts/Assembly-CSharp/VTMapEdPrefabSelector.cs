using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTMapEdPrefabSelector : MonoBehaviour
{
	public VTMapEditor editor;

	public VTMapEdObjectCategoryList catList;

	public string category;

	public Text titleText;

	public ScrollRect scrollRect;

	public GameObject itemTemplate;

	public GameObject placePrefabButton;

	public GameObject cancelPlacementButton;

	private VTMapEdPrefab[] prefabs;

	private List<RenderTexture> thumbTextures = new List<RenderTexture>();

	private List<GameObject> listObjs = new List<GameObject>();

	public GameObject loadingObject;

	public Transform loadBar;

	public void Open(string category)
	{
		base.gameObject.SetActive(value: true);
		if (this.category != category || prefabs == null)
		{
			this.category = category;
			prefabs = VTMapEdResources.GetPrefabs(category);
		}
		titleText.text = category;
		StartCoroutine(SetupListRoutine());
	}

	public void Close()
	{
		foreach (RenderTexture thumbTexture in thumbTextures)
		{
			thumbTexture.Release();
			Object.Destroy(thumbTexture);
		}
		thumbTextures.Clear();
		foreach (GameObject listObj in listObjs)
		{
			Object.Destroy(listObj);
		}
		listObjs.Clear();
		base.gameObject.SetActive(value: false);
		catList.Open();
	}

	private IEnumerator SetupListRoutine()
	{
		loadingObject.SetActive(value: true);
		float lineHeight = ((RectTransform)itemTemplate.transform).rect.height;
		for (int i = 0; i < prefabs.Length; i++)
		{
			GameObject gameObject = Object.Instantiate(itemTemplate, scrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			VTMapEdPrefab obj = prefabs[i];
			VTMapEdPrefab component = obj.GetComponent<VTMapEdPrefab>();
			VTMapEdPrefabListItem component2 = gameObject.GetComponent<VTMapEdPrefabListItem>();
			if ((bool)component)
			{
				component2.nameText.text = component.prefabName;
				component2.descriptionText.text = component.prefabDescription;
			}
			else
			{
				component2.nameText.text = "Unknown";
				component2.descriptionText.text = "ERROR: No info.  Please notify the developer.";
			}
			component2.selector = this;
			component2.idx = i;
			GameObject gameObject2 = Object.Instantiate(obj.gameObject);
			gameObject2.transform.position = Vector3.zero;
			gameObject2.transform.rotation = Quaternion.identity;
			gameObject2.SetActive(value: true);
			Bounds bounds = default(Bounds);
			Renderer[] componentsInChildren = gameObject2.GetComponentsInChildren<Renderer>();
			foreach (Renderer renderer in componentsInChildren)
			{
				renderer.gameObject.layer = 26;
				bounds.Encapsulate(renderer.bounds);
			}
			Camera thumbnailCamera = editor.thumbnailCamera;
			thumbnailCamera.cullingMask = 67108864;
			thumbnailCamera.gameObject.SetActive(value: true);
			float num = bounds.size.x * 6f;
			Vector3 vector2 = (thumbnailCamera.transform.position = new Vector3(0f - num, num, num));
			thumbnailCamera.transform.LookAt(bounds.center);
			thumbnailCamera.nearClipPlane = Mathf.Min(1f, num * 0.5f);
			thumbnailCamera.farClipPlane = Mathf.Max(1000f, num * 2f);
			float a = Vector3.Angle(bounds.center + bounds.extents - vector2, bounds.center - bounds.extents - vector2) * 1.25f;
			thumbnailCamera.fieldOfView = Mathf.Min(a, 60f);
			RenderTexture renderTexture2 = (thumbnailCamera.targetTexture = new RenderTexture(128, 128, 8));
			thumbnailCamera.Render();
			thumbnailCamera.targetTexture = null;
			thumbnailCamera.gameObject.SetActive(value: false);
			thumbTextures.Add(renderTexture2);
			component2.thumbImage.texture = renderTexture2;
			Object.Destroy(gameObject2);
			listObjs.Add(gameObject);
			float x = (float)(i + 1) / (float)prefabs.Length;
			loadBar.localScale = new Vector3(x, 1f, 1f);
			yield return null;
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineHeight * (float)prefabs.Length);
		scrollRect.ClampVertical();
		loadingObject.SetActive(value: false);
		itemTemplate.SetActive(value: false);
	}

	public void SelectPrefab(int idx)
	{
		editor.BeginPlacingPrefab(prefabs[idx].gameObject);
		placePrefabButton.SetActive(value: true);
		cancelPlacementButton.SetActive(value: true);
	}

	public void CancelPlacement()
	{
		editor.CancelPlacement();
		cancelPlacementButton.SetActive(value: false);
		placePrefabButton.SetActive(value: false);
	}

	public void PlacePrefab()
	{
		if (editor.PlacePrefab())
		{
			cancelPlacementButton.SetActive(value: false);
			placePrefabButton.SetActive(value: false);
		}
	}
}
