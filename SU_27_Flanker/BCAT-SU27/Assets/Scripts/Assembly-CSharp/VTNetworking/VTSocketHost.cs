using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FlatBuffers;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace VTNetworking{

public class VTSocketHost : SocketManager
{
	public struct ConnectedClient
	{
		public SteamId steamId;

		public Connection connection;

		public ConnectedClient(SteamId s, Connection c)
		{
			steamId = s;
			connection = c;
		}
	}

	private struct PingTest
	{
		public int randNum;

		public float timeSent;
	}

	private delegate void MsgDelegate(Connection connection, NetIdentity identity, byte[] bData, int contentSize, long messageNum, long recvTime, int channel);

	private class CValidator : MonoBehaviour
	{
		private float startTime;

		private Coroutine r;

		public void Begin(Connection c)
		{
			startTime = Time.realtimeSinceStartup;
			r = StartCoroutine(VRoutine(c));
		}

		public void Validate()
		{
			float num = Time.realtimeSinceStartup - startTime;
			Debug.Log($"Validated connection in {num} seconds");
			if (r != null)
			{
				StopCoroutine(r);
			}
			UnityEngine.Object.Destroy(base.gameObject);
		}

		private IEnumerator VRoutine(Connection c)
		{
			for (float t = 0f; t < 5f; t += Time.deltaTime)
			{
				yield return null;
			}
			c.Close();
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private struct BufferedInstantiation
	{
		public int entityID;

		public ulong ownerID;

		public byte[] msgBytes;
	}

	public VTNetworkReceiver receiver;

	private ByteArrayAllocator bAlloc;

	private ByteBuffer bbuf;

	public List<ConnectedClient> connectedClients = new List<ConnectedClient>();

	public Dictionary<ulong, ConnectedClient> connectedClientsDict = new Dictionary<ulong, ConnectedClient>();

	private Dictionary<SteamId, int> pings = new Dictionary<SteamId, int>();

	private Dictionary<SteamId, PingTest> pingTests = new Dictionary<SteamId, PingTest>();

	private byte[] headerBuffer = new byte[1];

	private Dictionary<VTNMessageHeaders, MsgDelegate> messageActions;

	private Dictionary<uint, CValidator> cValidators = new Dictionary<uint, CValidator>();

	private byte[] pingTestBuffer = new byte[2];

	private byte[] pingInfoBuffer = new byte[128];

	private byte[] tsBfr = new byte[9];

	private List<BufferedInstantiation> bufferedInstantiations = new List<BufferedInstantiation>();

	public event Action<NetIdentity> OnClientConnected;

	public event Action<SteamId> OnClientDisconnected;

	public VTSocketHost()
	{
		Debug.Log("VTSocketHost ctor");
		SetupMessageActions();
	}

	public void Dispose()
	{
		if (bAlloc != null)
		{
			bAlloc.Dispose();
			bAlloc = null;
		}
		if (bbuf != null)
		{
			bbuf.Dispose();
			bbuf = null;
		}
	}

	public void CloseConnectionTo(SteamId user)
	{
		if (connectedClientsDict.TryGetValue(user, out var value))
		{
			value.connection.Close();
			Debug.Log($"VTSocketHost - Forcing disconnect user {value.steamId} ({new Friend(value.steamId).Name})");
		}
	}

	public float GetAverageClientPingsSeconds()
	{
		if (pings.Count < 1)
		{
			return 0f;
		}
		float num = 0f;
		foreach (int value in pings.Values)
		{
			num += (float)value;
		}
		return num / 1000f / (float)pings.Count;
	}

	public int GetPingMs(SteamId client)
	{
		if (pings.TryGetValue(client, out var value))
		{
			return value;
		}
		return -1;
	}

	public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
	{
		base.OnMessage(connection, identity, data, size, messageNum, recvTime, channel);
		VTNetworkManager.instance.ReportReceivedData(size);
		if (bAlloc == null)
		{
			byte[] buffer = new byte[size];
			bAlloc = new ByteArrayAllocator(buffer);
			bbuf = new ByteBuffer(bAlloc, 0);
		}
		else if (bAlloc.Buffer.Length < size)
		{
			bAlloc.GrowFront(size);
		}
		bbuf.Position = 0;
		int num = size - 1;
		Marshal.Copy(data, headerBuffer, 0, 1);
		Marshal.Copy(data + 1, bAlloc.Buffer, 0, num);
		VTNMessageHeaders vTNMessageHeaders = (VTNMessageHeaders)headerBuffer[0];
		switch (vTNMessageHeaders)
		{
		case VTNMessageHeaders.SyncState:
		{
			for (int i = 0; i < connectedClients.Count; i++)
			{
				ConnectedClient connectedClient = connectedClients[i];
				if ((ulong)connectedClient.steamId != (ulong)(SteamId)identity)
				{
					connectedClient.connection.SendMessage(data, size, SendType.NoNagle | SendType.Reliable);
					VTNetworkManager.instance.ReportSentData(size);
				}
			}
			VTNetworkManager.instance.ReceiveSyncMessage(bbuf, bAlloc, num);
			break;
		}
		case VTNMessageHeaders.DirectedRPCs:
		{
			byte[] buffer2 = bAlloc.Buffer;
			ulong num2 = VTNetUtils.BytesToULong(buffer2[0], buffer2[1], buffer2[2], buffer2[3], buffer2[4], buffer2[5], buffer2[6], buffer2[7]);
			ConnectedClient value2;
			if (num2 == BDSteamClient.mySteamID)
			{
				VTNetworkManager.instance.ReceiveSyncMessage(bbuf, bAlloc, num - 8, 8);
			}
			else if (connectedClientsDict.TryGetValue(num2, out value2))
			{
				value2.connection.SendMessage(data, size, SendType.NoNagle | SendType.Reliable);
				VTNetworkManager.instance.ReportSentData(size);
			}
			break;
		}
		case VTNMessageHeaders.Voice:
		{
			byte[] buffer3 = bAlloc.Buffer;
			ulong num3 = VTNetUtils.BytesToULong(buffer3[0], buffer3[1], buffer3[2], buffer3[3], buffer3[4], buffer3[5], buffer3[6], buffer3[7]);
			ConnectedClient value3;
			if (num3 == BDSteamClient.mySteamID)
			{
				if ((bool)VTNetworkVoice.instance)
				{
					ulong incomingID = VTNetUtils.BytesToULong(buffer3[8], buffer3[9], buffer3[10], buffer3[11], buffer3[12], buffer3[13], buffer3[14], buffer3[15]);
					VTNetworkVoice.instance.ReceiveVTNetVoiceData(incomingID, buffer3, 16, num - 16);
				}
			}
			else if (connectedClientsDict.TryGetValue(num3, out value3))
			{
				value3.connection.SendMessage(data, size, SendType.NoNagle | SendType.NoDelay);
				VTNetworkManager.instance.ReportSentData(size);
			}
			break;
		}
		default:
		{
			if (messageActions.TryGetValue(vTNMessageHeaders, out var value))
			{
				value(connection, identity, bAlloc.Buffer, num, messageNum, recvTime, channel);
			}
			break;
		}
		}
	}

	private void SetupMessageActions()
	{
		messageActions = new Dictionary<VTNMessageHeaders, MsgDelegate>();
		messageActions.Add(VTNMessageHeaders.Ping, MsgPingReturn);
		messageActions.Add(VTNMessageHeaders.InstantiateRequest, MsgInstantiateRequest);
		messageActions.Add(VTNMessageHeaders.Destroy, MsgDestroy);
		messageActions.Add(VTNMessageHeaders.NewConnection, MsgNewConnection);
		messageActions.Add(VTNMessageHeaders.NewClientReadyForResync, MsgNewClientReadyForResync);
	}

	private void MsgNewConnection(Connection connection, NetIdentity identity, byte[] bData, int contentSize, long messageNum, long recvTime, int channel)
	{
		NewClientConnected(connection, identity);
	}

	private void MsgNewClientReadyForResync(Connection connection, NetIdentity identity, byte[] bData, int contentSize, long messageNum, long recvTime, int channel)
	{
		NewClientReadyForResync(connection, identity);
	}

	private void MsgPingReturn(Connection connection, NetIdentity identity, byte[] bData, int contentSize, long messageNum, long recvTime, int channel)
	{
		int num = bData[0];
		if (pingTests.TryGetValue(identity, out var value) && value.randNum == num)
		{
			int value2 = Mathf.RoundToInt(1000f * ((Time.realtimeSinceStartup - value.timeSent) / 2f));
			pings[identity] = value2;
			pingTests.Remove(identity);
		}
	}

	private void MsgInstantiateRequest(Connection connection, NetIdentity identity, byte[] bData, int contentSize, long messageNum, long recvTime, int channel)
	{
		Debug.Log($"Got instantiate request from identity: {identity}, steamID: {identity.SteamId}, name: {new Friend(identity.SteamId).Name}");
		VTNetworkManager.instance.ReceiveInstantiateRequestMessage(bData, contentSize, identity);
	}

	private void MsgDestroy(Connection connection, NetIdentity identity, byte[] bData, int contentSize, long messageNum, long recvTime, int channel)
	{
		int num = BitConverter.ToInt32(bData, 0);
		VTNetworkManager.instance.SendNetDestroyCommandToClients(num);
		UnregisterInstantiation(num);
	}

	public override void OnConnecting(Connection connection, ConnectionInfo data)
	{
		Debug.LogFormat("A client is connecting...\nID: {0}\nState: {1}\nDetailed Status: {2}", new Friend(data.Identity.SteamId).ToString(), data.State.ToString(), connection.DetailedStatus());
		connection.Accept();
	}

	public override void OnConnected(Connection connection, ConnectionInfo data)
	{
		base.OnConnected(connection, data);
		CValidator cValidator = new GameObject("cv").AddComponent<CValidator>();
		cValidator.Begin(connection);
		cValidators.Add(connection.Id, cValidator);
	}

	private void NewClientConnected(Connection connection, NetIdentity identity)
	{
		if (cValidators.TryGetValue(connection.Id, out var value))
		{
			value.Validate();
		}
		ConnectedClient connectedClient = new ConnectedClient(identity, connection);
		connectedClients.Add(connectedClient);
		connectedClientsDict.Add(identity.SteamId.Value, connectedClient);
		Debug.Log($"A user has connected: {identity} [{new Friend(identity.SteamId).Name}] connectionID: {connection.Id}");
		VTNetworkManager.instance.InitSceneEntitiesOnNewConnection(connection);
		if (identity.IsSteamId)
		{
			if (!pings.ContainsKey(identity.SteamId))
			{
				pings.Add(identity.SteamId, -1);
			}
			else
			{
				Debug.LogError("VTSocketHost pings already contains key: " + identity.SteamId.Value);
			}
		}
		if (VTNetworkManager.instance.passwordHost != null && !VTNetworkManager.IsConnectingUserValidated(identity.SteamId))
		{
			Debug.Log("VTSocketHost: An unvalidated client connected to the host!");
			connection.Close();
		}
	}

	private void NewClientReadyForResync(Connection connection, NetIdentity identity)
	{
		foreach (BufferedInstantiation bufferedInstantiation in bufferedInstantiations)
		{
			if (bufferedInstantiation.ownerID == (ulong)identity.SteamId)
			{
				bufferedInstantiation.msgBytes[0] = 3;
			}
			else
			{
				bufferedInstantiation.msgBytes[0] = 4;
			}
			VTNetworkManager.instance.ReportSentData(bufferedInstantiation.msgBytes.Length);
			connection.SendMessage(bufferedInstantiation.msgBytes, SendType.NoNagle | SendType.Reliable);
		}
		VTNetworkManager.instance.SendBaselinesToNewConnection(connection, identity.SteamId);
		VTNetUtils.ULongToBytes(identity.SteamId.Value, out var a, out var b, out var c, out var d, out var e, out var f, out var g, out var h);
		byte[] data = new byte[9] { 9, a, b, c, d, e, f, g, h };
		foreach (ConnectedClient connectedClient in connectedClients)
		{
			if ((ulong)connectedClient.steamId != (ulong)identity.SteamId)
			{
				VTNetworkManager.instance.ReportSentData(9);
				Connection connection2 = connectedClient.connection;
				connection2.SendMessage(data);
			}
		}
		this.OnClientConnected?.Invoke(identity);
	}

	public override void OnConnectionChanged(Connection connection, ConnectionInfo data)
	{
		base.OnConnectionChanged(connection, data);
	}

	public override void OnDisconnected(Connection connection, ConnectionInfo data)
	{
		base.OnDisconnected(connection, data);
		ulong num = 0uL;
		for (int i = 0; i < connectedClients.Count; i++)
		{
			if ((uint)connectedClients[i].connection == (uint)connection)
			{
				Debug.Log($"Client '{new Friend(connectedClients[i].steamId).Name} ({connectedClients[i].steamId}) [{data.Identity.SteamId.Value}]' has disconnected. Destroying all objects owned by them.\nDetailed Status: {connection.DetailedStatus()}");
				VTNetworkManager.instance.NetDestroyAllObjectsByOwner(connectedClients[i].steamId);
				num = connectedClients[i].steamId;
			}
		}
		if (num != 0L)
		{
			VTNetUtils.ULongToBytes(num, out var a, out var b, out var c, out var d, out var e, out var f, out var g, out var h);
			byte[] data2 = new byte[9] { 10, a, b, c, d, e, f, g, h };
			for (int j = 0; j < connectedClients.Count; j++)
			{
				if ((uint)connectedClients[j].connection != (uint)connection)
				{
					VTNetworkManager.instance.ReportSentData(9);
					connectedClients[j].connection.SendMessage(data2);
				}
			}
			this.OnClientDisconnected?.Invoke(num);
			pings.Remove(num);
		}
		connectedClients.RemoveAll((ConnectedClient x) => (uint)x.connection == (uint)connection);
		connectedClientsDict.Remove(num);
	}

	public void SendPingTestsToClients()
	{
		foreach (ConnectedClient connectedClient in connectedClients)
		{
			SendPingTest(connectedClient);
		}
	}

	private void SendPingTest(ConnectedClient c)
	{
		if (!pingTests.ContainsKey(c.steamId))
		{
			int size = pingTestBuffer.Length;
			pingTestBuffer[0] = 0;
			int num = UnityEngine.Random.Range(0, 256);
			pingTestBuffer[1] = (byte)num;
			pingTests.Add(c.steamId, new PingTest
			{
				randNum = num,
				timeSent = Time.realtimeSinceStartup
			});
			VTNetworkManager.instance.ReportSentData(size);
			c.connection.SendMessage(pingTestBuffer, SendType.NoNagle | SendType.Reliable);
		}
	}

	public void SendPingInfosToClients()
	{
		int num = 8;
		int num2 = 4;
		int count = connectedClients.Count;
		int num3 = 1 + count * (num + num2);
		if (num3 > pingInfoBuffer.Length)
		{
			Debug.Log("New max ping info buffer size: " + num3);
			pingInfoBuffer = new byte[num3];
		}
		pingInfoBuffer[0] = 7;
		int num4 = 1;
		for (int i = 0; i < connectedClients.Count; i++)
		{
			int value = -1;
			pings.TryGetValue(connectedClients[i].steamId, out value);
			VTNetUtils.IntToBytes(value, out var a, out var b, out var c, out var d);
			VTNetUtils.ULongToBytes(connectedClients[i].steamId, out var a2, out var b2, out var c2, out var d2, out var e, out var f, out var g, out var h);
			pingInfoBuffer[num4] = a2;
			pingInfoBuffer[num4 + 1] = b2;
			pingInfoBuffer[num4 + 2] = c2;
			pingInfoBuffer[num4 + 3] = d2;
			pingInfoBuffer[num4 + 4] = e;
			pingInfoBuffer[num4 + 5] = f;
			pingInfoBuffer[num4 + 6] = g;
			pingInfoBuffer[num4 + 7] = h;
			pingInfoBuffer[num4 + 8] = a;
			pingInfoBuffer[num4 + 9] = b;
			pingInfoBuffer[num4 + 10] = c;
			pingInfoBuffer[num4 + 11] = d;
			num4 += num + num2;
		}
		for (int j = 0; j < connectedClients.Count; j++)
		{
			VTNetworkManager.instance.ReportSentData(num3);
			connectedClients[j].connection.SendMessage(pingInfoBuffer, 0, num3, SendType.NoNagle | SendType.Reliable);
		}
	}

	public void SendTimeSyncToClients()
	{
		int val = Mathf.RoundToInt(VTNetworkManager.GetNetworkTimestamp() * 10000f);
		tsBfr[0] = 8;
		VTNetUtils.IntToBytes(val, out tsBfr[1], out tsBfr[2], out tsBfr[3], out tsBfr[4]);
		for (int i = 0; i < connectedClients.Count; i++)
		{
			ConnectedClient connectedClient = connectedClients[i];
			VTNetUtils.IntToBytes(pings[connectedClient.steamId], out tsBfr[5], out tsBfr[6], out tsBfr[7], out tsBfr[8]);
			VTNetworkManager.instance.ReportSentData(tsBfr.Length);
			connectedClient.connection.SendMessage(tsBfr, SendType.NoNagle | SendType.Reliable);
		}
	}

	public void RegisterInstantiation(byte[] msgBytes, ulong ownerID, int entityID)
	{
		bufferedInstantiations.Add(new BufferedInstantiation
		{
			entityID = entityID,
			msgBytes = msgBytes,
			ownerID = ownerID
		});
	}

	public void UnregisterInstantiation(int entityID)
	{
		int num = -1;
		for (int i = 0; i < bufferedInstantiations.Count; i++)
		{
			if (bufferedInstantiations[i].entityID == entityID)
			{
				num = i;
				break;
			}
		}
		if (num >= 0)
		{
			bufferedInstantiations.RemoveAt(num);
		}
	}
}

}