// Copyright (c) 2012-2013 Rotorz Limited. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

using Rotorz.ReorderableList.Internal;

namespace Rotorz.ReorderableList {

	/// <summary>
	/// List control which can be used within custom editor windows and inspectors with
	/// support for drag and drop reordering of list items.
	/// </summary>
	[Serializable]
	public class ReorderableListControl {

		#region Generic List Abstraction

		/// <summary>
		/// Implementation of reorderable list data for generic lists.
		/// </summary>
		private sealed class GenericReorderableListData<T> : IReorderableListData {

			public List<T> list;

			public ReorderableListControl.ItemDrawer<T> itemDrawer;
			public float itemHeight;

			/// <summary>
			/// Initializes a new instance of <see cref="GenericReorderableListData{T}"/>.
			/// </summary>
			/// <param name="list">The list which can be reordered.</param>
			/// <param name="itemDrawer">Callback to draw list item.</param>
			/// <param name="itemHeight">Height of list item in pixels.</param>
			public GenericReorderableListData(List<T> list, ReorderableListControl.ItemDrawer<T> itemDrawer, float itemHeight) {
				this.list = list;
				this.itemDrawer = itemDrawer ?? ReorderableListGUI.DefaultItemDrawer;
				this.itemHeight = itemHeight;
			}

			#region IReorderableListData - Implementation

			/// <inheritdoc/>
			public int Count {
				get { return list.Count; }
			}

			/// <inheritdoc/>
			public void AddNew() {
				list.Add(default(T));
			}
			/// <inheritdoc/>
			public void Insert(int index) {
				list.Insert(index, default(T));
			}
			/// <inheritdoc/>
			public void Duplicate(int index) {
				list.Insert(index + 1, list[index]);
			}
			/// <inheritdoc/>
			public void Remove(int index) {
				list.RemoveAt(index);
			}
			/// <inheritdoc/>
			public void Move(int sourceIndex, int destIndex) {
				if (destIndex > sourceIndex)
					--destIndex;

				T item = list[sourceIndex];
				list.RemoveAt(sourceIndex);
				list.Insert(destIndex, item);
			}
			/// <inheritdoc/>
			public void Clear() {
				list.Clear();
			}

			/// <inheritdoc/>
			public void DrawItem(Rect position, int index) {
				list[index] = itemDrawer(position, list[index]);
			}

			/// <inheritdoc/>
			public float GetItemHeight(int index) {
				return itemHeight;
			}

			#endregion

		}

		#endregion

		/// <summary>
		/// Invoked to draw list item.
		/// </summary>
		/// <remarks>
		/// <para>GUI controls must be positioned absolutely within the given rectangle since
		/// list items must be sized consistently.</para>
		/// </remarks>
		/// <example>
		/// <para>The following listing presents a text field for each list item:</para>
		/// <code language="csharp"><![CDATA[
		/// using UnityEngine;
		/// using UnityEditor;
		/// 
		/// using System.Collections.Generic;
		/// 
		/// public class ExampleWindow : EditorWindow {
		///     public List<string> wishlist = new List<string>();
		/// 
		///     private void OnGUI() {
		///         ReorderableListGUI.ListField(wishlist, DrawListItem);
		///     }
		/// 
		///     private string DrawListItem(Rect position, string value) {
		///         // Text fields do not like `null` values!
		///         if (value == null)
		///             value = "";
		///         return EditorGUI.TextField(position, value);
		///     }
		/// }
		/// ]]></code>
		/// <code language="unityscript"><![CDATA[
		/// import System.Collections.Generic;
		/// 
		/// class ExampleWindow extends EditorWindow {
		///     var wishlist:List.<String>;
		/// 
		///     function OnGUI() {
		///         ReorderableListGUI.ListField(wishlist, DrawListItem);
		///     }
		/// 
		///     function DrawListItem(position:Rect, value:String):String {
		///         // Text fields do not like `null` values!
		///         if (value == null)
		///             value = '';
		///         return EditorGUI.TextField(position, value);
		///     }
		/// }
		/// ]]></code>
		/// </example>
		/// <typeparam name="T">Type of item list.</typeparam>
		/// <param name="position">Position of list item.</param>
		/// <param name="item">The list item.</param>
		/// <returns>
		/// The modified value.
		/// </returns>
		public delegate T ItemDrawer<T>(Rect position, T item);

