using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VRPointer : MonoBehaviour
{
    public Transform rayTransform;
	private LineRenderer lineRenderer;

	private Vector3 _startPoint;
	private Vector3 _forward;
	private Vector3 _endPoint;
	private bool _hitTarget;

	public float lineMaxDistance = 10.0f;

	public bool showLineOnlyOnUI;

	private void Start()
	{
		lineRenderer = GetComponent<LineRenderer>();
	}


	public void SetCursorRay(Transform t)
	{
		_startPoint = t.position;
		_forward = t.forward;

		_hitTarget = false;
	}
	public void SetCursorStartDest(Vector3 start, Vector3 dest, Vector3 normal)
	{
		_startPoint = start;
		_endPoint = dest;

		_hitTarget = true;
	}

	private void LateUpdate()
	{
		RenderLine();
	}

	private void RenderLine()
	{
		if(_hitTarget)
		{
			lineRenderer.positionCount = 2;
			lineRenderer.SetPosition(0, _startPoint);
			lineRenderer.SetPosition(1, _endPoint);
		}
		else
		{
			if(showLineOnlyOnUI)
			{
				lineRenderer.positionCount = 0;
			}
			else
			{
				lineRenderer.positionCount = 2;
				lineRenderer.SetPosition(0, _startPoint);
				lineRenderer.SetPosition(1, _startPoint + _forward * lineMaxDistance);
			}
		}

	}
}
