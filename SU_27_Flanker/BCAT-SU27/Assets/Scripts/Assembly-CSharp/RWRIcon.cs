using UnityEngine;
using UnityEngine.UI;

public class RWRIcon : MonoBehaviour
{
	public Text labelText;

	public GameObject hatIcon;

	public GameObject diamondIcon;

	public GameObject halfCircleTop;

	public GameObject halfCircleBottom;

	public GameObject allIconsParent;

	public Color normalColor = Color.green;

	public Color missileColor = Color.red;

	public FixedPoint detectionPoint;

	private Image halfCircleImageTop;

	private Image halfCircleImageBottom;

	[HideInInspector]
	public Actor actor;

	[HideInInspector]
	public DashRWR rwr;

	private float timeFound;

	private float persistTime;

	private float timeFiredMissile;

	private bool firedMissile;

	private float hiddenPersistTime = 10f;

	private bool locked;

	private float timeLocked;

	private bool isLastDetected;

	private int actorID;

	private bool setupComplete;

	public Text debugText;

	public bool isLocked => locked;

	private void Awake()
	{
		if (!setupComplete)
		{
			Setup();
		}
		if ((bool)debugText)
		{
			Object.Destroy(debugText.gameObject);
		}
	}

	private void Setup()
	{
		halfCircleImageTop = halfCircleTop.GetComponent<Image>();
		halfCircleImageBottom = halfCircleBottom.GetComponent<Image>();
		diamondIcon.GetComponent<Image>().color = normalColor;
		hatIcon.GetComponent<Image>().color = normalColor;
		setupComplete = true;
	}

	public void UpdateStatus(Actor actor, float persistTime, string radarLabel)
	{
		this.actor = actor;
		actorID = actor.actorID;
		timeFound = Time.time;
		this.persistTime = persistTime;
		allIconsParent.SetActive(value: true);
		if (!setupComplete)
		{
			Setup();
		}
		if (actor.role == Actor.Roles.Missile)
		{
			labelText.text = "M";
			diamondIcon.SetActive(value: false);
			hatIcon.SetActive(value: false);
			halfCircleTop.SetActive(value: true);
			halfCircleBottom.SetActive(value: true);
			halfCircleImageTop.color = missileColor;
			halfCircleImageBottom.color = missileColor;
			labelText.color = missileColor;
			base.transform.SetAsLastSibling();
		}
		else
		{
			labelText.text = radarLabel;
			hatIcon.SetActive(actor.finalCombatRole == Actor.Roles.Air);
			halfCircleImageTop.color = normalColor;
			halfCircleImageBottom.color = normalColor;
			labelText.color = normalColor;
		}
	}

	public void SetLastDetected(bool lastDetected)
	{
		isLastDetected = lastDetected;
	}

	public void SetLocked()
	{
		locked = true;
		timeLocked = Time.time;
		timeFound = Time.time;
		allIconsParent.SetActive(value: true);
	}

	public void SetAsThreat(bool threat)
	{
		diamondIcon.SetActive(threat);
	}

	public void SetFiredMissile()
	{
		timeFiredMissile = Time.time;
		firedMissile = true;
	}

	private void Update()
	{
		if (Time.time - timeFound > hiddenPersistTime || actor == null)
		{
			rwr.RemovePing(actorID);
			SetAsThreat(threat: false);
			SetLastDetected(lastDetected: false);
			base.gameObject.SetActive(value: false);
			locked = false;
		}
		else if (Time.time - timeFound > persistTime)
		{
			allIconsParent.SetActive(value: false);
		}
		else
		{
			if (actor.finalCombatRole == Actor.Roles.Missile)
			{
				return;
			}
			if (locked)
			{
				halfCircleTop.SetActive(value: true);
				halfCircleBottom.SetActive(value: true);
				if (Time.time - timeLocked > 1f)
				{
					locked = false;
				}
			}
			else
			{
				halfCircleTop.SetActive(isLastDetected);
				halfCircleBottom.SetActive(value: false);
			}
			if (firedMissile)
			{
				if (Time.time - timeFiredMissile < 5f)
				{
					halfCircleTop.SetActive(value: true);
					bool active = Mathf.RoundToInt(Time.time * 6f) % 2 == 0;
					halfCircleBottom.SetActive(active);
				}
				else
				{
					firedMissile = false;
				}
			}
		}
	}
}
