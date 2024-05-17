using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MassUpdater : MonoBehaviour
{
	public float baseMass;

	private Rigidbody rb;

	private List<Component> massObjects = new List<Component>();

	public float updateInterval = -1f;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void Start()
	{
		UpdateMassObjects();
	}

	private void OnEnable()
	{
		if (updateInterval > 0f)
		{
			StartCoroutine(IntervalUpdateRoutine());
		}
		else
		{
			StartCoroutine(ConstantUpdateRoutine());
		}
	}

	private IEnumerator ConstantUpdateRoutine()
	{
		yield return null;
		while (base.enabled)
		{
			UpdateMass();
			yield return null;
		}
	}

	private IEnumerator IntervalUpdateRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(updateInterval);
		yield return null;
		yield return new WaitForSeconds(Random.Range(0f, updateInterval));
		while (base.enabled)
		{
			UpdateMass();
			yield return wait;
		}
	}

	public void UpdateMassObjects()
	{
		massObjects.Clear();
		IMassObject[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IMassObject>();
		for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
		{
			Component component = (Component)componentsInChildrenImplementing[i];
			if ((bool)component)
			{
				massObjects.Add(component);
			}
		}
		UpdateMass();
	}

	public void RemoveMassObject(IMassObject o)
	{
		massObjects.Remove((Component)o);
	}

	private void UpdateMass()
	{
		float num = baseMass;
		int count = massObjects.Count;
		bool flag = false;
		for (int i = 0; i < count; i++)
		{
			if (massObjects[i] != null)
			{
				num += ((IMassObject)massObjects[i]).GetMass();
			}
			else
			{
				flag = true;
			}
		}
		if (flag)
		{
			massObjects.RemoveAll((Component x) => x == null);
		}
		if (!rb)
		{
			rb = GetComponent<Rigidbody>();
		}
		rb.mass = num;
	}

	[ContextMenu("Print MassObjects")]
	public void DebugPrintMassObjects()
	{
		Debug.Log("Mass Objects: ");
		foreach (Component massObject in massObjects)
		{
			if (massObject != null)
			{
				Debug.Log(massObject.gameObject.name + " (" + ((IMassObject)massObject).GetMass() + "t)");
			}
			else
			{
				Debug.Log("null");
			}
		}
		UpdateMass();
		Debug.Log($" = TOTAL: {rb.mass}t");
	}
}
