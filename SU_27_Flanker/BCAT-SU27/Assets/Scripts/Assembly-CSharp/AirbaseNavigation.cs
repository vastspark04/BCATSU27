using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

public class AirbaseNavigation : MonoBehaviour
{
	[Serializable]
	public class BakedRoute
	{
		public List<AirbaseNavNode> nodes;

		public AirbaseNavNode startNode
		{
			get
			{
				if (nodes != null && nodes.Count > 0)
				{
					return nodes[0];
				}
				return null;
			}
		}

		public AirbaseNavNode endNode
		{
			get
			{
				if (nodes != null && nodes.Count > 0)
				{
					return nodes[nodes.Count - 1];
				}
				return null;
			}
		}
	}

	private delegate bool DestinationRequirement(AirbaseNavNode node, object[] data);

	public class AsyncPathRequest
	{
		private bool _done;

		private List<AirbaseNavNode> _path;

		private object lockObj = new object();

		public bool done
		{
			get
			{
				lock (lockObj)
				{
					return _done;
				}
			}
		}

		public List<AirbaseNavNode> path
		{
			get
			{
				lock (lockObj)
				{
					return _path;
				}
			}
		}

		public void SetFinishedPath(List<AirbaseNavNode> nodes)
		{
			lock (lockObj)
			{
				_done = true;
				_path = nodes;
			}
		}
	}

	private class AirbaseNavPath
	{
		public List<AirbaseNavNode> nodeSequence = new List<AirbaseNavNode>();

		public float totalDistance;

		public void RecalculateDistance()
		{
			totalDistance = 0f;
			for (int i = 1; i < nodeSequence.Count; i++)
			{
				totalDistance += Vector3.Distance(nodeSequence[i].ts_position, nodeSequence[i - 1].ts_position);
			}
		}

		public List<Transform> GetTransformList()
		{
			List<Transform> list = new List<Transform>();
			for (int i = 0; i < nodeSequence.Count; i++)
			{
				list.Add(nodeSequence[i].transform);
			}
			return list;
		}
	}

	public List<AirbaseNavNode> navNodes = new List<AirbaseNavNode>();

	[Header("Gizmos")]
	public bool drawGizmos = true;

	public Color connectionColor = Color.white;

	public float nodeSize = 5f;

	[HideInInspector]
	public AirbaseNavNode.NodeTypes newNodeType;

	[HideInInspector]
	public AirbaseNavNode hoverNode;

	public List<BakedRoute> bakedRoutes;

	[ContextMenu("Bake Routes")]
	public void BakeRoutes()
	{
		List<AirbaseNavNode> list = new List<AirbaseNavNode>();
		List<AirbaseNavNode> list2 = new List<AirbaseNavNode>();
		List<AirbaseNavNode> list3 = new List<AirbaseNavNode>();
		foreach (AirbaseNavNode navNode in navNodes)
		{
			if (navNode.nodeType == AirbaseNavNode.NodeTypes.Exit)
			{
				list3.Add(navNode);
			}
			else if (navNode.nodeType == AirbaseNavNode.NodeTypes.Parking)
			{
				list.Add(navNode);
			}
			else if (navNode.nodeType == AirbaseNavNode.NodeTypes.TakeOff)
			{
				list2.Add(navNode);
			}
		}
		bakedRoutes = new List<BakedRoute>();
		foreach (AirbaseNavNode item in list)
		{
			foreach (AirbaseNavNode item2 in list2)
			{
				AirbaseNavPath pathTo = GetPathTo(item, item2);
				if (pathTo != null)
				{
					bakedRoutes.Add(new BakedRoute
					{
						nodes = pathTo.nodeSequence
					});
				}
			}
		}
		foreach (AirbaseNavNode item3 in list3)
		{
			foreach (AirbaseNavNode item4 in list)
			{
				AirbaseNavPath pathTo2 = GetPathTo(item3, item4);
				if (pathTo2 != null)
				{
					bakedRoutes.Add(new BakedRoute
					{
						nodes = pathTo2.nodeSequence
					});
				}
			}
		}
	}

	private BakedRoute GetBakedRoute(AirbaseNavNode startNode, AirbaseNavNode destNode)
	{
		for (int i = 0; i < bakedRoutes.Count; i++)
		{
			BakedRoute bakedRoute = bakedRoutes[i];
			if (bakedRoute.startNode == startNode && bakedRoute.endNode == destNode)
			{
				return bakedRoute;
			}
		}
		return null;
	}

