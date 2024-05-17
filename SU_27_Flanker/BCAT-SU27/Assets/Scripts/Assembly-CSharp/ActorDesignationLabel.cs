using UnityEngine;
using UnityEngine.UI;

public class ActorDesignationLabel : MonoBehaviour
{
	public Actor actor;

	public VTText vtText;

	public Text text;

	public bool twoLine;

	private void Start()
	{
		if (!actor)
		{
			actor = GetComponentInParent<Actor>();
		}
		UpdateLabel();
		actor.OnSetDesignation += Actor_OnSetDesignation;
	}

	private void OnDestroy()
	{
		actor.OnSetDesignation -= Actor_OnSetDesignation;
	}

	private void Actor_OnSetDesignation(Actor.Designation obj)
	{
		UpdateLabel();
	}

	private void UpdateLabel()
	{
		if ((bool)vtText)
		{
			if (twoLine)
			{
				vtText.text = $"{actor.designation.letter}\n{actor.designation.num1} {actor.designation.num2}";
			}
			else
			{
				vtText.text = $"{actor.designation.letter} {actor.designation.num1} {actor.designation.num2}";
			}
			vtText.ApplyText();
		}
		if ((bool)text)
		{
			if (twoLine)
			{
				text.text = $"{actor.designation.letter}\n{actor.designation.num1}-{actor.designation.num2}";
			}
			else
			{
				text.text = $"{actor.designation.letter} {actor.designation.num1}-{actor.designation.num2}";
			}
		}
	}
}
