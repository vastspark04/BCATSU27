using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using UnityEngine;
using VTOLVR.SteamWorkshop;

public static class VTSteamWorkshopUtils
{
	public delegate void WorkshopItemExistsDelegate(bool ableToUpdate, string message);

	private class WorkshopQueryBehaviour : MonoBehaviour
	{
		private PublishedFileId fileID;

		private WorkshopItemExistsDelegate onComplete;

		public void CheckUpdateValid(PublishedFileId fileID, WorkshopItemExistsDelegate onComplete)
		{
			this.fileID = fileID;
			this.onComplete = onComplete;
			StartCoroutine(CheckUpdateValidRoutine());
		}

		private IEnumerator CheckUpdateValidRoutine()
		{
			Debug.Log("Checking if we can update a workshop item.");
			Task<Item?> task = SteamUGC.QueryFileAsync(fileID);
			while (!task.IsCompleted)
			{
				yield return null;
			}
			bool flag = false;
			string text;
			if (task.Result.HasValue)
			{
				Item value = task.Result.Value;
				if (value.Owner.IsMe)
				{
					if (value.CreatorApp.Value != BDSteamClient.APP_ID)
					{
						text = "This is not a VTOL VR item!";
					}
					else
					{
						flag = true;
						text = "Item exists.";
					}
				}
				else
				{
					text = $"You're not the owner of this item. (ownerID == {value.Owner.Id})";
				}
			}
			else
			{
				text = "Item not found.";
			}
			Debug.Log("- We could " + (flag ? "" : "NOT ") + "update this item. (" + text + ")");
			onComplete?.Invoke(flag, text);
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public class WorkshopItemInfo
	{
		public ulong PublishedFileId { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public string IconFileName { get; set; }

		public List<string> Tags { get; set; }
	}

	private class UploadToWorkshopBehaviour : MonoBehaviour
	{
		private WorkshopItemUpdate u;

		private Editor ed;

		private Action<WorkshopItemUpdateEventArgs> onComplete;

		public void Upload(WorkshopItemUpdate u, Editor ed, Action<WorkshopItemUpdateEventArgs> onComplete)
		{
			this.ed = ed;
			this.onComplete = onComplete;
			this.u = u;
			StartCoroutine(UploadRoutine());
		}

		private IEnumerator UploadRoutine()
		{
			Task<PublishResult> task = ed.SubmitAsync(u);
			Debug.Log("Beginning workshop upload task.");
			while (!task.IsCompleted)
			{
				yield return null;
			}
			WorkshopItemUpdateEventArgs obj = default(WorkshopItemUpdateEventArgs);
			if (task.Result.NeedsWorkshopAgreement)
			{
				obj.IsError = true;
				obj.ErrorMessage = "The user must agree to the Steam Workshop Legal Agreement. (https://steamcommunity.com/sharedfiles/workshoplegalagreement)";
			}
			else if (task.Result.Success)
			{
				obj.IsError = false;
				obj.ErrorMessage = "Upload success!";
			}
			else
			{
				obj.IsError = true;
				obj.ErrorMessage = task.Result.Result.ToString();
			}
			if (u.PublishedFileId == 0L)
			{
				Debug.Log("Uploading xml with new file ID");
				u.PublishedFileId = task.Result.FileId;
				CreateItemUpdateFile(u);
			}
			Debug.Log($"Workshop upload complete. IsError={obj.IsError}, ErrorMessage={obj.ErrorMessage}");
			onComplete?.Invoke(obj);
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public class ItemDownload
	{
		public Item item;

		public bool isDone;

		public float progress => item.DownloadAmount;
	}

	private class AsyncDownloadBehaviour : MonoBehaviour
	{
		private ItemDownload id;

		private Action<Item> onComplete;

		public void Download(ItemDownload id, Action<Item> onComplete)
		{
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			this.id = id;
			this.onComplete = onComplete;
			StartCoroutine(DownloadRoutine());
		}

		private IEnumerator DownloadRoutine()
		{
			Task<bool> task = id.item.Subscribe();
			while (!task.IsCompleted)
			{
				yield return null;
			}
			id.isDone = true;
			onComplete?.Invoke(id.item);
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public class VTWorkshopUploadTempFile
	{
		private bool _disposed;

		public string tempPath { get; private set; }

		public string origPath { get; private set; }

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}
			Debug.Log("Disposing temporary file: " + tempPath);
			if (Directory.Exists(tempPath))
			{
				if (Directory.Exists(origPath))
				{
					int length = tempPath.Length;
					string[] files = Directory.GetFiles(tempPath, "*.xml", SearchOption.AllDirectories);
					foreach (string text in files)
					{
						string path = text.Substring(length + 1);
						string text2 = Path.Combine(origPath, path);
						if (File.Exists(text2))
						{
							File.Delete(text2);
						}
						bool flag = false;
						int num = 0;
						Exception ex = null;
						while (!flag && num < 5)
						{
							try
							{
								File.Move(text, text2);
								flag = true;
							}
							catch (Exception ex2)
							{
								ex = ex2;
								Thread.Sleep(100);
							}
							finally
							{
								num++;
							}
						}
						if (flag)
						{
							Debug.Log("Copied workshop xml from " + text + " to " + text2);
							continue;
						}
						Debug.LogError("Failed to copy xml from workshop upload temp file.\nf: " + text + "\noutPath: " + text2 + "\n" + ((ex != null) ? ex.ToString() : ""));
					}
				}
				else
				{
					Debug.Log("- orig file does not exist!");
				}
			}
			else
			{
				Debug.Log("- Temp file does not exist!");
			}
			tempPath = string.Empty;
			_disposed = true;
		}

		public VTWorkshopUploadTempFile(string path, string[] excludeExtensions = null)
		{
			Debug.Log("Creating temp file for upload: " + tempPath);
			origPath = path;
			string text = Path.Combine(Path.GetTempPath(), "VTVRTEMP");
			int num = 0;
			tempPath = text + num;
			while (Directory.Exists(tempPath))
			{
				num++;
				tempPath = text + num;
			}
			VTResources.CopyDirectory(origPath, tempPath, excludeExtensions);
		}

		~VTWorkshopUploadTempFile()
		{
			Dispose();
		}
	}

	public struct ScenarioValidation
	{
		public bool valid;

		public List<string> messages;

		public VTScenarioInfo scenarioInfo;

		public VTCampaignInfo campaignInfo;
	}

	private const int byteSize = 256;

	private const int key = 88;

	private static byte[] readEncodeBuffer = new byte[0];

	private static string[] excludeExtsOnSWUpload = new string[8] { ".xml", ".meta", ".exe", ".bat", ".dll", ".scr", ".bin", ".py" };

	public static void CheckCanUpdateItem(PublishedFileId fileID, WorkshopItemExistsDelegate onComplete)
	{
		new GameObject("WorkshopUpdateValidator").AddComponent<WorkshopQueryBehaviour>().CheckUpdateValid(fileID, onComplete);
	}

	public static void UploadToWorkshop(WorkshopItemUpdate u, Action<WorkshopItemUpdateEventArgs> onComplete)
	{
		Editor ed = ((u.PublishedFileId != 0L) ? new Editor(u.PublishedFileId) : Editor.NewCommunityFile.ForAppId(BDSteamClient.APP_ID));
		ed.WithTitle(u.Name);
		ed.WithDescription(u.Description);
		ed.WithPreviewFile(u.IconPath);
		ed.WithContent(u.ContentPath);
		ed.WithPublicVisibility();
		foreach (string tag in u.Tags)
		{
			ed.WithTag(tag);
		}
		if (!string.IsNullOrEmpty(u.ChangeNote))
		{
			ed.WithChangeLog(u.ChangeNote);
		}
		CreateItemUpdateFile(u);
		new GameObject("Uploader").AddComponent<UploadToWorkshopBehaviour>().Upload(u, ed, onComplete);
	}

	public static WorkshopItemUpdate GetItemUpdateFromFolder(string path)
	{
		string[] files = Directory.GetFiles(path, "*.xml");
		int num = 0;
		if (num < files.Length)
		{
			string path2 = files[num];
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(WorkshopItemInfo));
			using StringReader textReader = new StringReader(File.ReadAllText(path2));
			WorkshopItemInfo workshopItemInfo = (WorkshopItemInfo)xmlSerializer.Deserialize(textReader);
			return new WorkshopItemUpdate
			{
				PublishedFileId = workshopItemInfo.PublishedFileId,
				Name = workshopItemInfo.Name,
				Description = workshopItemInfo.Description,
				IconPath = Path.Combine(path, workshopItemInfo.IconFileName),
				Tags = workshopItemInfo.Tags
			};
		}
		return null;
	}

	public static void CreateItemUpdateFile(WorkshopItemUpdate u)
	{
		string contentPath = u.ContentPath;
		Debug.Log("Creating a WorkshopItemInfo xml for " + u.Name);
		string[] files = Directory.GetFiles(contentPath, "*.xml");
		foreach (string text in files)
		{
			if (Path.GetFileName(text).Contains("WorkshopItemInfo"))
			{
				Debug.Log(" - There's already an item info file.  Deleting it. (" + text + ")");
				File.Delete(text);
			}
		}
		WorkshopItemInfo workshopItemInfo = new WorkshopItemInfo();
		workshopItemInfo.Name = u.Name;
		workshopItemInfo.Description = u.Description;
		workshopItemInfo.PublishedFileId = u.PublishedFileId;
		workshopItemInfo.Tags = u.Tags;
		workshopItemInfo.IconFileName = Path.GetFileName(u.IconPath);
		string path = Path.Combine(contentPath, "WorkshopItemInfo.xml");
		StringBuilder stringBuilder = new StringBuilder();
		using StringWriter textWriter = new StringWriter(stringBuilder);
		new XmlSerializer(typeof(WorkshopItemInfo)).Serialize(textWriter, workshopItemInfo);
		File.Create(path).Dispose();
		File.WriteAllText(path, stringBuilder.ToString());
	}

	public static string ReadFirstLine(string path)
	{
		if (!File.Exists(path))
		{
			return string.Empty;
		}
		return File.ReadLines(path).First();
	}

	public static ConfigNode ReadWorkshopConfig(string path)
	{
		if (!File.Exists(path))
		{
			Debug.Log("Tried to read a workshop config but it does not exist (" + path + ")");
			return null;
		}
		switch (ReadFirstLine(path))
		{
		case "CustomScenario":
		case "CAMPAIGN":
		case "VTMapCustom":
			Debug.Log("Workshop config file was not encrypted! (" + path + ")");
			return ConfigNode.LoadFromFile(path);
		default:
		{
			using Stream stream = File.Open(path, FileMode.Open);
			int num = (int)stream.Length;
			if (readEncodeBuffer.Length < num)
			{
				readEncodeBuffer = new byte[num];
			}
			byte[] array = readEncodeBuffer;
			int num2 = 0;
			int data;
			while ((data = stream.ReadByte()) != -1)
			{
				array[num2] = WSDecode(data);
				num2++;
			}
			return ConfigNode.ParseNode(Encoding.UTF8.GetString(array, 0, num));
		}
		}
	}

	public static void WriteWorkshopConfig(ConfigNode configNode, string outPath)
	{
		string s = ConfigNode.WriteNode(configNode, 0);
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		using Stream stream = File.Open(outPath, FileMode.OpenOrCreate);
		for (int i = 0; i < bytes.Length; i++)
		{
			stream.WriteByte(WSEncode(bytes[i]));
		}
	}

	public static void WSEncode(string filepath)
	{
		byte[] array = File.ReadAllBytes(filepath);
		WSEncode(array);
		File.WriteAllBytes(filepath + "b", array);
		File.Delete(filepath);
	}

	public static void WSEncode(byte[] byteArray)
	{
		for (int i = 0; i < byteArray.Length; i++)
		{
			byteArray[i] = WSEncode(byteArray[i]);
		}
	}

	private static byte WSEncode(int data)
	{
		return (byte)((data + 88) % 256);
	}

	private static byte WSDecode(int data)
	{
		return (byte)((data - 88) % 256);
	}

	public static void WSDecode(byte[] byteArray)
	{
		for (int i = 0; i < byteArray.Length; i++)
		{
			byteArray[i] = WSDecode(byteArray[i]);
		}
	}

	public static void WSDecode(string filepath)
	{
		if (!filepath.EndsWith("b"))
		{
			Debug.Log("Tried to wsDecode a file that did not have an extension ending with 'b'");
			return;
		}
		byte[] array = File.ReadAllBytes(filepath);
		WSDecode(array);
		File.WriteAllBytes(filepath.Substring(0, filepath.Length - 1), array);
		File.Delete(filepath);
	}

	public static void EncodeAllWorkshopConfigs(string path)
	{
		string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
		foreach (string text in files)
		{
			string extension = Path.GetExtension(text);
			switch (extension)
			{
			case ".vts":
			case ".vtm":
			case ".vtc":
			{
				if (extension == ".vtm")
				{
					string text2 = Path.Combine(Path.GetDirectoryName(text), "height.png");
					if (File.Exists(text2))
					{
						WSEncode(text2);
					}
				}
				string outPath = text + "b";
				WriteWorkshopConfig(ConfigNode.LoadFromFile(text), outPath);
				File.Delete(text);
				break;
			}
			}
		}
	}

	public static ItemDownload AsyncSubscribeItem(Item item, Action<Item> onComplete)
	{
		ItemDownload itemDownload = new ItemDownload();
		itemDownload.item = item;
		new GameObject("SWItemDownloader").AddComponent<AsyncDownloadBehaviour>().Download(itemDownload, onComplete);
		return itemDownload;
	}

	public static VTWorkshopUploadTempFile GetSWUploadFile(string origPath)
	{
		VTWorkshopUploadTempFile vTWorkshopUploadTempFile = new VTWorkshopUploadTempFile(origPath, excludeExtsOnSWUpload);
		EncodeAllWorkshopConfigs(vTWorkshopUploadTempFile.tempPath);
		return vTWorkshopUploadTempFile;
	}

	public static ScenarioValidation ValidateScenarioForUpload(string scenarioID, string campaignID)
	{
		bool _valid = true;
		List<string> _messages = new List<string>();
		Action<string> action = delegate(string m)
		{
			_valid = false;
			_messages.Add(m);
		};
		ScenarioValidation result = default(ScenarioValidation);
		if (!string.IsNullOrEmpty(scenarioID))
		{
			VTScenarioInfo customScenario = VTResources.GetCustomScenario(scenarioID, campaignID);
			VTCampaignInfo vTCampaignInfo = null;
			if (!string.IsNullOrEmpty(campaignID))
			{
				vTCampaignInfo = VTResources.GetCustomCampaign(campaignID);
			}
			if (customScenario != null)
			{
				result.scenarioInfo = customScenario;
				ConfigNode config = customScenario.config;
				ConfigNode node = config.GetNode("UNITS");
				if (node != null)
				{
					bool flag = false;
					foreach (ConfigNode node3 in node.GetNodes("UnitSpawner"))
					{
						if (node3.HasValue("unitID"))
						{
							if (vTCampaignInfo != null && vTCampaignInfo.multiplayer)
							{
								if (!flag && node3.GetValue("unitID") == "MultiplayerSpawn")
								{
									flag = true;
								}
							}
							else if (!flag && node3.GetValue("unitID") == "PlayerSpawn")
							{
								flag = true;
							}
						}
						else
						{
							action("Invalid unit config: No ID.");
						}
					}
					if (!flag)
					{
						action("No player spawn.");
					}
				}
				else
				{
					action("No units.");
				}
				if (vTCampaignInfo != null && vTCampaignInfo.availability == VTCampaignInfo.AvailabilityModes.Sequential)
				{
					ConfigNode node2 = config.GetNode("OBJECTIVES");
					if (node2 == null || node2.GetNodes("Objective").Count == 0)
					{
						action("No objectives! Unable to complete campaign.");
					}
				}
				if (config.HasValue("scenarioName"))
				{
					string value = config.GetValue("scenarioName");
					if (string.IsNullOrEmpty(value) || value == "untitled")
					{
						action("Invalid scenario name: \"" + value + "\"");
					}
				}
				else
				{
					action("Invalid config: No scenarioName.");
				}
				if (string.IsNullOrEmpty(customScenario.campaignID))
				{
					if (config.HasValue("mapID"))
					{
						string value2 = config.GetValue("mapID");
						VTMap map = VTResources.GetMap(value2);
						if (map == null || map.sceneName == VTResources.customMapSceneName)
						{
							string path = Path.Combine(customScenario.directoryPath, value2);
							bool flag2 = false;
							if (Directory.Exists(path))
							{
								string[] files = Directory.GetFiles(path, "*.vtm*", SearchOption.TopDirectoryOnly);
								foreach (string text in files)
								{
									if (text.EndsWith(".vtm") || text.EndsWith(".vtmb"))
									{
										flag2 = true;
									}
								}
							}
							if (!flag2)
							{
								action("Map must be packed!");
							}
						}
					}
					else
					{
						action("Invalid config: No mapID");
					}
				}
				if (config.HasValue("scenarioDescription"))
				{
					if (string.IsNullOrEmpty(config.GetValue("scenarioDescription")))
					{
						action("Empty description.");
					}
				}
				else
				{
					action("Invalid config: No scenarioDescription.");
				}
			}
			else
			{
				action("Scenario does not exist!");
			}
		}
		else
		{
			action("No scenario ID! Save scenario first.");
		}
		result.valid = _valid;
		if (_valid)
		{
			_messages.Add("Scenario valid!");
		}
		result.messages = _messages;
		return result;
	}

	public static ScenarioValidation ValidateCampaign(string campaignID)
	{
		bool _valid = true;
		List<string> _messages = new List<string>();
		Action<string> action = delegate(string m)
		{
			_valid = false;
			_messages.Add(m);
		};
		ScenarioValidation result = default(ScenarioValidation);
		if (!string.IsNullOrEmpty(campaignID))
		{
			VTCampaignInfo customCampaign = VTResources.GetCustomCampaign(campaignID);
			if (customCampaign != null)
			{
				result.campaignInfo = customCampaign;
				if (string.IsNullOrEmpty(customCampaign.campaignName) || customCampaign.campaignName == "New Campaign" || customCampaign.campaignName == "untitled")
				{
					action("Invalid campaign name: " + customCampaign.campaignName);
				}
				if (string.IsNullOrEmpty(customCampaign.description) || customCampaign.description == "Enter description...")
				{
					action("Invalid campaign description.");
				}
				foreach (VTScenarioInfo allScenario in customCampaign.allScenarios)
				{
					Debug.Log("validating campaign scenario " + allScenario.id);
					ConfigNode config = allScenario.config;
					if (config.HasValue("mapID"))
					{
						string value = config.GetValue("mapID");
						Debug.Log("- mapID: " + value);
						VTMap map = VTResources.GetMap(value);
						if (map != null)
						{
							Debug.Log("- map.sceneName: " + map.sceneName);
						}
						if (!(map == null) && !(map.sceneName == VTResources.customMapSceneName) && !string.IsNullOrEmpty(map.sceneName))
						{
							continue;
						}
						string path = Path.Combine(customCampaign.directoryPath, value);
						bool flag = false;
						if (Directory.Exists(path))
						{
							string[] files = Directory.GetFiles(path, "*.vtm*", SearchOption.TopDirectoryOnly);
							foreach (string text in files)
							{
								if (text.EndsWith(".vtm") || text.EndsWith(".vtmb"))
								{
									flag = true;
								}
							}
						}
						if (!flag)
						{
							action(allScenario.id + ": Map must be packed!");
						}
					}
					else
					{
						action("Invalid " + allScenario.id + " config: No mapID");
					}
				}
			}
			else
			{
				action("Campaign does not exist: \"" + campaignID + "\"");
			}
		}
		else
		{
			action("Empty campaignID!");
		}
		result.valid = _valid;
		if (_valid)
		{
			_messages.Add("Scenario valid!");
		}
		result.messages = _messages;
		foreach (string message in result.messages)
		{
			Debug.Log(message);
		}
		return result;
	}

	public static string GetAuthorName(Item item)
	{
		string authorName = string.Empty;
		if (!TryGetAuthorName(item.Description, "\n\nby ", out authorName))
		{
			TryGetAuthorName(item.Description, "\r\n\r\nby", out authorName);
		}
		return authorName;
	}

	private static bool TryGetAuthorName(string description, string splitter, out string authorName)
	{
		if (description.Contains(splitter))
		{
			string[] array = description.Split(new string[1] { splitter }, StringSplitOptions.RemoveEmptyEntries);
			authorName = array[array.Length - 1];
			authorName = authorName.Trim('\n', '\r', ' ');
			if (authorName.Contains("\n"))
			{
				authorName = authorName.Substring(0, authorName.IndexOf('\n'));
			}
			return true;
		}
		authorName = string.Empty;
		return false;
	}
}
