using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTMapMenuOpenUI : MonoBehaviour
{
	public class MapListItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public VTMapMenuOpenUI menu;

		public int idx;

		public void OnPointerClick(PointerEventData e)
		{
			menu.SelectMap(idx);
		}
	}

	public VTMapEditorMenu editorMenu;

	public ScrollRect scrollRect;

	public GameObject listItemTemplate;

	public Transform selectionTf;

	private List<GameObject> listObjs = new List<GameObject>();

	private float lineHeight;

	public RawImage previewImage;

	public Text nameText;

	public Button[] itemDependentButtons;

	public GameObject loadingObj;

	public Transform loadingBar;

	public TextBackgroundFitter descriptionText;

	public bool allowBuiltInMaps;

	public bool allowWorkshopMaps;

	private List<VTMap> cMaps;

	private int currIdx;

	private List<VTMapCustom> swMaps;

	public event Action<VTMap> OnMapSelected;

	public event Action OnBackButtonPressed;

	private void Start()
	{
	}

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		foreach (GameObject listObj in listObjs)
		{
			UnityEngine.Object.Destroy(listObj);
		}
		listObjs.Clear();
		VTResources.LoadMaps();
		if (SteamClient.IsValid && allowWorkshopMaps)
		{
			StartCoroutine(LoadSWMapsRoutine());
		}
		else
		{
			SetupList();
		}
	}

	private IEnumerator LoadSWMapsRoutine()
	{
		Debug.Log("Map UI is loading SW maps");
		if ((bool)loadingObj)
		{
			loadingObj.SetActive(value: true);
		}
		if ((bool)loadingBar)
		{
			loadingBar.transform.localScale = new Vector3(0f, 1f, 1f);
		}
		VTResources.WorkshopMapListRequest req = VTResources.LoadSteamWorkshopMaps(previewsOnly: true);
		while (!req.isDone)
		{
			if ((bool)loadingBar)
			{
				loadingBar.transform.localScale = new Vector3(req.progress, 1f, 1f);
			}
			yield return null;
		}
		swMaps = req.maps;
		Debug.Log($"Map UI's request completed with {swMaps.Count} maps");
		if ((bool)loadingObj)
		{
			loadingObj.SetActive(value: false);
		}
		SetupList();
	}

	private void SetupList()
	{
		lineHeight = ((RectTransform)listItemTemplate.transform).rect.height;
		if (allowBuiltInMaps)
		{
			cMaps = VTResources.GetMaps();
		}
		else
		{
			cMaps = new List<VTMap>();
			foreach (VTMapCustom allCustomMap in VTResources.GetAllCustomMaps())
			{
				cMaps.Add(allCustomMap);
			}
		}
		if (swMaps != null)
		{
			foreach (VTMapCustom swMap in swMaps)
			{
				cMaps.Add(swMap);
			}
		}
		cMaps.RemoveAll((VTMap x) => x is VTMapCustom && ((VTMapCustom)x).mapType != VTMapGenerator.VTMapTypes.HeightMap);
		bool flag = false;
		for (int i = 0; i < cMaps.Count; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(listItemTemplate, scrollRect.content);
			gameObject.SetActive(value: true);
			int num = i;
			if (cMaps[i] is VTMapCustom && ((VTMapCustom)cMaps[i]).isSteamWorkshopMap && flag)
			{
				num++;
			}
			gameObject.transform.localPosition = new Vector3(0f, (float)(-num) * lineHeight, 0f);
			if (cMaps[i] is VTMapCustom && ((VTMapCustom)cMaps[i]).isSteamWorkshopMap && !flag)
			{
				flag = true;
				Text componentInChildren = gameObject.GetComponentInChildren<Text>();
				componentInChildren.text = "==Steam Workshop==";
				componentInChildren.color = new Color(0.49f, 0.6f, 1f, 1f);
				i--;
			}
			else
			{
				MapListItem mapListItem = gameObject.AddComponent<MapListItem>();
				mapListItem.menu = this;
				mapListItem.idx = i;
				if (string.IsNullOrEmpty(cMaps[i].mapName))
				{
					gameObject.GetComponentInChildren<Text>().text = cMaps[i].mapID;
				}
				else
				{
					gameObject.GetComponentInChildren<Text>().text = cMaps[i].mapName;
				}
			}
			listObjs.Add(gameObject);
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)(cMaps.Count + 1) * lineHeight);
		scrollRect.ClampVertical();
		scrollRect.verticalNormalizedPosition = 1f;
		listItemTemplate.SetActive(value: false);
		SelectMap(-1);
	}

	private void SelectMap(int idx)
	{
		currIdx = idx;
		if (idx >= 0)
		{
			VTMap vTMap = cMaps[idx];
			selectionTf.gameObject.SetActive(value: true);
			int num = idx;
			if (vTMap is VTMapCustom && ((VTMapCustom)vTMap).isSteamWorkshopMap)
			{
				num++;
			}
			selectionTf.localPosition = new Vector3(0f, (float)(-num) * lineHeight, 0f);
			nameText.text = vTMap.mapID;
			if ((bool)vTMap.previewImage)
			{
				previewImage.gameObject.SetActive(value: true);
				previewImage.texture = cMaps[idx].previewImage;
			}
			else if (vTMap is VTMapCustom && (bool)((VTMapCustom)vTMap).heightMap)
			{
				previewImage.gameObject.SetActive(value: true);
				previewImage.texture = ((VTMapCustom)vTMap).heightMap;
			}
			else
			{
				previewImage.gameObject.SetActive(value: false);
			}
			if ((bool)descriptionText)
			{
				if (!string.IsNullOrEmpty(vTMap.mapDescription))
				{
					descriptionText.gameObject.SetActive(value: true);
					descriptionText.SetText(vTMap.mapDescription);
				}
				else
				{
					descriptionText.gameObject.SetActive(value: false);
				}
			}
			previewImage.transform.localScale = (float)cMaps[idx].mapSize / 64f * Vector3.one;
		}
		else
		{
			selectionTf.gameObject.SetActive(value: false);
			previewImage.gameObject.SetActive(value: false);
			nameText.text = "Select map";
			if ((bool)descriptionText)
			{
				descriptionText.gameObject.SetActive(value: false);
			}
		}
		itemDependentButtons.SetInteractable(idx >= 0);
	}

	public void OkayButton()
	{
		if ((bool)editorMenu)
		{
			VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.MapEditor;
			VTResources.LaunchMap(cMaps[currIdx].mapID);
		}
		if (this.OnMapSelected != null)
		{
			this.OnMapSelected(cMaps[currIdx]);
		}
	}

	public void DeleteButton()
	{
		if ((bool)editorMenu)
		{
			editorMenu.confirmDialogue.DisplayConfirmation("Delete map?", "Are you sure you want to delete this map? Any missions using this map will no longer work!", FinallyDelete, null);
		}
	}

	private void FinallyDelete()
	{
		Directory.Delete(VTResources.GetMapDirectoryPath(cMaps[currIdx].mapID), recursive: true);
		Open();
	}

	public void BackButton()
	{
		if ((bool)editorMenu)
		{
			editorMenu.gameObject.SetActive(value: true);
		}
		base.gameObject.SetActive(value: false);
		if (this.OnBackButtonPressed != null)
		{
			this.OnBackButtonPressed();
		}
	}
}
