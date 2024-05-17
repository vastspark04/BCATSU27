public static class ActorExtensions
{
	public static string DebugName(this Actor a)
	{
		if (a == null)
		{
			return "null";
		}
		if (!a.unitSpawn)
		{
			return $"{a.gameObject.name} ({a.actorName})";
		}
		return a.unitSpawn.unitSpawner.GetUIDisplayName();
	}
}
