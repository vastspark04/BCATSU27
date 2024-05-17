using System.Collections;
using UnityEngine;

public class BobbleHead : MonoBehaviour
{
	public GameObject headObject;

	public float headMass = 0.01f;

	public float linearLimit = 0.03f;

	public float angularLimit = 10f;

	public float positionSpring = 5f;

	public float positionDamper;

	public Vector3 positionTarget;

	public float rotationSpring = 0.05f;

	public Vector3 headAnchor;

	[Space]
	public bool testShaking;

	public float shakeRate;

	public float shakeIntensity;

	private Rigidbody shipRB;

	private Rigidbody bParentRB;

	private Vector3 rot;

	private Vector3 pos;

	private bool rdy;

	private IEnumerator Start()
	{
		if (!GameSettings.CurrentSettings.GetBoolSetting("SHOW_BOBBLEHEAD"))
		{
			base.gameObject.SetActive(value: false);
			yield break;
		}
		while (!FlightSceneManager.isFlightReady && (!VTMapManager.fetch || !VTMapManager.fetch.scenarioReady))
		{
			yield return null;
		}
		yield return new WaitForSeconds(1f);
		shipRB = GetComponentInParent<Rigidbody>();
		bParentRB = shipRB;
		rot = shipRB.rotation.eulerAngles;
		pos = shipRB.position;
		headObject.transform.parent = null;
		Rigidbody rigidbody = headObject.AddComponent<Rigidbody>();
		rigidbody.mass = headMass;
		rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		headObject.AddComponent<FloatingOriginTransform>().SetRigidbody(rigidbody);
		ConfigurableJoint configurableJoint = headObject.AddComponent<ConfigurableJoint>();
		configurableJoint.connectedBody = bParentRB;
		configurableJoint.anchor = headAnchor;
		configurableJoint.xMotion = ConfigurableJointMotion.Limited;
		configurableJoint.yMotion = ConfigurableJointMotion.Limited;
		configurableJoint.zMotion = ConfigurableJointMotion.Limited;
		configurableJoint.angularXMotion = ConfigurableJointMotion.Limited;
		configurableJoint.angularYMotion = ConfigurableJointMotion.Limited;
		configurableJoint.angularZMotion = ConfigurableJointMotion.Limited;
		configurableJoint.linearLimit = new SoftJointLimit
		{
			limit = linearLimit
		};
		SoftJointLimit softJointLimit = new SoftJointLimit
		{
			limit = angularLimit
		};
		SoftJointLimit softJointLimit3 = (configurableJoint.angularZLimit = softJointLimit);
		SoftJointLimit softJointLimit5 = (configurableJoint.angularYLimit = softJointLimit3);
		SoftJointLimit softJointLimit8 = (configurableJoint.highAngularXLimit = (configurableJoint.lowAngularXLimit = softJointLimit5));
		JointDrive jointDrive = new JointDrive
		{
			positionSpring = positionSpring,
			positionDamper = positionDamper,
			maximumForce = float.MaxValue
		};
		JointDrive jointDrive3 = (configurableJoint.zDrive = jointDrive);
		JointDrive jointDrive6 = (configurableJoint.xDrive = (configurableJoint.yDrive = jointDrive3));
		configurableJoint.rotationDriveMode = RotationDriveMode.Slerp;
		configurableJoint.slerpDrive = new JointDrive
		{
			positionSpring = rotationSpring,
			maximumForce = float.MaxValue
		};
		configurableJoint.projectionMode = JointProjectionMode.PositionAndRotation;
		configurableJoint.targetPosition = -positionTarget;
		rdy = true;
		if (testShaking)
		{
			bParentRB.isKinematic = true;
		}
	}

	private void OnDestroy()
	{
		if ((bool)headObject && Application.isPlaying)
		{
			Object.Destroy(headObject);
		}
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)headObject)
		{
			Gizmos.DrawSphere(headObject.transform.TransformPoint(headAnchor), linearLimit);
		}
	}
}
