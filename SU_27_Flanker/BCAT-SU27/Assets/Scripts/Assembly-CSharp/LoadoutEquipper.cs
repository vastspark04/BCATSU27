using UnityEngine;

public class LoadoutEquipper : MonoBehaviour
{
	public WeaponManager wm;

	public string[] equips;

	[Tooltip("Normalized fuel")]
	public float fuel;

	public int[] cms = new int[2] { 1000, 1000 };

	private void Start()
	{
		if (!wm)
		{
			wm = GetComponent<WeaponManager>();
			if (!wm)
			{
				return;
			}
		}
		Loadout loadout = new Loadout();
		loadout.hpLoadout = equips;
		loadout.normalizedFuel = fuel;
		loadout.cmLoadout = cms;
		wm.EquipWeapons(loadout);
	}
}
