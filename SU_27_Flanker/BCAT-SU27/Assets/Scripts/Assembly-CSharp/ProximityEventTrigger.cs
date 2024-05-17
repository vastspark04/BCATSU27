using UnityEngine;
using UnityEngine.Events;

public class ProximityEventTrigger : MonoBehaviour
{
	public float radius;

	private float sqrRadius;

	public bool triggerOnPlayer;

	public Transform[] triggeringTransforms;

	public bool planarRadius;

	public UnityEvent OnTrigger;

	private bool triggered;

	private void Start()
	{
		sqrRadius = radius * radius;
	}

	private void Update()
	{
		if (triggered || !FlightSceneManager.isFlightReady)
		{
			return;
		}
		Vector3 position = base.transform.position;
		if (triggerOnPlayer)
		{
			Vector3 position2 = FlightSceneManager.instance.playerActor.position;
			if (planarRadius)
			{
				position.y = (position2.y = 0f);
			}
			if ((position - position2).sqrMagnitude < sqrRadius)
			{
				if (OnTrigger != null)
				{
					OnTrigger.Invoke();
				}
				triggered = true;
				return;
			}
		}
		for (int i = 0; i < triggeringTransforms.Length; i++)
		{
			Vector3 position3 = triggeringTransforms[i].position;
			if (planarRadius)
			{
				position.y = (position3.y = 0f);
			}
			if ((position - position3).sqrMagnitude < sqrRadius)
			{
				if (OnTrigger != null)
				{
					OnTrigger.Invoke();
				}
				triggered = true;
				break;
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (planarRadius)
		{
			int num = 32;
			float num2 = 360f / (float)num;
			float num3 = 2000f;
			for (int i = 0; i < num; i++)
			{
				int num4 = (i + 1) % num;
				Vector3 vector = Quaternion.AngleAxis(num2 * (float)i, Vector3.up) * new Vector3(0f, 0f, radius);
				Vector3 vector2 = Quaternion.AngleAxis(num2 * (float)num4, Vector3.up) * new Vector3(0f, 0f, radius);
				Gizmos.DrawLine(base.transform.position + vector + num3 * Vector3.up, base.transform.position + vector2 + num3 * Vector3.up);
				Gizmos.DrawLine(base.transform.position + vector, base.transform.position + vector2);
				Gizmos.DrawLine(base.transform.position + vector, base.transform.position + vector + num3 * Vector3.up);
			}
		}
		else
		{
			Gizmos.DrawWireSphere(base.transform.position, radius);
		}
	}
}
