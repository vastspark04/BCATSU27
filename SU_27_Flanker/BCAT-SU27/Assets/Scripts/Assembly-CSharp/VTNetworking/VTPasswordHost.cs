using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace VTNetworking{

public class VTPasswordHost : SocketManager
{
	private const int MAX_WRONG_GUESSES = 5;

	private byte[] pwBuffer = new byte[1024];

	public string hostPassword;

	private Dictionary<SteamId, Connection> connections = new Dictionary<SteamId, Connection>();

	private Dictionary<SteamId, int> wrongGuesses = new Dictionary<SteamId, int>();

	private byte[] response = new byte[1];

	public event Action<SteamId> OnPasswordValid;

	public event Action<SteamId> OnPasswordInvalid;

	~VTPasswordHost()
	{
	}

	public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
	{
		if (wrongGuesses.TryGetValue(identity.SteamId, out var value) && value >= 5)
		{
			connection.Close();
			return;
		}
		if (!connections.ContainsKey(identity.SteamId))
		{
			connections.Add(identity.SteamId, connection);
		}
		Marshal.Copy(data, pwBuffer, 0, Mathf.Min(pwBuffer.Length, size));
		int count = pwBuffer[0];
		string @string = Encoding.UTF8.GetString(pwBuffer, 1, count);
		response[0] = 0;
		if (@string == hostPassword)
		{
			response[0] = 1;
			this.OnPasswordValid?.Invoke(identity.SteamId);
		}
		else
		{
			if (wrongGuesses.TryGetValue(identity.SteamId, out var value2))
			{
				value2++;
				wrongGuesses[identity.SteamId] = value2;
			}
			else
			{
				wrongGuesses.Add(identity.SteamId, 1);
			}
			this.OnPasswordInvalid?.Invoke(identity.SteamId);
		}
		connection.SendMessage(response, SendType.NoDelay | SendType.Reliable);
	}

	public void CloseConnectionTo(SteamId id)
	{
		if (connections.TryGetValue(id, out var value))
		{
			value.Close();
		}
	}

	public override void OnDisconnected(Connection connection, ConnectionInfo info)
	{
		base.OnDisconnected(connection, info);
		SteamId steamId = default(SteamId);
		foreach (KeyValuePair<SteamId, Connection> connection2 in connections)
		{
			if ((uint)connection2.Value == (uint)connection)
			{
				steamId = connection2.Key;
				break;
			}
		}
		if ((ulong)steamId != 0L)
		{
			connections.Remove(steamId);
		}
	}
}

}