	public List<AirbaseNavNode> GetTakeoffPath(Vector3 startPos, Vector3 startDirection, out Runway takeoffRunway, float minLength)
	{
		AirbaseNavNode closestNode = GetClosestNode(startPos, startDirection, exit: false);
		object[] requirementData = new object[1] { minLength };
		AirbaseNavPath path = GetPath(closestNode, startDirection, AirbaseNavNode.NodeTypes.TakeOff, CheckRunwayRequirements, requirementData);
		if (path != null)
		{
			takeoffRunway = path.nodeSequence[path.nodeSequence.Count - 1].takeoffRunway;
			return path.nodeSequence;
		}
		takeoffRunway = null;
		return null;
	}

	public List<AirbaseNavNode> GetParkingPath_OLD(Vector3 startPos, Vector3 startDirection, float minParkingLotSize, bool allowVtol = false)
	{
		AirbaseNavNode closestNode = GetClosestNode(startPos, startDirection, exit: true);
		object[] requirementData = new object[2] { minParkingLotSize, allowVtol };
		return GetPath(closestNode, startDirection, AirbaseNavNode.NodeTypes.Parking, CheckParkingAvailable, requirementData)?.nodeSequence;
	}

	public List<AirbaseNavNode> GetParkingPath(Vector3 startPos, Vector3 startDirection, AirbaseNavNode destinationParking)
	{
		AirbaseNavNode closestExit = GetClosestExit(startPos, startDirection, destinationParking);
		object[] requirementData = new object[1] { destinationParking };
		DestinationRequirement requirementFunc = (AirbaseNavNode n, object[] data) => n == (AirbaseNavNode)data[0];
		return GetPath(closestExit, startDirection, AirbaseNavNode.NodeTypes.Parking, requirementFunc, requirementData)?.nodeSequence;
	}

	public AsyncPathRequest GetParkingPathAsync(Vector3 startPos, Vector3 startDirection, AirbaseNavNode destinationParking)
	{
		AirbaseNavNode closestExit = GetClosestExit(startPos, startDirection, destinationParking);
		object[] requirementData = new object[1] { destinationParking };
		DestinationRequirement requirementFunc = (AirbaseNavNode n, object[] data) => n == (AirbaseNavNode)data[0];
		return GetPathAsync(closestExit, startDirection, AirbaseNavNode.NodeTypes.Parking, requirementFunc, requirementData);
	}

	public AsyncPathRequest GetTakeoffPathAsync(Vector3 startPos, Vector3 startDirection, float minLength)
	{
		AirbaseNavNode closestNode = GetClosestNode(startPos, startDirection, exit: false);
		object[] requirementData = new object[1] { minLength };
		return GetPathAsync(closestNode, startDirection, AirbaseNavNode.NodeTypes.TakeOff, CheckRunwayRequirements, requirementData);
	}

	public List<AirbaseNavNode> GetPathTo(Vector3 startPos, Vector3 destPos)
	{
		AirbaseNavNode closestNode = GetClosestNode(startPos);
		AirbaseNavNode closestNode2 = GetClosestNode(destPos);
		BakedRoute bakedRoute = GetBakedRoute(closestNode, closestNode2);
		if (bakedRoute != null)
		{
			return bakedRoute.nodes;
		}
		return GetPathTo(closestNode, closestNode2)?.nodeSequence;
	}

	private AirbaseNavPath GetPathTo(AirbaseNavNode startNode, AirbaseNavNode destinationNode)
	{
		List<AirbaseNavPath> list = new List<AirbaseNavPath>();
		List<AirbaseNavNode> pathStub = new List<AirbaseNavNode>();
		RecurrGetPathTo(pathStub, startNode, destinationNode, list);
		if (list.Count > 0)
		{
			return list[0];
		}
		return null;
	}

	private AirbaseNavPath GetPath(AirbaseNavNode startNode, Vector3 startDirection, AirbaseNavNode.NodeTypes destType, DestinationRequirement requirementFunc, object[] requirementData)
	{
		List<AirbaseNavPath> list = new List<AirbaseNavPath>();
		List<AirbaseNavNode> pathStub = new List<AirbaseNavNode>();
		RecurrGetPathTo(pathStub, startNode, destType, list, requirementFunc, requirementData);
		if (list.Count > 0)
		{
			return list[0];
		}
		return null;
	}

