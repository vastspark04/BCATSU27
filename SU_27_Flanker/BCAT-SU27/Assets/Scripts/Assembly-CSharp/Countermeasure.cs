using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Countermeasure : MonoBehaviour
{
	public Battery battery;

	public CountermeasureManager manager;

	public int maxCount;

	private int[] counts = new int[2];

	public Text countText;

	private bool isRemote;

	public int count
	{
		get
		{
			return leftCount + rightCount;
		}
		set
		{
			SetCount(value);
		}
	}

	public int leftCount
	{
		get
		{
			return counts[0];
		}
		set
		{
			counts[0] = value;
		}
	}

	public int rightCount
	{
		get
		{
			return counts[1];
		}
		set
		{
			counts[1] = value;
		}
	}

	public event Action OnFiredCM;

	public event Action<int> OnCountUpdated;

	protected virtual void Awake()
	{
		if (!QuicksaveManager.isQuickload)
		{
			count = maxCount;
		}
		UpdateCountText();
	}

	public bool FireCM()
	{
		if (count < 1)
		{
			return false;
		}
		OnFireCM();
		UpdateCountText();
		if (this.OnFiredCM != null)
		{
			this.OnFiredCM();
		}
		return true;
	}

	public void UpdateCountText()
	{
		if ((bool)countText)
		{
			countText.text = count.ToString();
			if (count < 1)
			{
				countText.color = Color.red;
			}
			else if (count < 10)
			{
				countText.color = Color.yellow;
			}
			else
			{
				countText.color = Color.green;
			}
		}
		this.OnCountUpdated?.Invoke(count);
	}

	public void SetCount(int c)
	{
		int num = maxCount / 2;
		int num2 = c - count;
		if (num2 > 0)
		{
			int num3 = 0;
			if (rightCount < leftCount)
			{
				num3 = 1;
			}
			while (num2 > 0 && count < maxCount)
			{
				if (counts[num3] < num)
				{
					counts[num3]++;
					num2--;
				}
				num3 = (num3 + 1) % 2;
			}
		}
		else if (num2 < 0)
		{
			num2 = -num2;
			int num4 = 0;
			if (rightCount > leftCount)
			{
				num4 = 1;
			}
			while (num2 > 0 && count > 0)
			{
				if (counts[num4] > 0)
				{
					counts[num4]--;
					num2--;
				}
				num4 = (num4 + 1) % 2;
			}
		}
		UpdateCountText();
	}

	public bool ConsumeCM(int side)
	{
		if (isRemote)
		{
			return true;
		}
		if (counts[side] > 0)
		{
			counts[side]--;
			UpdateCountText();
			return true;
		}
		return false;
	}

	public void SetNormalizedCount(float n)
	{
		int num = Mathf.RoundToInt(n * (float)maxCount);
		SetCount(num);
	}

	private void OnEnable()
	{
		StartCoroutine(BattRoutine());
	}

	private IEnumerator BattRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(1f);
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 1f));
		while (base.enabled && (bool)countText && (bool)battery)
		{
			countText.enabled = battery.Drain(0.01f * Time.deltaTime);
			yield return wait;
		}
	}

	protected virtual void OnFireCM()
	{
	}

	public void SetEnabled(int e)
	{
		if ((bool)manager)
		{
			manager.SetCM(manager.countermeasures.IndexOf(this), e);
		}
		else
		{
			base.enabled = e > 0;
		}
	}

	public void SetRemote()
	{
		isRemote = true;
	}
}
