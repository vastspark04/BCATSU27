using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SpatialTracking;

public class VRHead : MonoBehaviour
{
	public static Vector3 playAreaPosition = Vector3.zero;

	public static Quaternion playAreaRotation = Quaternion.identity;

	public bool useOffset;

	public float height = 1.1f;

	private Transform myTransform;

	public static VRHead instance { get; private set; }

	public static Vector3 position
	{
		get
		{
			if ((bool)instance)
			{
				return instance.myTransform.position;
			}
			return Vector3.zero;
		}
	}

	public Camera cam { get; private set; }

	public float fieldOfView
	{
		get
		{
			if ((bool)cam)
			{
				return cam.fieldOfView;
			}
			return 60f;
		}
	}

	public float stereoSeparation => cam.stereoSeparation;

	public static event UnityAction OnVRHeadChanged;

	private void Awake()
	{
		instance = this;
		myTransform = base.transform;
		cam = GetComponent<Camera>();
		TrackedPoseDriver component = GetComponent<TrackedPoseDriver>();
		if ((bool)component)
		{
			component.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Head);
		}
		if ((bool)cam)
		{
			float[] array = new float[32];
			array[0] = cam.farClipPlane;
			array[4] = cam.farClipPlane;
			cam.layerCullDistances = array;
			cam.layerCullSpherical = true;
			if (!GetComponent<CurrentCamera>())
			{
				base.gameObject.AddComponent<CurrentCamera>();
			}
		}
	}

	private void OnEnable()
	{
		OnVRHeadChanged += VRHeadChanged;
		if (VRHead.OnVRHeadChanged != null)
		{
			VRHead.OnVRHeadChanged();
		}
	}

	private void OnDisable()
	{
		OnVRHeadChanged -= VRHeadChanged;
		if (VRHead.OnVRHeadChanged != null)
		{
			VRHead.OnVRHeadChanged();
		}
	}

	private void VRHeadChanged()
	{
		if (base.enabled && base.gameObject.activeInHierarchy)
		{
			instance = this;
			if ((bool)GetComponent<AudioListener>())
			{
				GetComponent<AudioListener>().enabled = true;
			}
		}
	}

	private void Start()
	{
		if (useOffset)
		{
			base.transform.parent.localPosition = playAreaPosition;
			base.transform.parent.localRotation = playAreaRotation;
		}
	}

	public void DisableCameras()
	{
		Camera[] componentsInChildren = cam.GetComponentsInChildren<Camera>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
	}

	public static void ReCenter()
	{
		if ((bool)instance)
		{
			instance._ReCenter();
		}
	}

	public void _ReCenter()
	{
		Transform parent = base.transform.parent;
		if ((bool)parent && (bool)parent.parent)
		{
			Vector3 forward = parent.parent.InverseTransformVector(base.transform.forward);
			forward.y = 0f;
			Quaternion rotation = Quaternion.LookRotation(forward);
			parent.localRotation = Quaternion.Inverse(rotation) * parent.localRotation;
			Vector3 vector = (vector = parent.localRotation * Vector3.forward);
			parent.localRotation = Quaternion.LookRotation(vector, Vector3.up);
			playAreaRotation = parent.localRotation;
			Vector3 vector2 = parent.parent.InverseTransformPoint(base.transform.position);
			vector2.y -= height;
			parent.localPosition -= vector2;
			playAreaPosition = parent.localPosition;
			GameSettings.SaveGameSettings();
		}
	}
}
