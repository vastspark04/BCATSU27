using System.Collections;
using UnityEngine;

public class HPEquipDropTank : HPEquippable
{
	public FuelTank fuelTank;

	private FuelTank parentFuelTank;

	public MinMax jettisonRotation = new MinMax(0.7f, 1.1f);

	private Rigidbody rb;

	private Coroutine uiUpdateRoutine;

	public override float GetTotalCost()
	{
		return unitCost + fuelTank.fuel * VTOLVRConstants.FUEL_UNIT_COST;
	}

	private IEnumerator UIUpdateRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(1f);
		while (true)
		{
			if ((bool)base.weaponManager && (bool)base.weaponManager.ui && base.weaponManager.ui.mfdPage.isOpen)
			{
				base.weaponManager.ui.UpdateDisplay();
			}
			yield return wait;
		}
	}

	protected override void OnEquip()
	{
		parentFuelTank = base.weaponManager.GetComponent<FuelTank>();
		if (!parentFuelTank.subFuelTanks.Contains(fuelTank))
		{
			parentFuelTank.subFuelTanks.Add(fuelTank);
		}
		parentFuelTank.subFuelTanks.RemoveAll((FuelTank x) => x == null);
		if ((bool)base.weaponManager && (bool)base.weaponManager.ui)
		{
			uiUpdateRoutine = StartCoroutine(UIUpdateRoutine());
		}
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		if ((bool)parentFuelTank)
		{
			parentFuelTank.DetachFueltank(fuelTank);
		}
	}

	public override void OnConfigDetach(LoadoutConfigurator configurator)
	{
		base.OnConfigDetach(configurator);
		if ((bool)parentFuelTank)
		{
			parentFuelTank.DetachFueltank(fuelTank);
		}
	}

	protected override void OnJettison()
	{
		base.OnJettison();
		rb = base.gameObject.GetComponent<Rigidbody>();
		parentFuelTank.DetachFueltank(fuelTank);
		rb.AddTorque(jettisonRotation.Random() * rb.transform.right, ForceMode.VelocityChange);
		if (uiUpdateRoutine != null)
		{
			StopCoroutine(uiUpdateRoutine);
		}
	}

	public override int GetCount()
	{
		return Mathf.RoundToInt(fuelTank.fuel);
	}

	public override float GetEstimatedMass()
	{
		return fuelTank.baseMass + fuelTank.maxFuel * fuelTank.fuelDensity;
	}

	public override void OnQuicksaveEquip(ConfigNode eqNode)
	{
		base.OnQuicksaveEquip(eqNode);
		eqNode.SetValue("normFuel", fuelTank.fuelFraction);
	}

	public override void OnQuickloadEquip(ConfigNode eqNode)
	{
		base.OnQuickloadEquip(eqNode);
		float normFuel = ConfigNodeUtils.ParseFloat(eqNode.GetValue("normFuel"));
		fuelTank.SetNormFuel(normFuel);
	}
}
