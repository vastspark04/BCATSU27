using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTMapEdRoadsPanel : VTEdUITab
{
	private enum EditStates
	{
		Idle,
		WaitingStart,
		WaitingMid,
		WaitingEnd,
		Constructing,
		Deleting
	}

	public VTMapEditor editor;

	public ScrollRect scrollRect;

	public GameObject listItemTemplate;

	private float lineHeight;

	public GameObject placementButton;

	public Text placementStatusText;

	public GameObject cancelButton;

	[Header("Preview")]
	public LineRenderer previewLR;

	public LineRenderer bezPreviewLR;

	public SpriteRenderer roadCursor;

	public Gradient goodPlacementColor;

	public Gradient badPlacementColor;

	public LineRenderer roadHeightLine;

	private SpriteRenderer surfaceCursor;

	private List<GameObject> listObjs = new List<GameObject>();

	private EditStates state;

	private BezierRoadProfile currentRoadProfile;

	private int currRoadProfileIdx;

	private float surfaceHeight;

	private BezierRoadSystem roadSystem;

	private BezierRoadSystem.BezierRoadSegment delSegment;

	private Coroutine deletingRoutine;

	private Coroutine placementRoutine;

	private bool placementValid;

	private FixedPoint startPt;

	private FixedPoint midPt;

	private FixedPoint endPt;

	private Vector3[] previewLrPoints = new Vector3[16];

	private BezierRoadSystem.RoadSnapInfo startSnapInfo;

	private BezierRoadSystem.RoadSnapInfo endSnapInfo;

	private float surfaceHeightAdjustSpeed = 1000f;

	private float maxSurfaceHeight = 500f;

	private void Start()
	{
		surfaceCursor = Object.Instantiate(roadCursor.gameObject).GetComponent<SpriteRenderer>();
		surfaceCursor.gameObject.SetActive(value: false);
	}

	public override void OnOpenedTab()
	{
		base.OnOpenedTab();
		roadSystem = ((VTMapCustom)VTMapManager.fetch.map).roadSystem;
		state = EditStates.Idle;
		placementButton.SetActive(value: false);
		cancelButton.SetActive(value: false);
		SetupList();
	}

	private void SetupList()
	{
		foreach (GameObject listObj in listObjs)
		{
			Object.Destroy(listObj);
		}
		listObjs.Clear();
		lineHeight = ((RectTransform)listItemTemplate.transform).rect.height;
		for (int i = 0; i < VTMapRoadsBezier.instance.roadProfiles.Length; i++)
		{
			BezierRoadProfile bezierRoadProfile = VTMapRoadsBezier.instance.roadProfiles[i];
			GameObject gameObject = Object.Instantiate(listItemTemplate, scrollRect.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			VTMapEdRoadListItem component = gameObject.GetComponent<VTMapEdRoadListItem>();
			component.nameText.text = bezierRoadProfile.profileName;
			component.descriptionText.text = bezierRoadProfile.profileDescription;
			component.thumbImage.texture = bezierRoadProfile.thumbnail;
			component.idx = i;
			component.roadPanel = this;
			listObjs.Add(gameObject);
		}
		listItemTemplate.SetActive(value: false);
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)VTMapRoadsBezier.instance.roadProfiles.Length * lineHeight);
		scrollRect.ClampVertical();
	}

	public void SelectRoadSet(int idx)
	{
		if (state == EditStates.Deleting)
		{
			CancelDeleteButton();
		}
		placementButton.SetActive(value: true);
		cancelButton.SetActive(value: true);
		currentRoadProfile = VTMapRoadsBezier.instance.roadProfiles[idx];
		currRoadProfileIdx = idx;
		float radius = currentRoadProfile.radius;
		SetLRWidths(radius * 2f);
		roadCursor.transform.localScale = 2f * radius * Vector3.one;
		roadCursor.gameObject.SetActive(value: true);
		state = EditStates.WaitingStart;
		placementRoutine = StartCoroutine(PlaceRoadRoutine());
	}

	private void SetLRWidths(float width)
	{
		LineRenderer lineRenderer = previewLR;
		LineRenderer lineRenderer2 = previewLR;
		LineRenderer lineRenderer3 = bezPreviewLR;
		float num2 = (bezPreviewLR.endWidth = width);
		float num4 = (lineRenderer3.startWidth = num2);
		float num7 = (lineRenderer.startWidth = (lineRenderer2.endWidth = num4));
	}

	public void DeleteButton()
	{
		state = EditStates.Deleting;
		placementButton.gameObject.SetActive(value: true);
		placementStatusText.text = "Select road segments to delete.";
		cancelButton.SetActive(value: true);
		deletingRoutine = StartCoroutine(DeletingRoutine());
	}

	private IEnumerator DeletingRoutine()
	{
		previewLR.colorGradient = badPlacementColor;
		previewLR.positionCount = previewLrPoints.Length;
		while (state == EditStates.Deleting)
		{
			if (CursorRaycast(out var hit, out var ray))
			{
				BezierRoadSystem.BezierRoadSegment closestSegment = roadSystem.GetClosestSegment(hit.point, ray);
				if (closestSegment != null)
				{
					previewLR.gameObject.SetActive(value: true);
					if (delSegment != closestSegment)
					{
						delSegment = closestSegment;
						SetLRWidths(VTMapRoadsBezier.instance.roadProfiles[delSegment.type].radius * 2f);
					}
					for (int i = 0; i < previewLrPoints.Length; i++)
					{
						float t = (float)i / (float)(previewLrPoints.Length - 1);
						Vector3D point = delSegment.curve.GetPoint(t);
						previewLrPoints[i] = VTMapManager.GlobalToWorldPoint(point);
					}
					previewLR.SetPositions(previewLrPoints);
				}
				else
				{
					previewLR.gameObject.SetActive(value: false);
					delSegment = null;
				}
			}
			else
			{
				previewLR.gameObject.SetActive(value: false);
				delSegment = null;
			}
			yield return null;
		}
		placementButton.gameObject.SetActive(value: false);
	}

	public void CancelDeleteButton()
	{
		if (deletingRoutine != null)
		{
			StopCoroutine(deletingRoutine);
		}
		state = EditStates.Idle;
		previewLR.gameObject.SetActive(value: false);
		placementButton.gameObject.SetActive(value: false);
		cancelButton.SetActive(value: false);
	}

	private void StartRoad(Vector3 worldPoint)
	{
		startPt = new FixedPoint(worldPoint);
		if (startSnapInfo.snapped)
		{
			startPt.point = startSnapInfo.worldSnapPoint;
			if (Physics.Raycast(startPt.point + new Vector3(0f, 2000f, 0f), Vector3.down, out var hitInfo, 4000f, 1))
			{
				surfaceHeight = (startPt.point - hitInfo.point).magnitude;
			}
		}
		state = EditStates.WaitingMid;
		roadCursor.gameObject.SetActive(value: false);
	}

	public void OnClickWorld()
	{
		if (state == EditStates.WaitingStart)
		{
			if (placementValid && CursorRaycast(out var hit))
			{
				StartRoad(hit.point + surfaceHeight * Vector3.up);
			}
		}
		else if (state == EditStates.WaitingMid)
		{
			if (!placementValid || !CursorRaycast(out var hit2))
			{
				return;
			}
			if (Input.GetKey(KeyCode.LeftShift))
			{
				endPt = new FixedPoint(hit2.point + surfaceHeight * Vector3.up);
				if (endSnapInfo.snapped)
				{
					endPt.point = endSnapInfo.worldSnapPoint;
				}
				midPt = new FixedPoint(Vector3.Lerp(startPt.point, endPt.point, 0.5f));
				state = EditStates.Constructing;
			}
			else
			{
				midPt = new FixedPoint(hit2.point + surfaceHeight * Vector3.up);
				state = EditStates.WaitingEnd;
			}
		}
		else if (state == EditStates.WaitingEnd)
		{
			if (placementValid && CursorRaycast(out var hit3))
			{
				endPt = new FixedPoint(hit3.point + surfaceHeight * Vector3.up);
				if (endSnapInfo.snapped)
				{
					endPt.point = endSnapInfo.worldSnapPoint;
				}
				state = EditStates.Constructing;
			}
		}
		else if (state == EditStates.Deleting && delSegment != null)
		{
			roadSystem.DeleteSegment(delSegment);
			delSegment = null;
		}
	}

	public void CancelRoadPlacement()
	{
		if (state == EditStates.Deleting)
		{
			CancelDeleteButton();
			return;
		}
		state = EditStates.Idle;
		placementButton.SetActive(value: false);
		cancelButton.SetActive(value: false);
		previewLR.gameObject.SetActive(value: false);
		bezPreviewLR.gameObject.SetActive(value: false);
		roadCursor.gameObject.SetActive(value: false);
		surfaceCursor.gameObject.SetActive(value: false);
		roadHeightLine.gameObject.SetActive(value: false);
		placementValid = false;
		if (placementRoutine != null)
		{
			StopCoroutine(placementRoutine);
		}
	}

	private bool CursorRaycast(out RaycastHit hit)
	{
		return Physics.Raycast(editor.editorCamera.cam.ScreenPointToRay(Input.mousePosition), out hit, 100000f, 1);
	}

	private bool CursorRaycast(out RaycastHit hit, out Ray ray)
	{
		ray = editor.editorCamera.cam.ScreenPointToRay(Input.mousePosition);
		return Physics.Raycast(ray, out hit, 100000f, 1);
	}

	private void SpaceToCancelPlacement()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			CancelRoadPlacement();
		}
	}

	private bool PositionWithinMapBounds(Vector3 worldPos)
	{
		int gridSize = VTMapGenerator.fetch.gridSize;
		IntVector2 intVector = VTMapGenerator.fetch.ChunkGridAtPos(worldPos);
		if (intVector.x >= 0 && intVector.x < gridSize && intVector.y >= 0)
		{
			return intVector.y < gridSize;
		}
		return false;
	}

	private IEnumerator PlaceRoadRoutine()
	{
		previewLR.colorGradient = goodPlacementColor;
		bezPreviewLR.colorGradient = goodPlacementColor;
		while (state == EditStates.WaitingStart)
		{
			UpdateSurfaceHeight();
			placementValid = true;
			string empty = string.Empty;
			if (CursorRaycast(out var hit, out var ray) && PositionWithinMapBounds(hit.point))
			{
				roadCursor.gameObject.SetActive(value: true);
				roadCursor.transform.position = hit.point + surfaceHeight * Vector3.up;
				startSnapInfo = roadSystem.SnapRoadPoint(hit.point, ray);
				if (startSnapInfo.snapped)
				{
					roadCursor.transform.position = startSnapInfo.worldSnapPoint;
					Physics.Raycast(roadCursor.transform.position + 2000f * Vector3.up, Vector3.down, out hit, 4000f, 1);
				}
				UpdateSurfaceCursor(hit.point, roadCursor.transform.position);
			}
			else
			{
				roadCursor.gameObject.SetActive(value: false);
				placementValid = false;
				DisableSurfaceCursor();
			}
			placementStatusText.text = $"{currentRoadProfile.profileName}\nSelect a starting point.\n{empty}";
			SpaceToCancelPlacement();
			yield return null;
		}
		bezPreviewLR.positionCount = 2;
		while (state == EditStates.WaitingMid)
		{
			UpdateSurfaceHeight();
			placementValid = true;
			placementStatusText.text = $"{currentRoadProfile.profileName}\nSelect a midpoint.";
			if (CursorRaycast(out var hit2, out var ray2))
			{
				bezPreviewLR.gameObject.SetActive(value: true);
				Vector3 vector = hit2.point + surfaceHeight * Vector3.up;
				UpdateSurfaceCursor(hit2.point, vector);
				bezPreviewLR.SetPosition(0, startPt.point);
				bezPreviewLR.SetPosition(1, vector);
				Vector3D b = VTMapManager.WorldToGlobalPoint(vector);
				bool flag = roadSystem.CheckIsBridge(new BezierCurveD(startPt.globalPoint, Vector3D.Lerp(startPt.globalPoint, b, 0.5f), b));
				if (startSnapInfo.snapped && SnapIsIntersection(startSnapInfo) && SnapIsBridge(startSnapInfo) != flag)
				{
					placementValid = false;
					placementStatusText.text = $"{currentRoadProfile.profileName}\nSelect a midpoint.\nCan't intersect bridge with road.";
				}
				if (placementValid && Input.GetKey(KeyCode.LeftShift))
				{
					Vector3 point = hit2.point;
					Vector3 vector2 = hit2.point + surfaceHeight * Vector3.up;
					endSnapInfo = roadSystem.SnapRoadPoint(hit2.point, ray2);
					if (endSnapInfo.snapped)
					{
						vector2 = endSnapInfo.worldSnapPoint;
						if (Physics.Raycast(vector2 + 2000f * Vector3.up, Vector3.down, out var hitInfo, 4000f, 1))
						{
							point = hitInfo.point;
						}
					}
					bezPreviewLR.SetPosition(1, vector2);
					UpdateSurfaceCursor(point, vector2);
				}
			}
			else
			{
				placementValid = false;
				bezPreviewLR.gameObject.SetActive(value: false);
				DisableSurfaceCursor();
			}
			Gradient gradient3 = (bezPreviewLR.colorGradient = (previewLR.colorGradient = (placementValid ? goodPlacementColor : badPlacementColor)));
			SpaceToCancelPlacement();
			yield return null;
		}
		previewLR.positionCount = previewLrPoints.Length;
		previewLR.gameObject.SetActive(value: true);
		while (state == EditStates.WaitingEnd)
		{
			UpdateSurfaceHeight();
			placementValid = true;
			placementStatusText.text = $"{currentRoadProfile.profileName}\nSelect an end point.";
			if (CursorRaycast(out var hit3, out var ray3))
			{
				previewLR.gameObject.SetActive(value: true);
				Vector3 point2 = hit3.point;
				Vector3 vector3 = hit3.point + surfaceHeight * Vector3.up;
				endSnapInfo = roadSystem.SnapRoadPoint(hit3.point, ray3);
				if (endSnapInfo.snapped)
				{
					vector3 = endSnapInfo.worldSnapPoint;
					if (Physics.Raycast(vector3 + 2000f * Vector3.up, Vector3.down, out var hitInfo2, 4000f, 1))
					{
						point2 = hitInfo2.point;
					}
				}
				UpdateSurfaceCursor(point2, vector3);
				bezPreviewLR.positionCount = 3;
				bezPreviewLR.SetPosition(0, startPt.point);
				bezPreviewLR.SetPosition(1, midPt.point);
				bezPreviewLR.SetPosition(2, vector3);
				Vector3D vector3D = VTMapManager.WorldToGlobalPoint(vector3);
				BezierCurveD curve = new BezierCurveD(startPt.globalPoint, midPt.globalPoint, vector3D);
				for (int i = 0; i < previewLrPoints.Length; i++)
				{
					float t = (float)i / (float)(previewLrPoints.Length - 1);
					Vector3D point3 = curve.GetPoint(t);
					previewLrPoints[i] = VTMapManager.GlobalToWorldPoint(point3);
				}
				previewLR.SetPositions(previewLrPoints);
				bool flag2 = roadSystem.CheckIsBridge(curve);
				if (startSnapInfo.snapped && SnapIsIntersection(startSnapInfo) && SnapIsBridge(startSnapInfo) != flag2)
				{
					placementValid = false;
					placementStatusText.text = $"{currentRoadProfile.profileName}\nSelect an end point.\nCan't intersect bridge with road.";
				}
				if (endSnapInfo.snapped && SnapIsIntersection(endSnapInfo) && SnapIsBridge(endSnapInfo) != flag2)
				{
					placementValid = false;
					placementStatusText.text = $"{currentRoadProfile.profileName}\nSelect an end point.\nCan't intersect bridge with road.";
				}
			}
			else
			{
				bezPreviewLR.positionCount = 2;
				bezPreviewLR.SetPosition(0, startPt.point);
				bezPreviewLR.SetPosition(1, midPt.point);
				previewLR.gameObject.SetActive(value: false);
				placementStatusText.text = $"{currentRoadProfile.profileName}\nSelect an end point.\nINVALID PLACEMENT!";
				placementValid = false;
				DisableSurfaceCursor();
			}
			Gradient gradient3 = (bezPreviewLR.colorGradient = (previewLR.colorGradient = (placementValid ? goodPlacementColor : badPlacementColor)));
			SpaceToCancelPlacement();
			yield return null;
		}
		previewLR.gameObject.SetActive(value: false);
		bezPreviewLR.gameObject.SetActive(value: false);
		placementButton.SetActive(value: false);
		cancelButton.SetActive(value: false);
		roadCursor.gameObject.SetActive(value: false);
		DisableSurfaceCursor();
		BezierCurveD inCurve = new BezierCurveD(startPt.globalPoint, midPt.globalPoint, endPt.globalPoint);
		BezierRoadSystem.RoadSnapInfo roadSnapInfo = roadSystem.AddNewRoad(inCurve, startSnapInfo, endSnapInfo, currRoadProfileIdx);
		SelectRoadSet(currRoadProfileIdx);
		startSnapInfo = roadSnapInfo;
		StartRoad(roadSnapInfo.worldSnapPoint);
	}

	private bool SnapIsIntersection(BezierRoadSystem.RoadSnapInfo snapInfo)
	{
		if (snapInfo.snapped)
		{
			if (snapInfo.snappedSegmentEnd != null)
			{
				return false;
			}
			return true;
		}
		return false;
	}

	private bool SnapIsBridge(BezierRoadSystem.RoadSnapInfo snapInfo)
	{
		if (snapInfo.snapped)
		{
			if (snapInfo.snappedIntersection != null)
			{
				return snapInfo.snappedIntersection.attachedSegments[0].bridge;
			}
			if (snapInfo.snappedSegmentsInsert != null)
			{
				if (!snapInfo.snappedSegmentsInsert[0].bridge)
				{
					return snapInfo.snappedSegmentsInsert[1].bridge;
				}
				return true;
			}
			if (snapInfo.snappedSegmentSplit != null)
			{
				return snapInfo.snappedSegmentSplit.bridge;
			}
		}
		return false;
	}

	private void UpdateSurfaceHeight()
	{
		if (Input.GetKey(KeyCode.LeftControl))
		{
			surfaceHeight += Input.mouseScrollDelta.y * surfaceHeightAdjustSpeed * Time.deltaTime;
			surfaceHeight = Mathf.Clamp(surfaceHeight, 0f, maxSurfaceHeight);
		}
	}

	private void UpdateSurfaceCursor(Vector3 surfacePoint, Vector3 heightPoint)
	{
		if (surfaceHeight > 1f)
		{
			roadHeightLine.gameObject.SetActive(value: true);
			roadHeightLine.SetPosition(0, surfacePoint);
			roadHeightLine.SetPosition(1, heightPoint);
			surfaceCursor.transform.position = surfacePoint;
			surfaceCursor.gameObject.SetActive(value: true);
		}
		else
		{
			DisableSurfaceCursor();
		}
	}

	private void DisableSurfaceCursor()
	{
		surfaceCursor.gameObject.SetActive(value: false);
		roadHeightLine.gameObject.SetActive(value: false);
	}
}
