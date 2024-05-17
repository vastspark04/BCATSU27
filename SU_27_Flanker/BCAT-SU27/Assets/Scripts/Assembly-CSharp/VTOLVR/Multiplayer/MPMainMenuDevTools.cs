using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace VTOLVR.Multiplayer{

public class MPMainMenuDevTools : MonoBehaviour
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct Banana
	{
		public string Name => "Banana{cool}";
	}

	public PilotSelectUI pilotSelect;

	public VTMPMainMenu mpMenu;

	private string alpha = "abcdefghijklmnopqrstuvwxyz";

	[ContextMenu("Test pw hash")]
	private void TestPWHash()
	{
		ulong num = VTOLMPLobbyManager.HashPassword("kool76561197976114634", 12345uL);
		ulong num2 = VTOLMPLobbyManager.HashPassword("devtest76561197976114634", 12345uL);
		Debug.Log($"kool:{num} devtest:{num2}");
	}

	[ContextMenu("test bytes string")]
	private void TestIntString()
	{
		string text = "A B";
		int num = VTNetUtils.Encode3CharString(text);
		string arg = VTNetUtils.Decode3CharString(num);
		Debug.Log($"{text} => {num} => {arg}");
	}

	[ContextMenu("format exception?")]
	private void FormatExceptionTest()
	{
		string text = "{foo}";
		Debug.Log("this is a " + text);
		Func<string> func = () => "{bar}";
		Debug.Log("this is a " + func());
		Debug.Log("does " + default(Banana).Name + " work?");
		Debug.Log("does [" + default(Banana).Name + "] work?");
		Debug.Log($"does {text} {func} [{default(Banana).Name}] work?");
		Debug.LogFormat(default(Banana).Name + " work?");
	}

	[ContextMenu("Test ULONG Ints")]
	private void TestUlongInts()
	{
		ulong u = 18446743747292037120uL;
		VTNetUtils.ULongToInts(76561198377211630uL, out var a, out var b);
		VTNetUtils.ULongToInts(u, out var a2, out var b2);
		Debug.Log($"a={a}, b={b}        x={a2}, y={b2}");
	}

	[ContextMenu("Test type mask")]
	private void TestTypeMask()
	{
		int num = 2;
		int num2 = 1073741824;
		int num3 = (num2 & 0x60000000) >> 29;
		Debug.Log($"type={num} masked={num2} mask={1610612736} returned={num3}");
	}

	[ContextMenu("Test Time UTC")]
	private void TestTime()
	{
		CultureInfo cultureInfo = new CultureInfo("en-US");
		string text = DateTime.UtcNow.ToString(cultureInfo);
		Debug.Log(text);
		Thread.Sleep(1000);
		DateTime utcNow = DateTime.UtcNow;
		DateTime dateTime = DateTime.Parse(text);
		Debug.Log("delta: " + (utcNow - dateTime).TotalSeconds + "s");
		int num = 0;
		Debug.Log(string.Format("1 minute: {0}:{1}", num, 1.ToString("00")));
	}

	private void MissionElapsedTime(string mUtc, out int hours, out int minutes)
	{
		if (!string.IsNullOrEmpty(mUtc))
		{
			CultureInfo provider = new CultureInfo("en-US");
			if (DateTime.TryParse(mUtc, provider, DateTimeStyles.None, out var result))
			{
				TimeSpan timeSpan = DateTime.UtcNow - result;
				minutes = timeSpan.Minutes;
				hours = timeSpan.Hours;
			}
		}
		minutes = -1;
		hours = -1;
	}

	[ContextMenu("Test NSV")]
	private void TestNSV()
	{
		for (int i = 0; i < 100; i++)
		{
			TestShiftOrigin();
			float x = UnityEngine.Random.Range(-196000, 196000);
			float y = UnityEngine.Random.Range(0, 9000);
			float z = UnityEngine.Random.Range(-196000, 196000);
			Vector3 vector = new Vector3(x, y, z);
			FloatingOrigin.WorldToNetPoint(vector, out var nsv, out var offset);
			Vector3 vector2 = FloatingOrigin.NetToWorldPoint(offset, nsv);
			Vector3 vector3 = vector - vector2;
			if (vector3.magnitude > 500f)
			{
				Debug.LogError($"NSV Error: {vector3} ({vector}, {vector2})");
				continue;
			}
			Vector3 vector4 = vector3;
			Debug.Log("NSV Error: " + vector4.ToString());
		}
	}

	private void TestShiftOrigin()
	{
		float x = UnityEngine.Random.Range(-196000, 196000);
		float y = UnityEngine.Random.Range(0, 9000);
		float z = UnityEngine.Random.Range(-196000, 196000);
		FloatingOrigin.instance.ShiftOrigin(new Vector3(x, y, z), immediate: true);
	}

	[ContextMenu("Test Double->Ints")]
	private void TestDoubleToInts()
	{
		for (int i = 0; i < 10; i++)
		{
			double num = UnityEngine.Random.Range(-250000f, 250000f);
			num *= (double)UnityEngine.Random.Range(0.65123f, 2.412324f);
			ShipMoverSync.DoubleToInts(num, out var a, out var b);
			double num2 = ShipMoverSync.IntsToDouble(a, b);
			Debug.Log($"{num} => [{a}, {b}] => {num2}");
		}
	}

	private void Update()
	{
		if (Input.GetKey(KeyCode.RightShift))
		{
			if (Input.GetKeyDown(KeyCode.H))
			{
				QuickHost();
			}
			if (Input.GetKeyDown(KeyCode.J))
			{
				QuickJoin();
			}
		}
	}

	private void QuickHost()
	{
		pilotSelect.SelectPilotButton();
		pilotSelect.StartSelectedPilotButton();
		mpMenu.Open();
		mpMenu.HostGameButton();
		mpMenu.FinallyHostGameButton();
	}

	private void QuickJoin()
	{
		StartCoroutine(QuickJoinRoutine());
	}

	private IEnumerator QuickJoinRoutine()
	{
		pilotSelect.SelectPilotButton();
		pilotSelect.StartSelectedPilotButton();
		mpMenu.Open();
		while (base.enabled)
		{
			yield return new WaitForSeconds(0.5f);
			VTMPLobbyListItem[] componentsInChildren = mpMenu.GetComponentsInChildren<VTMPLobbyListItem>(includeInactive: false);
			if (componentsInChildren.Length != 0)
			{
				componentsInChildren[0].JoinButton();
				break;
			}
		}
	}

	[ContextMenu("TestPWH")]
	private void TestPWH()
	{
		ulong seed = 76561197976114634uL;
		Debug.Log("seed = " + seed);
		int length = alpha.Length;
		Dictionary<ulong, int> dictionary = new Dictionary<ulong, int>();
		for (int i = 0; i < 1000; i++)
		{
			string text = string.Empty;
			for (int j = 0; j < 4; j++)
			{
				text += alpha[UnityEngine.Random.Range(0, length)];
			}
			text += "76561197976114634";
			ulong num = VTOLMPLobbyManager.HashPassword(text, seed);
			if (dictionary.ContainsKey(num))
			{
				dictionary[num]++;
			}
			else
			{
				dictionary.Add(num, 1);
			}
			Debug.Log($"pw: {text}, h1: {VTOLMPLobbyManager.HashPassword(text, 12345uL)}, h2: {num}");
		}
		foreach (KeyValuePair<ulong, int> item in dictionary)
		{
			Debug.Log($"hash: {item.Key}, collisions: {item.Value}");
		}
	}

	[ContextMenu("Test PWH Dict")]
	private void TestPWHDict()
	{
	}
}

}