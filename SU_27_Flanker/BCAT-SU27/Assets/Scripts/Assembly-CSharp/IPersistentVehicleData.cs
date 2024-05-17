public interface IPersistentVehicleData
{
	void OnSaveVehicleData(ConfigNode vDataNode);

	void OnLoadVehicleData(ConfigNode vDataNode);
}
