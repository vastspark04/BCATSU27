using System.Collections;
using UnityEngine;

public class SeatAdjuster : MonoBehaviour, IPersistentDataSaver
{
	public Battery battery;

	public Transform seatTransform;

	public float speed;

	public float maxHeight;

	public float minHeight;

	private float h;

	public Vector3 axis = Vector3.up;

	private Vector3 origPos;

	private bool moving;

	private int moveDir;

	private float rampUpMult;

	private EjectionSeat ejectionSeat;

	private void Start()
	{
		if (!seatTransform)
		{
			seatTransform = base.transform;
		}
		if (!battery)
		{
			battery = base.transform.root.GetComponentInChildren<Battery>(includeInactive: true);
		}
		ejectionSeat = GetComponent<EjectionSeat>();
		origPos = seatTransform.localPosition;
		axis = seatTransform.parent.InverseTransformDirection(seatTransform.TransformDirection(axis));
		if (PilotSaveManager.current != null)
		{
			h = Mathf.Clamp(PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).seatHeight, minHeight, maxHeight);
			seatTransform.localPosition = origPos + h * axis;
		}
	}

	private IEnumerator MoveRoutine()
	{
		while (moving)
		{
			if ((bool)ejectionSeat && ejectionSeat.ejected)
			{
				moving = false;
			}
			else if (!battery.Drain(0.01f * Time.deltaTime))
			{
				Stop();
			}
			else
			{
				rampUpMult = Mathf.MoveTowards(rampUpMult, 1f, Time.deltaTime);
				h += rampUpMult * speed * (float)moveDir * Time.deltaTime;
				h = Mathf.Clamp(h, minHeight, maxHeight);
				seatTransform.localPosition = origPos + h * axis;
			}
			yield return null;
		}
	}

	public void StartRaiseSeat()
	{
		if (battery.Drain(0.01f * Time.deltaTime))
		{
			moving = true;
			moveDir = 1;
			StartCoroutine(MoveRoutine());
		}
	}

	public void StartLowerSeat()
	{
		if (battery.Drain(0.01f * Time.deltaTime))
		{
			moving = true;
			moveDir = -1;
			StartCoroutine(MoveRoutine());
		}
	}

	public void Stop()
	{
		moving = false;
		rampUpMult = 0f;
	}

	public void SavePersistentData()
	{
		if (PilotSaveManager.current != null)
		{
			PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).seatHeight = h;
		}
	}
}
