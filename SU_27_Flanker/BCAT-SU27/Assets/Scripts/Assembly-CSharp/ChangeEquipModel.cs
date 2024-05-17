using UnityEngine;

public class ChangeEquipModel : MonoBehaviour
{
	public HPEquippable equip;

	public GameObject[] defaultModels;

	public GameObject[] lowPolyModels;

	public bool switchToLowOnEquip = true;

	public bool hideOnAIEquip = true;

	public bool destroyUnused = true;

	private void Awake()
	{
		equip.OnEquipped += Equip_OnEquipped;
	}

	private void Equip_OnEquipped()
	{
		if (switchToLowOnEquip)
		{
			defaultModels.SetActive(active: false);
			lowPolyModels.SetActive(active: true);
		}
		if (!equip.weaponManager.isPlayer && hideOnAIEquip)
		{
			defaultModels.SetActive(active: false);
			lowPolyModels.SetActive(active: false);
		}
		if (!destroyUnused)
		{
			return;
		}
		GameObject[] array = defaultModels;
		foreach (GameObject gameObject in array)
		{
			if ((bool)gameObject && !gameObject.activeSelf)
			{
				Object.Destroy(gameObject);
			}
		}
		array = lowPolyModels;
		foreach (GameObject gameObject2 in array)
		{
			if ((bool)gameObject2 && !gameObject2.activeSelf)
			{
				Object.Destroy(gameObject2);
			}
		}
	}
}
