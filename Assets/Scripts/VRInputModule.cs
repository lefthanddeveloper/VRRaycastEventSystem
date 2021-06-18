using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace UnityEngine.EventSystems
{
    public class VRInputModule : PointerInputModule
    {
		[SerializeField] private VRPointer vrPointerL;
		[SerializeField] private VRPointer vrPointerR;

		public override void Process()
		{
			if(vrPointerL != null)
			{
				//ProcessMouseEvent();
			}
			if(vrPointerR != null)
			{

			}
		}

		private void ProcessMouseEvent(MouseState mouseData, bool isLeftController)
		{
			bool pressed = mouseData.AnyPressesThisFrame();
			bool released = mouseData.AnyReleasesThisFrame();

			MouseButtonEventData leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

			if(!UseMouse(pressed, released, leftButtonData.buttonData))
			{
				return;
			}

			ProcessMousePress(leftButtonData, isLeftController);
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

		private void ProcessMousePress(MouseButtonEventData data, bool isLeft)
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
				if(pointerEvent.isVRPointer())
				{
					pointerEvent.SetSwipeStart(Input.mousePosition);
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

		//private MouseState GetPointerDataLeft()
		//{
		//	VRPointerEventData pointerData;
		//	GetPointerDataLeft(kMouseLeftId, out pointerData, true);
		//	pointerData.Reset();

		//	pointerData.worldSpaceRay = new Ray(vrPointerL.rayTransform.position, vrPointerL.rayTransform.forward);
		//	//pointerData.scrollDelta = GetExtraScrollDeltaLeft();  it needs to be made

		//	pointerData.button = PointerEventData.InputButton.Left;
		//	pointerData.useDragThreshold = true;

		//	eventSystem.RaycastAll(pointerData, m_RaycastResultCache);
		//	RaycastResult raycast = FindFirstRaycast(m_RaycastResultCache);
		//	pointerData.pointerCurrentRaycast = raycast;
		//	m_RaycastResultCache.Clear();



		//}

		//private MouseState GetPointerDataRight()
		//{

		//}

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

