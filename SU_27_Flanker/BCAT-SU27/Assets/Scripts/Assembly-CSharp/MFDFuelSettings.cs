using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MFDFuelSettings : MonoBehaviour
{
	public FuelTank internalFuelTank;

	private MFDPage mfdPage;

	private VehicleMaster vm;

	private MeasurementManager measurements;

	[Header("Fuel Levels UI")]
	public RectTransform internalFuelBar;

	public Text internalFuelText;

	public Transform externalBar;

	public Text externalFuelText;

	public Transform bingoBar;

	[Header("Bingo Fuel UI")]
	public Text bingoFuelText;

	public Text bingoEstimationText;

	public float lowUsage;

	public float highUsage;

	public float bingoAdjStartRate;

	public float bingoAdjIncreaseRate;

	public float bingoAdjMaxRate;

	private Coroutine updateRoutine;

	private Coroutine bingoAdjRoutine;

	private void Awake()
	{
		vm = GetComponentInParent<VehicleMaster>();
		vm.OnSetNormBingoFuel += Vm_OnSetNormBingoFuel;
		measurements = GetComponentInParent<MeasurementManager>();
		mfdPage = GetComponent<MFDPage>();
		mfdPage.OnActivatePage.AddListener(OnActivatePage);
		mfdPage.OnDeactivatePage.AddListener(OnDeactivatePage);
	}

	private void OnActivatePage()
	{
		Vm_OnSetNormBingoFuel(vm.normBingoLevel);
		updateRoutine = StartCoroutine(UpdateRoutine());
	}

	private void OnDeactivatePage()
	{
		if (updateRoutine != null)
		{
			StopCoroutine(UpdateRoutine());
		}
	}

	private IEnumerator UpdateRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(0.2f);
		while (base.enabled)
		{
			UpdateFuelUI();
			yield return wait;
		}
	}

	private void UpdateFuelUI()
	{
		internalFuelBar.localScale = new Vector3(internalFuelTank.fuelFraction, 1f, 1f);
		internalFuelText.text = Mathf.RoundToInt(internalFuelTank.fuel).ToString();
		internalFuelTank.GetSubFuelInfo(out var subCurrFuel, out var subMaxFuel);
		float x = subCurrFuel / Mathf.Max(0.01f, subMaxFuel);
		externalBar.localScale = new Vector3(x, 1f, 1f);
		externalFuelText.text = Mathf.RoundToInt(subCurrFuel).ToString();
	}

	private void Vm_OnSetNormBingoFuel(float normBingoFuel)
	{
		Vector3 localPosition = bingoBar.localPosition;
		localPosition.x = internalFuelBar.localPosition.x + internalFuelBar.rect.width * normBingoFuel;
		bingoBar.localPosition = localPosition;
		float num = normBingoFuel * internalFuelTank.maxFuel;
		bingoFuelText.text = Mathf.Round(num).ToString();
		float distance = Mathf.Round(num * highUsage);
		float distance2 = Mathf.Round(num * lowUsage);
		string text = $"{measurements.FormattedDistance(distance)} - {measurements.FormattedDistance(distance2)}";
		bingoEstimationText.text = text;
	}

	public void PressBingoUp()
	{
		if (bingoAdjRoutine != null)
		{
			StopCoroutine(bingoAdjRoutine);
		}
		bingoAdjRoutine = StartCoroutine(BingoAdjustRoutine(1));
	}

	public void ReleaseBingoUp()
	{
		if (bingoAdjRoutine != null)
		{
			StopCoroutine(bingoAdjRoutine);
		}
	}

	public void PressBingoDown()
	{
		if (bingoAdjRoutine != null)
		{
			StopCoroutine(bingoAdjRoutine);
		}
		bingoAdjRoutine = StartCoroutine(BingoAdjustRoutine(-1));
	}

	public void ReleaseBingoDown()
	{
		if (bingoAdjRoutine != null)
		{
			StopCoroutine(bingoAdjRoutine);
		}
	}

	private IEnumerator BingoAdjustRoutine(int dir)
	{
		float adjRate = bingoAdjStartRate;
		while (base.enabled)
		{
			vm.normBingoLevel += (float)dir * adjRate * Time.deltaTime;
			adjRate = Mathf.Min(adjRate + bingoAdjIncreaseRate * Time.deltaTime, bingoAdjMaxRate);
			yield return null;
		}
	}
}
