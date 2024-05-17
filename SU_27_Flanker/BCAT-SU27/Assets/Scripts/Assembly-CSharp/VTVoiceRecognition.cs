using System.Collections.Generic;
using UnityEngine;

public static class VTVoiceRecognition
{
	public enum WingmanCommands
	{
		Unrecognized,
		AttackMyTarget,
		FormUp,
		SpreadFar,
		SpreadMedium,
		SpreadClose,
		Orbit,
		EngageTargets,
		Disengage,
		DisengageAndFormUp,
		GoRefuel,
		ReturnToBase,
		RearmAtBase,
		Fox,
		RadarOn,
		RadarOff
	}

	public enum ATCCommands
	{
		Unrecognized,
		RequestingLanding,
		RequestingTakeoff,
		RequestingVerticalLanding,
		RequestingVerticalTakeoff,
		CancelRequest,
		RequestRearm,
		Meatball,
		ClaraBall,
		WavingOff
	}

	public enum AWACSCommands
	{
		Unrecognized,
		Picture,
		BogeyDope,
		RTB
	}

	private struct AWACSVoiceCommandMapping
	{
		public AWACSCommands command;

		public string[] inputs;

		public AWACSVoiceCommandMapping(AWACSCommands command, params string[] inputArgs)
		{
			this.command = command;
			inputs = inputArgs;
		}
	}

	private struct ATCVoiceCommandMapping
	{
		public ATCCommands command;

		public string[] inputs;

		public ATCVoiceCommandMapping(ATCCommands command, params string[] inputArgs)
		{
			this.command = command;
			inputs = inputArgs;
		}
	}

	private struct VoiceCommandMapping
	{
		public WingmanCommands command;

		public string[] inputs;

		public VoiceCommandMapping(WingmanCommands command, params string[] inputArgs)
		{
			this.command = command;
			inputs = inputArgs;
		}
	}

	private static Dictionary<string, WingmanCommands> wingmanCommands;

	public static string[] wingmanRecognitionPhrases;

	private static VoiceCommandMapping[] defaultWingmanMapping;

	private static Dictionary<string, ATCCommands> atcCommands;

	private static Dictionary<string, ATCCommands> specifiedAtcCommands;

	private static string[] numberStrings;

	private static ATCVoiceCommandMapping[] atcMapping;

	private static AWACSVoiceCommandMapping[] awacsMapping;

	private static Dictionary<string, AWACSCommands> awacsCommands;

