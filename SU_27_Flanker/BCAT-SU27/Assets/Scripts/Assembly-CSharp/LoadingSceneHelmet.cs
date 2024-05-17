using UnityEngine;

public class LoadingSceneHelmet : MonoBehaviour
{
	public Transform headTransform;

	public GameObject headHelmet;

	public float radius;

	private bool grabbed;

	public Transform radiusTf;

	public float returnRadius;

	public float antiGrav;

	public AudioSource equipAudioSource;

	private Vector3 startPosition;

	private Quaternion startRotation;

	private Rigidbody rb;

	private VRHandController c;

	private float returnTimer;

	private void Start()
	{
		VRInteractable component = GetComponent<VRInteractable>();
		component.OnStopInteraction += Ir_OnStopInteraction;
		component.OnStartInteraction += Ir_OnStartInteraction;
		rb = GetComponent<Rigidbody>();
		startPosition = base.transform.position;
		startRotation = base.transform.rotation;
	}

	private void Ir_OnStartInteraction(VRHandController controller)
	{
		c = controller;
		grabbed = true;
	}

	private void Ir_OnStopInteraction(VRHandController controller)
	{
		grabbed = false;
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)radiusTf)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(radiusTf.position, returnRadius);
		}
	}

	private void Update()
	{
		if ((grabbed || rb.velocity.sqrMagnitude > 0.1f) && Vector3.Distance(base.transform.position, headTransform.position) < radius)
		{
			if ((bool)c)
			{
				c.ReleaseFromInteractable();
			}
			headHelmet.SetActive(value: true);
			base.gameObject.SetActive(value: false);
			LoadingSceneController.instance.PlayerReady();
			equipAudioSource.Play();
		}
		if (grabbed)
		{
			return;
		}
		if ((base.transform.position - radiusTf.position).magnitude > returnRadius)
		{
			returnTimer += Time.deltaTime;
			if (returnTimer > 3f)
			{
				rb.velocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
				base.transform.position = startPosition;
				base.transform.rotation = startRotation;
				returnTimer = 0f;
			}
		}
		else
		{
			returnTimer = 0f;
		}
	}
}
