public class HPEquipFuelTank : HPEquippable
{
	public float fuelCapacity;

	private bool addedFuel;

	protected override void OnEquip()
	{
		base.OnEquip();
		if (!addedFuel)
		{
			base.weaponManager.GetComponent<FuelTank>().maxFuel += fuelCapacity;
			addedFuel = true;
		}
	}

	public override void OnConfigAttach(LoadoutConfigurator configurator)
	{
		base.OnConfigAttach(configurator);
		if (!addedFuel)
		{
			float num;
			if (configurator.uiOnly)
			{
				num = configurator.fuelKnob.currentValue;
				configurator.ui_maxFuel += fuelCapacity;
			}
			else
			{
				FuelTank component = configurator.wm.GetComponent<FuelTank>();
				_ = component.fuel;
				num = component.fuelFraction;
				component.maxFuel += fuelCapacity;
			}
			configurator.fullInfo.SetNormFuel(num);
			configurator.fuelKnob.SetKnobValue(num);
			addedFuel = true;
		}
	}

	public override void OnConfigDetach(LoadoutConfigurator configurator)
	{
		base.OnConfigDetach(configurator);
		if (addedFuel)
		{
			float num;
			if (configurator.uiOnly)
			{
				num = configurator.fuelKnob.currentValue;
				configurator.ui_maxFuel -= fuelCapacity;
			}
			else
			{
				FuelTank component = configurator.wm.GetComponent<FuelTank>();
				_ = component.fuel;
				num = component.fuelFraction;
				component.maxFuel -= fuelCapacity;
			}
			configurator.fullInfo.SetNormFuel(num);
			configurator.fuelKnob.SetKnobValue(num);
			addedFuel = false;
		}
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		if (addedFuel)
		{
			base.weaponManager.GetComponent<FuelTank>().maxFuel -= fuelCapacity;
			addedFuel = false;
		}
	}

	public override float GetEstimatedMass()
	{
		return fuelCapacity * 0.00071f;
	}
}
