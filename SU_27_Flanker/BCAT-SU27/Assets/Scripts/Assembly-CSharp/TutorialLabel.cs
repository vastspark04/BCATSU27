using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialLabel : MonoBehaviour
{
	public Transform labelTextTransform;

	public float maxLabelAngle = 30f;

	public float labelDistance = 1000f;

	public float labelScale = 0.0066f;

	public float labelHeight = 15f;

	public LineRenderer lr;

	public float lineWidth = 1.5f;

	public float lineEndWidth = 0.3f;

	public Text text;

	public Transform textLinePoint;

	public AudioSource tutAudioSource;

	public AudioClip tutObjectiveClip;

	public AudioClip tutCompleteClip;

	public VideoPlayer videoPlayer;

	public RawImage videoPlayerImage;

	private Coroutine updateRoutine;

	private string lastVideoRelativeURL;

	private Coroutine videoRoutine;

	public int lineCharCount;

	private StringBuilder sb;

	private Vector3 lerpedLocal = Vector3.zero;

	public static TutorialLabel instance { get; private set; }

	private void Awake()
	{
		instance = this;
		HideLabel();
	}

	private void Start()
	{
		FlightSceneManager.instance.OnExitScene += HideLabel;
	}

	private IEnumerator UpdateRoutine(Transform lineTarget, float timeLimit)
	{
		float t = Time.time;
		while (base.enabled)
		{
			UpdateLabel(lineTarget);
			if (timeLimit > 0f && Time.time - t > timeLimit)
			{
				HideLabel();
				break;
			}
			yield return null;
		}
	}

	public void DisplayLabel(string labelText, Transform lineTarget, float timeLimit = -1f)
	{
		StopVideoPrepare();
		labelTextTransform.gameObject.SetActive(value: true);
		text.enabled = true;
		text.text = FormattedLabel(labelText);
		if (updateRoutine != null)
		{
			StopCoroutine(updateRoutine);
		}
		updateRoutine = StartCoroutine(UpdateRoutine(lineTarget, timeLimit));
	}

	public void DisplayLabelWithVideo(string labelText, Transform lineTarget, VTSVideoReference video, bool loop, float timeLimit = -1f)
	{
		if (videoPlayer.isPlaying && videoPlayerImage.enabled && lastVideoRelativeURL == video.relativeUrl)
		{
			labelTextTransform.gameObject.SetActive(value: true);
			text.enabled = true;
			text.text = FormattedLabel(labelText);
			if (updateRoutine != null)
			{
				StopCoroutine(updateRoutine);
			}
			updateRoutine = StartCoroutine(UpdateRoutine(lineTarget, timeLimit));
		}
		else
		{
			lastVideoRelativeURL = video.relativeUrl;
			StopVideoPrepare();
			videoRoutine = StartCoroutine(LabelWithVideoRoutine(labelText, lineTarget, video, loop, timeLimit));
		}
	}

	private void StopVideoPrepare()
	{
		if (videoRoutine != null)
		{
			StopCoroutine(videoRoutine);
		}
		if ((bool)videoPlayer)
		{
			if (videoPlayer.isPlaying)
			{
				videoPlayer.Stop();
			}
			videoPlayerImage.enabled = false;
		}
	}

	private IEnumerator LabelWithVideoRoutine(string labelText, Transform lineTarget, VTSVideoReference video, bool loop, float timeLimit)
	{
		if (updateRoutine != null)
		{
			StopCoroutine(updateRoutine);
		}
		labelTextTransform.gameObject.SetActive(value: true);
		text.enabled = false;
		videoPlayerImage.enabled = false;
		videoPlayer.gameObject.SetActive(value: true);
		if (videoPlayer.isPlaying)
		{
			videoPlayer.Stop();
		}
		videoPlayer.clip = null;
		yield return null;
		if (PilotSaveManager.currentCampaign.isBuiltIn)
		{
			videoPlayer.source = VideoSource.VideoClip;
			videoPlayer.clip = VTResources.GetBuiltInScenarioVideo(PilotSaveManager.currentCampaign.campaignID, PilotSaveManager.currentScenario.scenarioID, video.relativeUrl);
		}
		else
		{
			videoPlayer.source = VideoSource.Url;
			videoPlayer.url = "file://" + video.url;
		}
		videoPlayer.isLooping = loop;
		videoPlayer.Prepare();
		yield return null;
		while (!videoPlayer.isPrepared)
		{
			yield return null;
		}
		videoPlayerImage.enabled = true;
		videoPlayer.Play();
		labelTextTransform.gameObject.SetActive(value: true);
		text.enabled = true;
		text.text = FormattedLabel(labelText);
		if (updateRoutine != null)
		{
			StopCoroutine(updateRoutine);
		}
		updateRoutine = StartCoroutine(UpdateRoutine(lineTarget, timeLimit));
	}

	private string FormattedLabel(string s)
	{
		return s;
	}

	public void PlayTutObjectiveSound()
	{
		tutAudioSource.Stop();
		tutAudioSource.PlayOneShot(tutObjectiveClip);
	}

	public void PlayTutCompleteSound()
	{
		tutAudioSource.Stop();
		tutAudioSource.PlayOneShot(tutCompleteClip);
	}

	public void HideLabel()
	{
		if (updateRoutine != null)
		{
			StopCoroutine(updateRoutine);
		}
		labelTextTransform.gameObject.SetActive(value: false);
		if ((bool)lr)
		{
			lr.gameObject.SetActive(value: false);
		}
		if ((bool)videoPlayer)
		{
			if (videoPlayer.isPlaying)
			{
				videoPlayer.Stop();
			}
			videoPlayer.gameObject.SetActive(value: false);
		}
	}

	private void UpdateLabel(Transform lineTarget)
	{
		Vector3 vector2;
		if ((bool)lineTarget)
		{
			Vector3 target = lineTarget.position - VRHead.instance.transform.position;
			Vector3 vector = Vector3.RotateTowards(VRHead.instance.transform.forward, target, maxLabelAngle * ((float)Math.PI / 180f), float.MaxValue);
			vector2 = VRHead.instance.transform.position + vector;
		}
		else
		{
			vector2 = VRHead.instance.transform.position + VRHead.instance.transform.parent.TransformDirection(Quaternion.Inverse(VRHead.playAreaRotation) * Vector3.forward) * labelDistance;
		}
		float num = labelScale * Vector3.Distance(vector2, VRHead.instance.transform.position);
		if ((bool)lineTarget)
		{
			vector2 += labelHeight * num * VRHead.instance.transform.up;
		}
		lerpedLocal = Vector3.Lerp(lerpedLocal, VRHead.instance.transform.parent.InverseTransformPoint(vector2), 10f * Time.deltaTime);
		Vector3 position = VRHead.instance.transform.parent.TransformPoint(lerpedLocal);
		labelTextTransform.position = position;
		labelTextTransform.rotation = Quaternion.LookRotation(labelTextTransform.position - VRHead.instance.transform.position, VRHead.instance.transform.parent.up);
		labelTextTransform.localScale = num * Vector3.one;
		if ((bool)lr)
		{
			if ((bool)lineTarget)
			{
				lr.gameObject.SetActive(value: true);
				lr.startWidth = lineWidth * num;
				lr.endWidth = lineEndWidth * num;
				lr.SetPosition(0, textLinePoint.position);
				lr.SetPosition(1, lineTarget.position);
			}
			else
			{
				lr.gameObject.SetActive(value: false);
			}
		}
	}
}
