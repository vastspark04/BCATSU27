using System.Collections;
using UnityEngine;

public class VTEdWaypointRenderer : MonoBehaviour
{
	private Waypoint waypoint;

	private TextMesh text;

	public void Setup(VTScenarioEditor editor, Waypoint waypoint)
	{
		this.waypoint = waypoint;
		GameObject gameObject = new GameObject("Sprite");
		gameObject.transform.parent = base.transform;
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localScale = Vector3.one;
		SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
		VTScenarioEditor.EditorSprite waypointSprite = editor.waypointSprite;
		spriteRenderer.sprite = waypointSprite.sprite;
		spriteRenderer.color = waypointSprite.color;
		spriteRenderer.sharedMaterial = editor.spriteMaterial;
		IconScaleTest iconScaleTest = gameObject.AddComponent<IconScaleTest>();
		iconScaleTest.maxDistance = editor.spriteMaxDist;
		iconScaleTest.applyScale = true;
		iconScaleTest.directional = false;
		iconScaleTest.faceCamera = true;
		iconScaleTest.scale = waypointSprite.size * editor.globalSpriteScale;
		iconScaleTest.cameraUp = true;
		iconScaleTest.updateRoutine = true;
		iconScaleTest.enabled = false;
		iconScaleTest.enabled = true;
		GameObject gameObject2 = new GameObject("Label");
		text = gameObject2.AddComponent<TextMesh>();
		text.text = waypoint.name + " [" + waypoint.id + "]";
		gameObject2.transform.parent = gameObject.transform;
		gameObject2.transform.localPosition = new Vector3(0f, 0.062f / iconScaleTest.scale, 0f);
		gameObject2.transform.localRotation = Quaternion.identity;
		text.fontSize = editor.iconLabelFontSize;
		gameObject2.transform.localScale = 0.035f / iconScaleTest.scale * Vector3.one;
		text.anchor = TextAnchor.LowerCenter;
		text.color = spriteRenderer.color;
		StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		yield return new WaitForSeconds(Random.Range(0f, 0.2f));
		while (base.enabled)
		{
			text.text = waypoint.name + " [" + waypoint.id + "]";
			yield return new WaitForSeconds(0.2f);
		}
	}
}
