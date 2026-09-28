using System;
using System.Reflection;
using HSUIKit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HSIBL
{
	/// <summary>
	/// Studio toolbar toggle for the HSLRE window: clones "Button Center", places it in the first
	/// free cell of the toolbar grid (the convention other HS1 mods use), silences the template's own
	/// listeners and resets its tint.
	/// </summary>
	internal static class KitToolbarButton
	{
		private const int ColumnWidth = 40, FirstRowOffset = 80, RowHeight = 40, MaxCells = 32;
		private static Image _image;
		private static bool _on;

		private static readonly FieldInfo _buttonUndoField = typeof(StudioScene).GetField("buttonUndo", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Install(HSUIKit.VoidHandler onClick)
		{
			// opt-in (modprefs.ini [HSIBL] ToolbarButton=1, next to its "shortcut"): most people use the hotkey; the early-access build ships
			// without toolbar icons
			if (IllusionPlugin.ModPrefs.GetBool("HSIBL", "ToolbarButton", false, true) == false)
				return;
			try
			{
				StudioScene scene = UnityEngine.Object.FindObjectOfType<StudioScene>();
				if (scene == null || ReferenceEquals(_buttonUndoField, null))
					return;
				Button undo = _buttonUndoField.GetValue(scene) as Button;
				if (undo == null || undo.transform.parent == null)
					return;
				Transform row = undo.transform.parent;
				if (row.Find("Button HSLRE") != null)
					return;
				Transform template = row.Find("Button Center");
				if (template == null)
					return;
				RectTransform templateRect = (RectTransform)template;

				GameObject clone = UnityEngine.Object.Instantiate(template.gameObject);
				clone.name = "Button HSLRE";
				clone.transform.SetParent(row, true);
				clone.transform.localScale = template.localScale;
				RectTransform rect = (RectTransform)clone.transform;
				rect.anchorMin = templateRect.anchorMin;
				rect.anchorMax = templateRect.anchorMax;
				rect.offsetMin = templateRect.offsetMin;
				rect.offsetMax = templateRect.offsetMax;
				int cell = FindFreeCell(row, rect, templateRect);
				rect.anchoredPosition = templateRect.anchoredPosition + new Vector2(cell % 2 * ColumnWidth, FirstRowOffset + cell / 2 * RowHeight);
				clone.SetActive(true);

				Button button = clone.GetComponent<Button>();
				if (button != null)
				{
					button.onClick.RemoveAllListeners();
					for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
						button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
					button.onClick.AddListener(() => onClick());
					ColorBlock colors = button.colors;
					colors.normalColor = colors.highlightedColor = colors.pressedColor = colors.disabledColor = Color.white;
					colors.colorMultiplier = 1f;
					button.colors = colors;
				}
				_image = ToolbarBackground.Apply(clone); // dark square behind the icon, like vanilla (_shared\ToolbarBackground.cs)
				_image.sprite = KitPanelIcons.Toolbar;
				_image.type = Image.Type.Simple;
				SetOn(_on);
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogError("HSLRE: could not add the toolbar button.\n" + e);
			}
		}

		public static void SetOn(bool on)
		{
			_on = on;
			if (_image != null)
				_image.color = on ? Theme.Accent : new Color(0.86f, 0.86f, 0.86f, 1f);
		}

		private static int FindFreeCell(Transform row, RectTransform self, RectTransform template)
		{
			bool[] occupied = new bool[MaxCells];
			for (int i = 0; i < row.childCount; i++)
			{
				Transform child = row.GetChild(i);
				if (child == self.transform)
					continue;
				RectTransform childRect = child as RectTransform;
				if (childRect == null)
					continue;
				Vector2 relative = childRect.anchoredPosition - template.anchoredPosition;
				for (int cell = 0; cell < MaxCells; cell++)
				{
					if (Mathf.Abs(relative.x - cell % 2 * ColumnWidth) <= 2f && Mathf.Abs(relative.y - (FirstRowOffset + cell / 2 * RowHeight)) <= 2f)
					{
						occupied[cell] = true;
						break;
					}
				}
			}
			for (int cell = 0; cell < MaxCells; cell++)
				if (occupied[cell] == false)
					return cell;
			return MaxCells;
		}
	}
}
