public interface IPersistantConfigNode
{
	void SaveToParentNode(ConfigNode parentNode);

	void LoadFromNode(ConfigNode node);

	string PersistantNodeName();
}