		/// <summary>
		/// Invoked to draw content for empty list.
		/// </summary>
		/// <remarks>
		/// <para>Callback should make use of <c>GUILayout</c> for to present controls.</para>
		/// </remarks>
		/// <example>
		/// <para>The following listing displays a label for empty list control:</para>
		/// <code language="csharp"><![CDATA[
		/// using UnityEngine;
		/// using UnityEditor;
		/// 
		/// using System.Collections.Generic;
		/// 
		/// public class ExampleWindow : EditorWindow {
		///     private List<string> _list;
		/// 
		///     private void OnEnable() {
		///         _list = new List<string>();
		///     }
		///     private void OnGUI() {
		///         ReorderableListGUI.ListField(_list, ReorderableListGUI.TextFieldItemDrawer, DrawEmptyMessage);
		///     }
		/// 
		///     private string DrawEmptyMessage() {
		///         GUILayout.Label("List is empty!", EditorStyles.miniLabel);
		///     }
		/// }
		/// ]]></code>
		/// <code language="unityscript"><![CDATA[
		/// import System.Collections.Generic;
		/// 
		/// class ExampleWindow extends EditorWindow {
		///     private var _list:List.<String>;
		/// 
		///     function OnEnable() {
		///         _list = new List.<String>();
		///     }
		///     function OnGUI() {
		///         ReorderableListGUI.ListField(_list, ReorderableListGUI.TextFieldItemDrawer, DrawEmptyMessage);
		///     }
		/// 
		///     function DrawEmptyMessage() {
		///         GUILayout.Label('List is empty!', EditorStyles.miniLabel);
		///     }
		/// }
		/// ]]></code>
		/// </example>
		public delegate void DrawEmpty();

		#region Custom Styles

		/// <summary>
		/// Background color of anchor list item.
		/// </summary>
		public static readonly Color AnchorBackgroundColor;

		private static GUIContent RemoveButtonNormalContent { get; set; }
		private static GUIContent RemoveButtonActiveContent { get; set; }

		static ReorderableListControl() {
			AnchorBackgroundColor = EditorGUIUtility.isProSkin
				? new Color(0, 0, 0, 0.5f)
				: new Color(0, 0, 0, 0.3f);

			RemoveButtonNormalContent = new GUIContent(ReorderableListResources.texRemoveButton);
			RemoveButtonActiveContent = new GUIContent(ReorderableListResources.texRemoveButtonActive);
		}

		#endregion

		/// <summary>
		/// Position of rectangle which is shown to higlight target position when dragging.
		/// </summary>
		private static Rect s_DragHighlighter;

		/// <summary>
		/// Zero-based index of anchored list item.
		/// </summary>
		private static int s_AnchorIndex = -1;
		/// <summary>
		/// Zero-based index of target list item for reordering.
		/// </summary>
		private static int s_TargetIndex = -1;

		/// <summary>
		/// Unique ID of list control which should be automatically focused. A value
		/// of zero indicates that no control is to be focused.
		/// </summary>
		private static int s_AutoFocusControlID = 0;
		/// <summary>
		/// Zero-based index of item which should be focused.
		/// </summary>
		private static int s_AutoFocusIndex;

		/// <summary>
		/// Zero-based index of list item which is currently being drawn.
		/// </summary>
		private static int s_CurrentItemIndex;
		/// <summary>
		/// Gets zero-based index of list item which is currently being drawn;
		/// or a value of -1 if no item is currently being drawn.
		/// </summary>
		/// <remarks>
		/// <para>Use <see cref="ReorderableListGUI.currentItemIndex"/> instead.</para>
		/// </remarks>
		internal static int currentItemIndex {
			get { return s_CurrentItemIndex; }
		}

		#region Properties

		[SerializeField]
		private ReorderableListFlags _flags;

		/// <summary>
		/// Gets or sets flags which affect behavior of control.
		/// </summary>
		public ReorderableListFlags flags {
			get { return _flags; }
			set { _flags = value; }
		}

		[SerializeField]
		private GUIStyle _containerStyle;
		[SerializeField]
		private GUIStyle _addButtonStyle;
		[SerializeField]
		private GUIStyle _removeButtonStyle;

		/// <summary>
		/// Gets or sets style used to draw background of list control.
		/// </summary>
		/// <seealso cref="ReorderableListGUI.containerStyle"/>
		public GUIStyle containerStyle {
			get { return _containerStyle; }
			set { _containerStyle = value; }
		}
		/// <summary>
		/// Gets or sets style used to draw add button.
		/// </summary>
		/// <seealso cref="ReorderableListGUI.addButtonStyle"/>
		public GUIStyle addButtonStyle {
			get { return _addButtonStyle; }
			set { _addButtonStyle = value; }
		}
		/// <summary>
		/// Gets or sets style used to draw remove button.
		/// </summary>
		/// <seealso cref="ReorderableListGUI.removeButtonStyle"/>
		public GUIStyle removeButtonStyle {
			get { return _removeButtonStyle; }
			set { _removeButtonStyle = value; }
		}

