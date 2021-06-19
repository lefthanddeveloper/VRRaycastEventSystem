using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEngine.EventSystems
{
	public class VRPointerEventData : PointerEventData
	{
		public VRPointerEventData(EventSystem eventSystem) : base(eventSystem) { }

		public Ray worldSpaceRay;
		public Vector2 swipeStart;

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine("<b>Position</b>: " + position);
			sb.AppendLine("<b>delta</b>: " + delta);
			sb.AppendLine("<b>eligibleForClick</b>: " + eligibleForClick);
			sb.AppendLine("<b>pointerEnter</b>: " + pointerEnter);
			sb.AppendLine("<b>pointerPress</b>: " + pointerPress);
			sb.AppendLine("<b>lastPointerPress</b>: " + lastPress);
			sb.AppendLine("<b>pointerDrag</b>: " + pointerDrag);
			sb.AppendLine("<b>worldSpaceRay</b>: " + worldSpaceRay);
			sb.AppendLine("<b>swipeStart</b>: " + swipeStart);
			sb.AppendLine("<b>Use Drag Threshold</b>: " + useDragThreshold);
			return sb.ToString();
		}

	}
	public static class PointerEventData2Extension
	{
		public static bool IsVRPointer(this PointerEventData pointerEventData)
		{
			return (pointerEventData is VRPointerEventData);
		}

		public static Ray GetRayData(this PointerEventData pointerEventData)
		{
			VRPointerEventData vrPointerEventData = pointerEventData as VRPointerEventData;
			Assert.IsNotNull(vrPointerEventData);

			return vrPointerEventData.worldSpaceRay;
		}

		public static void SetSwipeStartData(this PointerEventData pointerEventData, Vector2 start)
		{
			VRPointerEventData vrPointerEventData = pointerEventData as VRPointerEventData;
			Assert.IsNotNull(vrPointerEventData);

			vrPointerEventData.swipeStart = start;
		}

	}

}
