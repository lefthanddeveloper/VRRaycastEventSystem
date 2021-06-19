using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace UnityEngine.EventSystems
{
    public class VRInputModule : PointerInputModule
    {
		[SerializeField] private VRPointer vrPointerL;
		[SerializeField] private VRPointer vrPointerR;

		[NonSerialized]
		public VRUIRaycaster activeGraphicRaycaster;
		
		//continuous update
		public override void Process()
		{
			if(vrPointerL != null)
			{
				ProcessMouseEvent(GetPointerDataLeft(), true);
			}
			if(vrPointerR != null)
			{
				ProcessMouseEvent(GetPointerDataRight(), false);
			}
		}

		#region unnecessary
		//[SerializeField]
		//private bool m_AllowActivationOnMobileDevice;

		//public bool allowActivationOnMobileDevice
		//{
		//	get { return m_AllowActivationOnMobileDevice; }
		//	set { m_AllowActivationOnMobileDevice = value; }
		//}

		//[SerializeField]
		//private string m_SubmitButton = "Submit";

		//[SerializeField]
		//private string m_CancelButton = "Cancel";

		//[SerializeField]
		//private string m_HorizontalAxis = "Horizontal";

		//[SerializeField]
		//private string m_VerticalAxis = "Vertical";

		//private Vector2 m_LastMousePosition;
		//private Vector2 m_MousePosition;

		//public override void UpdateModule()
		//{
		//	m_LastMousePosition = m_MousePosition;
		//	m_MousePosition = Input.mousePosition;
		//}

		//public override bool IsModuleSupported()
		//{
		//	return m_AllowActivationOnMobileDevice || Input.mousePresent;
		//}

		//public override bool ShouldActivateModule()
		//{
		//	if (!base.ShouldActivateModule())
		//		return false;

		//	var shouldActivate = Input.GetButtonDown(m_SubmitButton);
		//	shouldActivate |= Input.GetButtonDown(m_CancelButton);
		//	shouldActivate |= !Mathf.Approximately(Input.GetAxisRaw(m_HorizontalAxis), 0.0f);
		//	shouldActivate |= !Mathf.Approximately(Input.GetAxisRaw(m_VerticalAxis), 0.0f);
		//	shouldActivate |= (m_MousePosition - m_LastMousePosition).sqrMagnitude > 0.0f;
		//	shouldActivate |= Input.GetMouseButtonDown(0);
		//	return shouldActivate;
		//}

		//public override void ActivateModule()
		//{
		//	base.ActivateModule();
		//	m_MousePosition = Input.mousePosition;
		//	m_LastMousePosition = Input.mousePosition;

		//	GameObject toSelect = eventSystem.currentSelectedGameObject;
		//	if(toSelect == null)
		//	{
		//		toSelect = eventSystem.firstSelectedGameObject;
		//	}
		//	eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
		//}

		//public override void DeactivateModule()
		//{
		//	base.DeactivateModule();
		//	print("DeactivateModule()");
		//	ClearSelection();
		//}

		//protected new void ClearSelection()
		//{
		//	print("ClearSelection()");
		//	BaseEventData baseEventData = GetBaseEventData();

		//	foreach(var pointer in m_PointerData.Values)
		//	{
		//		HandlePointerExitAndEnter(pointer, null);
		//	}
		//	foreach(var pointer in m_VRRayPointerData_LeftHand.Values)
		//	{
		//		HandlePointerExitAndEnter(pointer, null);
		//	}
		//	foreach(var pointer in m_VRRayPointerData_RightHand.Values)
		//	{
		//		HandlePointerExitAndEnter(pointer, null);
		//	}
		//	m_PointerData.Clear();
		//	m_VRRayPointerData_LeftHand.Clear();
		//	m_VRRayPointerData_RightHand.Clear();
		//	eventSystem.SetSelectedGameObject(null, baseEventData);
		//}

		#endregion

		private void ProcessMouseEvent(MouseState mouseData, bool isLeftController)
		{
			bool pressed = mouseData.AnyPressesThisFrame();
			bool released = mouseData.AnyReleasesThisFrame();

			MouseButtonEventData leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

			if(!UseMouse(pressed, released, leftButtonData.buttonData))
			{
				return;
			}

			//ProcessMousePress(leftButtonData, isLeftController);
			ProcessMousePress(leftButtonData);
			ProcessMove(leftButtonData.buttonData);
			ProcessDrag(leftButtonData.buttonData);

			if(!Mathf.Approximately(leftButtonData.buttonData.scrollDelta.sqrMagnitude, 0.0f))
			{
				GameObject scrollHanlder = ExecuteEvents.GetEventHandler<IScrollHandler>(leftButtonData.buttonData.pointerCurrentRaycast.gameObject);
				ExecuteEvents.ExecuteHierarchy(scrollHanlder, leftButtonData.buttonData, ExecuteEvents.scrollHandler);
			}
		}

		private static bool UseMouse(bool pressed, bool released, PointerEventData pointerData)
		{
			if(pressed || released || IsPointerMoving(pointerData) || pointerData.IsScrolling())
			{
				return true;
			}

			return false;
		}

		private static bool IsPointerMoving(PointerEventData pointerData)
		{
			if (pointerData.IsVRPointer())
			{
				return true;
			}
			else return pointerData.IsPointerMoving();
		}

		private void ProcessMousePress(MouseButtonEventData data)
		{
			PointerEventData pointerEvent = data.buttonData;
			GameObject currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

			if(data.PressedThisFrame())
			{
				pointerEvent.eligibleForClick = true;
				pointerEvent.delta = Vector2.zero;
				pointerEvent.dragging = false;
				pointerEvent.useDragThreshold = true;
				pointerEvent.pressPosition = pointerEvent.position;
				if(pointerEvent.IsVRPointer())
				{
					pointerEvent.SetSwipeStartData(Input.mousePosition);
				}
				pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

				DeselectIfSelectionChanged(currentOverGo, pointerEvent);

				GameObject newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);
				if(newPressed == null)
				{
					newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
				}

				float time = Time.unscaledTime;
				if(newPressed == pointerEvent.lastPress)
				{
					float diffTime = time - pointerEvent.clickTime;
					if(diffTime < 0.3f)
					{
						++pointerEvent.clickCount;
					}
					else
					{
						pointerEvent.clickCount = 1;
					}
					//pointerEvent.clickTime = time;
				}
				else
				{
					pointerEvent.clickCount = 1;
				}

				pointerEvent.pointerPress = newPressed;
				pointerEvent.rawPointerPress = currentOverGo;

				pointerEvent.clickTime = time;

				pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

				if(pointerEvent.pointerDrag != null)
				{
					ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
				}
			}

			if(data.ReleasedThisFrame())
			{
				ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);
				GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
				if(pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
				{
					ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
				}
				else if(pointerEvent.pointerDrag != null)
				{
					ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
				}

				pointerEvent.eligibleForClick = false;
				pointerEvent.pointerPress = null;
				pointerEvent.rawPointerPress = null;

				if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
				{
					ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);
				}

				pointerEvent.dragging = false;
				pointerEvent.pointerDrag = null;

				if(currentOverGo != pointerEvent.pointerEnter)
				{
					HandlePointerExitAndEnter(pointerEvent, null);
					HandlePointerExitAndEnter(pointerEvent, currentOverGo);
				}
			}
		}

		private readonly MouseState m_MouseStateL = new MouseState();
		private readonly MouseState m_MouseStateR = new MouseState();

		private MouseState GetPointerDataLeft()
		{
			VRPointerEventData pointerData;
			GetPointerDataLeft(kMouseLeftId, out pointerData, true);
			pointerData.Reset();

			pointerData.worldSpaceRay = new Ray(vrPointerL.rayTransform.position, vrPointerL.rayTransform.forward);
			//pointerData.scrollDelta = Vector2.zero;

			pointerData.button = PointerEventData.InputButton.Left;
			pointerData.useDragThreshold = true;

			eventSystem.RaycastAll(pointerData, m_RaycastResultCache);
			RaycastResult raycast = FindFirstRaycast(m_RaycastResultCache);
			pointerData.pointerCurrentRaycast = raycast;
			m_RaycastResultCache.Clear();

			if (vrPointerL == null) return null;

			vrPointerL.SetCursorRay(vrPointerL.rayTransform);
			VRUIRaycaster vrUIRaycaster = raycast.module as VRUIRaycaster;
			if(vrUIRaycaster)
			{
				pointerData.position = vrUIRaycaster.GetScreenPosition(raycast);
				RectTransform graphicRect = raycast.gameObject.GetComponent<RectTransform>();

				if(graphicRect != null)
				{
					Vector3 worldPos = raycast.worldPosition;
					Vector3 normal = GetRectTransformNormal(graphicRect);

					vrPointerL.SetCursorStartDest(vrPointerL.rayTransform.position, worldPos, normal);
					
				}
			}

			m_MouseStateL.SetButtonState(PointerEventData.InputButton.Left, GetPointerButtonStateL(), pointerData);

			return m_MouseStateL;
		}

		private MouseState GetPointerDataRight()
		{
			VRPointerEventData pointerData;
			GetPointerDataRight(kMouseLeftId, out pointerData, true);
			pointerData.Reset();

			pointerData.worldSpaceRay = new Ray(vrPointerR.rayTransform.position, vrPointerR.rayTransform.forward);
			//pointerData.scrollDelta = Vector2.zero;

			pointerData.button = PointerEventData.InputButton.Left;
			pointerData.useDragThreshold = true;

			eventSystem.RaycastAll(pointerData, m_RaycastResultCache);
			RaycastResult raycast = FindFirstRaycast(m_RaycastResultCache);
			pointerData.pointerCurrentRaycast = raycast;
			m_RaycastResultCache.Clear();

			if (vrPointerR != null) vrPointerR.SetCursorRay(vrPointerR.rayTransform);
			VRUIRaycaster vrUIRaycaster = raycast.module as VRUIRaycaster;
			if (vrUIRaycaster)
			{
				pointerData.position = vrUIRaycaster.GetScreenPosition(raycast);
				RectTransform graphicRect = raycast.gameObject.GetComponent<RectTransform>();

				if (graphicRect != null)
				{
					Vector3 worldPos = raycast.worldPosition;
					Vector3 normal = GetRectTransformNormal(graphicRect);

					if (vrPointerR != null) vrPointerR.SetCursorStartDest(vrPointerR.rayTransform.position, worldPos, normal);
				}
			}

			m_MouseStateR.SetButtonState(PointerEventData.InputButton.Left, GetPointerButtonStateR(), pointerData);

			return m_MouseStateR;
		}

		// check if controller is triggered or not
		virtual protected PointerEventData.FramePressState GetPointerButtonStateL()
		{
			bool pressed = false;
			bool released = false;

			pressed = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
			released = OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);

			if(pressed && released)
			{
				return PointerEventData.FramePressState.PressedAndReleased;
			}
			if(pressed)
			{
				return PointerEventData.FramePressState.Pressed;
			}
			if(released)
			{
				return PointerEventData.FramePressState.Released;
			}

			return PointerEventData.FramePressState.NotChanged;
		}

		// check if controller is triggered or not
		virtual protected PointerEventData.FramePressState GetPointerButtonStateR()
		{
			bool pressed = false;
			bool released = false;

			pressed = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
			released = OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

			if (pressed && released)
			{
				return PointerEventData.FramePressState.PressedAndReleased;
			}
			if (pressed)
			{
				return PointerEventData.FramePressState.Pressed;
			}
			if (released)
			{
				return PointerEventData.FramePressState.Released;
			}

			return PointerEventData.FramePressState.NotChanged;
		}


		private Vector3 GetRectTransformNormal(RectTransform rectTransform)
		{
			Vector3[] corners = new Vector3[4];
			rectTransform.GetWorldCorners(corners);
			Vector3 BottomEdge = corners[3] - corners[0];
			Vector3 LeftEdge = corners[1] - corners[0];
			rectTransform.GetWorldCorners(corners);
			return Vector3.Cross(BottomEdge, LeftEdge).normalized;
		}




		#region PointerEventData Pool
		protected Dictionary<int, VRPointerEventData> m_VRRayPointerData_LeftHand = new Dictionary<int, VRPointerEventData>();

		protected bool GetPointerDataLeft(int id, out VRPointerEventData data, bool create)
		{
			if(!m_VRRayPointerData_LeftHand.TryGetValue(id, out data) && create)
			{
				data = new VRPointerEventData(eventSystem)
				{
					pointerId = id,
				};

				m_VRRayPointerData_LeftHand.Add(id, data);
				return true;
			}
			return false;
		}

		protected Dictionary<int, VRPointerEventData> m_VRRayPointerData_RightHand = new Dictionary<int, VRPointerEventData>();
		protected bool GetPointerDataRight(int id, out VRPointerEventData data, bool create)
		{
			if(!m_VRRayPointerData_RightHand.TryGetValue(id, out data) && create)
			{
				data = new VRPointerEventData(eventSystem)
				{
					pointerId = id,
				};

				m_VRRayPointerData_RightHand.Add(id, data);
				return true;
			}
			return false;
		}


		#endregion


	}

}

