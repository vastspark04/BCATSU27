using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatEmitter : MonoBehaviour
{
	public static List<HeatEmitter> emitters = new List<HeatEmitter>();

	public float startHeat;

	private float _heat;

	public bool fwdAspect;

	private Vector3 _vel = Vector3.zero;

	public float cooldownRate;

	public Actor actor;

	public bool isCountermeasure;

	private float dirtyHeat;

	private static WaitForSeconds wait;

	private static float waitTime = 0.2f;

	public float heat => _heat;

	public Vector3 velocity
	{
		get
		{
			if (!actor)
			{
				return _vel;
			}
			return actor.velocity;
		}
		set
		{
			_vel = value;
		}
	}

	public void AddHeat(float addHeat)
	{
		dirtyHeat += addHeat;
	}

	private void Awake()
	{
		if (wait == null)
		{
			wait = new WaitForSeconds(waitTime);
		}
	}

	private IEnumerator UpdateRoutine()
	{
		yield return new WaitForSeconds(Random.Range(0f, waitTime));
		while (base.enabled)
		{
			_heat += dirtyHeat;
			dirtyHeat = 0f;
			_heat = Mathf.Lerp(heat, 0f, Mathf.Min(0.5f, VTOLVRConstants.GLOBAL_COOLDOWN_FACTOR * cooldownRate * waitTime));
			yield return wait;
		}
	}

	private void OnEnable()
	{
		emitters.Add(this);
		if (!actor)
		{
			actor = GetComponentInParent<Actor>();
		}
		_heat = startHeat;
		StartCoroutine(UpdateRoutine());
	}

	private void OnDisable()
	{
		emitters.Remove(this);
	}
}
