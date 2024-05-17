using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTMapEdObjectCategoryList : MonoBehaviour
{
	public class CatListItem : MonoBehaviour
	{
		public int idx;

		public VTMapEdObjectCategoryList catList;

		public void OnClick()
		{
			catList.SelectItem(idx);
		}
	}

	public VTMapEdObjectsTab objectsTab;

	public VTMapEdPrefabSelector prefabSelector;

	public ScrollRect scrollRect;

	public GameObject itemTemplate;

	private List<GameObject> listObjs = new List<GameObject>();

	private float lineHeight;

	private List<string> cats;

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		SetupList();
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		objectsTab.Open();
	}

	private void SetupList()
	{
		lineHeight = ((RectTransform)itemTemplate.transform).rect.height;
		foreach (GameObject listObj in listObjs)
		{
			Object.Destroy(listObj);
		}
		listObjs.Clear();
		cats = VTMapEdResources.GetAllCategories();
		for (int i = 0; i < cats.Count; i++)
		{
			GameObject gameObject = Object.Instantiate(itemTemplate, scrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			string text = cats[i].ToLower();
			text = text.Substring(0, 1).ToUpper() + text.Substring(1, text.Length - 1);
			gameObject.GetComponentInChildren<Text>().text = text;
			CatListItem catListItem = gameObject.AddComponent<CatListItem>();
			catListItem.idx = i;
			catListItem.catList = this;
			gameObject.GetComponentInChildren<Button>().onClick.AddListener(catListItem.OnClick);
			listObjs.Add(gameObject);
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineHeight * (float)cats.Count);
		scrollRect.ClampVertical();
		itemTemplate.SetActive(value: false);
	}

	public void SelectItem(int idx)
	{
		base.gameObject.SetActive(value: false);
		prefabSelector.Open(cats[idx]);
	}
}
