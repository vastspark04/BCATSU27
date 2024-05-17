public class VTSODestructible : VTStaticObject
{
	public Health health;

	private bool died;

	protected override void OnSpawned()
	{
		base.OnSpawned();
		health.OnDeath.AddListener(OnDeath);
	}

	private void OnDeath()
	{
		died = true;
	}

	[VTEvent("Destroy", "Destroy the object.")]
	public void VTE_Destroy()
	{
		health.Kill();
	}

	[SCCUnitProperty("Destroyed", true)]
	public bool SCC_IsDestroyed()
	{
		return died;
	}
}
