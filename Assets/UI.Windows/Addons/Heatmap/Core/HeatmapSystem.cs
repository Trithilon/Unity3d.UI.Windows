﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI.Windows.Components;
using UnityEngine.UI.Windows.Plugins.Heatmap.Components;
using UnityEngine.UI.Windows.Plugins.Heatmap.Events;
using System.Linq;
using UnityEngine.UI.Windows.Types;

namespace UnityEngine.UI.Windows.Plugins.Heatmap.Core {
	
	public enum ClickType : byte {
		Component,
		Screen,
	}

	public class HeatmapSystem {

		public static void Put() {
			
			HeatmapSystem.Put(null, WindowSystemInput.GetPointerPosition(), ClickType.Screen);
			
		}

		public static void Put(IHeatmapHandler component) {

			var wComp = component as WindowComponent;

			var corners = new Vector3[4];
			(wComp.transform as RectTransform).GetWorldCorners(corners);
			
			var leftBottom = HeatmapSystem.GetScreenPoint(wComp, corners[0]);
			//var topRight = HeatmapSystem.GetScreenPoint(wComp, corners[2]);

			var inputPosition = WindowSystemInput.GetPointerPosition();
			//var w = topRight.x - leftBottom.x;
			//var h = topRight.y - leftBottom.y;

			var pos = new Vector2(inputPosition.x - leftBottom.x, inputPosition.y - leftBottom.y);

			HeatmapSystem.Put(component, pos, ClickType.Component);

		}

		private static Vector3 GetScreenPoint(WindowComponent component, Vector3 worldPoint) {
			
			return component.GetWindow().workCamera.WorldToScreenPoint(worldPoint);
			
		}

		public static void Put(IHeatmapHandler component, Vector2 localPoint, ClickType clickType) {

			// Normalize coords - make it ready to save
			var fullScreen = new Vector2(Screen.width, Screen.height);
			var current = localPoint;
			var screenNormalized = new Vector2(current.x / fullScreen.x, current.y / fullScreen.y);

			var localNormalizedPoint = screenNormalized;

			var tag = LayoutTag.None;
			WindowBase screen = null;
			if (component != null) {

				// Find component position
				var rectTransform = (component as WindowComponent).transform as RectTransform;
				if (rectTransform != null) {

					var offset = Vector2.zero;
					/*var scrolls = (component as WindowComponent).GetComponentsInParent<ScrollRect>();
					if (scrolls != null && scrolls.Length > 0) {

						var scroll = scrolls[0];
						var scrollRect = (scroll.transform as RectTransform).rect;

						offset = new Vector2(scrollRect.width * scroll.normalizedPosition.x, scrollRect.height * (1f - scroll.normalizedPosition.y));

					}*/

					var elementRect = rectTransform.rect;
					elementRect.x += offset.x;
					elementRect.y += offset.y;

					// Clamp localPoint to element rect

					localNormalizedPoint = new Vector2(localPoint.x / elementRect.width, localPoint.y / elementRect.height);

					//Debug.Log(elementRect, component as ButtonComponent);

				}

				screen = component.GetWindow();

				var comp = component as WindowComponent;
				if (comp != null) {

					var layout = comp.GetLayoutRoot() as WindowLayoutElement;
					if (layout != null) {

						tag = layout.tag;

					}

				}

			} else {

				screen = WindowSystem.GetCurrentWindow();

			}

			// Send point to server
			HeatmapSystem.Send(tag, screen, component as WindowComponent, localNormalizedPoint);

		}

		public static void Send(LayoutTag tag, WindowBase window, WindowComponent component, Vector2 localNormalizedPoint) {

			var flowWindow = Flow.FlowSystem.GetWindow(window);
			if (flowWindow == null) {
				
				Debug.LogWarningFormat("[ Heatmap ] FlowWindow not found. Source {0} used ({1}).", window, tag);
				return;

			}

			// TODO: Send to server
			// Request the new map from server.
			// Use tag, window and component as a keys.

			// Offline
			#if UNITY_EDITOR
			var settings = ME.EditorUtilities.GetAssetsOfType<HeatmapSettings>(useCache: false).FirstOrDefault();
			#else
			HeatmapSettings settings = null;
			#endif
			if (settings == null) return;

			var data = settings.data.Get(flowWindow);

			data.status = HeatmapSettings.WindowsData.Window.Status.Loading;

			data.size = (window as LayoutWindowType).layout.GetLayoutInstance().GetSize();
			data.AddPoint(localNormalizedPoint, tag, component);

			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(settings);
			#endif

			data.status = HeatmapSettings.WindowsData.Window.Status.Loaded;

		}

	}

}
