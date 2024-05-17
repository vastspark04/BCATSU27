using UnityEngine;
using UnityEngine.UI;

public class HUDGunDirectorSight : MonoBehaviour
{
	public Transform pipperTransform;

	public Image rangeImage;

	public Transform targetBoxTransform;

	public Transform leadLineRotator;

	public Transform leadLine;

	public Transform fixedSightTransform;

	private FixedPoint targetPosition;

	private Vector3 targetVelocity;

	private bool targetLocked;

	[HideInInspector]
	public Vector3 gdsAimPoint;

	[HideInInspector]
	public Transform fireTransform;

	[HideInInspector]
	public float bulletVelocity;

	[HideInInspector]
	public HPEquippable weapon;

	public HUDGunFunnel gunFunnel;

	public bool useMagicTargetAcquisition = true;

	private Actor myActor;

	private Actor targetActor;

	private float lastTimeScanned;

	private float hudDepth;

	private Actor lastTgtActor;

	private Vector3[] velocities = new Vector3[8];

	private int velIdx;

	private float velocityInterval = 0.25f;

	private float lastVelTime;

	public bool isTargetLocked => targetLocked;

	
}
