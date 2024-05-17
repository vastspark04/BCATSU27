using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FloatingOrigin : MonoBehaviour
{
	public delegate void OriginShiftDelegate(Vector3 offset);

	private struct OriginShiftVector
	{
		public int x;

		public int y;

		public int z;

		public Vector3D toVector3D => new Vector3D(x, y, z);

		public OriginShiftVector(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	private List<Transform> transformsToShift = new List<Transform>(500);

	private List<Rigidbody> rigidbodiesToShift = new List<Rigidbody>(500);

	private List<IFloatingOriginShiftable> shiftables = new List<IFloatingOriginShiftable>(500);

	public const int cubeSize = 100;

	private static OriginShiftVector accumOffsetCube;

	public const string propertyName = "_GlobalOriginOffset";

	private int propertyId;

	private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

	private FloatingOriginUpdater updater;

	private Vector3 awaitingOffset;

	private const int NetShiftCubeSize = 5000;

	public static FloatingOrigin instance { get; private set; }

	public static Vector3D accumOffset { get; private set; }

	public event OriginShiftDelegate OnPreOriginShift;

	public event OriginShiftDelegate OnOriginShift;

	public event OriginShiftDelegate OnPostOriginShift;

	private void Awake()
	{
		instance = this;
		propertyId = Shader.PropertyToID("_GlobalOriginOffset");
		accumOffset = new Vector3D(0.0, 0.0, 0.0);
		Shader.SetGlobalVector(propertyId, Vector3.zero);
		accumOffsetCube = new OriginShiftVector(0, 0, 0);
		updater = base.gameObject.AddComponent<FloatingOriginUpdater>();
		updater.fo = this;
	}

	private void OnDestroy()
	{
		Shader.SetGlobalVector(propertyId, Vector3.zero);
		accumOffsetCube = new OriginShiftVector(0, 0, 0);
		accumOffset = new Vector3D(0.0, 0.0, 0.0);
	}

	public void GetCubeShiftVector(out int x, out int y, out int z)
	{
		x = accumOffsetCube.x;
		y = accumOffsetCube.y;
		z = accumOffsetCube.z;
	}

	public void ShiftOrigin(Vector3 centerOffset, bool immediate = false)
	{
		if (!updater.awaitingShift)
		{
			if (immediate)
			{
				Shift(centerOffset);
				return;
			}
			updater.awaitingShift = true;
			updater.awaitingOffset = centerOffset;
		}
	}

	private IEnumerator ShiftAtEndOfFrame(Vector3 centerOffset)
	{
		yield return waitForEndOfFrame;
		Shift(centerOffset);
	}

	public static Vector3 GlobalToWorldPoint(Vector3D globalPoint)
	{
		return (globalPoint - accumOffset).toVector3;
	}

	public static Vector3 GlobalToWorldPoint(Vector3 positionOffset, int gridX, int gridY, int gridZ)
	{
		return (new Vector3D(positionOffset) + 100f * new Vector3D(gridX, gridY, gridZ) - accumOffset).toVector3;
	}

	public static Vector3D WorldToGlobalPoint(Vector3 worldPoint)
	{
		return new Vector3D(worldPoint) + accumOffset;
	}

	private void Shift(Vector3 centerOffset)
	{
		if (this.OnPreOriginShift != null)
		{
			this.OnPreOriginShift(-centerOffset);
		}
		OriginShiftVector originShiftVector = new OriginShiftVector(Mathf.FloorToInt(centerOffset.x / 100f), Mathf.FloorToInt(centerOffset.y / 100f), Mathf.FloorToInt(centerOffset.z / 100f));
		centerOffset = originShiftVector.toVector3D.toVector3 * 100f;
		accumOffsetCube = new OriginShiftVector(accumOffsetCube.x + originShiftVector.x, accumOffsetCube.y + originShiftVector.y, accumOffsetCube.z + originShiftVector.z);
		accumOffset = accumOffsetCube.toVector3D * 100f;
		int count = transformsToShift.Count;
		bool flag = false;
		for (int i = 0; i < count; i++)
		{
			if (transformsToShift[i] == null)
			{
				flag = true;
			}
			else
			{
				transformsToShift[i].position -= centerOffset;
			}
		}
		int count2 = rigidbodiesToShift.Count;
		for (int j = 0; j < count2; j++)
		{
			if (rigidbodiesToShift[j] == null)
			{
				flag = true;
				continue;
			}
			rigidbodiesToShift[j].interpolation = RigidbodyInterpolation.None;
			if (rigidbodiesToShift[j].isKinematic)
			{
				Vector3 vector = rigidbodiesToShift[j].position - centerOffset + rigidbodiesToShift[j].velocity * Time.deltaTime;
				Vector3 position = vector - rigidbodiesToShift[j].velocity * Time.fixedDeltaTime;
				rigidbodiesToShift[j].position = position;
				rigidbodiesToShift[j].MovePosition(vector);
			}
			else
			{
				rigidbodiesToShift[j].position -= centerOffset;
			}
			rigidbodiesToShift[j].interpolation = RigidbodyInterpolation.Interpolate;
		}
		int count3 = shiftables.Count;
		for (int k = 0; k < count3; k++)
		{
			shiftables[k].OnFloatingOriginShift(-centerOffset);
		}
		if (flag)
		{
			transformsToShift.RemoveAll((Transform x) => x == null);
			rigidbodiesToShift.RemoveAll((Rigidbody x) => x == null);
		}
		if (this.OnOriginShift != null)
		{
			this.OnOriginShift(-centerOffset);
		}
		Shader.SetGlobalVector(propertyId, -accumOffset.toVector3);
		if (this.OnPostOriginShift != null)
		{
			this.OnPostOriginShift(-centerOffset);
		}
	}

	public void AddTransform(Transform t)
	{
		if ((bool)t && !transformsToShift.Contains(t))
		{
			transformsToShift.Add(t);
		}
	}

	public void RemoveTransform(Transform t)
	{
		if ((bool)t)
		{
			transformsToShift.Remove(t);
		}
	}

	public void AddRigidbody(Rigidbody rb)
	{
		if ((bool)rb && !rigidbodiesToShift.Contains(rb))
		{
			rigidbodiesToShift.Add(rb);
		}
	}

	public void RemoveRigidbody(Rigidbody rb)
	{
		if ((bool)rb)
		{
			rigidbodiesToShift.Remove(rb);
		}
	}

	public void AddQueuedFixedUpdateAction(UnityAction a)
	{
		updater.AddLateFixedUpdateAction(a);
	}

	public void AddShiftable(IFloatingOriginShiftable s)
	{
		shiftables.Add(s);
	}

	public void RemoveShiftable(IFloatingOriginShiftable s)
	{
		shiftables.Remove(s);
	}

	public static Vector3 NetToWorldPoint(Vector3 posOffset, int nsv)
	{
		DecodeNetShiftVector(nsv, out var x, out var y, out var z);
		return VTMapManager.GlobalToWorldPoint(5000f * new Vector3D(x, y, z) + new Vector3D(posOffset));
	}

	public static void WorldToNetPoint(Vector3 worldPos, out int nsv, out Vector3 offset)
	{
		Vector3D vector3D = VTMapManager.WorldToGlobalPoint(worldPos);
		Vector3 toVector = (vector3D / 5000f).toVector3;
		int num = Mathf.RoundToInt(toVector.x);
		int num2 = Mathf.RoundToInt(toVector.y);
		int num3 = Mathf.RoundToInt(toVector.z);
		offset = (vector3D - 5000f * new Vector3D(num, num2, num3)).toVector3;
		nsv = NetShiftVector(num, num2, num3);
	}

	public static int NetShiftVector(int x, int y, int z)
	{
		int num = (Math.Abs(x) << 20) & 0x3FF00000;
		num = ((x >= 0) ? (num & 0x1FF00000) : (num | 0x20000000));
		int num2 = (Math.Abs(y) << 10) & 0xFFC00;
		num2 = ((y >= 0) ? (num2 & 0x7FC00) : (num2 | 0x80000));
		int num3 = Math.Abs(z) & 0x3FF;
		num3 = ((z >= 0) ? (num3 & 0x1FF) : (num3 | 0x200));
		return num | num2 | num3;
	}

	public static void DecodeNetShiftVector(int v, out int x, out int y, out int z)
	{
		x = (v & 0x1FF00000) >> 20;
		if ((v & 0x20000000) == 536870912)
		{
			x = -x;
		}
		y = (v & 0x7FC00) >> 10;
		if ((v & 0x80000) == 524288)
		{
			y = -y;
		}
		z = v & 0x1FF;
		if ((v & 0x200) == 512)
		{
			z = -z;
		}
	}
}