		#endregion

		#region Construction

		/// <summary>
		/// Initializes a new instance of <see cref="ReorderableListControl"/>.
		/// </summary>
		public ReorderableListControl() {
			_containerStyle = ReorderableListGUI.containerStyle;
			_addButtonStyle = ReorderableListGUI.addButtonStyle;
			_removeButtonStyle = ReorderableListGUI.removeButtonStyle;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ReorderableListControl"/>.
		/// </summary>
		/// <param name="flags">Optional flags which affect behavior of control.</param>
		public ReorderableListControl(ReorderableListFlags flags)
			: this() {
			this.flags = flags;
		}

		#endregion

		#region Event Handling

		/// <summary>
		/// Draw add item button.
		/// </summary>
		/// <param name="position">Position of button.</param>
		/// <param name="controlID">Unique ID of list control.</param>
		/// <param name="list">The list which can be reordered.</param>
		private void DoAddButton(Rect position, int controlID, IReorderableListData list) {
			if (GUI.Button(position, GUIContent.none, addButtonStyle)) {
				// Append item to list.
				GUIUtility.keyboardControl = 0;
				list.AddNew();

				GUI.changed = true;
				ReorderableListGUI.indexOfChangedItem = -1;

				// Attempt to automatically focus list control.
				s_AutoFocusControlID = controlID;
				s_AutoFocusIndex = list.Count - 1;
			}
		}

		/// <summary>
		/// Draw remove button.
		/// </summary>
		/// <param name="position">Position of button.</param>
		/// <param name="forceActiveContent">Indicates if active content should be forced.</param>
		/// <returns>
		/// A value of <c>true</c> if clicked; otherwise <c>false</c>.
		/// </returns>
		private bool DoRemoveButton(Rect position, bool forceActiveContent) {
			int controlID = GUIUtility.GetControlID(FocusType.Passive);

			switch (Event.current.GetTypeForControl(controlID)) {
				case EventType.MouseDown:
					// Do not allow button to be pressed using right mouse button since
					// context menu should be shown instead!
					if (GUI.enabled && Event.current.button != 1 && position.Contains(Event.current.mousePosition)) {
						GUIUtility.hotControl = controlID;
						GUIUtility.keyboardControl = 0;
						Event.current.Use();
					}
					break;

				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlID)
						Event.current.Use();
					break;

				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlID) {
						GUIUtility.hotControl = 0;

						if (position.Contains(Event.current.mousePosition)) {
							Event.current.Use();
							return true;
						}
						else {
							Event.current.Use();
							return false;
						}
					}
					break;

				case EventType.Repaint:
					var content = (GUIUtility.hotControl == controlID || forceActiveContent)
						? RemoveButtonActiveContent
						: RemoveButtonNormalContent;
					removeButtonStyle.Draw(position, content, controlID);
					break;
			}

			return false;
		}

		/// <summary>
		/// Begin tracking drag and drop within list.
		/// </summary>
		/// <param name="controlID">Unique ID of list control.</param>
		/// <param name="itemIndex">Zero-based index of item which is going to be dragged.</param>
		private static void BeginTrackingReorderDrag(int controlID, int itemIndex) {
			GUIUtility.hotControl = controlID;
			GUIUtility.keyboardControl = 0;
			s_AnchorIndex = itemIndex;
			s_TargetIndex = itemIndex;
		}

		/// <summary>
		/// Stop tracking drag and drop.
		/// </summary>
		private static void StopTrackingReorderDrag() {
			GUIUtility.hotControl = 0;
			s_AnchorIndex = -1;
			s_TargetIndex = -1;
		}

		/// <summary>
		/// Gets a value indicating whether item in current list is currently being tracked.
		/// </summary>
		/// <param name="controlID">Unique ID of list control.</param>
		/// <returns>
		/// A value of <c>true</c> if item is being tracked; otherwise <c>false</c>.
		/// </returns>
		private static bool IsTrackingControl(int controlID) {
			return GUIUtility.hotControl == controlID;
		}

		/// <summary>
		/// Accept reordering.
		/// </summary>
		/// <param name="list">The list which can be reordered.</param>
		private static void AcceptReorderDrag(IReorderableListData list) {
			try {
				// Reorder list as needed!
				s_TargetIndex = Mathf.Clamp(s_TargetIndex, 0, list.Count + 1);
				if (s_TargetIndex != s_AnchorIndex && s_TargetIndex != s_AnchorIndex + 1) {
					list.Move(s_AnchorIndex, s_TargetIndex);
					GUI.changed = true;
				}
			}
			finally {
				StopTrackingReorderDrag();
			}
		}

