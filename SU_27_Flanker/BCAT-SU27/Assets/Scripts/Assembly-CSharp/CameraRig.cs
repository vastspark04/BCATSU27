using UnityEngine;

public class CameraRig : EMUI
{
	private Quaternion defaultRot;

	private Vector3 defaultPos = new Vector3(0f, 0f, 0f);

	public GameObject cam;

	public float zoomSens = 3f;

	public float rotSens = 6f;

	private Vector3 pos_old = new Vector3(0f, 0f, 0f);

	private GameObject pos_new;

	private bool m_UILockInstigator;

	private void Start()
	{
		defaultRot = base.transform.rotation;
		pos_new = new GameObject("pos_new");
		pos_new.transform.SetParent(base.transform);
		pos_new.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z);
		Quaternion rotation = cam.transform.rotation;
		pos_new.transform.rotation = rotation;
		defaultPos = pos_new.transform.position;
	}

	private void Update()
	{
		if (Input.GetAxis("Mouse ScrollWheel") != 0f)
		{
			float num = Input.GetAxis("Mouse ScrollWheel") * 6f;
			pos_new.transform.Translate(Vector3.forward * num);
		}
		pos_old = cam.transform.position;
		pos_old = Vector3.Lerp(pos_old, pos_new.transform.position, zoomSens * Time.deltaTime * 0.2f);
		cam.transform.position = pos_old;
		if (CheckGUI(0, ref m_UILockInstigator))
		{
			base.transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * rotSens, Space.World);
			base.transform.Rotate(Vector3.left * Input.GetAxis("Mouse Y") * rotSens, Space.Self);
		}
	}

	public void ResetTransform()
	{
		base.transform.rotation = defaultRot;
		pos_new.transform.position = defaultPos;
	}
}
