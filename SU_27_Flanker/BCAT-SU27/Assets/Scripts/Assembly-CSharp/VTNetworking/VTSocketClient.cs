using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using FlatBuffers;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace VTNetworking{

public class VTSocketClient : ConnectionManager
{
	private ByteArrayAllocator bAlloc;

	private ByteBuffer bbuf;

	private ulong _mySteamID;

	public string overrideDisconnectionReason;

	private byte[] headerBuffer = new byte[1];

	private List<SteamId> clientIds = new List<SteamId>();

	private const int MAX_TIMESYNCS = 32;

	private List<float> receivedClientDeltas = new List<float>();

	private List<float> receivedPings = new List<float>();

	private float lastMsgRecvd = -1f;

	private float nInPing;

	private float inPing;

	private float clientTimeSynced;

	private float _rts = -1f;

	private float receivedTimestamp;

	private float clientDelta;

	private float nClientDelta;

	private Dictionary<SteamId, int> pings = new Dictionary<SteamId, int>();

	private byte[] pingReturnBuffer = new byte[2];

	private List<int> executedInstantiateRequests = new List<int>();

	private ulong mySteamID
	{
		get
		{
			if (_mySteamID == 0L)
			{
				_mySteamID = SteamClient.SteamId.Value;
			}
			return _mySteamID;
		}
	}

	public ConnectionState connectionState { get; private set; }

	public float timeLastPinged { get; set; }

	public int GetPing(SteamId peer)
	{
		if (peer.Value != BDSteamClient.mySteamID && pings.TryGetValue(peer, out var value))
		{
			if (!pings.TryGetValue(SteamClient.SteamId, out var value2))
			{
				value2 = 0;
			}
			return value + value2;
		}
		if (pings.TryGetValue(SteamClient.SteamId, out value))
		{
			return value;
		}
		return -1;
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

	public override void OnConnecting(ConnectionInfo data)
	{
		base.OnConnecting(data);
		Debug.Log("VTSocketClient.OnConnecting() data.State: " + data.State);
	}

	public override void OnConnectionChanged(ConnectionInfo data)
	{
		base.OnConnectionChanged(data);
		Debug.Log("VTSocketClient.OnConnectionChanged() data.State: " + data.State);
		if (data.State == ConnectionState.ProblemDetectedLocally)
		{
			Debug.Log(" - Reason: " + data.EndReason);
		}
		connectionState = data.State;
	}

	public override void OnConnected(ConnectionInfo data)
	{
		base.OnConnected(data);
		Debug.Log("VTSocketClient.OnConnected() data.State: " + data.State.ToString() + ", steamID: " + data.Identity.SteamId.ToString());
		Connection.SendMessage(new byte[1] { 9 }, SendType.NoNagle | SendType.Reliable);
		VTNetworkManager.instance.ReportSentData(1);
		VTNetworkManager.instance.SendClientConnectedMessageWhenSceneReady();
	}

	public override void OnDisconnected(ConnectionInfo data)
	{
		base.OnDisconnected(data);
		string text = data.State.ToString();
		if (!string.IsNullOrEmpty(overrideDisconnectionReason))
		{
			text = overrideDisconnectionReason;
			overrideDisconnectionReason = string.Empty;
		}
		Debug.Log("VTSocketClient disconnected (" + text + ")");
		VTNetworkManager.ClientDisconnected(text);
	}

	public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
	{
		base.OnMessage(data, size, messageNum, recvTime, channel);
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
		switch (headerBuffer[0])
		{
		case 1:
			VTNetworkManager.instance.ReceiveSyncMessage(bbuf, bAlloc, num);
			break;
		case 12:
		{
			byte[] buffer3 = bAlloc.Buffer;
			if (VTNetUtils.BytesToULong(buffer3[0], buffer3[1], buffer3[2], buffer3[3], buffer3[4], buffer3[5], buffer3[6], buffer3[7]) == BDSteamClient.mySteamID)
			{
				VTNetworkManager.instance.ReceiveSyncMessage(bbuf, bAlloc, num - 8, 8);
			}
			else
			{
				Debug.LogError("Client received a directed RPC message that was not directed at us!");
			}
			break;
		}
		case 0:
			pingReturnBuffer[0] = 0;
			pingReturnBuffer[1] = bAlloc.Buffer[0];
			VTNetworkManager.instance.ReportSentData(2);
			Connection.SendMessage(pingReturnBuffer, SendType.NoNagle | SendType.Reliable);
			break;
		case 3:
		{
			ParseInstantiateCommand(bAlloc.Buffer, num, out var resourcePath2, out var reqId2, out var nsIds2, out var ownerID2, out var pos2, out var rot2, out var active2);
			if (executedInstantiateRequests.Contains(reqId2))
			{
				Debug.LogFormat("Received response to instantiate request #{0} but it was already executed.  Ignoring.", reqId2);
			}
			else
			{
				executedInstantiateRequests.Add(reqId2);
				VTNetworkManager.instance.ReceiveFinalInstantiateCommand(resourcePath2, nsIds2, reqId2, isMine: true, ownerID2, pos2, rot2, active2);
			}
			break;
		}
		case 4:
		{
			ParseInstantiateCommand(bAlloc.Buffer, num, out var resourcePath, out var reqId, out var nsIds, out var ownerID, out var pos, out var rot, out var active);
			if (ownerID == SteamClient.SteamId.Value)
			{
				Debug.LogError("Received an InstantiateCommand for a request we sent out.  It should have had an InstantiateRequestResponse header.");
				if (executedInstantiateRequests.Contains(reqId))
				{
					Debug.LogFormat("Received response to instantiate request #{0} but it was already executed.  Ignoring.", reqId);
				}
				else
				{
					executedInstantiateRequests.Add(reqId);
					VTNetworkManager.instance.ReceiveFinalInstantiateCommand(resourcePath, nsIds, reqId, isMine: true, ownerID, pos, rot, active);
				}
			}
			else
			{
				VTNetworkManager.instance.ReceiveFinalInstantiateCommand(resourcePath, nsIds, reqId, isMine: false, ownerID, pos, rot, active);
			}
			break;
		}
		case 5:
		{
			VTNetworkManager.instance.ParseSceneEntityInitMessage(bAlloc.Buffer, out var sceneEntityIdx, out var entityID, out var idList);
			VTNetSceneManager.instance.InitClientSceneEntity(sceneEntityIdx, entityID, idList);
			break;
		}
		case 6:
		{
			int id = BitConverter.ToInt32(bAlloc.Buffer, 0);
			VTNetworkManager.instance.ReceiveFinalNetDestroyCommand(id);
			break;
		}
		case 7:
			ReceivePingInfos(bAlloc.Buffer, num);
			break;
		case 8:
			ReceiveTimesync(bAlloc.Buffer);
			break;
		case 9:
			ReceiveNewConnectionData(bAlloc.Buffer);
			break;
		case 10:
			ReceiveDisconnectionData(bAlloc.Buffer);
			break;
		case 13:
		{
			byte[] buffer2 = bAlloc.Buffer;
			if ((bool)VTNetworkVoice.instance)
			{
				ulong incomingID = VTNetUtils.BytesToULong(buffer2[8], buffer2[9], buffer2[10], buffer2[11], buffer2[12], buffer2[13], buffer2[14], buffer2[15]);
				VTNetworkVoice.instance.ReceiveVTNetVoiceData(incomingID, buffer2, 16, num - 16);
			}
			break;
		}
		}
		timeLastPinged = Time.time;
	}

	private void ReceiveNewConnectionData(byte[] buffer)
	{
		ulong num = VTNetUtils.BytesToULong(buffer[0], buffer[1], buffer[2], buffer[3], buffer[4], buffer[5], buffer[6], buffer[7]);
		Debug.Log("Received new connection notification from client " + num);
		if (!clientIds.Contains(num))
		{
			clientIds.Add(num);
		}
		VTNetworkManager.instance.InvokeNewClientConnected(num);
	}

	private void ReceiveDisconnectionData(byte[] buffer)
	{
		ulong num = VTNetUtils.BytesToULong(buffer[0], buffer[1], buffer[2], buffer[3], buffer[4], buffer[5], buffer[6], buffer[7]);
		Debug.Log("Received disconnection notification from client " + num);
		clientIds.Remove(num);
	}

	public int GetTotalUserCount()
	{
		return 1 + clientIds.Count;
	}

	private void ReceiveTimesync(byte[] msgBytes)
	{
		int count = receivedClientDeltas.Count;
		if (count >= 32)
		{
			return;
		}
		int num = VTNetUtils.BytesToInt(msgBytes[0], msgBytes[1], msgBytes[2], msgBytes[3]);
		int num2 = VTNetUtils.BytesToInt(msgBytes[4], msgBytes[5], msgBytes[6], msgBytes[7]);
		nInPing = (float)num2 / 1000f;
		float num3 = (float)num / 10000f;
		if (count > 3)
		{
			_ = Time.realtimeSinceStartup;
			_ = lastMsgRecvd;
			lastMsgRecvd = Time.realtimeSinceStartup;
			receivedClientDeltas.Sort();
			receivedPings.Sort();
			float num4 = receivedClientDeltas[count / 2];
			float num5 = receivedPings[count / 2];
			float num6 = 0f;
			float num7 = 0f;
			for (int i = 0; i < count; i++)
			{
				num6 += receivedClientDeltas[i];
				num7 += receivedPings[i];
			}
			num6 /= (float)count;
			num7 /= (float)count;
			float num8 = 0f;
			float num9 = 0f;
			for (int j = 0; j < count; j++)
			{
				num8 += (num6 - receivedClientDeltas[j]) * (num6 - receivedClientDeltas[j]);
				num9 += (num7 - receivedPings[j]) * (num7 - receivedPings[j]);
			}
			num8 = Mathf.Sqrt(num8) / (float)count;
			num9 = Mathf.Sqrt(num9) / (float)count;
			float num10 = num6;
			float num11 = num7;
			num6 = 0f;
			num7 = 0f;
			int num12 = 0;
			int num13 = 0;
			for (int k = 0; k < count; k++)
			{
				if (Mathf.Abs(receivedClientDeltas[k] - num4) < num8)
				{
					num6 += receivedClientDeltas[k];
					num12++;
				}
				if (Mathf.Abs(receivedPings[k] - num5) < num9)
				{
					num7 += receivedPings[k];
					num13++;
				}
			}
			num6 = ((num12 <= 0) ? num10 : (num6 / (float)num12));
			num7 = ((num13 <= 0) ? num11 : (num7 / (float)num13));
			clientDelta = num6;
			inPing = num7;
		}
		else
		{
			lastMsgRecvd = Time.realtimeSinceStartup;
		}
		if (_rts < 0f)
		{
			_rts = num3 + inPing;
			receivedTimestamp = _rts;
		}
		else
		{
			_rts = num3 + inPing;
			float num14 = VTNetworkManager.GetNetworkTimestamp() + clientDelta;
			nClientDelta = num14 - _rts;
			receivedTimestamp = num14;
			receivedClientDeltas.Add(nClientDelta);
			receivedPings.Add(nInPing);
		}
		clientTimeSynced = Time.realtimeSinceStartup;
	}

	public float GetClientSyncedNetworkTime()
	{
		if (float.IsNaN(clientDelta))
		{
			Debug.LogError("clientDelta");
		}
		if (float.IsNaN(clientTimeSynced))
		{
			Debug.LogError("clientTimeSynced");
		}
		if (float.IsNaN(receivedTimestamp))
		{
			Debug.LogError("receivedTimestamp");
		}
		return 0f - clientDelta + (Time.realtimeSinceStartup - clientTimeSynced) + receivedTimestamp;
	}

	private void ReceivePingInfos(byte[] bytes, int size)
	{
		int num = 12;
		for (int i = 0; i < size; i += num)
		{
			byte a = bytes[i];
			byte b = bytes[i + 1];
			byte c = bytes[i + 2];
			byte d = bytes[i + 3];
			byte e = bytes[i + 4];
			byte f = bytes[i + 5];
			byte g = bytes[i + 6];
			byte h = bytes[i + 7];
			byte a2 = bytes[i + 8];
			byte b2 = bytes[i + 9];
			byte c2 = bytes[i + 10];
			byte d2 = bytes[i + 11];
			ulong num2 = VTNetUtils.BytesToULong(a, b, c, d, e, f, g, h);
			int value = VTNetUtils.BytesToInt(a2, b2, c2, d2);
			if (pings.ContainsKey(num2))
			{
				pings[num2] = value;
			}
			else
			{
				pings.Add(num2, value);
			}
		}
	}

	private void ParseInstantiateCommand(byte[] bytes, int size, out string resourcePath, out int reqId, out List<int> nsIds, out ulong ownerID, out Vector3 pos, out Quaternion rot, out bool active)
	{
		reqId = BitConverter.ToInt32(bytes, 0);
		ownerID = BitConverter.ToUInt64(bytes, 4);
		int num = bytes[12];
		nsIds = new List<int>(num);
		int num2 = 13;
		for (int i = 0; i < num; i++)
		{
			nsIds.Add(BitConverter.ToInt32(bytes, num2));
			num2 += 4;
		}
		pos = VTNetUtils.NetPosToWorldPos(ref bytes, num2);
		num2 += 24;
		rot = Quaternion.Euler(VTNetUtils.BytesToVector3(ref bytes, num2));
		num2 += 12;
		active = bytes[num2] > 0;
		num2++;
		if (VTNetworkManager.verboseLogs)
		{
			Debug.Log("final received resource path size : " + (size - num2));
		}
		resourcePath = Encoding.UTF8.GetString(bytes, num2, size - num2);
	}
}

}