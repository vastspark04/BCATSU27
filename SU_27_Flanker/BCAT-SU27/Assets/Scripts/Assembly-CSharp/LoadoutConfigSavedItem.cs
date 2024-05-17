using UnityEngine;
using UnityEngine.UI;

public class LoadoutConfigSavedItem : MonoBehaviour
{
	public Text nameText;

	private string saveName;

	private LoadoutConfigurator configurator;

	public void Setup(string saveName, LoadoutConfigurator config)
	{
		this.saveName = saveName;
		configurator = config;
		nameText.text = saveName;
	}

	public void LoadButton()
	{
		configurator.LoadLoadout(saveName);
	}

	public void SaveButton()
	{
		configurator.SaveLoadout(saveName);
	}

	public void DeleteButton()
	{
		configurator.DeleteLoadout(saveName);
	}
}
