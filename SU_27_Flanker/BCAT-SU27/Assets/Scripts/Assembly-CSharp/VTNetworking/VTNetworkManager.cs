using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FlatBuffers;
using K4os.Compression.LZ4;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using VTNetworking.FlatBuffers;

namespace VTNetworking{

public class VTNetworkManager : MonoBehaviour
{
	public enum SendModes
	{
		Reliable_NoNagle = 9,
		Reliable = 8
	}

	public delegate void DisconnectionEvent(string reason);

	private class NSIDServer
	{
		private int next;

		public int GetNext()
		{
			return next++;
		}
	}

	public enum NetStates
	{
		None,
		IsHost,
		IsClient
	}

	public enum ConnectionStates
	{
		None,
		Connecting,
		Connected
	}

	public enum ConnectionClosingReasons
	{
		Host_Stopped
	}

	private class ThreadedSendDataStatus
	{
		public FlatBufferBuilder fbb;

		public bool done;
	}

	public struct RPCInfo
	{
		public Type type;

		public string funcName;

		public int id;

		public SyncDataUp paramData;

		public RPCRequest request;

		public bool hasReceiver;

		public bool buffered;
	}

	private struct RPCReturn
	{
		public ulong requesterID;

		public int requestID;

		public object returnObj;
	}

	private class VTMethodInfo
	{
		public MethodInfo method;

		public Type[] paramTypes;
	}

	public struct RPCSenderInfo
	{
		public bool exists;

		public ulong senderId;
	}

	public class NetInstantiateRequest
	{
		public bool isReady;

		public int id;

		public GameObject obj;
	}

	private class AsyncExceptionThrower : MonoBehaviour
	{
		public void Send(string msg)
		{
			StartCoroutine(SendMsg(msg));
			UnityEngine.Object.Destroy(base.gameObject, 1f);
		}

		private IEnumerator SendMsg(string msg)
		{
			yield return null;
			throw new Exception(msg);
		}
	}

	private class RPCDict
	{
		public Dictionary<int, MethodInfo> indexToMethod;

		public Dictionary<string, int> methodNameToIndex;
	}

	private List<VTNetSync> myNetSyncs = new List<VTNetSync>();

	private Dictionary<int, SyncDataUp> upSyncs = new Dictionary<int, SyncDataUp>();

	private List<VTNetSync> remoteNetSyncs = new List<VTNetSync>();

	private Dictionary<int, VTNetSync> remoteSyncDict = new Dictionary<int, VTNetSync>();

	private Dictionary<int, VTNetSync> allSyncsDict = new Dictionary<int, VTNetSync>();

	private Dictionary<int, VTNetEntity> netEntities = new Dictionary<int, VTNetEntity>();

	public float clientSendInterval = 0.03f;

	public float hostSendInterval = 0.03f;

	public bool debugRPCs;

	public static bool verboseLogs = false;

	public SendModes sendMode = SendModes.Reliable_NoNagle;

	private static ulong mySteamID;

	private int finalMessageSize;

	private FlatBufferBuilder fbb;

	private Offset<SyncItem>[] itemOffsets = new Offset<SyncItem>[0];

	private List<RPCInfo> rpcInfos = new List<RPCInfo>();

	private Offset<Rpc>[] rpcs = new Offset<Rpc>[0];

	private NSIDServer newIDServer = new NSIDServer();

	private ByteArrayAllocator outByteAlloc;

	private static VTNetworkManager _instance;

	public bool analytics;

	private int _dirtyUpstreamRate;

	private int _dirtyDownstreamRate;

	private static bool wasEverActivated = false;

	private float timeoutMax = 10f;

	private const int PW_VIRTUAL_PORT = 1;

	private VTPasswordHost pwHost;

	private List<SteamId> socketPasswordWhitelist = new List<SteamId>();

	private VTPasswordClient pwClient;

	private Result sendMessageResult = Result.OK;

	private Coroutine clientRoutine;

	private Coroutine messageReceiveRoutine;

	private Coroutine analyticsRoutine;

	private static int maxDataReceived = 0;

	private static int maxDataSent = 0;

	private Result[] hostSendMessageResults = new Result[64];

	private byte[] compressionTgt = new byte[1];

	private Coroutine hostRoutine;

	private float lastNetworkRoutineUpdateTime;

	private ThreadedSendDataStatus statusObj = new ThreadedSendDataStatus();

	private float lastHostReceiveTime;

	private int maxRpcQueueCount;

	private int maxRpcReturnQueueCount;

	private static bool hasSentTimestampDebug = false;

	private Offset<SyncItem>[] dummyItemOffets = new Offset<SyncItem>[0];

	private static DateTime networkTimeStart;

	private static float realtimeClientTimeSynced;

	private static float _receivedNetworkTime;

	private List<RPCInfo> bufferedRPCs = new List<RPCInfo>();

	private float lastSendTime;

	private int startIdx;

	private int syncCount = 1;

	private int maxRPCsReceived;

	private float minDecompressionRatio = 1f;

	private int maxDecompressedSize;

	private byte[] decompressTgt = new byte[24576];

	private Dictionary<int, RPCRequest> rpcRequestsDict = new Dictionary<int, RPCRequest>();

	private Queue<RPCReturn> rpcReturnQueue = new Queue<RPCReturn>();

	private byte[] returnIDbytesBuffer = new byte[8];

	private Dictionary<Type, Dictionary<string, VTMethodInfo>> rpcDict = new Dictionary<Type, Dictionary<string, VTMethodInfo>>();

	public static RPCSenderInfo currentRPCInfo;

	private Dictionary<int, NetInstantiateRequest> netInstantiateRequests = new Dictionary<int, NetInstantiateRequest>();

	private static Dictionary<string, GameObject> overriddenResources = new Dictionary<string, GameObject>();

	private Dictionary<int, VTNetEntity> unregisteredEntities = new Dictionary<int, VTNetEntity>();

	private static Dictionary<Type, RPCDict> rpcDicts = new Dictionary<Type, RPCDict>();

	private byte[] voiceOutBuffer;

	private SendType voiceSendType = SendType.NoNagle | SendType.NoDelay;

	private SendType SYNC_SENDTYPE => (SendType)sendMode;

	public static float CurrentSendInterval
	{
		get
		{
			if (!isHost)
			{
				return instance.clientSendInterval;
			}
			return instance.hostSendInterval;
		}
	}

	public VTSocketClient socketClient { get; private set; }

	public ConnectionState clientConnectionState
	{
		get
		{
			if (socketClient != null)
			{
				return socketClient.connectionState;
			}
			return ConnectionState.None;
		}
	}

	public VTSocketHost socketHost { get; private set; }

	public VTSocketHost passwordHost { get; private set; }

	public static bool isHost => instance.netState == NetStates.IsHost;

	public NetStates netState { get; private set; }

	public ConnectionStates connectionState { get; private set; }

	public static bool hasInstance => _instance != null;

	public static VTNetworkManager instance
	{
		get
		{
			if (!_instance)
			{
				if (!isActivated && wasEverActivated)
				{
					return null;
				}
				GameObject obj = new GameObject("VTNetworkManager");
				obj.AddComponent<VTNetworkManager>();
				UnityEngine.Object.DontDestroyOnLoad(obj);
			}
			return _instance;
		}
		private set
		{
			_instance = value;
		}
	}

	public int upstreamRate { get; private set; }

	public int downstreamRate { get; private set; }

	public Result clientSendMessageResult => sendMessageResult;

	public static bool isActivated { get; private set; }

	public static float networkTime
	{
		get
		{
			if (isHost)
			{
				return (float)(DateTime.Now - networkTimeStart).TotalSeconds;
			}
			if (!instance || instance.socketClient == null)
			{
				return Time.realtimeSinceStartup;
			}
			return instance.socketClient.GetClientSyncedNetworkTime();
		}
	}

	public float debug_syncTimeAdjustDelta { get; private set; }

	public static event DisconnectionEvent OnDisconnected;

	public event Action OnUpdatedAnalytics;

	public event Action<SteamId> OnNewClientConnected;

	internal static void ClientDisconnected(string reason)
	{
		if (instance.netState == NetStates.IsClient)
		{
			StopClient();
		}
		VTNetworkManager.OnDisconnected?.Invoke(reason);
	}

	public Result GetHostToClientMessageResult(int clientIdx)
	{
		return hostSendMessageResults[clientIdx];
	}

	private void Awake()
	{
		Debug.Log("VTNetworkManager Awake");
		instance = this;
		isActivated = true;
		wasEverActivated = true;
		outByteAlloc = new ByteArrayAllocator(new byte[1024]);
		fbb = new FlatBufferBuilder(new ByteBuffer(outByteAlloc, 0));
		Dispatch.OnException = (Action<Exception>)Delegate.Combine(Dispatch.OnException, new Action<Exception>(SteamClientCallbackException));
		mySteamID = SteamClient.SteamId;
	}