	private AsyncPathRequest GetPathAsync(AirbaseNavNode startNode, Vector3 startDirection, AirbaseNavNode.NodeTypes destType, DestinationRequirement requirementFunc, object[] requirementData)
	{
		AsyncPathRequest asyncPathRequest = new AsyncPathRequest();
		object[] state = new object[6] { startNode, startDirection, destType, requirementFunc, requirementData, asyncPathRequest };
		ThreadPool.QueueUserWorkItem(T_GetPath, state);
		return asyncPathRequest;
	}

	private void T_GetPath(object args)
	{
		try
		{
			object[] obj = (object[])args;
			AirbaseNavNode currNode = (AirbaseNavNode)obj[0];
			_ = (Vector3)obj[1];
			AirbaseNavNode.NodeTypes destNodeType = (AirbaseNavNode.NodeTypes)obj[2];
			DestinationRequirement requirementFunc = (DestinationRequirement)obj[3];
			object[] requirementData = (object[])obj[4];
			AsyncPathRequest asyncPathRequest = (AsyncPathRequest)obj[5];
			List<AirbaseNavPath> list = new List<AirbaseNavPath>(1);
			List<AirbaseNavNode> pathStub = new List<AirbaseNavNode>();
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			RecurrGetPathTo(pathStub, currNode, destNodeType, list, requirementFunc, requirementData);
			if (list.Count > 0)
			{
				asyncPathRequest.SetFinishedPath(list[0].nodeSequence);
			}
			stopwatch.Stop();
			UnityEngine.Debug.Log("Recurr path took " + stopwatch.ElapsedMilliseconds + "ms");
		}
		catch (Exception message)
		{
			UnityEngine.Debug.LogError(message);
		}
	}

