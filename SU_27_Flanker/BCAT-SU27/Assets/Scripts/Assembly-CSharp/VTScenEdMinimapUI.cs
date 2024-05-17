using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTScenEdMinimapUI : MonoBehaviour
{
	private class MinimapUnit
	{
		public UnitSpawner unit;

		public Transform iconTf;
	}

	public VTScenarioEditor editor;

	public Image unitIconTemplate;

	public RectTransform mapTransform;

	public VTScenEdMinimapCam mapCam;

	public Color alliedColor;

	public Color enemyColor;

	public Color playerColor;

	public GameObject displayObj;

	private Dictionary<int, MinimapUnit> unitIconsDict = new Dictionary<int, MinimapUnit>();

	private List<MinimapUnit> unitIconsList = new List<MinimapUnit>();

	private ObjectPool iconPool;

	private float lastClickTime;

	private RectTransform displayTf;

	public RectTransform enlargeRefTf;

	private float normalSize;

	private bool enlarged;

	private void Awake()
	{
		editor.OnCreatedUnit += Editor_OnCreatedUnit;
		editor.OnDestroyedUnit += Editor_OnDestroyedUnit;
		iconPool = ObjectPool.CreateObjectPool(unitIconTemplate.gameObject, 10, canGrow: true, destroyOnLoad: true);
		unitIconTemplate.gameObject.SetActive(value: false);
		displayTf = (RectTransform)displayObj.transform;
		normalSize = displayTf.rect.height;
	}

	private void Start()
	{
		FloatingOrigin.instance.OnPostOriginShift += FloatingOrigin_instance_OnOriginShift;
		displayObj.SetActive(value: false);
	}

	public void Toggle()
	{
		displayObj.SetActive(!displayObj.activeSelf);
	}

	public void EnlargeButton()
	{
		if (!enlarged)
		{
			enlarged = true;
			return;
		}
		enlarged = false;
		displayTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, normalSize);
		displayTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, normalSize);
	}

	private void FloatingOrigin_instance_OnOriginShift(Vector3 offset)
	{
		UpdateIconPositions();
	}

	private void Editor_OnDestroyedUnit(int id)
	{
		MinimapUnit minimapUnit = unitIconsDict[id];
		minimapUnit.iconTf.gameObject.SetActive(value: false);
		unitIconsDict.Remove(id);
		unitIconsList.Remove(minimapUnit);
	}

	private void Editor_OnCreatedUnit(int id)
	{
		GameObject pooledObject = iconPool.GetPooledObject();
		pooledObject.gameObject.SetActive(value: true);
		Image component = pooledObject.GetComponent<Image>();
		UnitSpawner unit = editor.currentScenario.units.GetUnit(id);
		component.color = ((unit.team == Teams.Allied) ? alliedColor : enemyColor);
		if (unit.prefabUnitSpawn is PlayerSpawn)
		{
			component.color = playerColor;
		}
		MinimapUnit minimapUnit = new MinimapUnit();
		minimapUnit.unit = unit;
		minimapUnit.iconTf = pooledObject.transform;
		unitIconsDict.Add(id, minimapUnit);
		unitIconsList.Add(minimapUnit);
		minimapUnit.iconTf.SetParent(unitIconTemplate.transform.parent);
		minimapUnit.iconTf.localScale = Vector3.one;
		minimapUnit.iconTf.localRotation = Quaternion.identity;
	}

	public void ClickMap()
	{
		if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
		{
			editor.editorCamera.FocusOnPoint(MapToWorldPosition(MouseToMapPos()));
		}
		else
		{
			lastClickTime = Time.unscaledTime;
		}
	}

	private void LateUpdate()
	{
		if (enlarged)
		{
			displayTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, enlargeRefTf.rect.height);
			displayTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, enlargeRefTf.rect.height);
		}
		UpdateIconPositions();
	}

	private void UpdateIconPositions()
	{
		for (int i = 0; i < unitIconsList.Count; i++)
		{
			MinimapUnit minimapUnit = unitIconsList[i];
			minimapUnit.iconTf.localPosition = WorldToMapPosition(minimapUnit.unit.transform.position);
		}
	}

	public Vector3 WorldToMapPosition(Vector3 worldPos)
	{
		Vector3 vector = (worldPos - mapCam.transform.position) / mapCam.orthoSizes[mapCam.orthoIdx];
		return mapTransform.rect.width / 2f * new Vector3(vector.x, vector.z);
	}

	public Vector3 MapToWorldPosition(Vector3 mapPos)
	{
		Vector3 vector = mapPos / (mapTransform.rect.width / 2f);
		Vector3 result = mapCam.transform.position + new Vector3(vector.x, 0f, vector.y) * mapCam.orthoSizes[mapCam.orthoIdx];
		result.y = WaterPhysics.instance.height;
		return result;
	}

	private Vector3 MouseToMapPos()
	{
		Vector3 mousePosition = Input.mousePosition;
		return mapTransform.InverseTransformPoint(mousePosition);
	}
}
