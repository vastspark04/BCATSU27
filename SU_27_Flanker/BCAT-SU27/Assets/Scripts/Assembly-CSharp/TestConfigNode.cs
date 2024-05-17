using System.IO;
using UnityEngine;

public class TestConfigNode : MonoBehaviour
{
	private void Start()
	{
		string text = PilotSaveManager.saveDataPath + "/test.cfg";
		File.Create(text).Close();
		ConfigNode configNode = new ConfigNode();
		configNode.SetValue("[\"isFoo\"=true],[\"barOps\"=\"tenfold\"]", "true");
		configNode.SaveToFile(text);
		ConfigNode configNode2 = ConfigNode.LoadFromFile(text);
		string text2 = PilotSaveManager.saveDataPath + "/testOut.cfg";
		File.Create(text2).Close();
		configNode2.SaveToFile(text2);
	}
}
