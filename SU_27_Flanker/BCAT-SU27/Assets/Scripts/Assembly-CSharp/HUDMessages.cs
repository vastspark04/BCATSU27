using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDMessages : MonoBehaviour
{
	public Text messageText;

	private Dictionary<string, string> messages = new Dictionary<string, string>();

	public void SetMessage(string id, string message)
	{
		if (messages.ContainsKey(id))
		{
			messages[id] = message;
		}
		else
		{
			messages.Add(id, message);
		}
		UpdateMessages();
	}

	public void RemoveMessage(string id)
	{
		if (messages.ContainsKey(id))
		{
			messages.Remove(id);
			UpdateMessages();
		}
	}

	private void UpdateMessages()
	{
		string text = string.Empty;
		foreach (string value in messages.Values)
		{
			text = text + value + "\n";
		}
		messageText.text = text;
	}
}
