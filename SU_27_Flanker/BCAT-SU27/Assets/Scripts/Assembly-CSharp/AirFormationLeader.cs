using System.Collections.Generic;
using UnityEngine;

public class AirFormationLeader : MonoBehaviour, IQSVehicleComponent
{
	public enum FormationTypes
	{
		Vee,
		InvertVee,
		Spread,
		Echelon
	}

	public Actor actor;

	public float initialLag;

	public float lag = 15f;

	public float spread = 35f;

	private Transform[] formationTransforms = new Transform[50];

	private Dictionary<AIPilot, Transform> followers = new Dictionary<AIPilot, Transform>();

	public FormationTypes formationType;

	public bool HasFollowers()
	{
		return followers.Count > 0;
	}

	public int GetFormationIdx(Transform formationTf)
	{
		for (int i = 0; i < formationTransforms.Length; i++)
		{
			if (formationTransforms[i] == null)
			{
				return -1;
			}
			if (formationTransforms[i] == formationTf)
			{
				return i;
			}
		}
		return -1;
	}

	private void Awake()
	{
		if (!actor)
		{
			actor = GetComponent<Actor>();
		}
	}

	public Transform RegisterFollower(AIPilot pilot, out int transformIdx)
	{
		if (followers.ContainsKey(pilot))
		{
			Transform transform = followers[pilot];
			int num = (transformIdx = GetFormationIdx(transform));
			return transform;
		}
		Transform transform2 = new GameObject("FormationTransform").transform;
		followers.Add(pilot, transform2);
		transformIdx = -1;
		int i;
		for (i = 0; i < formationTransforms.Length; i++)
		{
			if (formationTransforms[i] == null)
			{
				formationTransforms[i] = transform2;
				transformIdx = i;
				break;
			}
		}
		transform2.parent = base.transform;
		transform2.localRotation = Quaternion.identity;
		transform2.localPosition = GetLocalFormationPosition(i);
		return transform2;
	}

	public Transform RegisterFollower(AIPilot pilot)
	{
		if (followers.ContainsKey(pilot))
		{
			return followers[pilot];
		}
		Transform transform = new GameObject("FormationTransform").transform;
		followers.Add(pilot, transform);
		int i;
		for (i = 0; i < formationTransforms.Length; i++)
		{
			if (formationTransforms[i] == null)
			{
				formationTransforms[i] = transform;
				break;
			}
		}
		transform.parent = base.transform;
		transform.localRotation = Quaternion.identity;
		transform.localPosition = GetLocalFormationPosition(i);
		return transform;
	}

	private void QS_RegisterFollower(AIPilot pilot, int idx)
	{
		if (followers.ContainsKey(pilot))
		{
			pilot.StopFollowingLeader();
		}
		Transform transform = new GameObject("FormationTransform").transform;
		followers.Add(pilot, transform);
		if (formationTransforms[idx] == null)
		{
			formationTransforms[idx] = transform;
			transform.parent = base.transform;
			transform.localRotation = Quaternion.identity;
			transform.localPosition = GetLocalFormationPosition(idx);
			pilot.FormOnPilot(this);
		}
		else
		{
			Debug.Log("Tried to quickload a follower but their formation idx is already used: " + pilot.gameObject.name, pilot.gameObject);
		}
	}

	public void UnregisterFollower(AIPilot pilot)
	{
		if (followers.ContainsKey(pilot))
		{
			Transform formationTf = followers[pilot];
			int formationIdx = GetFormationIdx(formationTf);
			if (formationIdx >= 0)
			{
				Object.Destroy(formationTransforms[formationIdx].gameObject);
				formationTransforms[formationIdx] = null;
			}
			followers.Remove(pilot);
		}
	}

	private Vector3 GetLocalFormationPosition(int index)
	{
		float num = index;
		num += 1f;
		float num2 = ((index % 2 != 0) ? 1 : (-1));
		float num3 = Mathf.Ceil(num / 2f);
		if (formationType == FormationTypes.Echelon)
		{
			num2 = 1f;
			num3 = num;
		}
		float x = num2 * num3 * spread;
		float num4 = num3 * lag * -1f;
		num4 -= initialLag;
		if (formationType == FormationTypes.InvertVee)
		{
			num4 = 0f - num4;
		}
		if (formationType == FormationTypes.Spread)
		{
			num4 = 0f;
		}
		return new Vector3(x, 0f, num4);
	}

	public void SetFormationType(FormationTypes t)
	{
		formationType = t;
		UpdateFormationPositions();
	}

	public void SetFormationType(int t)
	{
		SetFormationType((FormationTypes)t);
	}

	public void SetSpread(float sprd)
	{
		spread = sprd;
		UpdateFormationPositions();
	}

	private void UpdateFormationPositions()
	{
		for (int i = 0; i < formationTransforms.Length; i++)
		{
			if ((bool)formationTransforms[i])
			{
				formationTransforms[i].localPosition = GetLocalFormationPosition(i);
			}
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		if (followers.Count <= 0)
		{
			return;
		}
		ConfigNode configNode = new ConfigNode("FormationFollowers");
		qsNode.AddNode(configNode);
		foreach (KeyValuePair<AIPilot, Transform> follower in followers)
		{
			if (follower.Key != null)
			{
				int formationIdx = GetFormationIdx(follower.Value);
				if (formationIdx >= 0)
				{
					ConfigNode configNode2 = new ConfigNode("follower");
					configNode.AddNode(configNode2);
					configNode2.AddNode(QuicksaveManager.SaveActorIdentifierToNode(follower.Key.actor, "pilotActor"));
					configNode2.SetValue("idx", formationIdx);
				}
			}
		}
		configNode.SetValue("spread", spread);
		configNode.SetValue("lag", lag);
		configNode.SetValue("initialLag", initialLag);
		configNode.SetValue("formationType", formationType);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("FormationFollowers");
		if (node == null)
		{
			return;
		}
		List<AIPilot> list = new List<AIPilot>();
		foreach (AIPilot key in followers.Keys)
		{
			list.Add(key);
			if ((bool)followers[key])
			{
				Object.Destroy(followers[key].gameObject);
			}
		}
		foreach (AIPilot item in list)
		{
			item.StopFollowingLeader();
		}
		followers.Clear();
		for (int i = 0; i < formationTransforms.Length; i++)
		{
			if ((bool)formationTransforms[i])
			{
				Object.Destroy(formationTransforms[i].gameObject);
			}
			formationTransforms[i] = null;
		}
		foreach (ConfigNode node2 in node.GetNodes("follower"))
		{
			Actor actor = QuicksaveManager.RetrieveActorFromNode(node2.GetNode("pilotActor"));
			if ((bool)actor)
			{
				AIPilot component = actor.GetComponent<AIPilot>();
				int value = node2.GetValue<int>("idx");
				QS_RegisterFollower(component, value);
			}
			else
			{
				Debug.Log("Tried to quickload register a follower but their actor is missing...");
			}
		}
		spread = node.GetValue<float>("spread");
		lag = node.GetValue<float>("lag");
		initialLag = node.GetValue<float>("initialLag");
		FormationTypes value2 = node.GetValue<FormationTypes>("formationType");
		SetFormationType(value2);
	}
}
