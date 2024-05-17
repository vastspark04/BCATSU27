public static class VehicleEquipper
{
	private static Loadout _loadout;

	public static bool loadoutSet { get; private set; }

	public static Loadout loadout
	{
		get
		{
			return _loadout;
		}
		set
		{
			_loadout = value;
			loadoutSet = true;
		}
	}
}