		/// <summary>
		/// Cache of container heights mapped by control ID.
		/// </summary>
		private static Dictionary<int, float> s_ContainerHeightCache = new Dictionary<int, float>();
		/// <summary>
		/// Cache of zero height layout option to avoid unnecessary allocations.
		/// </summary>
		private static GUILayoutOption[] zeroHeight;

		private Rect BeginListContainer(int controlID) {
			// Since the number of layout rectangles must be consistent between events,
			// we get a rectangle both before and after drawning the container. We then
			// transfer the height from the second one to the first!

			if (zeroHeight == null)
				zeroHeight = new GUILayoutOption[] { GUILayout.Height(0) };

			Rect rect = GUILayoutUtility.GetRect(GUIContent.none, containerStyle, zeroHeight);

			if (Event.current.type != EventType.Layout) {
				rect.height = s_ContainerHeightCache.ContainsKey(controlID)
					? s_ContainerHeightCache[controlID]
					: 0;
			}

			return rect;
		}

		private void EndListContainer(int controlID, float totalHeight) {
			if (Event.current.type == EventType.Layout)
				s_ContainerHeightCache[controlID] = totalHeight;

			GUILayoutUtility.GetRect(0, totalHeight);
		}

		private Rect DoListField(int controlID, IReorderableListData list) {
			bool allowReordering = (flags & ReorderableListFlags.DisableReordering) == 0;
			bool includeRemoveButtons = (flags & ReorderableListFlags.HideRemoveButtons) == 0;

			bool trackingControl = IsTrackingControl(controlID);
			
			// Get local copy of event information for efficiency.
			EventType eventType = Event.current.GetTypeForControl(controlID);
			Vector2 mousePosition = Event.current.mousePosition;

			Rect containerRect = BeginListContainer(controlID);
			float totalHeight = 2;

			// Position of first item in list.
			float firstItemY = containerRect.y + containerStyle.padding.top;
			float newDragHighlighterY = firstItemY - 4;

			// We must put this back!
			Color restoreColor = GUI.color;

			switch (eventType) {
				case EventType.MouseDown:
				case EventType.MouseDrag:
					if (trackingControl) {
						// Cancel drag when other mouse button is pressed.
						if (Event.current.button != 0)
							StopTrackingReorderDrag();

						// Reset target index and adjust when looping through list items.
						if (mousePosition.y < firstItemY)
							s_TargetIndex = 0;
						else
							s_TargetIndex = list.Count;

						// Force repaint to occur so that dragging rectangle is visible.
						if (trackingControl)
							Event.current.Use();
					}
					break;

				case EventType.MouseUp:
					if (trackingControl) {
						// Allow user code to change control over reordering during drag.
						if (allowReordering)
							AcceptReorderDrag(list);
						else
							StopTrackingReorderDrag();
						Event.current.Use();
					}
					break;

				case EventType.KeyDown:
					if (trackingControl && Event.current.keyCode == KeyCode.Escape) {
						StopTrackingReorderDrag();
						Event.current.Use();
					}
					break;

				case EventType.ExecuteCommand:
					if (s_contextControlID == controlID) {
						int itemIndex = s_contextItemIndex;
						s_contextControlID = 0;
						s_contextItemIndex = 0;

						DoCommand(s_contextCommandName, itemIndex, list);

						Event.current.Use();
					}
					break;

				case EventType.Repaint:
					// Draw caption area of list.
					containerStyle.Draw(containerRect, GUIContent.none, false, false, false, false);
					break;
			}

			// Draw list items!
			Rect itemPosition = new Rect(containerRect.x + 2, firstItemY - 1, containerRect.width - 4, 0);
			Rect itemContentPosition = new Rect(itemPosition.x + 2, itemPosition.y + 1, itemPosition.width - 2, 0);
			Rect handlePosition = new Rect(itemPosition.x + 6, 0, 9, 5);
			Rect handleResponsePosition = new Rect(itemPosition.x, itemPosition.y + 1, 20, 0);

			// Make space for grab handle?
			if (allowReordering) {
				itemContentPosition.x += 20;
				itemContentPosition.width -= 20;
			}

			// Make space for remove buttons?
			Rect removeButtonPosition = default(Rect);
			if (includeRemoveButtons) {
				itemContentPosition.width -= removeButtonStyle.fixedWidth;
				removeButtonPosition = new Rect(itemContentPosition.xMax, itemContentPosition.y, removeButtonStyle.fixedWidth, 0);
			}

			bool canDragItem = (allowReordering && GUI.enabled);

			ReorderableListGUI.indexOfChangedItem = -1;

			for (int i = 0; i < list.Count; ++i) {
				// Update position for current item.
				itemContentPosition.y = itemPosition.yMax + 1;
				itemContentPosition.height = list.GetItemHeight(i);

				itemPosition.y = itemPosition.yMax;
				itemPosition.height = itemContentPosition.height + 4;
				totalHeight += itemPosition.height;

				float halfItemOffset = itemPosition.height / 2f;
				handlePosition.y = itemPosition.y + halfItemOffset - 3;

				handleResponsePosition.y = itemContentPosition.y;
				handleResponsePosition.height = itemContentPosition.height;
				removeButtonPosition.y = itemContentPosition.y;
				removeButtonPosition.height = itemContentPosition.height;

				// Adjust cursor when mouse pointer is positioned over grab handle.
				if (canDragItem)
					EditorGUIUtility.AddCursorRect(handleResponsePosition, MouseCursor.MoveArrow);

				// Draw grab handle.
				switch (eventType) {
					case EventType.Repaint:
						// Highlight background of anchor item.
						if (trackingControl && i == s_AnchorIndex) {
							GUI.color = AnchorBackgroundColor;
							GUI.DrawTexture(new Rect(itemPosition.x - 1, itemPosition.y - 1, itemPosition.width + 2, itemPosition.height + 1), EditorGUIUtility.whiteTexture);
							GUI.color = restoreColor;
						}

						if (allowReordering)
							GUI.DrawTexture(handlePosition, ReorderableListResources.texGrabHandle);

						if (i != 0 && (!trackingControl || (i != s_AnchorIndex && i != s_AnchorIndex + 1)))
							GUI.DrawTexture(new Rect(itemPosition.x, itemPosition.y - 1, itemPosition.width, 1), ReorderableListResources.texItemSplitter);
						break;

					case EventType.MouseDown:
						if (canDragItem && handleResponsePosition.Contains(mousePosition)) {
							if (Event.current.button == 0) {
								BeginTrackingReorderDrag(controlID, i);

								if (mousePosition.y >= itemPosition.y) {
									// Is mouse pointer hovering over upper half of item?
									if (mousePosition.y - itemPosition.y <= halfItemOffset) {
										s_TargetIndex = i;
										newDragHighlighterY = itemPosition.y - 3;
									}
									// Lower half?
									else {
										s_TargetIndex = i + 1;
										newDragHighlighterY = itemPosition.yMax - 3;
									}
								}

								Event.current.Use();
							}
							else {
								GUIUtility.keyboardControl = 0;
								EditorWindow.focusedWindow.Repaint();
							}
						}
						break;

					case EventType.MouseDrag:
						if (trackingControl && mousePosition.y >= itemPosition.y) {
							// Is mouse pointer hovering over upper half of item?
							if (mousePosition.y - itemPosition.y <= halfItemOffset) {
								s_TargetIndex = i;
								newDragHighlighterY = itemPosition.y - 3;
							}
							// Lower half?
							else {
								s_TargetIndex = i + 1;
								newDragHighlighterY = itemPosition.yMax - 3;
							}
						}
						break;
				}

				if (s_AutoFocusControlID == controlID && s_AutoFocusIndex == i)
					GUI.SetNextControlName("AutoFocus_" + controlID);

				EditorGUI.BeginChangeCheck();

				// Draw the actual list item!
				s_CurrentItemIndex = i;
				list.DrawItem(itemContentPosition, i);

				if (EditorGUI.EndChangeCheck())
					ReorderableListGUI.indexOfChangedItem = i;

				if (includeRemoveButtons)
					if (DoRemoveButton(removeButtonPosition, trackingControl && s_AnchorIndex == i)) {
						// Remove last entry in list.
						list.Remove(i);

						GUI.changed = true;
						ReorderableListGUI.indexOfChangedItem = -1;
					}

				// Check for context click?
				if (itemPosition.Contains(mousePosition)) {
					switch (Event.current.type) {
						case EventType.MouseDown:
							// Remove input focus from control before attempting a context click or drag.
							GUIUtility.keyboardControl = 0;
							Event.current.Use();
							break;

						case EventType.ContextClick:
							if (itemPosition.Contains(mousePosition)) {
								ShowContextMenu(controlID, i, list);
								Event.current.Use();
							}
							break;
					}
				}
			}

			s_CurrentItemIndex = -1;

			// Automatically focus control!
			if (s_AutoFocusControlID == controlID) {
				s_AutoFocusControlID = 0;
#if (UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2)
				GUI.FocusControl("AutoFocus_" + controlID);
#else
				EditorGUI.FocusTextInControl("AutoFocus_" + controlID);
#endif
			}

			containerRect.height = totalHeight;
			EndListContainer(controlID, totalHeight);

			// Fake control to catch input focus if auto focus was not possible.
			GUIUtility.GetControlID(FocusType.Keyboard);

			// Update position of drag rectangle.
			if (eventType == EventType.MouseDown || eventType == EventType.MouseDrag) {
				if (IsTrackingControl(controlID)) {
					// Update position of drag rectangle.
					s_DragHighlighter = new Rect(containerRect.x, newDragHighlighterY, containerRect.width, 4);
				}
			}
			
			return containerRect;
		}

