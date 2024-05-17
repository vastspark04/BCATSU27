using System.Collections;
using UnityEngine;

public class ModuleParachute : MonoBehaviour, IParentRBDependent
{
	public Rigidbody rb;

	public GameObject chuteObject;

	public GameObject chutePackObject;

	public Transform dragPointTransform;

	public float preDeployDragDelay = 1.25f;

	public float preDeployDrag;

	public float fullDeployDrag;

	public float dragTransitionSpeed;

	public float fullDeployDelay;

	public Animator animator;

	public string fireAnimTrigger;

	public string fullDeployAnimTrigger;

	private bool fired;

	private bool isFullDeployed;

	private bool isPreDeployed;

	private bool chuteCut;

	private float currentDrag;

	private void Awake()
	{
		if (!dragPointTransform)
		{
			dragPointTransform = chuteObject.transform;
		}
	}

	private IEnumerator ChutePhysicsRoutine()
	{
		while (!chuteCut)
		{
			yield return new WaitForFixedUpdate();
			float target = 0f;
			if (isFullDeployed)
			{
				target = fullDeployDrag;
			}
			else if (isPreDeployed)
			{
				target = preDeployDrag;
			}
			currentDrag = Mathf.MoveTowards(currentDrag, target, dragTransitionSpeed * Time.fixedDeltaTime);
			Vector3 pointVelocity = rb.GetPointVelocity(dragPointTransform.position);
			Vector3 force = currentDrag * AerodynamicsController.fetch.AtmosDensityAtPosition(chuteObject.transform.position) * (0f - pointVelocity.magnitude) * pointVelocity;
			rb.AddForceAtPosition(force, dragPointTransform.position);
		}
	}

	public void FireParachute()
	{
		if (!fired)
		{
			StartCoroutine(ChuteRoutine());
		}
	}

	public void CutParachute()
	{
		if (!chuteCut)
		{
			chuteCut = true;
			chuteObject.SetActive(value: false);
		}
	}

	private IEnumerator ChuteRotationRoutine()
	{
		while (!chuteCut)
		{
			chuteObject.transform.rotation = Quaternion.LookRotation(-rb.velocity, chuteObject.transform.up);
			yield return null;
		}
	}

	private IEnumerator ChuteRoutine()
	{
		if ((bool)chutePackObject)
		{
			chutePackObject.SetActive(value: false);
		}
		chuteObject.SetActive(value: true);
		fired = true;
		animator.SetTrigger(fireAnimTrigger);
		StartCoroutine(ChuteRotationRoutine());
		StartCoroutine(ChutePhysicsRoutine());
		yield return new WaitForSeconds(preDeployDragDelay);
		isPreDeployed = true;
		yield return new WaitForSeconds(fullDeployDelay - preDeployDragDelay);
		isFullDeployed = true;
		animator.SetTrigger(fullDeployAnimTrigger);
	}

	public void SetParentRigidbody(Rigidbody rb)
	{
		this.rb = rb;
	}
}