	private void Update()
	{
		if (netState == NetStates.IsClient && connectionState == ConnectionStates.Connected)
		{
			if (Time.time - socketClient.timeLastPinged > timeoutMax)
			{
				Debug.LogError($"Ending socket client since we have not received pings from the host for more than {timeoutMax} seconds.");
				socketClient.overrideDisconnectionReason = "Network routine crashed. Please report the bug.";
				StopClient();
			}
			else if (Time.time - lastNetworkRoutineUpdateTime > timeoutMax)
			{
				Debug.LogError("Ending socket client since the client routine has likely crashed.");
				StopClient();
			}
		}
		else if (netState == NetStates.IsHost && connectionState == ConnectionStates.Connected)
		{
			if (Time.time - lastNetworkRoutineUpdateTime > timeoutMax)
			{
				Debug.LogError("Ending socket host since the host routine has likely crashed.");
				StopHost(sendDisconnectedEvent: true);
			}
			else if (Time.time - lastHostReceiveTime > timeoutMax)
			{
				Debug.LogError("Ending socket host since the host RECEIVE routine has likely crashed.");
				StopHost(sendDisconnectedEvent: true);
			}
		}
	}

	private void SteamClientCallbackException(Exception e)
	{
		Debug.LogError("SteamClient Callback Exception: " + e);
	}

	public void RegisterMyNetSync(VTNetSync ns)
	{
		if (netState == NetStates.None)
		{
			if (verboseLogs)
			{
				Debug.LogError("Tried to register a local netSync but we're offline!");
			}
		}
		else if (!ns.wasRegistered)
		{
			ns.wasRegistered = true;
			if (verboseLogs)
			{
				Debug.LogFormat("VTNetworkManager: Registering local netsync. ID: {0}, Name: {1}", ns.id, ns.name + ":" + ns.GetType());
			}
			if (!ns.IsRPCOnly())
			{
				myNetSyncs.Add(ns);
			}
			allSyncsDict.Add(ns.id, ns);
		}
	}

	public void UnRegisterMyNetSync(VTNetSync ns)
	{
		if (netState == NetStates.None)
		{
			return;
		}
		if (!ns)
		{
			if (verboseLogs)
			{
				Debug.Log("VTNetworkManager: Tried to unregister a local netsync but it was null!");
			}
		}
		else
		{
			myNetSyncs.Remove(ns);
			allSyncsDict.Remove(ns.id);
		}
	}

	public void RegisterRemoteNetSync(VTNetSync ns)
	{
		if (netState == NetStates.None)
		{
			if (verboseLogs)
			{
				Debug.LogError("Tried to register a remote netSync but we're offline!");
			}
		}
		else if (!ns.wasRegistered)
		{
			ns.wasRegistered = true;
			if (verboseLogs)
			{
				Debug.LogFormat("VTNetworkManager: Registering remote netsync. ID: {0}, Name: {1}", ns.id, ns.name);
			}
			if (!ns.IsRPCOnly())
			{
				remoteNetSyncs.Add(ns);
			}
			remoteSyncDict.Add(ns.id, ns);
			allSyncsDict.Add(ns.id, ns);
		}
	}

	public void UnRegisterRemoteNetSync(VTNetSync ns)
	{
		if (netState == NetStates.None)
		{
			return;
		}
		if (!ns)
		{
			if (verboseLogs)
			{
				Debug.Log("VTNetworkManager: Tried to unregister a remote netsync but it was null!");
			}
		}
		else
		{
			remoteNetSyncs.Remove(ns);
			remoteSyncDict.Remove(ns.id);
			allSyncsDict.Remove(ns.id);
		}
	}

	public static void CreateHost()
	{
		if (instance.netState != 0)
		{
			Debug.LogError("VTNetworkManager tried to create a host but it's not in a valid state: " + instance.netState);
			return;
		}
		ClearSyncCollections();
		instance.netState = NetStates.IsHost;
		Debug.Log("Creating socket host.");
		instance.socketHost = SteamNetworkingSockets.CreateRelaySocket<VTSocketHost>();
		instance.StartHostRoutine();
	}

	public static void CreatePasswordHost(string password)
	{
		instance.pwHost = SteamNetworkingSockets.CreateRelaySocket<VTPasswordHost>(1);
		instance.pwHost.hostPassword = password;
		instance.pwHost.OnPasswordValid += PwHost_OnPasswordValid;
		instance.pwHost.OnPasswordInvalid += PwHost_OnPasswordInvalid;
		instance.StartCoroutine(instance.PasswordHostRoutine());
	}

	public static void SetHostPassword(string pw)
	{
		if (hasInstance)
		{
			if (instance.pwHost != null)
			{
				instance.pwHost.hostPassword = pw;
			}
			else
			{
				CreatePasswordHost(pw);
			}
		}
	}

	private static void PwHost_OnPasswordInvalid(SteamId obj)
	{
		if (hasInstance)
		{
			instance.StartCoroutine(instance.DelayedClosePWConnection(obj));
		}
	}

	private static void PwHost_OnPasswordValid(SteamId obj)
	{
		if (hasInstance)
		{
			instance.socketPasswordWhitelist.Add(obj);
			instance.StartCoroutine(instance.DelayedClosePWConnection(obj));
		}
	}

	public static bool IsConnectingUserValidated(SteamId id)
	{
		if (hasInstance)
		{
			return instance.socketPasswordWhitelist.Contains(id);
		}
		return false;
	}

	private IEnumerator DelayedClosePWConnection(SteamId id)
	{
		yield return new WaitForSeconds(0.3f);
		if (pwHost != null)
		{
			pwHost.CloseConnectionTo(id);
		}
	}

	private IEnumerator PasswordHostRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(0.25f);
		while (hasInstance && instance.pwHost != null)
		{
			pwHost.Receive();
			yield return wait;
		}
	}

	public static VTPasswordAttempt TrySocketPassword(SteamId hostId, string pw)
	{
		VTPasswordAttempt vTPasswordAttempt = new VTPasswordAttempt();
		instance.StartCoroutine(instance.VTPasswordAttemptRoutine(hostId, pw, vTPasswordAttempt));
		return vTPasswordAttempt;
	}

	private IEnumerator VTPasswordAttemptRoutine(SteamId hostId, string pw, VTPasswordAttempt attempt)
	{
		pwClient = SteamNetworkingSockets.ConnectRelay<VTPasswordClient>(hostId, 1);
		while (!pwClient.Connected)
		{
			yield return null;
		}
		pwClient.TryPassword(pw, attempt);
		WaitForSeconds wait = new WaitForSeconds(0.03f);
		while (attempt.status == VTPasswordAttempt.Statuses.Pending)
		{
			pwClient.Receive();
			yield return wait;
		}
		pwClient.Close();
		pwClient = null;
	}

	public void SendClientConnectedMessageWhenSceneReady()
	{
		StartCoroutine(ClientSceneRdyRoutine());
	}

	private IEnumerator ClientSceneRdyRoutine()
	{
		while (!VTNetSceneManager.instance || !VTNetSceneManager.instance.sceneEntitiesReady)
		{
			yield return null;
		}
		ReportSentData(1);
		socketClient.Connection.SendMessage(new byte[1] { 11 }, SendType.NoNagle | SendType.Reliable);
	}

	public void SendBaselinesToNewConnection(Connection c, SteamId user)
	{
		Action<FlatBufferBuilder> onBuilderFinish = delegate(FlatBufferBuilder fbb)
		{
			byte[] buffer = outByteAlloc.Buffer;
			int num = 1 + fbb.DataBuffer.Length - fbb.DataBuffer.Position;
			int num2 = fbb.DataBuffer.Position - 1;
			if (true)
			{
				int num3 = LZ4Codec.MaximumOutputSize(num) + 1;
				if (compressionTgt.Length < num3)
				{
					compressionTgt = new byte[num3];
				}
				int num4 = LZ4Codec.Encode(buffer, num2 + 1, num - 1, compressionTgt, 1, num3 - 1);
				if (num4 < 0)
				{
					Debug.LogError("Failed to LZ4 compress sync message.");
					return;
				}
				compressionTgt[0] = buffer[num2];
				buffer = compressionTgt;
				num2 = 0;
				num = num4 + 1;
			}
			ReportSentData(num);
			c.SendMessage(buffer, num2, num, SYNC_SENDTYPE);
		};
		BuildResync(myNetSyncs, bufferedRPCs, onBuilderFinish);
		BuildResync(remoteNetSyncs, null, onBuilderFinish);
		this.OnNewClientConnected?.Invoke(user);
	}

	internal void InvokeNewClientConnected(SteamId user)
	{
		this.OnNewClientConnected?.Invoke(user);
	}

	public static void StopHost(bool sendDisconnectedEvent = false)
	{
		if (instance.netState != NetStates.IsHost)
		{
			Debug.LogError("VTNetworkManager tried to stop hosting but it's not in a valid state: " + instance.netState);
			return;
		}
		Debug.Log("Stopping socket host.");
		instance.StopHostRoutines();
		foreach (Connection item in instance.socketHost.Connected)
		{
			try
			{
				item.Close(linger: false, 0, "Host has stopped hosting.");
			}
			catch (Exception ex)
			{
				Debug.LogError("Error while trying to close a connection: " + ex);
			}
		}
		if (SteamClient.IsValid)
		{
			instance.socketHost.Close();
		}
		instance.socketHost.Dispose();
		instance.socketHost = null;
		instance.netState = NetStates.None;
		instance.connectionState = ConnectionStates.None;
		ClearSyncCollections();
		if (sendDisconnectedEvent)
		{
			VTNetworkManager.OnDisconnected?.Invoke("Host routine crashed. Please report bug.");
		}
	}

