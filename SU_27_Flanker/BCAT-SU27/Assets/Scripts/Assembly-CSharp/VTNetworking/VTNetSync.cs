using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace VTNetworking{

public class VTNetSync : MonoBehaviour
{
	public struct DirectedRPC
	{
		public Type type;

		public string funcName;

		public int id;

		public SyncDataUp paramData;

		public ulong receiverID;
	}

	private VTNetEntity _netEntity;

	private bool gotNetEntity;

	internal bool wasRegistered;

	internal static Queue<VTNetworkManager.RPCInfo> rpcQueue = new Queue<VTNetworkManager.RPCInfo>();

	internal static Dictionary<ulong, List<DirectedRPC>> directedRPCs = new Dictionary<ulong, List<DirectedRPC>>();

	public SyncDataDownDelta.SDDBaseline sddBaseline = new SyncDataDownDelta.SDDBaseline();

	protected bool isOffline
	{
		get
		{
			if (VTNetworkManager.hasInstance)
			{
				return VTNetworkManager.instance.connectionState != VTNetworkManager.ConnectionStates.Connected;
			}
			return true;
		}
	}

	public VTNetEntity netEntity
	{
		get
		{
			if (!gotNetEntity)
			{
				gotNetEntity = true;
				_netEntity = GetComponent<VTNetEntity>();
				Transform parent = base.transform;
				while ((bool)parent.parent && !_netEntity)
				{
					parent = parent.parent;
					_netEntity = parent.GetComponent<VTNetEntity>();
				}
				if (!_netEntity)
				{
					Debug.LogErrorFormat(base.gameObject, "VTNetSync {0} does not have a VTNetEntity in parents!", base.gameObject.name);
				}
			}
			return _netEntity;
		}
	}

	public bool isMine
	{
		get
		{
			if ((bool)netEntity)
			{
				return netEntity.isMine;
			}
			return false;
		}
	}

	public bool isNetInitialized { get; private set; }

	public int id { get; set; }

	protected virtual void Awake()
	{
		if (isOffline && (!netEntity || !netEntity.isSceneEntity))
		{
			base.enabled = false;
			UnityEngine.Object.Destroy(this);
		}
	}

	internal void NetInitialize()
	{
		if (!isNetInitialized)
		{
			isNetInitialized = true;
			OnNetInitialized();
		}
	}

	protected virtual void OnNetInitialized()
	{
	}

	public virtual bool IsRPCOnly()
	{
		return false;
	}

	public virtual void UploadData(SyncDataUp d)
	{
	}

	public virtual void DownloadData(ISyncDataDown d)
	{
	}

	public void SendRPC(string funcName, params object[] parameters)
	{
		if (!VTNetworkManager.hasInstance || (VTNetworkManager.instance.netState == VTNetworkManager.NetStates.IsHost && VTNetworkManager.instance.socketHost.connectedClients.Count < 1))
		{
			return;
		}
		VTNetworkManager.RPCInfo item = default(VTNetworkManager.RPCInfo);
		item.type = GetType();
		item.id = id;
		item.funcName = funcName;
		item.paramData = new SyncDataUp(deltaCompress: false);
		item.hasReceiver = false;
		if (parameters != null)
		{
			foreach (object obj in parameters)
			{
				Type type = obj.GetType();
				if (type == typeof(float))
				{
					item.paramData.AddFloat((float)obj);
					continue;
				}
				if (type == typeof(int))
				{
					item.paramData.AddInt((int)obj);
					continue;
				}
				if (type == typeof(Vector3))
				{
					item.paramData.AddVector3((Vector3)obj);
					continue;
				}
				if (type == typeof(Quaternion))
				{
					item.paramData.AddQuaternion((Quaternion)obj);
					continue;
				}
				if (type == typeof(ulong))
				{
					VTNetUtils.ULongToInts((ulong)obj, out var a, out var b);
					item.paramData.AddInt(a);
					item.paramData.AddInt(b);
					continue;
				}
				throw new NotSupportedException("Tried to send an RPC with a parameter of invalid type " + type.Name + " (" + funcName + ")!");
			}
		}
		rpcQueue.Enqueue(item);
	}

	public void SendRPCBuffered(string funcName, params object[] parameters)
	{
		if (!VTNetworkManager.hasInstance)
		{
			return;
		}
		VTNetworkManager.RPCInfo rPCInfo = default(VTNetworkManager.RPCInfo);
		rPCInfo.buffered = true;
		rPCInfo.type = GetType();
		rPCInfo.id = id;
		rPCInfo.funcName = funcName;
		rPCInfo.hasReceiver = false;
		rPCInfo.paramData = new SyncDataUp(deltaCompress: false, reusable: true);
		if (parameters != null)
		{
			foreach (object obj in parameters)
			{
				Type type = obj.GetType();
				if (type == typeof(float))
				{
					rPCInfo.paramData.AddFloat((float)obj);
					continue;
				}
				if (type == typeof(int))
				{
					rPCInfo.paramData.AddInt((int)obj);
					continue;
				}
				if (type == typeof(Vector3))
				{
					rPCInfo.paramData.AddVector3((Vector3)obj);
					continue;
				}
				if (type == typeof(Quaternion))
				{
					rPCInfo.paramData.AddQuaternion((Quaternion)obj);
					continue;
				}
				if (type == typeof(ulong))
				{
					VTNetUtils.ULongToInts((ulong)obj, out var a, out var b);
					rPCInfo.paramData.AddInt(a);
					rPCInfo.paramData.AddInt(b);
					continue;
				}
				throw new NotSupportedException("Tried to send a buffered RPC with a parameter of type " + type.Name + " (" + funcName + ")");
			}
		}
		if (VTNetworkManager.instance.netState != VTNetworkManager.NetStates.IsHost || VTNetworkManager.instance.socketHost.connectedClients.Count > 0)
		{
			rpcQueue.Enqueue(rPCInfo);
		}
		if (VTNetworkManager.isHost)
		{
			VTNetworkManager.instance.AddBufferedRPC(rPCInfo);
		}
	}

	public void SendDirectedRPC(ulong receiverID, string funcName, params object[] parameters)
	{
		if (!VTNetworkManager.hasInstance)
		{
			return;
		}
		if (receiverID == 0L)
		{
			SendRPC(funcName, parameters);
			return;
		}
		if (receiverID == (ulong)SteamClient.SteamId)
		{
			Debug.LogError("Sent a directed RPC to a local netsync!  Just call the method locally!");
			return;
		}
		DirectedRPC item = default(DirectedRPC);
		item.type = GetType();
		item.id = id;
		item.funcName = funcName;
		item.receiverID = receiverID;
		item.paramData = new SyncDataUp(deltaCompress: false);
		if (parameters != null)
		{
			foreach (object o in parameters)
			{
				item.paramData.AddObject(o);
			}
		}
		if (directedRPCs.TryGetValue(receiverID, out var value))
		{
			value.Add(item);
			return;
		}
		value = new List<DirectedRPC>();
		value.Add(item);
		directedRPCs.Add(receiverID, value);
	}

	public RPCRequest SendRPCRequest(Type returnType, ulong receiverID, string funcName, params object[] parameters)
	{
		if (!VTNetworkManager.hasInstance)
		{
			return null;
		}
		if (receiverID == (ulong)SteamClient.SteamId)
		{
			Debug.LogError("Sent an RPC request to a local netsync!  Just call the method locally!");
			return null;
		}
		RPCRequest rPCRequest = new RPCRequest(returnType);
		VTNetworkManager.instance.RegisterRPCRequest(rPCRequest);
		VTNetworkManager.RPCInfo item = default(VTNetworkManager.RPCInfo);
		item.type = GetType();
		item.id = id;
		item.funcName = funcName;
		item.request = rPCRequest;
		item.hasReceiver = true;
		item.paramData = new SyncDataUp(deltaCompress: false);
		VTNetworkManager.ULongToInt32s(SteamClient.SteamId, out var aO, out var bO);
		VTNetworkManager.ULongToInt32s(receiverID, out var aO2, out var bO2);
		item.paramData.AddInt(aO);
		item.paramData.AddInt(bO);
		item.paramData.AddInt(aO2);
		item.paramData.AddInt(bO2);
		item.paramData.AddInt(rPCRequest.requestID);
		if (parameters != null)
		{
			foreach (object o in parameters)
			{
				item.paramData.AddObject(o);
			}
		}
		rpcQueue.Enqueue(item);
		return rPCRequest;
	}
}

}