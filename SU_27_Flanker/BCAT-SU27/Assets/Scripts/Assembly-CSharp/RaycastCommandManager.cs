using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class RaycastCommandManager : MonoBehaviour
{
	public class RaycastHandle
	{
		public Object host;

		private bool _hit;

		private bool dirty = true;

		private FixedPoint fp;

		private Vector3 _rNormal;

		private FixedPoint fp_origin;

		public Vector3 direction;

		public int layerMask;

		public float rayDistance;

		public bool hit
		{
			get
			{
				if (dirty)
				{
					DoRaycastNow();
				}
				return _hit;
			}
		}

		public Vector3 resultNormal => _rNormal;

		public Vector3 resultPoint
		{
			get
			{
				if (dirty)
				{
					DoRaycastNow();
				}
				return fp.point;
			}
		}

		public Vector3 origin
		{
			get
			{
				return fp_origin.point;
			}
			set
			{
				fp_origin = new FixedPoint(value);
			}
		}

		private void DoRaycastNow()
		{
			_hit = Physics.Raycast(origin, direction, out var hitInfo, rayDistance, layerMask, QueryTriggerInteraction.Ignore);
			SetResult(hitInfo);
			dirty = false;
		}

		public void SetResult(RaycastHit hit)
		{
			dirty = false;
			_hit = hit.collider != null;
			fp = new FixedPoint(hit.point);
			_rNormal = hit.normal;
		}
	}

	[Header("Testing")]
	public bool testBatched;

	public int testUnitCount = 30;

	private NativeArray<RaycastHit> results;

	private NativeArray<RaycastCommand> commands;

	private List<RaycastHandle> handles = new List<RaycastHandle>();

	private bool batchScheduled;

	private JobHandle jHandle;

	public static RaycastCommandManager instance { get; private set; }

	private void Awake()
	{
		commands = new NativeArray<RaycastCommand>(0, Allocator.Persistent);
		results = new NativeArray<RaycastHit>(0, Allocator.Persistent);
		instance = this;
	}

	private void OnDestroy()
	{
		_ = commands;
		commands.Dispose();
		_ = results;
		results.Dispose();
	}

	public RaycastHandle RegisterRaycaster(Object host)
	{
		for (int i = 0; i < handles.Count; i++)
		{
			if (!handles[i].host)
			{
				handles[i].host = host;
				handles[i].layerMask = 0;
				return handles[i];
			}
		}
		RaycastHandle raycastHandle = new RaycastHandle();
		raycastHandle.host = host;
		handles.Add(raycastHandle);
		if (handles.Count > results.Length)
		{
			NativeArray<RaycastHit> nativeArray = results;
			NativeArray<RaycastCommand> nativeArray2 = commands;
			results = new NativeArray<RaycastHit>(nativeArray.Length + handles.Count, Allocator.Persistent);
			commands = new NativeArray<RaycastCommand>(results.Length, Allocator.Persistent);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				results[j] = nativeArray[j];
				commands[j] = nativeArray2[j];
			}
			nativeArray.Dispose();
			nativeArray2.Dispose();
		}
		return raycastHandle;
	}

	private void Update()
	{
		int count = handles.Count;
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			RaycastHandle raycastHandle = handles[i];
			if (raycastHandle.host != null)
			{
				commands[i] = new RaycastCommand(raycastHandle.origin, raycastHandle.direction, raycastHandle.rayDistance, raycastHandle.layerMask);
				num++;
			}
			else
			{
				commands[i] = default(RaycastCommand);
			}
		}
		if (num > 0)
		{
			batchScheduled = true;
			jHandle = RaycastCommand.ScheduleBatch(commands, results, 1);
		}
	}

	private void LateUpdate()
	{
		if (!batchScheduled)
		{
			return;
		}
		int count = handles.Count;
		jHandle.Complete();
		for (int i = 0; i < count; i++)
		{
			RaycastHandle raycastHandle = handles[i];
			if (raycastHandle.layerMask > 0)
			{
				raycastHandle.SetResult(results[i]);
			}
		}
		batchScheduled = false;
	}

	[ContextMenu("Begin Test")]
	public void BeginTest()
	{
		for (int i = 0; i < testUnitCount; i++)
		{
			GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			obj.transform.localScale = 20f * Vector3.one;
			Object.DestroyImmediate(obj.GetComponent<Collider>());
			obj.AddComponent<FloatingOriginTransform>();
			Vector3 position = new Vector3(Random.Range(-500, 500), 0f, Random.Range(-500, 500));
			position.y = VTMapGenerator.fetch.GetTerrainAltitude(position) + WaterPhysics.waterHeight;
			obj.transform.position = position;
			obj.AddComponent<RCMTester>();
		}
	}
}