	private void RecurrGetPathTo(List<AirbaseNavNode> pathStub, AirbaseNavNode currNode, AirbaseNavNode destNode, List<AirbaseNavPath> pathList, float dist = 0f)
	{
		pathStub.Add(currNode);
		if (currNode == destNode)
		{
			if (pathList.Count == 0 || pathList[0].totalDistance > dist)
			{
				pathList.Clear();
				AirbaseNavPath airbaseNavPath = new AirbaseNavPath();
				airbaseNavPath.nodeSequence = pathStub;
				airbaseNavPath.totalDistance = dist;
				pathList.Add(airbaseNavPath);
			}
			return;
		}
		List<AirbaseNavNode> list = new List<AirbaseNavNode>(4);
		List<float> list2 = new List<float>(4);
		foreach (AirbaseNavNode connectedNode in currNode.connectedNodes)
		{
			float num = dist + Vector3.Distance(currNode.ts_position, connectedNode.ts_position);
			if ((pathList.Count <= 0 || !(pathList[0].totalDistance < num)) && !pathStub.Contains(connectedNode))
			{
				list2.Add(num);
				list.Add(connectedNode);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (i == list.Count - 1)
			{
				RecurrGetPathTo(pathStub, list[i], destNode, pathList, list2[i]);
			}
			else
			{
				RecurrGetPathTo(pathStub.Copy(), list[i], destNode, pathList, list2[i]);
			}
		}
	}

	private void RecurrGetPathTo(List<AirbaseNavNode> pathStub, AirbaseNavNode currNode, AirbaseNavNode.NodeTypes destNodeType, List<AirbaseNavPath> pathList, DestinationRequirement requirementFunc, object[] requirementData, float dist = 0f)
	{
		if (currNode == null)
		{
			return;
		}
		pathStub.Add(currNode);
		if (currNode.nodeType == destNodeType)
		{
			if ((requirementFunc == null || requirementFunc(currNode, requirementData)) && (pathList.Count == 0 || pathList[0].totalDistance > dist))
			{
				pathList.Clear();
				AirbaseNavPath airbaseNavPath = new AirbaseNavPath();
				airbaseNavPath.nodeSequence = pathStub;
				airbaseNavPath.totalDistance = dist;
				pathList.Add(airbaseNavPath);
			}
			return;
		}
		List<AirbaseNavNode> list = new List<AirbaseNavNode>(4);
		List<float> list2 = new List<float>(4);
		foreach (AirbaseNavNode connectedNode in currNode.connectedNodes)
		{
			float num = dist + Vector3.Distance(currNode.ts_position, connectedNode.ts_position);
			if ((pathList.Count <= 0 || !(pathList[0].totalDistance < num)) && (connectedNode.nodeType != AirbaseNavNode.NodeTypes.Parking || requirementFunc(connectedNode, requirementData)) && !pathStub.Contains(connectedNode))
			{
				list2.Add(num);
				list.Add(connectedNode);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (i == list.Count - 1)
			{
				RecurrGetPathTo(pathStub, list[i], destNodeType, pathList, requirementFunc, requirementData, list2[i]);
			}
			else
			{
				RecurrGetPathTo(pathStub.Copy(), list[i], destNodeType, pathList, requirementFunc, requirementData, list2[i]);
			}
		}
	}

	[Obsolete("Parking reservation is handled by AirportManager")]
	private bool CheckParkingAvailable(AirbaseNavNode node, object[] data)
	{
		float num = (float)data[0];
		bool flag = (bool)data[1];
		if (node.nodeType == AirbaseNavNode.NodeTypes.Parking && node.parkingSize > num)
		{
			if (node.vtolOnly && !flag)
			{
				return false;
			}
			return true;
		}
		return false;
	}

	private bool CheckRunwayRequirements(AirbaseNavNode node, object[] data)
	{
		float num = (float)data[0];
		return node.runwayLength > num;
	}

	public AirbaseNavNode GetClosestNode(Vector3 pos)
	{
		AirbaseNavNode result = null;
		float num = float.MaxValue;
		foreach (AirbaseNavNode navNode in navNodes)
		{
			float sqrMagnitude = (pos - navNode.transform.position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				result = navNode;
				num = sqrMagnitude;
			}
		}
		return result;
	}

	public AirbaseNavNode GetClosestNode(Vector3 pos, Vector3 startDirection, bool exit)
	{
		AirbaseNavNode result = null;
		AirbaseNavNode airbaseNavNode = null;
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		foreach (AirbaseNavNode navNode in navNodes)
		{
			if (exit && navNode.nodeType != AirbaseNavNode.NodeTypes.Exit)
			{
				continue;
			}
			Vector3 lhs = navNode.transform.position - pos;
			float sqrMagnitude = lhs.sqrMagnitude;
			if (Vector3.Dot(lhs, startDirection) > 0f || (navNode.nodeType == AirbaseNavNode.NodeTypes.Parking && sqrMagnitude < navNode.parkingSize * navNode.parkingSize))
			{
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					airbaseNavNode = navNode;
				}
			}
			else if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = navNode;
			}
		}
		if ((bool)airbaseNavNode)
		{
			return airbaseNavNode;
		}
		return result;
	}

	public AirbaseNavNode GetClosestExit(Vector3 pos, Vector3 startDirection, AirbaseNavNode destinationParking)
	{
		AirbaseNavNode result = null;
		AirbaseNavNode airbaseNavNode = null;
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		foreach (AirbaseNavNode navNode in navNodes)
		{
			if (navNode.nodeType != AirbaseNavNode.NodeTypes.Exit || !navNode.destinationParkingNodes.Contains(destinationParking))
			{
				continue;
			}
			Vector3 lhs = navNode.transform.position - pos;
			float sqrMagnitude = lhs.sqrMagnitude;
			if (Vector3.Dot(lhs, startDirection) > 0f)
			{
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					airbaseNavNode = navNode;
				}
			}
			else if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = navNode;
			}
		}
		if ((bool)airbaseNavNode)
		{
			return airbaseNavNode;
		}
		return result;
	}

	public bool AreNodesConnected(AirbaseNavNode a, AirbaseNavNode b)
	{
		List<AirbaseNavNode> list = new List<AirbaseNavNode>();
		list.Add(a);
		return RecurrAreNodesConnected(list, a, b);
	}

	private bool RecurrAreNodesConnected(List<AirbaseNavNode> checkedNodes, AirbaseNavNode currNode, AirbaseNavNode destNode)
	{
		if (currNode == destNode)
		{
			return true;
		}
		foreach (AirbaseNavNode connectedNode in currNode.connectedNodes)
		{
			if (!checkedNodes.Contains(connectedNode))
			{
				checkedNodes.Add(connectedNode);
				if (RecurrAreNodesConnected(checkedNodes, connectedNode, destNode))
				{
					return true;
				}
			}
		}
		return false;
	}
}
