using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

public class TestSerializeScenario : MonoBehaviour
{
	private void Start()
	{
		Read();
	}

	private void Read()
	{
		int num = 88;
		using Stream stream = File.Open("C:\\UnityProjects\\VTOLVR\\Assets\\CustomScenarios\\00_aiRearmTest\\00_aiRearmTest.vtsb", FileMode.OpenOrCreate);
		byte[] array = new byte[stream.Length];
		int num2 = 0;
		int num3;
		while ((num3 = stream.ReadByte()) != -1)
		{
			int num4 = (num3 - num) % 255;
			array[num2] = (byte)num4;
			num2++;
		}
		new BinaryFormatter();
		Encoding.UTF8.GetString(array);
	}

	private void Write()
	{
		string path = "C:\\UnityProjects\\VTOLVR\\Assets\\CustomScenarios\\00_aiRearmTest\\00_aiRearmTest.vtsb";
		ConfigNode.LoadFromFile("C:\\UnityProjects\\VTOLVR\\Assets\\CustomScenarios\\00_aiRearmTest\\00_aiRearmTest.vts");
		using Stream stream = File.Open("C:\\UnityProjects\\VTOLVR\\Assets\\CustomScenarios\\00_aiRearmTest\\00_aiRearmTest.vts", FileMode.Open);
		using Stream stream2 = File.Open(path, FileMode.OpenOrCreate);
		int num;
		while ((num = stream.ReadByte()) != -1)
		{
			int num2 = (num + 88) % 255;
			stream2.WriteByte((byte)num2);
		}
	}
}