	static VTVoiceRecognition()
	{
		wingmanCommands = new Dictionary<string, WingmanCommands>();
		defaultWingmanMapping = new VoiceCommandMapping[15]
		{
			new VoiceCommandMapping(WingmanCommands.AttackMyTarget, "attack my target", "fire on my target", "shoot my target", "attack this target", "strike my target", "strike this target", "shoot this", "attack this"),
			new VoiceCommandMapping(WingmanCommands.FormUp, "form up", "form on me", "form up on me", "get in formation", "fly formation", "form on my wing", "follow me", "get on my wing", "let's form up", "let's fly in formation", "fly with me", "rejoin"),
			new VoiceCommandMapping(WingmanCommands.SpreadFar, "spread out", "wide formation", "combat spread", "you're too close", "spread far", "tactical formation"),
			new VoiceCommandMapping(WingmanCommands.SpreadClose, "come closer", "tight formation", "close formation", "parade formation", "get in close", "form up close", "get closer", "spread close", "finger tip formation"),
			new VoiceCommandMapping(WingmanCommands.SpreadMedium, "spread medium", "medium spread", "form up medium"),
			new VoiceCommandMapping(WingmanCommands.Orbit, "orbit here", "break off", "stop following me", "stop following", "don't follow me", "do not follow me"),
			new VoiceCommandMapping(WingmanCommands.EngageTargets, "engage at will", "engage targets", "engage all targets", "attack all targets", "weapons free", "weapons hot", "combat ready", "destroy all targets", "engage"),
			new VoiceCommandMapping(WingmanCommands.Disengage, "disengage", "stop fighting", "do not engage", "don't engage", "don't shoot", "do not shoot", "hold your fire", "stop shooting", "stop engaging"),
			new VoiceCommandMapping(WingmanCommands.DisengageAndFormUp, "disengage and form up", "regroup", "let's regroup", "disengage and form on me", "disengage and follow me", "get back in formation"),
			new VoiceCommandMapping(WingmanCommands.GoRefuel, "go refuel", "go fill up", "go get gas", "fill up on gas", "go to the tanker", "refuel at the tanker"),
			new VoiceCommandMapping(WingmanCommands.ReturnToBase, "return to base", "let's go home", "arty bee"),
			new VoiceCommandMapping(WingmanCommands.RearmAtBase, "go ree arm", "return to base and ree arm", "arty bee and ree arm", "go back and ree arm", "go ree arm at base"),
			new VoiceCommandMapping(WingmanCommands.Fox, "fox", "fox one", "fox two", "fox three", "fox tree", "fox fox", "missile away", "rifle", "magnum", "bruiser", "pickle", "bombs away", "greyhound"),
			new VoiceCommandMapping(WingmanCommands.RadarOn, "radar on", "turn your radar on", "radars on", "search radar on"),
			new VoiceCommandMapping(WingmanCommands.RadarOff, "radar off", "radars off", "turn your radar off", "search radar off", "shut off your radar", "shut off radar", "turn off radar", "radar quiet")
		};
		atcCommands = new Dictionary<string, ATCCommands>();
		specifiedAtcCommands = new Dictionary<string, ATCCommands>();
		numberStrings = new string[10] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "niner" };
		awacsCommands = new Dictionary<string, AWACSCommands>();
		List<string> list = new List<string>();
		VoiceCommandMapping[] array = defaultWingmanMapping;
		for (int i = 0; i < array.Length; i++)
		{
			VoiceCommandMapping voiceCommandMapping = array[i];
			string[] inputs = voiceCommandMapping.inputs;
			foreach (string text in inputs)
			{
				string text2 = text;
				if (voiceCommandMapping.command != WingmanCommands.Fox)
				{
					text2 = "wingmen " + text;
				}
				wingmanCommands.Add(text2, voiceCommandMapping.command);
				list.Add(text2);
			}
		}
		wingmanRecognitionPhrases = list.ToArray();
	}

	public static void ClearSpecifiedATCCommands()
	{
		specifiedAtcCommands.Clear();
	}

	public static string[] GetPlayerATCCommands(string specifiedApName = null)
	{
		if (atcMapping == null)
		{
			atcMapping = new ATCVoiceCommandMapping[5]
			{
				new ATCVoiceCommandMapping(ATCCommands.RequestingLanding, "requesting landing", "request landing", "coming in for a landing", "i need to land", "ready to land", "ready for landing", "requesting permission to land", "inbound for landing", "inbound for landing full stop"),
				new ATCVoiceCommandMapping(ATCCommands.RequestingTakeoff, "requesting take off", "requesting permission to take off", "ready for take off", "taking off", "requesting taxi to runway", "request taxi", "requesting taxi", "ready to taxi", "ready to taxi to runway", "ready for departure", "requesting departure", "ready to depart", "i'm go for take off", "request to take off"),
				new ATCVoiceCommandMapping(ATCCommands.CancelRequest, "cancel request", "disregard request", "cancel landing", "forget that", "cancel that", "never mind", "disregard"),
				new ATCVoiceCommandMapping(ATCCommands.RequestingVerticalTakeoff, "requesting vertical take off", "ready for vertical take off", "ready for vertical lift off", "permission to take off vertically"),
				new ATCVoiceCommandMapping(ATCCommands.RequestingVerticalLanding, "requesting vertical landing", "permission to land vertically", "i need a landing pad", "inbound for vertical landing", "coming in for a vertical landing", "reserve a landing pad")
			};
		}
		Actor playerActor = FlightSceneManager.instance.playerActor;
		string text = playerActor.designation.letter.ToString().ToLower() + " " + numberStrings[playerActor.designation.num1] + " " + numberStrings[playerActor.designation.num2];
		Debug.Log("playerName string: " + text);
		List<string> list = new List<string>();
		bool flag = !string.IsNullOrEmpty(specifiedApName);
		if (flag)
		{
			list.Add(specifiedApName, specifiedApName + " " + text);
		}
		else
		{
			list.Add("eighty sea", "tower", "tower " + text, "eighty sea " + text);
		}
		List<string> list2 = new List<string>();
		if (!flag)
		{
			atcCommands.Clear();
		}
		ATCVoiceCommandMapping[] array = atcMapping;
		for (int i = 0; i < array.Length; i++)
		{
			ATCVoiceCommandMapping aTCVoiceCommandMapping = array[i];
			string[] inputs = aTCVoiceCommandMapping.inputs;
			foreach (string text2 in inputs)
			{
				foreach (string item in list)
				{
					string text3 = item + " " + text2;
					if (!flag)
					{
						atcCommands.Add(text3, aTCVoiceCommandMapping.command);
					}
					else
					{
						specifiedAtcCommands.Add(text3, aTCVoiceCommandMapping.command);
					}
					list2.Add(text3);
				}
			}
		}
		if (!flag)
		{
			string text4 = PilotSaveManager.currentVehicle.nickname.ToLower();
			string[] array2 = new string[4]
			{
				text + " " + text4,
				text,
				text4,
				string.Empty
			};
			array = new ATCVoiceCommandMapping[3]
			{
				new ATCVoiceCommandMapping(ATCCommands.Meatball, "ball"),
				new ATCVoiceCommandMapping(ATCCommands.ClaraBall, "clara ball", "clara"),
				new ATCVoiceCommandMapping(ATCCommands.WavingOff, "waving off", "going around", "aborting landing")
			};
			string[] inputs;
			for (int i = 0; i < array.Length; i++)
			{
				ATCVoiceCommandMapping aTCVoiceCommandMapping2 = array[i];
				inputs = aTCVoiceCommandMapping2.inputs;
				foreach (string text5 in inputs)
				{
					string[] array3 = array2;
					for (int k = 0; k < array3.Length; k++)
					{
						string text6 = array3[k] + " " + text5;
						list2.Add(text6);
						atcCommands.Add(text6, aTCVoiceCommandMapping2.command);
					}
				}
			}
			string text7 = "ground crew";
			ATCVoiceCommandMapping aTCVoiceCommandMapping3 = new ATCVoiceCommandMapping(ATCCommands.RequestRearm, "requesting ree arm", "requesting refuel", "ree arm and refuel", "ree arm", "refuel", "repair", "requesting repair", "I need fuel", "requesting service");
			inputs = aTCVoiceCommandMapping3.inputs;
			foreach (string text8 in inputs)
			{
				string text9 = text7 + " " + text8;
				atcCommands.Add(text9, aTCVoiceCommandMapping3.command);
				list2.Add(text9);
			}
		}
		return list2.ToArray();
	}

	public static bool TryRecognizeATCCommand(string command, out ATCCommands output)
	{
		command = command.ToLower();
		output = ATCCommands.Unrecognized;
		if (atcCommands.TryGetValue(command, out output))
		{
			return true;
		}
		return false;
	}

	public static bool TryRecognizeSpecifiedATCCommand(string command, out ATCCommands output)
	{
		command = command.ToLower();
		output = ATCCommands.Unrecognized;
		if (specifiedAtcCommands.TryGetValue(command, out output))
		{
			return true;
		}
		return false;
	}

	public static bool TryRecognizeWingmanCommand(string command, out WingmanCommands output)
	{
		command = command.ToLower();
		output = WingmanCommands.Unrecognized;
		if (wingmanCommands.TryGetValue(command, out output))
		{
			return true;
		}
		return false;
	}

	public static string[] GetPlayerAWACSCommands()
	{
		if (awacsMapping == null)
		{
			awacsMapping = new AWACSVoiceCommandMapping[3]
			{
				new AWACSVoiceCommandMapping(AWACSCommands.BogeyDope, "request bogey dope", "bogey dope", "ready for tasking", "bra", "who is next", "give me a target", "where are they"),
				new AWACSVoiceCommandMapping(AWACSCommands.Picture, "request picture", "picture"),
				new AWACSVoiceCommandMapping(AWACSCommands.RTB, "request arty bee", "arty bee")
			};
		}
		Actor playerActor = FlightSceneManager.instance.playerActor;
		string text = playerActor.designation.letter.ToString().ToLower() + " " + numberStrings[playerActor.designation.num1] + " " + numberStrings[playerActor.designation.num2];
		Debug.Log("playerName string: " + text);
		string[] array = new string[8]
		{
			"ay wax",
			"overlord",
			"magic",
			"dark star",
			"ay wax " + text,
			"overlord " + text,
			"magic " + text,
			"dark star " + text
		};
		List<string> list = new List<string>();
		awacsCommands.Clear();
		AWACSVoiceCommandMapping[] array2 = awacsMapping;
		for (int i = 0; i < array2.Length; i++)
		{
			AWACSVoiceCommandMapping aWACSVoiceCommandMapping = array2[i];
			string[] inputs = aWACSVoiceCommandMapping.inputs;
			foreach (string text2 in inputs)
			{
				string[] array3 = array;
				for (int k = 0; k < array3.Length; k++)
				{
					string text3 = array3[k] + " " + text2;
					awacsCommands.Add(text3, aWACSVoiceCommandMapping.command);
					list.Add(text3);
				}
			}
		}
		return list.ToArray();
	}

	public static bool TryRecognizeAwacsCommand(string command, out AWACSCommands output)
	{
		command = command.ToLower();
		output = AWACSCommands.Unrecognized;
		if (awacsCommands.TryGetValue(command, out output))
		{
			return true;
		}
		return false;
	}
}
