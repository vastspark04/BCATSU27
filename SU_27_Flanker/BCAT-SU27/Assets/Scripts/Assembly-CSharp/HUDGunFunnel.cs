using System.Collections;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class HUDGunFunnel : MonoBehaviour
{
	

	public CollimatedHUDUI hud;

	public GameObject displayObj;

	public UILineRenderer3D leftLine;

	public UILineRenderer3D rightLine;

	public WeaponManager wm;

	public Transform ccipTf;

	public float wingspan = 14f;

	public float minRange = 100f;

	public int rangeCount = 7;

	public float rangeInterval = 100f;

	public float minRotationOffset = 2f;

	private Vector3[] leftPoints;

	private Vector3[] rightPoints;

	public bool defaultActive;

	

	private bool active;

	private float lastRecTime;

	private Coroutine activeRoutine;

	public bool isActive => active;

	
}