		private Rect DoEmptyList(DrawEmpty drawEmpty) {
			Rect r = EditorGUILayout.BeginVertical(containerStyle);
			{
				if (drawEmpty != null)
					drawEmpty();
				else
					GUILayout.Space(5);
			}
			EditorGUILayout.EndVertical();
			return r;
		}

		/// <summary>
		/// Draw list field and handle other GUI events.
		/// </summary>
		/// <param name="list">Abstracted representation of list.</param>
		/// <param name="drawEmpty">Delegate for drawing empty list.</param>
		protected void DoListField(IReorderableListData list, DrawEmpty drawEmpty) {
			int controlID = GUIUtility.GetControlID(FocusType.Passive);

			// Correct if for some reason one or more styles are missing!
			containerStyle = containerStyle ?? ReorderableListGUI.containerStyle;
			addButtonStyle = addButtonStyle ?? ReorderableListGUI.addButtonStyle;
			removeButtonStyle = removeButtonStyle ?? ReorderableListGUI.removeButtonStyle;

			Rect containerPosition;
			if (list.Count > 0)
				containerPosition = DoListField(controlID, list);
			else
				containerPosition = DoEmptyList(drawEmpty);

			if ((flags & ReorderableListFlags.HideAddButton) == 0) {
				Rect addButtonRect = GUILayoutUtility.GetRect(0, addButtonStyle.fixedHeight);
				addButtonRect.width = addButtonStyle.fixedWidth;
				addButtonRect.x = containerPosition.xMax - addButtonRect.width;
				addButtonRect.y -= containerStyle.margin.bottom + 1;

				DoAddButton(addButtonRect, controlID, list);
			}
			
			// Highight drag rectangle?
			if (Event.current.type == EventType.Repaint) {
				// Note: Draw on top of other controls!
				if (IsTrackingControl(controlID)) {
					Color restoreColor = GUI.color;
					GUI.color = EditorGUIUtility.isProSkin
						? new Color(0.75f, 0.75f, 0.75f)
						: new Color(0.12f, 0.12f, 0.12f);
					GUI.DrawTexture(s_DragHighlighter, EditorGUIUtility.whiteTexture);
					GUI.color = restoreColor;
				}
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Draw list field control.
		/// </summary>
		/// <param name="list">The list which can be reordered.</param>
		/// <param name="drawItem">Callback to draw list item.</param>
		/// <param name="drawEmpty">Callback to draw custom content for empty list (optional).</param>
		/// <param name="itemHeight">Height of a single list item.</param>
		/// <typeparam name="T">Type of list item.</typeparam>
		public void Draw<T>(List<T> list, ItemDrawer<T> drawItem, DrawEmpty drawEmpty, float itemHeight) {
			DoListField(new GenericReorderableListData<T>(list, drawItem, itemHeight), drawEmpty);
		}

		/// <summary>
		/// Draw list field control.
		/// </summary>
		/// <param name="list">The list which can be reordered.</param>
		/// <param name="drawItem">Callback to draw list item.</param>
		/// <param name="itemHeight">Height of a single list item.</param>
		/// <typeparam name="T">Type of list item.</typeparam>
		public void Draw<T>(List<T> list, ItemDrawer<T> drawItem, float itemHeight) {
			DoListField(new GenericReorderableListData<T>(list, drawItem, itemHeight), null);
		}

		/// <summary>
		/// Draw list field control.
		/// </summary>
		/// <param name="list">The list which can be reordered.</param>
		/// <param name="drawItem">Callback to draw list item.</param>
		/// <typeparam name="T">Type of list item.</typeparam>
		public void Draw<T>(List<T> list, ItemDrawer<T> drawItem) {
			DoListField(new GenericReorderableListData<T>(list, drawItem, ReorderableListGUI.DefaultItemHeight), null);
		}

		/// <summary>
		/// Draw list field control.
		/// </summary>
		/// <param name="list">The list which can be reordered.</param>
		/// <param name="drawItem">Callback to draw list item.</param>
		/// <param name="drawEmpty">Callback to draw custom content for empty list.</param>
		/// <typeparam name="T">Type of list item.</typeparam>
		public void Draw<T>(List<T> list, ItemDrawer<T> drawItem, DrawEmpty drawEmpty) {
			DoListField(new GenericReorderableListData<T>(list, drawItem, ReorderableListGUI.DefaultItemHeight), drawEmpty);
		}

		#endregion

		#region Context Menu

		/// <summary>
		/// Content for "Move to Top" command.
		/// </summary>
		protected static readonly GUIContent commandMoveToTop = new GUIContent("Move to Top");
		/// <summary>
		/// Content for "Move to Bottom" command.
		/// </summary>
		protected static readonly GUIContent commandMoveToBottom = new GUIContent("Move to Bottom");
		/// <summary>
		/// Content for "Insert Above" command.
		/// </summary>
		protected static readonly GUIContent commandInsertAbove = new GUIContent("Insert Above");
		/// <summary>
		/// Content for "Insert Below" command.
		/// </summary>
		protected static readonly GUIContent commandInsertBelow = new GUIContent("Insert Below");
		/// <summary>
		/// Content for "Duplicate" command.
		/// </summary>
		protected static readonly GUIContent commandDuplicate = new GUIContent("Duplicate");
		/// <summary>
		/// Content for "Remove" command.
		/// </summary>
		protected static readonly GUIContent commandRemove = new GUIContent("Remove");
		/// <summary>
		/// Content for "Clear All" command.
		/// </summary>
		protected static readonly GUIContent commandClearAll = new GUIContent("Clear All");

		// Command control id and item index are assigned when context menu is shown.
		private static int s_contextControlID;
		private static int s_contextItemIndex;

		// Command name is assigned by default context menu handler.
		private static string s_contextCommandName;

		private void ShowContextMenu(int controlID, int itemIndex, IReorderableListData list) {
			GenericMenu menu = new GenericMenu();

			s_contextControlID = controlID;
			s_contextItemIndex = itemIndex;

			AddItemsToMenu(menu, itemIndex, list);

			if (menu.GetItemCount() > 0)
				menu.ShowAsContext();
		}

		/// <summary>
		/// Default functionality to handle context command.
		/// </summary>
		/// <example>
		/// <para>Can be used when adding custom items to the context menu:</para>
		/// <code language="csharp"><![CDATA[
		/// protected override void AddItemsToMenu(GenericMenu menu, int itemIndex, IReorderableListData list) {
		///     var specialCommand = new GUIContent("Special Command");
		///     menu.AddItem(specialCommand, false, defaultContextHandler, specialCommand);
		/// }
		/// ]]></code>
		/// <code language="unityscript"><![CDATA[
		/// function AddItemsToMenu(menu:GenericMenu, itemIndex:int, list:IReorderableListData) {
		///     var specialCommand = new GUIContent('Special Command');
		///     menu.AddItem(specialCommand, false, defaultContextHandler, specialCommand);
		/// }
		/// ]]></code>
		/// </example>
		/// <seealso cref="AddItemsToMenu"/>
		protected static GenericMenu.MenuFunction2 defaultContextHandler = DefaultContextMenuHandler;

		private static void DefaultContextMenuHandler(object userData) {
			var commandContent = userData as GUIContent;
			if (commandContent == null || string.IsNullOrEmpty(commandContent.text))
				return;

			s_contextCommandName = commandContent.text;

			var e = EditorGUIUtility.CommandEvent("ReorderableListContextCommand");
			EditorWindow.focusedWindow.SendEvent(e);
		}

		/// <summary>
		/// Invoked to generate context menu for list item.
		/// </summary>
		/// <param name="menu">Menu which can be populated.</param>
		/// <param name="itemIndex">Zero-based index of item which was right-clicked.</param>
		/// <param name="list">The list which can be reordered.</param>
		protected virtual void AddItemsToMenu(GenericMenu menu, int itemIndex, IReorderableListData list) {
			if ((flags & ReorderableListFlags.DisableReordering) == 0) {
				if (itemIndex > 0)
					menu.AddItem(commandMoveToTop, false, defaultContextHandler, commandMoveToTop);
				else
					menu.AddDisabledItem(commandMoveToTop);

				if (itemIndex + 1 < list.Count)
					menu.AddItem(commandMoveToBottom, false, defaultContextHandler, commandMoveToBottom);
				else
					menu.AddDisabledItem(commandMoveToBottom);

				if ((flags & ReorderableListFlags.HideAddButton) == 0) {
					menu.AddSeparator("");

					menu.AddItem(commandInsertAbove, false, defaultContextHandler, commandInsertAbove);
					menu.AddItem(commandInsertBelow, false, defaultContextHandler, commandInsertBelow);

					if ((flags & ReorderableListFlags.DisableDuplicateCommand) == 0)
						menu.AddItem(commandDuplicate, false, defaultContextHandler, commandDuplicate);
				}
			}

			if ((flags & ReorderableListFlags.HideRemoveButtons) == 0) {
				if (menu.GetItemCount() > 0)
					menu.AddSeparator("");

				menu.AddItem(commandRemove, false, defaultContextHandler, commandRemove);
				menu.AddSeparator("");
				menu.AddItem(commandClearAll, false, defaultContextHandler, commandClearAll);
			}
		}

		#endregion

		#region Command Handling

		/// <summary>
		/// Invoked to handle command.
		/// </summary>
		/// <remarks>
		/// <para>It is important to set the value of <c>GUI.changed</c> to <c>true</c> if any
		/// changes are made by command handler.</para>
		/// <para>Default command handling functionality can be inherited:</para>
		/// <code language="csharp"><![CDATA[
		/// protected override bool HandleCommand<T>(string commandName, int itemIndex, List<T> list) {
		///     if (base.HandleContextCommand(itemIndex, list))
		///         return true;
		///     
		///     // Place default command handling code here...
		///     switch (commandName) {
		///         case "Your Command":
		///             break;
		///     }
		/// 
		///     return false;
		/// }
		/// ]]></code>
		/// <code language="unityscript"><![CDATA[
		/// function HandleCommand<T>(commandName:String, itemIndex:int, list:List.<T>):boolean {
		///     if (base.HandleContextCommand(itemIndex, list))
		///         return true;
		///     
		///     // Place default command handling code here...
		///     switch (commandName) {
		///         case 'Your Command':
		///             break;
		///     }
		/// 
		///     return false;
		/// }
		/// ]]></code>
		/// </remarks>
		/// <param name="commandName">Name of command. This is the text shown in the context menu.</param>
		/// <param name="itemIndex">Zero-based index of item which was right-clicked.</param>
		/// <param name="list">The list which can be reordered.</param>
		/// <returns>
		/// A value of <c>true</c> if command was known; otherwise <c>false</c>.
		/// </returns>
		protected virtual bool HandleCommand(string commandName, int itemIndex, IReorderableListData list) {
			switch (commandName) {
				case "Move to Top":
					list.Move(itemIndex, 0);
					break;
				case "Move to Bottom":
					list.Move(itemIndex, list.Count);
					break;
				case "Insert Above":
					list.Insert(itemIndex);
					break;
				case "Insert Below":
					list.Insert(itemIndex + 1);
					break;
				case "Duplicate":
					list.Duplicate(itemIndex);
					break;
				case "Remove":
					list.Remove(itemIndex);
					break;
				case "Clear All":
					list.Clear();
					break;

				default:
					return false;
			}

			GUI.changed = true;
			ReorderableListGUI.indexOfChangedItem = -1;

			return true;
		}

		/// <summary>
		/// Call to manually perform command.
		/// </summary>
		/// <remarks>
		/// <para>Warning message is logged to console if attempted to execute unknown command.</para>
		/// </remarks>
		/// <param name="commandName">Name of command. This is the text shown in the context menu.</param>
		/// <param name="itemIndex">Zero-based index of item which was right-clicked.</param>
		/// <param name="list">The list which can be reordered.</param>
		/// <returns>
		/// A value of <c>true</c> if command was known; otherwise <c>false</c>.
		/// </returns>
		public bool DoCommand(string commandName, int itemIndex, IReorderableListData list) {
			if (!HandleCommand(s_contextCommandName, itemIndex, list)) {
				Debug.LogWarning("Unknown context command.");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Call to manually perform command.
		/// </summary>
		/// <remarks>
		/// <para>Warning message is logged to console if attempted to execute unknown command.</para>
		/// </remarks>
		/// <param name="command">Content representing command.</param>
		/// <param name="itemIndex">Zero-based index of item which was right-clicked.</param>
		/// <param name="list">The list which can be reordered.</param>
		/// <returns>
		/// A value of <c>true</c> if command was known; otherwise <c>false</c>.
		/// </returns>
		public bool DoCommand<T>(GUIContent command, int itemIndex, IReorderableListData list) {
			return DoCommand(command.text, itemIndex, list);
		}

		#endregion

	}

}