	public static bool CreateClient(SteamId hostId)
	{
		if (instance.netState != 0)
		{
			Debug.LogError("VTNetworkManager tried to start as a client but it's not in a valid state: " + instance.netState);
			return false;
		}
		ClearSyncCollections();
		Debug.Log("Creating socket client. Connecting to host: " + new Friend(hostId).ToString());
		instance.netState = NetStates.IsClient;
		instance.connectionState = ConnectionStates.Connecting;
		try
		{
			instance.socketClient = SteamNetworkingSockets.ConnectRelay<VTSocketClient>(hostId);
		}
		catch (Exception ex)
		{
			Debug.LogErrorFormat("Exception when trying to create a socket relay client: \n{0}", ex);
			StopClient();
			return false;
		}
		instance.StartClientRoutine();
		return true;
	}

	public static void StopClient()
	{
		if (instance.netState != NetStates.IsClient)
		{
			Debug.LogError("VTNetworkManager tried to stop as a client but it's not in a valid state: " + instance.netState);
			return;
		}
		Debug.Log("Stopping socket client.");
		instance.StopClientRoutines();
		if (instance.socketClient != null)
		{
			if (SteamClient.IsValid)
			{
				instance.socketClient.Close();
			}
			instance.socketClient.Dispose();
			instance.socketClient = null;
		}
		instance.netState = NetStates.None;
		instance.connectionState = ConnectionStates.None;
		ClearSyncCollections();
	}

	private static void ClearSyncCollections()
	{
		Debug.Log("VTNetworkManager: Clearing all sync collections.");
		if (hasInstance)
		{
			instance.myNetSyncs.Clear();
			instance.upSyncs.Clear();
			instance.remoteNetSyncs.Clear();
			instance.remoteSyncDict.Clear();
			instance.netEntities.Clear();
			instance.unregisteredEntities.Clear();
			instance.allSyncsDict.Clear();
			instance.rpcRequestsDict.Clear();
			instance.rpcReturnQueue.Clear();
			instance.bufferedRPCs.Clear();
			instance.rpcInfos.Clear();
			instance.netInstantiateRequests.Clear();
			if (instance.fbb != null)
			{
				instance.fbb.Clear();
			}
			instance.OnNewClientConnected = null;
		}
		VTNetSync.rpcQueue.Clear();
		VTNetSync.directedRPCs.Clear();
	}

	private void StartClientRoutine()
	{
		clientRoutine = StartCoroutine(ClientRoutine());
	}

	private void StopClientRoutines()
	{
		if (clientRoutine != null)
		{
			StopCoroutine(clientRoutine);
		}
		if (messageReceiveRoutine != null)
		{
			StopCoroutine(messageReceiveRoutine);
		}
		if (analyticsRoutine != null)
		{
			StopCoroutine(analyticsRoutine);
		}
	}

	private IEnumerator ClientRoutine()
	{
		Debug.Log("Socket client connecting...");
		while (netState == NetStates.IsClient && socketClient.Connecting)
		{
			socketClient.timeLastPinged = Time.time;
			yield return null;
		}
		if (socketClient.Connected)
		{
			Debug.Log("Socket client successfully connected!");
			connectionState = ConnectionStates.Connected;
			lastNetworkRoutineUpdateTime = Time.time;
			yield return null;
			Action<FlatBufferBuilder> SendData = delegate(FlatBufferBuilder fbb)
			{
				byte[] buffer = outByteAlloc.Buffer;
				int num = 1 + fbb.DataBuffer.Length - fbb.DataBuffer.Position;
				int num2 = fbb.DataBuffer.Position - 1;
				if (true)
				{
					int num3 = LZ4Codec.MaximumOutputSize(num) + 1;
					if (compressionTgt.Length < num3)
					{
						compressionTgt = new byte[num3];
					}
					int num4 = LZ4Codec.Encode(buffer, num2 + 1, num - 1, compressionTgt, 1, num3 - 1);
					if (num4 < 0)
					{
						Debug.LogError("Failed to LZ4 compress sync message.");
						return;
					}
					compressionTgt[0] = buffer[num2];
					buffer = compressionTgt;
					num2 = 0;
					num = num4 + 1;
				}
				ReportSentData(num);
				sendMessageResult = socketClient.Connection.SendMessage(buffer, num2, num, SYNC_SENDTYPE);
			};
			messageReceiveRoutine = StartCoroutine(ClientReceiveRoutine());
			analyticsRoutine = StartCoroutine(AnalyticsRoutine());
			WaitForSeconds wait = new WaitForSeconds(clientSendInterval);
			while (netState == NetStates.IsClient)
			{
				BuildSyncMessage(SendData);
				SendDirectedRPCs();
				lastNetworkRoutineUpdateTime = Time.time;
				yield return wait;
			}
		}
		else
		{
			Debug.Log("Socket client failed to connect...");
			StopClient();
		}
	}

