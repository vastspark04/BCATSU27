public interface IQSMissileComponent
{
	void OnQuicksavedMissile(ConfigNode qsNode, float elapsedTime);

	void OnQuickloadedMissile(ConfigNode qsNode, float elapsedTime);
}
