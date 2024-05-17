using UnityEngine;

public class TempVTOLEquipper : MonoBehaviour
{
	public WeaponManager wm;

	private void Start()
	{
		if (VehicleEquipper.loadoutSet)
		{
			wm.EquipWeapons(VehicleEquipper.loadout);
		}
		else
		{
			BombLoadout();
		}
	}

	private void StandardLoadout()
	{
		Loadout loadout = new Loadout();
		loadout.normalizedFuel = 1500f;
		loadout.hpLoadout = new string[5] { "gau-8", "hellfirex4", "h70-4x4", "sidewinderx2", "mk82x3" };
		wm.EquipWeapons(loadout);
	}

	private void IRISTest()
	{
		Loadout loadout = new Loadout();
		loadout.normalizedFuel = 1500f;
		loadout.hpLoadout = new string[5] { "gau-8", "iris-t-x1", "iris-t-x1", "iris-t-x1", "iris-t-x1" };
		wm.EquipWeapons(loadout);
	}

	private void HeatTestLoadout()
	{
		Loadout loadout = new Loadout();
		loadout.normalizedFuel = 1500f;
		loadout.hpLoadout = new string[5] { "gau-8", "sidewinderx2", "sidewinderx2", "sidewinderx2", "sidewinderx2" };
		wm.EquipWeapons(loadout);
	}

	private void MixedBagLoadout()
	{
		Loadout loadout = new Loadout();
		loadout.normalizedFuel = 1500f;
		loadout.hpLoadout = new string[5] { "gau-8", "hellfirex4", "h70-4x4", "cagm-6", "sidewinderx2" };
		wm.EquipWeapons(loadout);
	}

	private void BombLoadout()
	{
		Loadout loadout = new Loadout();
		loadout.normalizedFuel = 700f;
		loadout.hpLoadout = new string[5] { "gau-8", "mk82x2", "mk82x3", "mk82x3", "mk82x2" };
		wm.EquipWeapons(loadout);
	}

	private void ClusterMissile()
	{
		Loadout loadout = new Loadout();
		loadout.normalizedFuel = 1500f;
		loadout.hpLoadout = new string[5] { "gau-8", "cagm-6", "cagm-6", "cagm-6", "cagm-6" };
		wm.EquipWeapons(loadout);
	}
}