	private IEnumerator ClientReceiveRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(clientSendInterval);
		while (netState == NetStates.IsClient)
		{
			socketClient.Receive();
			yield return wait;
		}
	}

	private IEnumerator AnalyticsRoutine()
	{
		float interval = 0.2f;
		WaitForSeconds wait = new WaitForSeconds(interval);
		while (netState != 0)
		{
			upstreamRate = Mathf.RoundToInt((float)_dirtyUpstreamRate / interval);
			_dirtyUpstreamRate = 0;
			downstreamRate = Mathf.RoundToInt((float)_dirtyDownstreamRate / interval);
			_dirtyDownstreamRate = 0;
			if (this.OnUpdatedAnalytics != null)
			{
				this.OnUpdatedAnalytics();
			}
			yield return wait;
		}
	}

	public void ReportReceivedData(int size)
	{
		_dirtyDownstreamRate += size;
		if (size > maxDataReceived)
		{
			maxDataReceived = size;
			Debug.LogFormat("Max data received: {0}kb", (float)size / 1024f);
		}
	}

	public void ReportSentData(int size)
	{
		_dirtyUpstreamRate += size;
		if (size > maxDataSent)
		{
			maxDataSent = size;
			Debug.LogFormat("Max data sent: {0}kb", (float)size / 1024f);
		}
	}

	private void StartHostRoutine()
	{
		hostRoutine = StartCoroutine(HostRoutine());
	}

	private void StopHostRoutines()
	{
		if (hostRoutine != null)
		{
			StopCoroutine(hostRoutine);
		}
		if (messageReceiveRoutine != null)
		{
			StopCoroutine(messageReceiveRoutine);
		}
		if (analyticsRoutine != null)
		{
			StopCoroutine(analyticsRoutine);
		}
	}

	private IEnumerator HostRoutine()
	{
		float lastPingUpdateTime = 0f;
		networkTimeStart = DateTime.Now;
		connectionState = ConnectionStates.Connected;
		lastNetworkRoutineUpdateTime = Time.time;
		lastHostReceiveTime = Time.time;
		Action<FlatBufferBuilder> SendData = delegate(FlatBufferBuilder fbb)
		{
			int num = 1 + fbb.DataBuffer.Length - fbb.DataBuffer.Position;
			byte[] buffer = outByteAlloc.Buffer;
			int num2 = fbb.DataBuffer.Position - 1;
			if (true)
			{
				int num3 = LZ4Codec.MaximumOutputSize(num) + 1;
				if (compressionTgt.Length < num3)
				{
					compressionTgt = new byte[num3];
				}
				int num4 = LZ4Codec.Encode(buffer, num2 + 1, num - 1, compressionTgt, 1, num3 - 1);
				if (num4 < 0)
				{
					Debug.LogError("Failed to LZ4 compress sync message.");
					return;
				}
				compressionTgt[0] = buffer[num2];
				buffer = compressionTgt;
				num2 = 0;
				num = num4 + 1;
			}
			for (int i = 0; i < socketHost.connectedClients.Count; i++)
			{
				VTSocketHost.ConnectedClient connectedClient = socketHost.connectedClients[i];
				ReportSentData(num);
				hostSendMessageResults[i] = connectedClient.connection.SendMessage(buffer, num2, num, SYNC_SENDTYPE);
			}
		};
		messageReceiveRoutine = StartCoroutine(HostReceiveRoutine());
		analyticsRoutine = StartCoroutine(AnalyticsRoutine());
		WaitForSeconds wait = new WaitForSeconds(hostSendInterval);
		while (netState == NetStates.IsHost)
		{
			BuildSyncMessage(SendData);
			SendDirectedRPCs();
			if (Time.time - lastPingUpdateTime > 1f)
			{
				socketHost.SendPingTestsToClients();
				socketHost.SendPingInfosToClients();
				socketHost.SendTimeSyncToClients();
				lastPingUpdateTime = Time.time;
			}
			lastNetworkRoutineUpdateTime = Time.time;
			yield return wait;
		}
	}

	private void M_SendDataHost(object s)
	{
		ThreadedSendDataStatus threadedSendDataStatus = (ThreadedSendDataStatus)s;
		FlatBufferBuilder flatBufferBuilder = threadedSendDataStatus.fbb;
		int num = 1 + flatBufferBuilder.DataBuffer.Length - flatBufferBuilder.DataBuffer.Position;
		byte[] buffer = outByteAlloc.Buffer;
		int num2 = flatBufferBuilder.DataBuffer.Position - 1;
		if (true)
		{
			int num3 = LZ4Codec.MaximumOutputSize(num) + 1;
			if (compressionTgt.Length < num3)
			{
				compressionTgt = new byte[num3];
			}
			int num4 = LZ4Codec.Encode(buffer, num2 + 1, num - 1, compressionTgt, 1, num3 - 1);
			if (num4 < 0)
			{
				Debug.LogError("Failed to LZ4 compress sync message.");
				return;
			}
			compressionTgt[0] = buffer[num2];
			buffer = compressionTgt;
			num2 = 0;
			num = num4 + 1;
		}
		for (int i = 0; i < socketHost.connectedClients.Count; i++)
		{
			VTSocketHost.ConnectedClient connectedClient = socketHost.connectedClients[i];
			ReportSentData(num);
			hostSendMessageResults[i] = connectedClient.connection.SendMessage(buffer, num2, num, SYNC_SENDTYPE);
		}
		threadedSendDataStatus.done = true;
		threadedSendDataStatus.fbb = null;
	}

	private IEnumerator HostReceiveRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(clientSendInterval);
		while (netState == NetStates.IsHost)
		{
			lastHostReceiveTime = Time.time;
			socketHost.Receive();
			yield return wait;
		}
	}

	public void ClosePasswordHost()
	{
		if (passwordHost != null)
		{
			passwordHost.Close();
			passwordHost = null;
		}
		socketPasswordWhitelist.Clear();
	}

	private void OnDestroy()
	{
		Debug.Log("VTNetworkManager OnDestroy");
		if (netState == NetStates.IsClient)
		{
			StopClient();
		}
		else if (netState == NetStates.IsHost)
		{
			StopHost();
		}
		ClosePasswordHost();
		isActivated = false;
		Dispatch.OnException = (Action<Exception>)Delegate.Combine(Dispatch.OnException, new Action<Exception>(SteamClientCallbackException));
		SteamServer.Shutdown();
		SteamClient.Shutdown();
	}

	private void BuildSyncMessage(Action<FlatBufferBuilder> OnBuilderFinish)
	{
		List<VTNetSync> list = myNetSyncs;
		VectorOffset vectorOffset = default(VectorOffset);
		syncCount = list.Count;
		startIdx = 0;
		syncCount = Mathf.Min(syncCount, list.Count);
		if (itemOffsets.Length != list.Count)
		{
			itemOffsets = new Offset<SyncItem>[syncCount];
		}
		int num = startIdx;
		for (int i = 0; i < syncCount; i++)
		{
			VTNetSync vTNetSync = list[num];
			if (!upSyncs.TryGetValue(vTNetSync.id, out var value))
			{
				value = new SyncDataUp(deltaCompress: true);
				upSyncs.Add(vTNetSync.id, value);
			}
			vTNetSync.UploadData(value);
			if (netState == NetStates.IsHost)
			{
				value.CopyToLocalBaseline(vTNetSync.sddBaseline);
			}
			if (value.HasDelta())
			{
				Offset<SyncItem> offset = value.CreateSyncItem(fbb, vTNetSync.id);
				itemOffsets[i] = offset;
			}
			else
			{
				SyncItem.StartSyncItem(fbb);
				Offset<SyncItem> offset2 = SyncItem.EndSyncItem(fbb);
				itemOffsets[i] = offset2;
				value.ClearUnused();
			}
			num = (num + 1) % list.Count;
		}
		startIdx = num;
		bool flag = false;
		if (VTNetSync.rpcQueue.Count > maxRpcQueueCount)
		{
			maxRpcQueueCount = VTNetSync.rpcQueue.Count;
			Debug.LogFormat("New max RPC queue count: {0}", maxRpcQueueCount);
			flag = true;
		}
		if (debugRPCs && VTNetSync.rpcQueue.Count > 0)
		{
			Debug.Log("== OUTgoing RPCS: ");
		}
		while (VTNetSync.rpcQueue.Count > 0)
		{
			RPCInfo item = VTNetSync.rpcQueue.Dequeue();
			rpcInfos.Add(item);
			if ((flag && verboseLogs) || debugRPCs)
			{
				Debug.Log(item.funcName);
			}
		}
		if (rpcReturnQueue.Count > maxRpcReturnQueueCount)
		{
			maxRpcReturnQueueCount = rpcReturnQueue.Count;
			Debug.LogFormat("New max RPC Return queue count: {0}", maxRpcReturnQueueCount);
		}
		while (rpcReturnQueue.Count > 0)
		{
			RPCReturn rPCReturn = rpcReturnQueue.Dequeue();
			RPCInfo item2 = default(RPCInfo);
			item2.funcName = string.Empty;
			item2.id = -2;
			item2.paramData = new SyncDataUp(deltaCompress: false);
			ULongToInt32s(rPCReturn.requesterID, out var aO, out var bO);
			item2.paramData.AddInt(aO);
			item2.paramData.AddInt(bO);
			item2.paramData.AddInt(rPCReturn.requestID);
			item2.paramData.AddObject(rPCReturn.returnObj);
			rpcInfos.Add(item2);
		}
		vectorOffset = SyncPackage.CreateItemsVector(fbb, itemOffsets);
		bool flag2 = false;
		VectorOffset rpcsOffset = default(VectorOffset);
		if (rpcInfos.Count > 0)
		{
			flag2 = true;
			if (rpcs.Length != rpcInfos.Count)
			{
				rpcs = new Offset<Rpc>[rpcInfos.Count];
			}
			for (int j = 0; j < rpcInfos.Count; j++)
			{
				RPCInfo rPCInfo = rpcInfos[j];
				Offset<SyncItem> paramsOffset = rPCInfo.paramData.CreateSyncItem(fbb, rPCInfo.id, forceAll: true);
				int num2;
				if (rPCInfo.id == -2)
				{
					num2 = -1;
				}
				else
				{
					num2 = GetRPCIndex(rPCInfo.type, rPCInfo.funcName);
					if (rPCInfo.buffered)
					{
						num2 |= int.MinValue;
					}
				}
				rpcs[j] = Rpc.CreateRpc(fbb, num2, paramsOffset);
			}
			rpcsOffset = SyncPackage.CreateRpcsVector(fbb, rpcs);
			rpcInfos.Clear();
		}
		SyncPackage.StartSyncPackage(fbb);
		float networkTimestamp = GetNetworkTimestamp();
		if (!hasSentTimestampDebug && float.IsNaN(networkTimestamp))
		{
			VTNetUtils.SendExceptionReport("float.IsNaN(timestamp) being sent!!");
			hasSentTimestampDebug = true;
		}
		SyncPackage.AddTimestamp(fbb, GetNetworkTimestamp());
		SyncPackage.AddOwnerID(fbb, BDSteamClient.mySteamID);
		SyncPackage.AddItems(fbb, vectorOffset);
		if (flag2)
		{
			SyncPackage.AddRpcs(fbb, rpcsOffset);
		}
		Offset<SyncPackage> offset3 = SyncPackage.EndSyncPackage(fbb);
		fbb.Finish(offset3.Value);
		if (fbb.DataBuffer.Position == 0)
		{
			fbb.DataBuffer.GrowFront(fbb.DataBuffer.Length + 1);
			fbb.DataBuffer.Position++;
		}
		outByteAlloc.Buffer[fbb.DataBuffer.Position - 1] = 1;
		OnBuilderFinish?.Invoke(fbb);
		fbb.Clear();
	}

	private void SendDirectedRPCs()
	{
		if (VTNetSync.directedRPCs.Count <= 0)
		{
			return;
		}
		foreach (KeyValuePair<ulong, List<VTNetSync.DirectedRPC>> directedRPC in VTNetSync.directedRPCs)
		{
			List<VTNetSync.DirectedRPC> value = directedRPC.Value;
			if (value.Count > 0)
			{
				ulong key = directedRPC.Key;
				BuildDirectedRPCMessage(key, value);
				value.Clear();
			}
		}
	}

	private void SendDirectedData(ulong receiver, FlatBufferBuilder fbb)
	{
		byte[] buffer = outByteAlloc.Buffer;
		int num = 9 + fbb.DataBuffer.Length - fbb.DataBuffer.Position;
		int num2 = fbb.DataBuffer.Position - 9;
		if (true)
		{
			int num3 = LZ4Codec.MaximumOutputSize(num) + 9;
			if (compressionTgt.Length < num3)
			{
				compressionTgt = new byte[num3];
			}
			int num4 = LZ4Codec.Encode(buffer, num2 + 9, num - 9, compressionTgt, 9, num3 - 9);
			if (num4 < 0)
			{
				Debug.LogError("Failed to LZ4 compress directedRPC message.");
				return;
			}
			for (int i = 0; i < 9; i++)
			{
				compressionTgt[i] = buffer[num2 + i];
			}
			buffer = compressionTgt;
			num2 = 0;
			num = num4 + 9;
		}
		if (isHost)
		{
			if (socketHost.connectedClientsDict.TryGetValue(receiver, out var value))
			{
				ReportSentData(num);
				sendMessageResult = value.connection.SendMessage(buffer, num2, num, SYNC_SENDTYPE);
			}
		}
		else
		{
			ReportSentData(num);
			sendMessageResult = socketClient.Connection.SendMessage(buffer, num2, num, SYNC_SENDTYPE);
		}
	}

	private void BuildDirectedRPCMessage(ulong receiver, List<VTNetSync.DirectedRPC> dRpcs)
	{
		VectorOffset vectorOffset = default(VectorOffset);
		vectorOffset = SyncPackage.CreateItemsVector(fbb, dummyItemOffets);
		bool flag = false;
		VectorOffset rpcsOffset = default(VectorOffset);
		if (dRpcs.Count > 0)
		{
			flag = true;
			if (rpcs.Length != dRpcs.Count)
			{
				rpcs = new Offset<Rpc>[dRpcs.Count];
			}
			for (int i = 0; i < dRpcs.Count; i++)
			{
				VTNetSync.DirectedRPC directedRPC = dRpcs[i];
				Offset<SyncItem> paramsOffset = directedRPC.paramData.CreateSyncItem(fbb, directedRPC.id, forceAll: true);
				int funcId = ((directedRPC.id != -2) ? GetRPCIndex(directedRPC.type, directedRPC.funcName) : (-1));
				rpcs[i] = Rpc.CreateRpc(fbb, funcId, paramsOffset);
			}
			rpcsOffset = SyncPackage.CreateRpcsVector(fbb, rpcs);
			rpcInfos.Clear();
		}
		SyncPackage.StartSyncPackage(fbb);
		float networkTimestamp = GetNetworkTimestamp();
		if (!hasSentTimestampDebug && float.IsNaN(networkTimestamp))
		{
			VTNetUtils.SendExceptionReport("float.IsNaN(timestamp) being sent!!");
			hasSentTimestampDebug = true;
		}
		SyncPackage.AddTimestamp(fbb, GetNetworkTimestamp());
		SyncPackage.AddOwnerID(fbb, BDSteamClient.mySteamID);
		SyncPackage.AddItems(fbb, vectorOffset);
		if (flag)
		{
			SyncPackage.AddRpcs(fbb, rpcsOffset);
		}
		Offset<SyncPackage> offset = SyncPackage.EndSyncPackage(fbb);
		fbb.Finish(offset.Value);
		if (fbb.DataBuffer.Position < 9)
		{
			int num = 9 - fbb.DataBuffer.Position;
			fbb.DataBuffer.GrowFront(fbb.DataBuffer.Length + num);
			fbb.DataBuffer.Position += num;
		}
		byte[] array = returnIDbytesBuffer;
		VTNetUtils.ULongToBytes(receiver, out array[0], out array[1], out array[2], out array[3], out array[4], out array[5], out array[6], out array[7]);
		for (int j = 0; j < 8; j++)
		{
			outByteAlloc.Buffer[fbb.DataBuffer.Position - (j + 1)] = array[7 - j];
		}
		outByteAlloc.Buffer[fbb.DataBuffer.Position - 9] = 12;
		SendDirectedData(receiver, fbb);
		fbb.Clear();
	}

	internal void RegisterRPCRequest(RPCRequest r)
	{
		r.requestID = newIDServer.GetNext();
		rpcRequestsDict.Add(r.requestID, r);
	}

	internal static void ULongToInt32s(ulong val, out int aO, out int bO)
	{
		VTNetUtils.ULongToBytes(val, out var a, out var b, out var c, out var d, out var e, out var f, out var g, out var h);
		aO = VTNetUtils.BytesToInt(a, b, c, d);
		bO = VTNetUtils.BytesToInt(e, f, g, h);
	}

	internal static ulong Int32sToULong(int aI, int bI)
	{
		VTNetUtils.IntToBytes(aI, out var a, out var b, out var c, out var d);
		VTNetUtils.IntToBytes(bI, out var a2, out var b2, out var c2, out var d2);
		return VTNetUtils.BytesToULong(a, b, c, d, a2, b2, c2, d2);
	}

	public static void TestUlongInts()
	{
		for (int i = 0; i < 64; i++)
		{
			ulong num = 0uL;
			for (int j = 0; j < 100; j++)
			{
				num += (ulong)((long)UnityEngine.Random.Range(0, int.MaxValue) + (long)UnityEngine.Random.Range(0, int.MaxValue));
			}
			ULongToInt32s(num, out var aO, out var bO);
			ulong num2 = Int32sToULong(aO, bO);
			if (num == num2)
			{
				Debug.LogFormat("{0} => {1} {2} => {3}", num, aO, bO, num2);
			}
			else
			{
				Debug.LogErrorFormat("{0} => {1} {2} => {3}", num, aO, bO, num2);
			}
		}
	}

	public static float GetNetworkTimestamp()
	{
		return networkTime;
	}

	public static int GetPing(SteamId userId)
	{
		if (instance.netState == NetStates.IsClient)
		{
			return instance.socketClient.GetPing(userId);
		}
		if (instance.netState == NetStates.IsHost)
		{
			return instance.socketHost.GetPingMs(userId);
		}
		return -1;
	}

	public static float GetPingSeconds(SteamId userId)
	{
		int ping = GetPing(userId);
		if (ping > 0)
		{
			return (float)ping / 1000f;
		}
		return -1f;
	}

	private void BuildResync(List<VTNetSync> netSyncs, List<RPCInfo> _bufferedRPCs, Action<FlatBufferBuilder> OnBuilderFinish)
	{
		syncCount = netSyncs.Count;
		startIdx = 0;
		if (itemOffsets.Length != netSyncs.Count)
		{
			itemOffsets = new Offset<SyncItem>[syncCount];
		}
		int num = startIdx;
		for (int i = 0; i < syncCount; i++)
		{
			VTNetSync vTNetSync = netSyncs[num];
			if (!upSyncs.TryGetValue(vTNetSync.id, out var value))
			{
				value = new SyncDataUp(deltaCompress: true);
				upSyncs.Add(vTNetSync.id, value);
			}
			vTNetSync.sddBaseline.UploadData(value);
			Debug.Log($"Built resync item: {vTNetSync.GetType()} ({vTNetSync.id}) : {value.DebugStacks()}");
			Offset<SyncItem> offset = value.CreateSyncItem(fbb, vTNetSync.id, forceAll: true);
			itemOffsets[i] = offset;
			num = (num + 1) % netSyncs.Count;
		}
		startIdx = num;
		VectorOffset itemsOffset = SyncPackage.CreateItemsVector(fbb, itemOffsets);
		bool flag = false;
		if (_bufferedRPCs != null && _bufferedRPCs.Count > 0)
		{
			Debug.Log($"Sending {_bufferedRPCs.Count} buffered RPCs");
			foreach (RPCInfo _bufferedRPC in _bufferedRPCs)
			{
				rpcInfos.Add(_bufferedRPC);
			}
		}
		VectorOffset rpcsOffset = default(VectorOffset);
		if (rpcInfos.Count > 0)
		{
			flag = true;
			if (rpcs.Length != rpcInfos.Count)
			{
				rpcs = new Offset<Rpc>[rpcInfos.Count];
			}
			for (int j = 0; j < rpcInfos.Count; j++)
			{
				RPCInfo rPCInfo = rpcInfos[j];
				Offset<SyncItem> paramsOffset = rPCInfo.paramData.CreateSyncItem(fbb, rPCInfo.id, forceAll: true);
				int rPCIndex = GetRPCIndex(rPCInfo.type, rPCInfo.funcName);
				rpcs[j] = Rpc.CreateRpc(fbb, rPCIndex, paramsOffset);
			}
			rpcsOffset = SyncPackage.CreateRpcsVector(fbb, rpcs);
			rpcInfos.Clear();
		}
		SyncPackage.StartSyncPackage(fbb);
		SyncPackage.AddTimestamp(fbb, GetNetworkTimestamp());
		SyncPackage.AddOwnerID(fbb, BDSteamClient.mySteamID);
		SyncPackage.AddItems(fbb, itemsOffset);
		if (flag)
		{
			SyncPackage.AddRpcs(fbb, rpcsOffset);
		}
		Offset<SyncPackage> offset2 = SyncPackage.EndSyncPackage(fbb);
		fbb.Finish(offset2.Value);
		if (fbb.DataBuffer.Position == 0)
		{
			fbb.DataBuffer.GrowFront(fbb.DataBuffer.Length + 1);
			fbb.DataBuffer.Position++;
		}
		outByteAlloc.Buffer[fbb.DataBuffer.Position - 1] = 1;
		OnBuilderFinish?.Invoke(fbb);
		fbb.Clear();
	}

	public void AddBufferedRPC(RPCInfo rpc)
	{
		bufferedRPCs.Add(rpc);
	}

	public void ReceiveSyncMessage(ByteBuffer buf, ByteArrayAllocator bAlloc, int contentSize, int offset = 0)
	{
		if (true)
		{
			int num = contentSize * 255;
			if (decompressTgt.Length < num)
			{
				decompressTgt = new byte[num * 2];
				Debug.LogFormat("VTNetworkManager is growing decompression buffer to {0}kb", (float)decompressTgt.Length / 1024f);
			}
			int num2 = LZ4Codec.Decode(bAlloc.Buffer, offset, contentSize, decompressTgt, 0, decompressTgt.Length);
			if (bAlloc.Length < num2)
			{
				bAlloc.GrowFront(num2);
			}
			Buffer.BlockCopy(decompressTgt, 0, bAlloc.Buffer, 0, num2);
			buf.Position = 0;
			float num3 = (float)contentSize / (float)num2;
			if (num3 < minDecompressionRatio)
			{
				minDecompressionRatio = num3;
				Debug.LogFormat("New minimum decompression ratio: {0}%", minDecompressionRatio * 100f);
			}
			if (num2 > num)
			{
				num = num2;
				Debug.Log($"New maximum decompressed message size: {(float)num2 / 1024f} kb");
			}
		}
		SyncPackage rootAsSyncPackage = SyncPackage.GetRootAsSyncPackage(buf);
		float timestamp = rootAsSyncPackage.Timestamp;
		ulong ownerID = rootAsSyncPackage.OwnerID;
		int itemsLength = rootAsSyncPackage.ItemsLength;
		float ping = GetNetworkTimestamp() - timestamp;
		for (int i = 0; i < itemsLength; i++)
		{
			if (!rootAsSyncPackage.Items(i).HasValue)
			{
				continue;
			}
			SyncItem value = rootAsSyncPackage.Items(i).Value;
			if (value.IntsLength > 0)
			{
				int id = value.Id;
				if (remoteSyncDict.TryGetValue(id, out var value2))
				{
					SyncDataDownDelta syncDataDownDelta = new SyncDataDownDelta(value, timestamp, ping, value2.sddBaseline);
					value2.DownloadData(syncDataDownDelta);
				}
			}
		}
		int rpcsLength = rootAsSyncPackage.RpcsLength;
		if (rpcsLength <= 0)
		{
			return;
		}
		if (rpcsLength > maxRPCsReceived)
		{
			maxRPCsReceived = rpcsLength;
			Debug.Log("New max RPCs received: " + maxRPCsReceived);
		}
		for (int j = 0; j < rpcsLength; j++)
		{
			Rpc value3 = rootAsSyncPackage.Rpcs(j).Value;
			int funcId = value3.FuncId;
			bool flag = ((funcId >> 31) & 1) == 1;
			if (funcId == -2)
			{
				continue;
			}
			funcId &= 0x7FFFFFFF;
			SyncItem value4 = value3.Params.Value;
			int id2 = value4.Id;
			if (id2 == -2)
			{
				SyncDataDown paramData = new SyncDataDown(value4, timestamp, ping);
				if (GetRPCReturnReceiverID(ref paramData) == mySteamID)
				{
					int nextInt = paramData.GetNextInt();
					if (rpcRequestsDict.TryGetValue(nextInt, out var value5))
					{
						object returnObject = paramData.GetReturnObject(value5.returnType);
						value5.OnResponse(returnObject);
						rpcRequestsDict.Remove(nextInt);
					}
					else
					{
						Debug.LogFormat("We did not find an RPC request waiting for this result... id:{0}", nextInt);
					}
				}
				continue;
			}
			SyncDataDown paramData2 = new SyncDataDown(value4, timestamp, ping);
			if (!allSyncsDict.TryGetValue(id2, out var value6) || !value6)
			{
				continue;
			}
			if (!value6)
			{
				if (debugRPCs)
				{
					Debug.LogError("Tried to call RPC on a null VTNetSync!");
				}
				continue;
			}
			MethodInfo rPCMethod = GetRPCMethod(value6.GetType(), funcId);
			if (rPCMethod != null)
			{
				if (debugRPCs)
				{
					Debug.Log("Received RPC: " + rPCMethod.Name + " for " + value6.name + " (" + value6.gameObject.name + ")");
				}
				if (FireRPC(value6, rPCMethod.Name, paramData2, ownerID, (flag && isHost) ? id2 : (-1), out var returnObj, out var requestID, out var requesterID))
				{
					rpcReturnQueue.Enqueue(new RPCReturn
					{
						requesterID = requesterID,
						returnObj = returnObj,
						requestID = requestID
					});
				}
			}
			else if (debugRPCs)
			{
				Debug.LogError(" === ! Failed to get RPC method ! ===");
			}
		}
	}

	private ulong GetRPCReturnReceiverID(ref SyncDataDown paramData)
	{
		int nextInt = paramData.GetNextInt();
		int nextInt2 = paramData.GetNextInt();
		return Int32sToULong(nextInt, nextInt2);
	}

	private VTMethodInfo GetMethodInfo(VTNetSync netSync, string funcName)
	{
		Type type = netSync.GetType();
		if (!rpcDict.TryGetValue(type, out var value))
		{
			value = new Dictionary<string, VTMethodInfo>();
			rpcDict.Add(type, value);
		}
		if (value.TryGetValue(funcName, out var value2))
		{
			return value2;
		}
		value2 = new VTMethodInfo();
		value2.method = type.GetMethod(funcName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		ParameterInfo[] parameters = value2.method.GetParameters();
		if (parameters == null || parameters.Length == 0)
		{
			value2.paramTypes = null;
		}
		else
		{
			value2.paramTypes = new Type[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				value2.paramTypes[i] = parameters[i].ParameterType;
			}
		}
		value.Add(funcName, value2);
		return value2;
	}

	private bool FireRPC(VTNetSync netSync, string funcName, SyncDataDown paramData, ulong callerID, int bufferedId, out object returnObj, out int requestID, out ulong requesterID)
	{
		currentRPCInfo.exists = true;
		currentRPCInfo.senderId = callerID;
		requestID = -1;
		requesterID = 0uL;
		returnObj = null;
		VTMethodInfo methodInfo = GetMethodInfo(netSync, funcName);
		if (methodInfo == null)
		{
			Debug.LogError($"No RPC method info found for {netSync.GetType()}:{funcName}()");
			return false;
		}
		bool flag = methodInfo.method.ReturnType != typeof(void);
		if (flag)
		{
			requesterID = GetRPCReturnReceiverID(ref paramData);
			if (GetRPCReturnReceiverID(ref paramData) != mySteamID)
			{
				return false;
			}
			requestID = paramData.GetNextInt();
		}
		object[] array = null;
		if (methodInfo.paramTypes != null)
		{
			int num = methodInfo.paramTypes.Length;
			array = new object[methodInfo.paramTypes.Length];
			try
			{
				for (int i = 0; i < num; i++)
				{
					if (methodInfo.paramTypes[i] == typeof(Vector3))
					{
						array[i] = paramData.GetNextVector3();
						continue;
					}
					if (methodInfo.paramTypes[i] == typeof(float))
					{
						array[i] = paramData.GetNextFloat();
						continue;
					}
					if (methodInfo.paramTypes[i] == typeof(int))
					{
						array[i] = paramData.GetNextInt();
						continue;
					}
					if (methodInfo.paramTypes[i] == typeof(Quaternion))
					{
						array[i] = paramData.GetNextQuaternion();
						continue;
					}
					if (methodInfo.paramTypes[i] == typeof(ulong))
					{
						array[i] = paramData.GetNextULong();
						continue;
					}
					throw new InvalidOperationException("Attempted to fire an RPC with an unsupported parameter type: " + methodInfo.paramTypes[i]);
				}
			}
			catch (ArgumentOutOfRangeException arg)
			{
				Debug.LogError($"Exception when retrieving parameters irn RPC {netSync.GetType().Name}.{funcName}()\n{arg}");
			}
			if (bufferedId >= 0)
			{
				RPCInfo item = default(RPCInfo);
				item.type = netSync.GetType();
				item.funcName = funcName;
				item.id = bufferedId;
				item.paramData = new SyncDataUp(deltaCompress: false, reusable: true);
				for (int j = 0; j < array.Length; j++)
				{
					item.paramData.AddObject(array[j]);
				}
				bufferedRPCs.Add(item);
			}
		}
		returnObj = methodInfo.method.Invoke(netSync, array);
		currentRPCInfo.exists = false;
		currentRPCInfo.senderId = 0uL;
		return flag;
	}

	public static NetInstantiateRequest NetInstantiate(string resourcePath, Vector3 pos, Quaternion rot, bool active = true)
	{
		if (verboseLogs)
		{
			Debug.LogFormat("Calling NetInstantiate for {0}", resourcePath);
		}
		if (instance.netState == NetStates.None)
		{
			Debug.LogErrorFormat("Tried to NetInstantiate but net state is invalid: {0}", instance.netState);
			return null;
		}
		if (instance.connectionState != ConnectionStates.Connected)
		{
			Debug.LogErrorFormat("Tried to NetInstantiate but connectionState is invalid: {0}", instance.connectionState);
		}
		NetInstantiateRequest netInstantiateRequest = new NetInstantiateRequest();
		netInstantiateRequest.id = instance.newIDServer.GetNext();
		if (instance.netState == NetStates.IsHost)
		{
			List<int> list = instance.GenerateIDList(resourcePath);
			instance.netInstantiateRequests.Add(netInstantiateRequest.id, netInstantiateRequest);
			instance.ReceiveFinalInstantiateCommand(resourcePath, list, netInstantiateRequest.id, isMine: true, mySteamID, pos, rot, active);
			instance.SendInstantiateCommandToClients(resourcePath, list, mySteamID, netInstantiateRequest.id, pos, rot, active);
		}
		else
		{
			instance.netInstantiateRequests.Add(netInstantiateRequest.id, netInstantiateRequest);
			instance.SendInstantiateRequestMessage(resourcePath, netInstantiateRequest.id, pos, rot, active);
		}
		return netInstantiateRequest;
	}

	private List<int> GenerateIDList(string resourcePath)
	{
		GameObject instantiatePrefab = GetInstantiatePrefab(resourcePath);
		if (!instantiatePrefab)
		{
			Debug.LogError("GenerateIDList prefab not found: " + resourcePath);
		}
		VTNetEntity component = instantiatePrefab.GetComponent<VTNetEntity>();
		if (!component)
		{
			Debug.LogError("GenerateIDList prefab did not have a VTNetEntity. Components:");
			Component[] components = component.GetComponents<Component>();
			foreach (Component component2 in components)
			{
				Debug.Log(" - " + component2.GetType().Name);
			}
		}
		int count = component.netSyncs.Count;
		List<int> list = new List<int>(count);
		list.Add(newIDServer.GetNext());
		for (int j = 0; j < count; j++)
		{
			list.Add(newIDServer.GetNext());
		}
		return list;
	}

	public static void RegisterOverrideResource(string path, GameObject prefab)
	{
		if (overriddenResources.ContainsKey(path))
		{
			overriddenResources[path] = prefab;
		}
		else
		{
			overriddenResources.Add(path, prefab);
		}
	}

	private static GameObject GetInstantiatePrefab(string resourcePath)
	{
		GameObject value = null;
		if (!overriddenResources.TryGetValue(resourcePath, out value) || !value)
		{
			value = Resources.Load<GameObject>(resourcePath);
		}
		return value;
	}

	internal void ReceiveFinalInstantiateCommand(string resourcePath, List<int> netSyncIds, int requestId, bool isMine, ulong ownerID, Vector3 position, Quaternion rotation, bool active)
	{
		if (netEntities.ContainsKey(netSyncIds[0]))
		{
			Debug.LogErrorFormat("Received an instantiate command that we already received before!! id: {0}", netSyncIds[0]);
			return;
		}
		if (verboseLogs)
		{
			Debug.LogFormat("Received final instantiate command: {0} ({1}) id: {2}", resourcePath, isMine ? "mine" : "not mine", netSyncIds[0]);
		}
		GameObject instantiatePrefab = GetInstantiatePrefab(resourcePath);
		if (!instantiatePrefab)
		{
			Debug.LogError("VTNetworkManager: Received command to instantiate but the resource was not found! '" + resourcePath + "'");
		}
		instantiatePrefab.SetActive(active);
		GameObject gameObject = UnityEngine.Object.Instantiate(instantiatePrefab, position, rotation);
		gameObject.SetActive(active);
		instantiatePrefab.SetActive(value: true);
		VTNetEntity component = gameObject.GetComponent<VTNetEntity>();
		component.isMine = isMine;
		component.entityID = netSyncIds[0];
		component.ownerID = ownerID;
		netEntities.Add(component.entityID, component);
		if (netSyncIds.Count != component.netSyncs.Count + 1)
		{
			Debug.LogError("Received an instantiate command where the ID list count does not match the VTNetEntity's netSync count!!", gameObject);
			return;
		}
		for (int i = 0; i < component.netSyncs.Count; i++)
		{
			if ((bool)component.netSyncs[i])
			{
				component.netSyncs[i].id = netSyncIds[i + 1];
			}
		}
		component.RegisterSyncs();
		if (isMine && netInstantiateRequests.TryGetValue(requestId, out var value))
		{
			value.obj = gameObject;
			value.isReady = true;
		}
		component.InitializeSyncs();
	}

	private void SendInstantiateCommandToClients(string resourcePath, List<int> idList, SteamId? requestingClient, int requestID, Vector3 pos, Quaternion rot, bool active)
	{
		if (netState != NetStates.IsHost)
		{
			Debug.LogError("Tried to send instantiate command to clients but we're not a host!");
			return;
		}
		byte[] bytes = Encoding.UTF8.GetBytes(resourcePath);
		byte[] bytes2 = BitConverter.GetBytes(requestID);
		byte[] bytes3 = BitConverter.GetBytes(requestingClient.Value.Value);
		int num = 14 + idList.Count * 4 + 36 + 1 + bytes.Length;
		byte[] byteArr = new byte[num];
		int num2 = 1;
		for (int i = 0; i < bytes2.Length; i++)
		{
			byteArr[num2] = bytes2[i];
			num2++;
		}
		for (int j = 0; j < bytes3.Length; j++)
		{
			byteArr[num2] = bytes3[j];
			num2++;
		}
		byteArr[num2] = (byte)idList.Count;
		num2++;
		for (int k = 0; k < idList.Count; k++)
		{
			byte[] bytes4 = BitConverter.GetBytes(idList[k]);
			for (int l = 0; l < bytes4.Length; l++)
			{
				byteArr[num2 + l] = bytes4[l];
			}
			num2 += 4;
		}
		VTNetUtils.WorldPosToNetPos(pos, ref byteArr, num2);
		num2 += 24;
		VTNetUtils.Vector3ToBytes(rot.eulerAngles, ref byteArr, num2);
		num2 += 12;
		byteArr[num2] = (byte)(active ? 1u : 0u);
		num2++;
		for (int m = 0; m < bytes.Length; m++)
		{
			byteArr[num2] = bytes[m];
			num2++;
		}
		foreach (VTSocketHost.ConnectedClient connectedClient in socketHost.connectedClients)
		{
			byte b = (byteArr[0] = (byte)((!requestingClient.HasValue || (ulong)connectedClient.steamId != (ulong)requestingClient.Value) ? 4 : 3));
			Connection connection = connectedClient.connection;
			connection.SendMessage(byteArr);
			ReportSentData(num);
		}
		ulong ownerID = (requestingClient.HasValue ? requestingClient.Value.Value : mySteamID);
		socketHost.RegisterInstantiation(byteArr, ownerID, idList[0]);
	}

	private void SendInstantiateRequestMessage(string resourcePath, int requestID, Vector3 worldPos, Quaternion rotation, bool active)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(resourcePath);
		byte[] bytes2 = BitConverter.GetBytes(requestID);
		int num = 42 + bytes.Length;
		byte[] byteArr = new byte[num];
		byteArr[0] = 2;
		int num2 = 1;
		for (int i = 0; i < bytes2.Length; i++)
		{
			byteArr[num2] = bytes2[i];
			num2++;
		}
		VTNetUtils.WorldPosToNetPos(worldPos, ref byteArr, num2);
		num2 += 24;
		VTNetUtils.Vector3ToBytes(rotation.eulerAngles, ref byteArr, num2);
		num2 += 12;
		byteArr[num2] = (byte)(active ? 1u : 0u);
		num2++;
		for (int j = 0; j < bytes.Length; j++)
		{
			byteArr[num2] = bytes[j];
			num2++;
		}
		if (netState == NetStates.IsClient)
		{
			ReportSentData(num);
			socketClient.Connection.SendMessage(byteArr);
			Debug.LogFormat("Sent instantiate request #{0} for {1} from client to host.", requestID, resourcePath);
		}
		else if (netState == NetStates.IsHost)
		{
			Debug.LogError("The host is sending an instantiate request, but it should not be! (It completes it's own requests locally.)");
		}
	}

	public void ReceiveInstantiateRequestMessage(byte[] buffer, int size, SteamId requestingClient)
	{
		int num = BitConverter.ToInt32(buffer, 0);
		int num2 = 41;
		Vector3 vector = VTNetUtils.NetPosToWorldPos(ref buffer, 4);
		Quaternion quaternion = Quaternion.Euler(VTNetUtils.BytesToVector3(ref buffer, 28));
		bool active = buffer[num2 - 1] > 0;
		string @string = Encoding.UTF8.GetString(buffer, num2, size - num2);
		Debug.Log("Received instantiate request. path size: " + (size - num2) + " path: " + @string);
		List<int> list = GenerateIDList(@string);
		SendInstantiateCommandToClients(@string, list, requestingClient, num, vector, quaternion, active);
		ReceiveFinalInstantiateCommand(@string, list, num, isMine: false, requestingClient.Value, vector, quaternion, active);
	}

	public void RegisterSceneEntity(VTNetEntity ent)
	{
		if (netState != NetStates.IsHost)
		{
			Debug.LogError("Tried to register a scene entity but we're not a host!");
			return;
		}
		if (!ent.isSceneEntity)
		{
			Debug.LogError("Tried to register an entity as a scene object but 'isSceneEntity' was false!");
			return;
		}
		List<int> list = new List<int>();
		ent.entityID = newIDServer.GetNext();
		list.Add(ent.entityID);
		for (int i = 0; i < ent.netSyncs.Count; i++)
		{
			int next = newIDServer.GetNext();
			list.Add(next);
			ent.netSyncs[i].id = next;
		}
		ent.RegisterSyncs();
		byte[] array = CreateSceneEntityInitMessage(ent);
		foreach (Connection item in socketHost.Connected)
		{
			item.SendMessage(array);
			ReportSentData(array.Length);
		}
		ent.InitializeSyncs();
		VTNetSceneManager.instance.InitHostSceneEntity(ent.sceneEntityIdx);
	}

	public VTNetEntity GetEntity(int entityID)
	{
		if (netEntities.TryGetValue(entityID, out var value))
		{
			return value;
		}
		return null;
	}

	public void InitSceneEntitiesOnNewConnection(Connection conn)
	{
		if ((bool)VTNetSceneManager.instance)
		{
			for (int i = 0; i < VTNetSceneManager.instance.sceneEntities.Length; i++)
			{
				byte[] array = CreateSceneEntityInitMessage(VTNetSceneManager.instance.sceneEntities[i]);
				conn.SendMessage(array);
				ReportSentData(array.Length);
			}
		}
	}

	public void NetDestroyAllObjectsByOwner(SteamId id)
	{
		List<VTNetEntity> list = new List<VTNetEntity>();
		foreach (VTNetEntity value in netEntities.Values)
		{
			if ((bool)value && value.ownerID == (ulong)id)
			{
				list.Add(value);
			}
		}
		foreach (VTNetEntity item in list)
		{
			NetDestroy(item.gameObject);
		}
	}

	private byte[] CreateSceneEntityInitMessage(VTNetEntity ent)
	{
		byte b = 5;
		byte[] bytes = BitConverter.GetBytes(ent.sceneEntityIdx);
		List<int> list = new List<int>();
		list.Add(ent.entityID);
		for (int i = 0; i < ent.netSyncs.Count; i++)
		{
			list.Add(ent.netSyncs[i].id);
		}
		byte[] array = new byte[1 + bytes.Length + list.Count * 4];
		int num = 0;
		array[num] = b;
		num++;
		int num2 = 0;
		while (num2 < bytes.Length)
		{
			array[num] = bytes[num2];
			num2++;
			num++;
		}
		for (int j = 0; j < list.Count; j++)
		{
			byte[] bytes2 = BitConverter.GetBytes(list[j]);
			int num3 = 0;
			while (num3 < bytes2.Length)
			{
				array[num] = bytes2[num3];
				num3++;
				num++;
			}
		}
		return array;
	}

	public void ParseSceneEntityInitMessage(byte[] msgBytes, out int sceneEntityIdx, out int entityID, out List<int> idList)
	{
		sceneEntityIdx = BitConverter.ToInt32(msgBytes, 0);
		entityID = BitConverter.ToInt32(msgBytes, 4);
		idList = new List<int>();
		int num = 8;
		try
		{
			VTNetEntity vTNetEntity = VTNetSceneManager.instance.sceneEntities[sceneEntityIdx];
			int num2 = msgBytes.Length - num;
			int num3 = vTNetEntity.netSyncs.Count * 4;
			if (num2 != num3)
			{
				Debug.LogErrorFormat("Received a Scene Entity Init Message with an unexpected id list size. Expected:{0}, Received:{1}, EntIdx:{2} ({3})", num3, num2, sceneEntityIdx, vTNetEntity.gameObject.name);
			}
			int num4 = 0;
			while (num4 < vTNetEntity.netSyncs.Count)
			{
				idList.Add(BitConverter.ToInt32(msgBytes, num));
				num4++;
				num += 4;
			}
		}
		catch (Exception ex)
		{
			Debug.Log("Exception in ParseSceneEntityInitMessage. sceneEntityIdx=" + sceneEntityIdx + ", entityID=" + entityID + ", b=" + num + ", msgBytes.Length=" + msgBytes.Length + "\n" + ex);
		}
	}

	public static void NetDestroyObject(GameObject go)
	{
		instance.NetDestroy(go);
	}

	public static void NetDestroyDelayed(GameObject go, float delay)
	{
		instance.StartCoroutine(instance.DelayedNetDestroyRtn(go, delay));
	}

	private IEnumerator DelayedNetDestroyRtn(GameObject go, float delay)
	{
		float t = Time.time;
		while (Time.time - t < delay)
		{
			yield return null;
		}
		if ((bool)go)
		{
			NetDestroy(go);
		}
	}

	public void NetDestroy(GameObject go)
	{
		if (connectionState != ConnectionStates.Connected || netState == NetStates.None)
		{
			Debug.Log("Tried to NetDestroy but we are not connected. Destroying object locally.");
			if ((bool)go)
			{
				UnityEngine.Object.Destroy(go);
			}
		}
		else
		{
			if (!go)
			{
				return;
			}
			VTNetEntity component = go.GetComponent<VTNetEntity>();
			if (!component || !component.hasRegistered)
			{
				Debug.Log("Tried to NetDestroy a gameObject with no VTNetEntity or is not registered. Normally destroying it. (" + UIUtils.GetHierarchyString(go) + ")");
				UnityEngine.Object.Destroy(go);
				return;
			}
			if (isHost)
			{
				SendNetDestroyCommandToClients(component.entityID);
				return;
			}
			byte[] bytes = BitConverter.GetBytes(component.entityID);
			int num = 5;
			byte[] array = new byte[num];
			array[0] = 6;
			int num2 = 1;
			int num3 = 0;
			while (num3 < bytes.Length)
			{
				array[num2] = bytes[num3];
				num3++;
				num2++;
			}
			instance.ReportSentData(num);
			socketClient.Connection.SendMessage(array);
		}
	}

	public void SendNetDestroyCommandToClients(int id)
	{
		if (!isHost)
		{
			return;
		}
		Debug.Log("Received command to destroy net entity (host) " + id);
		if (netEntities.TryGetValue(id, out var value) || unregisteredEntities.TryGetValue(id, out value))
		{
			if ((bool)value)
			{
				UnityEngine.Object.Destroy(value.gameObject);
			}
			netEntities.Remove(id);
			unregisteredEntities.Remove(id);
		}
		else
		{
			Debug.LogError(" - Destroy target did not exist!");
		}
		byte[] bytes = BitConverter.GetBytes(id);
		int num = 5;
		byte[] array = new byte[num];
		array[0] = 6;
		int num2 = 1;
		int num3 = 0;
		while (num3 < bytes.Length)
		{
			array[num2] = bytes[num3];
			num3++;
			num2++;
		}
		foreach (VTSocketHost.ConnectedClient connectedClient in socketHost.connectedClients)
		{
			connectedClient.connection.SendMessage(array);
			ReportSentData(num);
		}
		netEntities.Remove(id);
		unregisteredEntities.Remove(id);
	}

	public void ReceiveFinalNetDestroyCommand(int id)
	{
		Debug.Log("Received command to destroy net entity " + id);
		if (netEntities.TryGetValue(id, out var value) || unregisteredEntities.TryGetValue(id, out value))
		{
			if ((bool)value)
			{
				UnityEngine.Object.Destroy(value.gameObject, 0.1f);
			}
			netEntities.Remove(id);
			unregisteredEntities.Remove(id);
		}
	}

	public void UnregisterNetEntity(VTNetEntity ne)
	{
		if (isHost)
		{
			socketHost.UnregisterInstantiation(ne.entityID);
		}
		if (ne.hasRegistered)
		{
			ne.UnregisterSyncs();
			netEntities.Remove(ne.entityID);
			if (!unregisteredEntities.ContainsKey(ne.entityID))
			{
				unregisteredEntities.Add(ne.entityID, ne);
			}
		}
	}

	private static int GetRPCIndex(Type t, string methodName)
	{
		if (!rpcDicts.TryGetValue(t, out var value))
		{
			value = new RPCDict();
			GenerateRPCDicts(t, out value.indexToMethod, out value.methodNameToIndex);
			rpcDicts.Add(t, value);
		}
		if (!value.methodNameToIndex.TryGetValue(methodName, out var value2))
		{
			string text = "Missing RPC method: " + t.Name + ":" + methodName;
			SendAsyncException(text);
			Debug.LogError(text);
			return -2;
		}
		return value2;
	}

	public static void SendAsyncException(string msg)
	{
		new GameObject("exception").AddComponent<AsyncExceptionThrower>().Send(msg);
	}

	private static MethodInfo GetRPCMethod(Type t, int idx)
	{
		if (!rpcDicts.TryGetValue(t, out var value))
		{
			value = new RPCDict();
			GenerateRPCDicts(t, out value.indexToMethod, out value.methodNameToIndex);
			rpcDicts.Add(t, value);
		}
		if (value.indexToMethod.TryGetValue(idx, out var value2))
		{
			return value2;
		}
		Debug.LogError($"Could not get RPC method from type {t.Name} at index {idx}");
		return null;
	}

	private static void GenerateRPCDicts(Type type, out Dictionary<int, MethodInfo> indexToMethod, out Dictionary<string, int> methodNameToIndex)
	{
		if (verboseLogs)
		{
			Debug.Log("Generating RPC Dict for " + type.Name);
		}
		List<MethodInfo> list = new List<MethodInfo>();
		indexToMethod = new Dictionary<int, MethodInfo>();
		methodNameToIndex = new Dictionary<string, int>();
		MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in methods)
		{
			VTRPCAttribute customAttribute = methodInfo.GetCustomAttribute<VTRPCAttribute>(inherit: true);
			if (customAttribute == null)
			{
				continue;
			}
			if (customAttribute.index < 0)
			{
				list.Add(methodInfo);
				continue;
			}
			indexToMethod.Add(customAttribute.index, methodInfo);
			methodNameToIndex.Add(methodInfo.Name, customAttribute.index);
			if (verboseLogs)
			{
				Debug.Log($" - [{customAttribute.index}] {methodInfo.Name}");
			}
		}
		list.Sort((MethodInfo a, MethodInfo b) => a.Name.CompareTo(b.Name));
		int j = 0;
		foreach (MethodInfo item in list)
		{
			for (; indexToMethod.ContainsKey(j); j++)
			{
			}
			indexToMethod.Add(j, item);
			methodNameToIndex.Add(item.Name, j);
			if (verboseLogs)
			{
				Debug.Log($" - [{j}] {item.Name}");
			}
		}
	}

	public void SendVoiceData(SteamId receiver, byte[] buffer, int count)
	{
		if (voiceOutBuffer == null || voiceOutBuffer.Length < count + 1 + 8 + 8)
		{
			voiceOutBuffer = new byte[count + 17];
		}
		byte[] array = voiceOutBuffer;
		Buffer.BlockCopy(buffer, 0, array, 17, count);
		array[0] = 13;
		VTNetUtils.ULongToBytes(receiver.Value, out array[1], out array[2], out array[3], out array[4], out array[5], out array[6], out array[7], out array[8]);
		VTNetUtils.ULongToBytes(BDSteamClient.mySteamID, out array[9], out array[10], out array[11], out array[12], out array[13], out array[14], out array[15], out array[16]);
		int num = count + 1 + 8 + 8;
		if (isHost)
		{
			if (socketHost.connectedClientsDict.TryGetValue(receiver.Value, out var value))
			{
				ReportSentData(num);
				value.connection.SendMessage(array, 0, num, voiceSendType);
			}
		}
		else
		{
			ReportSentData(num);
			socketClient.Connection.SendMessage(array, 0, num, voiceSendType);
		}
	}
}

}