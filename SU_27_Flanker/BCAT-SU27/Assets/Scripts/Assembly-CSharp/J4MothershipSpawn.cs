public class J4MothershipSpawn : AIUnitSpawn
{
	public J4Mothership mothership;

	[VTEvent("Enter", "Enter the mothership.")]
	public void Enter()
	{
		mothership.Enter();
	}

	[VTEvent("Fire", "Fire the lazors")]
	public void FireBeam()
	{
		mothership.FireBeam();
	}

	[VTEvent("Disengage Fighters", "Fighters disengage and orbit mother position.", new string[] { "Radius", "Altitude" })]
	public void DisengageFighters([VTRangeParam(1000f, 15000f)] float radius, [VTRangeParam(150f, 9000f)] float alt)
	{
		foreach (J4Mothership.J4Fighter fighter in mothership.fighters)
		{
			if ((bool)fighter)
			{
				fighter.OrbitMother(radius, alt);
			}
		}
	}
}
