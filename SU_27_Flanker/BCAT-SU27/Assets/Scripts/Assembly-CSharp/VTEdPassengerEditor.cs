using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTEdPassengerEditor : MonoBehaviour
{
	public VTScenarioEditor editor;

	public ScrollRect seatsScrollRect;

	public GameObject seatTemplate;

	public GameObject passengerTemplate;

	public ScrollRect unitsScrollRect;

	public GameObject availableUnitTemplate;

	public Text limitText;

	private UnitSpawner unitSpawner;

	private List<GameObject> seatObjects = new List<GameObject>();

	private List<GameObject> passengersObjects = new List<GameObject>();

	private float seatsLineHeight;

	private List<GameObject> unitsObjects = new List<GameObject>();

	private float unitsLineHeight;

	private int maxPassengers;

	private List<UnitSpawner> currentPassengers = new List<UnitSpawner>();

	public void OpenForUnit(UnitSpawner uSpawner)
	{
		unitSpawner = uSpawner;
		base.gameObject.SetActive(value: true);
		maxPassengers = ((ICanHoldPassengers)uSpawner.prefabUnitSpawn).GetMaximumPassengers();
		seatTemplate.SetActive(value: false);
		passengerTemplate.SetActive(value: false);
		availableUnitTemplate.SetActive(value: false);
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("VTEdPassengerEditor");
		currentPassengers.Clear();
		foreach (UnitSpawner childSpawner in unitSpawner.childSpawners)
		{
			currentPassengers.Add(childSpawner);
		}
		SetupLists();
	}

	private void SetupLists()
	{
		seatsLineHeight = ((RectTransform)seatTemplate.transform).rect.height;
		foreach (GameObject seatObject in seatObjects)
		{
			Object.Destroy(seatObject);
		}
		seatObjects.Clear();
		foreach (GameObject passengersObject in passengersObjects)
		{
			Object.Destroy(passengersObject);
		}
		passengersObjects.Clear();
		for (int i = 0; i < maxPassengers; i++)
		{
			GameObject gameObject = Object.Instantiate(seatTemplate, seatsScrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * seatsLineHeight, 0f);
			seatObjects.Add(gameObject);
			gameObject.GetComponentInChildren<Text>().text = $"Seat {i + 1}:";
		}
		for (int j = 0; j < currentPassengers.Count; j++)
		{
			GameObject gameObject2 = Object.Instantiate(passengerTemplate, seatsScrollRect.content);
			gameObject2.SetActive(value: true);
			gameObject2.transform.localPosition = new Vector3(0f, (float)(-j) * seatsLineHeight, 0f);
			passengersObjects.Add(gameObject2);
			int pIdx = j;
			gameObject2.GetComponent<Button>().onClick.AddListener(delegate
			{
				RemovePassenger(currentPassengers[pIdx]);
			});
			gameObject2.GetComponentInChildren<Text>().text = currentPassengers[j].name;
		}
		seatsScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)maxPassengers * seatsLineHeight);
		seatsScrollRect.ClampVertical();
		foreach (GameObject unitsObject in unitsObjects)
		{
			Object.Destroy(unitsObject);
		}
		unitsObjects.Clear();
		unitsLineHeight = ((RectTransform)availableUnitTemplate.transform).rect.height;
		Dictionary<int, UnitSpawner> obj = ((unitSpawner.team == Teams.Allied) ? VTScenario.current.units.alliedUnits : VTScenario.current.units.enemyUnits);
		int num = 0;
		foreach (UnitSpawner unit in obj.Values)
		{
			if (!unit.prefabUnitSpawn.GetComponent<Soldier>() || currentPassengers.Contains(unit) || ((bool)unit.parentSpawner && unit.parentSpawner != unitSpawner))
			{
				continue;
			}
			GameObject gameObject3 = Object.Instantiate(availableUnitTemplate, unitsScrollRect.content);
			gameObject3.SetActive(value: true);
			gameObject3.transform.localPosition = new Vector3(0f, (float)(-num) * unitsLineHeight, 0f);
			if (currentPassengers.Count == maxPassengers)
			{
				gameObject3.GetComponent<Button>().interactable = false;
			}
			else
			{
				gameObject3.GetComponent<Button>().onClick.AddListener(delegate
				{
					AddPassenger(unit);
				});
			}
			gameObject3.GetComponentInChildren<Text>().text = unit.GetUIDisplayName();
			unitsObjects.Add(gameObject3);
			num++;
		}
		unitsScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num * unitsLineHeight);
		unitsScrollRect.ClampVertical();
		limitText.text = $"{currentPassengers.Count}/{maxPassengers}";
	}

	public void AddPassenger(UnitSpawner passenger)
	{
		currentPassengers.Add(passenger);
		SetupLists();
	}

	public void RemovePassenger(UnitSpawner passenger)
	{
		currentPassengers.Remove(passenger);
		SetupLists();
	}

	public void OkayButton()
	{
		ICanHoldPassengers canHoldPassengers = (ICanHoldPassengers)this.unitSpawner.prefabUnitSpawn;
		Transform transform = this.unitSpawner.prefabUnitSpawn.transform;
		this.unitSpawner.DetachAllChildren();
		for (int i = 0; i < currentPassengers.Count; i++)
		{
			UnitSpawner unitSpawner = currentPassengers[i];
			Vector3 worldPoint = this.unitSpawner.transform.TransformPoint(transform.InverseTransformPoint(canHoldPassengers.GetSeatTransform(i).position));
			unitSpawner.SetGlobalPosition(VTMapManager.WorldToGlobalPoint(worldPoint));
			this.unitSpawner.AttachChild(unitSpawner);
		}
		CancelButton();
	}

	public void CancelButton()
	{
		base.gameObject.SetActive(value: false);
		editor.UnblockEditor(base.transform);
		editor.editorCamera.inputLock.RemoveLock("VTEdPassengerEditor");
	}
}
