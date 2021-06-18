using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VRUIRaycaster : GraphicRaycaster, IPointerEnterHandler
{
	struct RaycastHit
	{
		public Graphic graphic;
		public Vector3 worldPos;
		public bool fromMouse;
	}

	public int sortOrder = 0;

	protected VRUIRaycaster() { }

	[NonSerialized]
	private Canvas m_Canvas;

	private Canvas canvas
	{
		get
		{
			if (m_Canvas != null)
			{
				return m_Canvas;
			}
			m_Canvas = GetComponent<Canvas>();
			return m_Canvas;
		}
	}

	public override Camera eventCamera
	{
		get
		{
			return canvas.worldCamera;
		}
	}

	public override int sortOrderPriority
	{
		get
		{
			return sortOrder;
		}
	}

	protected override void Start()
	{
		if(!canvas.worldCamera)
		{
			Debug.Log("Canvas does not have an event camera attached. Attaching OVRCameraRig.centerEyeAnchor as default.");
			OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
			canvas.worldCamera = rig.centerEyeAnchor.gameObject.GetComponent<Camera>();
		}
	}

	[NonSerialized]
	private List<RaycastHit> m_RaycastResults = new List<RaycastHit>();

	private void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList, Ray ray, bool checkForBlocking)
	{
		if (canvas == null) return;

		float hitDistance = float.MaxValue;

		if(checkForBlocking && blockingObjects != BlockingObjects.None)
		{
			float dist = eventCamera.farClipPlane;
			if(blockingObjects == BlockingObjects.ThreeD || blockingObjects == BlockingObjects.All)
			{
				var hits = Physics.RaycastAll(ray, dist, m_BlockingMask);
				if(hits.Length > 0 && hits[0].distance < hitDistance)
				{
					hitDistance = hits[0].distance;
				}
			}
			if(blockingObjects == BlockingObjects.TwoD || blockingObjects == BlockingObjects.All)
			{
				var hits = Physics2D.GetRayIntersectionAll(ray, dist, m_BlockingMask);
				if(hits.Length >0 && hits[0].fraction * dist < hitDistance)
				{
					hitDistance = hits[0].fraction * dist;
				}
			}
		}

		m_RaycastResults.Clear();

		GraphicRaycast(canvas, ray, m_RaycastResults);

		for(int index =0; index<m_RaycastResults.Count; index++)
		{
			var go = m_RaycastResults[index].graphic.gameObject;
			bool appendGraphic = true;

			if(ignoreReversedGraphics)
			{
				var cameraForward = ray.direction;
				var dir = go.transform.rotation * Vector3.forward;
				appendGraphic = Vector3.Dot(cameraForward, dir) > 0;
			}

			if(eventCamera.transform.InverseTransformPoint(m_RaycastResults[index].worldPos).z <= 0)
			{
				appendGraphic = false;
			}

			if(appendGraphic)
			{
				float distance = Vector3.Distance(ray.origin, m_RaycastResults[index].worldPos);

				if(distance >= hitDistance)
				{
					continue;
				}

				var castResult = new RaycastResult
				{
					gameObject = go,
					module = this,
					distance = distance,
					index = resultAppendList.Count,
					depth = m_RaycastResults[index].graphic.depth,

					worldPosition = m_RaycastResults[index].worldPos
				};
				resultAppendList.Add(castResult);
			}
		}
	}

	//여기까지 했음 이어서...

	[NonSerialized]
	static readonly List<RaycastHit> s_SortedGraphics = new List<RaycastHit>();
	private void GraphicRaycast(Canvas canvas, Ray ray, List<RaycastHit> results)
	{
		IList<Graphic> foundGraphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
		s_SortedGraphics.Clear();
		for(int i=0; i< foundGraphics.Count; i++)
		{
			Graphic graphic = foundGraphics[i];
			if (graphic.depth == -1) continue;

			Vector3 worldPos;
			if(RayIntersectsRectTransform(graphic.rectTransform, ray, out worldPos))
			{
				Vector2 screenPos = eventCamera.WorldToScreenPoint(worldPos);
				if(graphic.Raycast(screenPos, eventCamera))
				{
					RaycastHit hit;
					hit.graphic = graphic;
					hit.worldPos = worldPos;
					hit.fromMouse = false;
					s_SortedGraphics.Add(hit);
				}
			}
		}

		s_SortedGraphics.Sort((g1, g2) => g2.graphic.depth.CompareTo(g1.graphic.depth));

		for(int i=0; i< s_SortedGraphics.Count;++i)
		{
			results.Add(s_SortedGraphics[i]);
		}
	}

	static bool RayIntersectsRectTransform(RectTransform rectTransform, Ray ray, out Vector3 worldPos)
	{
		Vector3[] corners = new Vector3[4];
		rectTransform.GetWorldCorners(corners);
		Plane plane = new Plane(corners[0], corners[1], corners[2]);

		float enter;
		if(!plane.Raycast(ray, out enter))
		{
			worldPos = Vector3.zero;
			return false;
		}

		Vector3 intersection = ray.GetPoint(enter);
		Vector3 BottomEdge = corners[3] - corners[0];
		Vector3 LeftEdge = corners[1] - corners[0];
		float BottomDot = Vector3.Dot(intersection - corners[0], BottomEdge);
		float LeftDot = Vector3.Dot(intersection - corners[0], LeftEdge);
		if(BottomDot < BottomEdge.sqrMagnitude && LeftDot < LeftEdge.sqrMagnitude && BottomDot >=0 && LeftDot >= 0)
		{
			worldPos = corners[0] + LeftDot * LeftEdge / LeftEdge.sqrMagnitude + BottomDot * BottomEdge / BottomEdge.sqrMagnitude;
			return true;
		}
		else
		{
			worldPos = Vector3.zero;
			return false;
		}
	}


	public void OnPointerEnter(PointerEventData eventData)
	{
		
	}
}

