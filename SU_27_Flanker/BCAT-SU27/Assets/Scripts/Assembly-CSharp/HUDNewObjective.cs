using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUDNewObjective : MonoBehaviour, ILocalizationUser
{
	public Actor actor;

	public AudioSource audioSource;

	public AudioClip notificationClip;

	public Text msgText;

	public float displayTime;

	public float bounceFactor;

	public float jumpInterval;

	public float jumpPower;

	public float gravity;

	public float fadeOutRate;

	private string s_newObj = "NEW OBJECTIVE";

	private string s_objComplete = "OBJECTIVE COMPLETE";

	private string s_objFailed = "OBJECTIVE FAILED";

	private float newMsgTime;

	private Coroutine notifRoutine;

	private Coroutine physRtn;

	private Transform msgTransform => msgText.transform;

	public void ApplyLocalization()
	{
		s_newObj = VTLocalizationManager.GetString("s_newObj", s_newObj, "Objective popup message in HUD");
		s_objComplete = VTLocalizationManager.GetString("s_objComplete", s_objComplete, "Objective popup message in HUD");
		s_objFailed = VTLocalizationManager.GetString("s_objFailed", s_objFailed, "Objective popup message in HUD");
	}

	private void Awake()
	{
		if (!actor)
		{
			actor = GetComponentInParent<Actor>();
		}
		ApplyLocalization();
	}

	private void Start()
	{
		if ((bool)MissionManager.instance)
		{
			MissionManager.instance.OnObjectiveRegistered += OnObjectiveRegistered;
			MissionManager.instance.OnObjectiveCompleted += OnObjectiveCompleted;
			MissionManager.instance.OnObjectiveFailed += OnObjectiveFailed;
		}
	}

	private void OnDestroy()
	{
		if ((bool)MissionManager.instance)
		{
			MissionManager.instance.OnObjectiveRegistered -= OnObjectiveRegistered;
			MissionManager.instance.OnObjectiveCompleted -= OnObjectiveCompleted;
			MissionManager.instance.OnObjectiveFailed -= OnObjectiveFailed;
		}
	}

	private void OnDisable()
	{
		msgTransform.gameObject.SetActive(value: false);
	}

	private void OnObjectiveRegistered(MissionObjective obj)
	{
		if (obj.team == actor.team)
		{
			newMsgTime = Time.time;
			DisplayNotif($"[ {s_newObj} ]\n{obj.objectiveName}");
		}
	}

	private void OnObjectiveCompleted(MissionObjective obj)
	{
		if (obj.team == actor.team && base.gameObject.activeInHierarchy)
		{
			StartCoroutine(DisplayIfNotNew($"[ {s_objComplete} ]\n{obj.objectiveName}"));
		}
	}

	private void OnObjectiveFailed(MissionObjective obj)
	{
		if (obj.team == actor.team && base.gameObject.activeInHierarchy)
		{
			StartCoroutine(DisplayIfNotNew($"[ {s_objFailed} ]\n{obj.objectiveName}"));
		}
	}

	private IEnumerator DisplayIfNotNew(string msg)
	{
		yield return new WaitForSeconds(1f);
		if (Time.time - newMsgTime > 2f)
		{
			DisplayNotif(msg);
		}
	}

	public void DisplayNotif(string msg)
	{
		if (notifRoutine != null)
		{
			StopCoroutine(notifRoutine);
		}
		if (physRtn != null)
		{
			StopCoroutine(physRtn);
		}
		audioSource.PlayOneShot(notificationClip);
		if (base.gameObject.activeInHierarchy)
		{
			msgText.text = msg;
			notifRoutine = StartCoroutine(NotifRoutine());
		}
	}

	private IEnumerator NotifRoutine()
	{
		msgTransform.localPosition = Vector3.zero;
		msgTransform.gameObject.SetActive(value: true);
		physRtn = StartCoroutine(PhysRoutine());
		Color clr = msgText.color;
		clr.a = 1f;
		msgText.color = clr;
		yield return new WaitForSeconds(displayTime);
		while (clr.a > 0f)
		{
			clr.a -= fadeOutRate * Time.deltaTime;
			msgText.color = clr;
			yield return null;
		}
		StopCoroutine(physRtn);
		msgTransform.gameObject.SetActive(value: false);
	}

	private IEnumerator PhysRoutine()
	{
		float vel = jumpPower;
		float pos = 0f;
		float kickTime = Time.time;
		while (true)
		{
			pos += vel * Time.deltaTime;
			vel -= gravity * Time.deltaTime;
			if (pos < 0f)
			{
				pos = 0f;
				if (Time.time - kickTime > jumpInterval)
				{
					kickTime = Time.time;
					vel = jumpPower;
				}
				else
				{
					vel = (0f - vel) * bounceFactor;
				}
			}
			msgTransform.localPosition = new Vector3(0f, pos, 0f);
			yield return null;
		}
	}
}
