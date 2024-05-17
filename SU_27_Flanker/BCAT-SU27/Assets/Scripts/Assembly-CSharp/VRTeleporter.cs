using System.Collections;
using UnityEngine;

public class VRTeleporter : MonoBehaviour
{
	public Material lineMaterial;

	public float lineWidth = 0.1f;

	public float moveSpeed;

	public float arcSpeed;

	public int maxRayCount = 100;

	public float simDeltaTime = 0.1f;

	public bool instantTp;

	private bool drawing;

	private bool validPort = true;

	private LineRenderer lr;

	private Vector3 tpPosition;

	private Vector3 playAreaTpPos;

	public static bool canPort = true;

	private Transform playAreaTransform;

	private VRHandController controller;

	public float touchMoveSpeed = 6f;

	public float movementRayDist = 3f;

	private Collider tpCollider;

	private static MovingPlatform movingPlatform;

	private static VRTeleporter platformMover = null;

	private bool thumbstickMode;

	private bool waitingTurnReset;

	private void Start()
	{
		thumbstickMode = GameSettings.IsThumbstickMode();
		lr = new GameObject().AddComponent<LineRenderer>();
		lr.transform.parent = base.transform;
		lr.positionCount = maxRayCount;
		lr.material = lineMaterial;
		lr.startWidth = lineWidth;
		lr.endWidth = lineWidth;
		for (int i = 0; i < maxRayCount; i++)
		{
			lr.SetPosition(i, (float)i * 0.1f * Vector3.forward);
		}
		lr.gameObject.SetActive(value: false);
		controller = GetComponent<VRHandController>();
		controller.OnStickPressed += Controller_PadClicked;
		controller.OnStickUnpressed += Controller_PadUnclicked;
		playAreaTransform = controller.transform.parent;
		if ((bool)FloatingOrigin.instance)
		{
			FloatingOrigin.instance.OnOriginShift += OnOriginShift;
		}
		if (Physics.Raycast(VRHead.position, Vector3.down, out var hitInfo, 10f, 1))
		{
			movingPlatform = hitInfo.collider.GetComponent<MovingPlatform>();
		}
		platformMover = this;
	}

	private void OnOriginShift(Vector3 offset)
	{
		playAreaTpPos += offset;
	}

	private void Controller_PadUnclicked(VRHandController controller)
	{
		if (drawing)
		{
			drawing = false;
			lr.gameObject.SetActive(value: false);
			if (canPort && validPort)
			{
				Teleport();
			}
		}
	}

	private void Controller_PadClicked(VRHandController controller)
	{
		if (canPort && controller.triggerClicked)
		{
			drawing = true;
			lr.gameObject.SetActive(value: true);
		}
	}

	private void LateUpdate()
	{
		if (drawing)
		{
			lr.positionCount = maxRayCount;
			Vector3 vector = base.transform.forward * arcSpeed;
			Vector3 position = base.transform.position;
			lr.SetPosition(0, base.transform.position);
			tpCollider = null;
			for (int i = 1; i < maxRayCount; i++)
			{
				Vector3 vector2 = position;
				position += vector * simDeltaTime;
				if (Physics.Linecast(vector2, position, out var hitInfo, 16777217))
				{
					if (hitInfo.collider.gameObject.layer == 0)
					{
						validPort = true;
					}
					else
					{
						validPort = false;
					}
					lr.positionCount = i + 1;
					lr.SetPosition(i, hitInfo.point);
					tpPosition = hitInfo.point;
					tpCollider = hitInfo.collider;
					if ((bool)WaterPhysics.instance && tpPosition.y < WaterPhysics.instance.height)
					{
						Ray ray = new Ray(vector2, position - vector2);
						WaterPhysics.instance.waterPlane.Raycast(ray, out var enter);
						tpPosition = ray.GetPoint(enter);
					}
					if (Vector3.Angle(hitInfo.normal, Vector3.up) > 75f)
					{
						tpPosition += hitInfo.normal;
					}
					break;
				}
				lr.SetPosition(i, position);
				vector += Physics.gravity * simDeltaTime;
			}
		}
		if ((bool)movingPlatform && canPort && platformMover == this)
		{
			playAreaTransform.position += movingPlatform.GetVelocity(VRHead.position) * Time.deltaTime;
		}
		if (waitingTurnReset && ((thumbstickMode && Mathf.Abs(controller.stickAxis.x) < 0.08f) || (!thumbstickMode && !controller.stickPressed)))
		{
			waitingTurnReset = false;
		}
		if (controller.triggerClicked || (!controller.stickPressed && (controller.isLeft || !thumbstickMode)))
		{
			return;
		}
		if (!controller.isLeft)
		{
			if (!waitingTurnReset && (controller.stickPressDown || GameSettings.IsThumbstickMode()) && Mathf.Abs(controller.stickAxis.x) > 0.15f)
			{
				Quaternion quaternion = Quaternion.AngleAxis(Mathf.Sign(controller.stickAxis.x) * 45f, playAreaTransform.up);
				playAreaTransform.rotation = quaternion * playAreaTransform.rotation;
				waitingTurnReset = true;
			}
			return;
		}
		Vector2 stickAxis = controller.stickAxis;
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 right = base.transform.right;
		right.y = 0f;
		right.Normalize();
		Vector3 vector3 = forward * stickAxis.y + right * stickAxis.x;
		if (Physics.Raycast(new Ray(VRHead.instance.transform.position + vector3.normalized, Vector3.down), out var hitInfo2, movementRayDist, 16777217) && hitInfo2.collider.gameObject.layer == 0)
		{
			Vector3 vector4 = playAreaTransform.InverseTransformPoint(VRHead.instance.transform.position);
			vector4.y = 0f;
			Vector3 vector5 = playAreaTransform.TransformVector(vector4);
			Vector3 target = hitInfo2.point - vector5 + playAreaTransform.TransformVector(VRHead.playAreaPosition).y * Vector3.up;
			playAreaTransform.position = Vector3.MoveTowards(playAreaTransform.position, target, vector3.magnitude * touchMoveSpeed * Time.deltaTime);
		}
	}

	private void Teleport()
	{
		Vector3 vector = playAreaTransform.InverseTransformPoint(VRHead.instance.transform.position);
		vector.y = 0f;
		Vector3 vector2 = playAreaTransform.TransformVector(vector);
		playAreaTpPos = tpPosition - vector2 + playAreaTransform.TransformVector(VRHead.playAreaPosition).y * Vector3.up;
		if (instantTp)
		{
			playAreaTransform.position = playAreaTpPos;
		}
		else
		{
			StartCoroutine(TpRoutine());
		}
		if ((bool)tpCollider)
		{
			movingPlatform = tpCollider.GetComponent<MovingPlatform>();
		}
	}

	private IEnumerator TpRoutine()
	{
		canPort = false;
		while ((playAreaTransform.position - playAreaTpPos).sqrMagnitude > 0.01f)
		{
			playAreaTransform.position = Vector3.MoveTowards(playAreaTransform.position, playAreaTpPos, moveSpeed * Time.deltaTime);
			yield return null;
		}
		canPort = true;
	}
}
