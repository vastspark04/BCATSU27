using UnityEngine;
using UnityEngine.UI;

public class VTEdUnitSelectorButton : MonoBehaviour
{
	public Text labelText;

	public GameObject subUnitTemplate;

	private VTEdUnitSelector selector;

	private UnitSpawner uSpawner;

	private float height;

	private int subUnitIdx;

	public void Setup(VTEdUnitSelector selector, UnitSpawner uSpawner, bool allowSubunits)
	{
		this.selector = selector;
		this.uSpawner = uSpawner;
		float num = ((RectTransform)base.transform).rect.height;
		float num2 = ((RectTransform)subUnitTemplate.transform).rect.height;
		labelText.text = uSpawner.GetUIDisplayName();
		GetComponent<Button>().onClick.AddListener(OnClick);
		height = num;
		if (allowSubunits && uSpawner.prefabUnitSpawn is AIUnitSpawn)
		{
			AIUnitSpawn aIUnitSpawn = (AIUnitSpawn)uSpawner.prefabUnitSpawn;
			for (int i = 0; i < aIUnitSpawn.subUnits.Count; i++)
			{
				Actor subActor = aIUnitSpawn.subUnits[i];
				GameObject obj = Object.Instantiate(subUnitTemplate, subUnitTemplate.transform.parent);
				obj.SetActive(value: true);
				obj.transform.localPosition = subUnitTemplate.transform.localPosition + num2 * (float)i * Vector3.down;
				obj.GetComponent<VTEdUnitSelectorButton>().SetupSubunit(selector, uSpawner, subActor, i);
				height += num2;
			}
		}
		subUnitTemplate.SetActive(value: false);
	}

	public void SetupSubunit(VTEdUnitSelector selector, UnitSpawner uSpawner, Actor subActor, int idx)
	{
		this.selector = selector;
		this.uSpawner = uSpawner;
		subUnitIdx = idx;
		labelText.text = idx + 1 + ": " + subActor.actorName;
		GetComponent<Button>().onClick.AddListener(OnClickSubUnit);
	}

	private void OnClick()
	{
		selector.SelectUnit(uSpawner);
	}

	private void OnClickSubUnit()
	{
		selector.SelectSubUnit(uSpawner, subUnitIdx);
	}

	public float GetHeight()
	{
		return height;
	}
}
