using System;
using System.Collections.Generic;
using System.Linq;
using Studio;
using UnityEngine;

namespace HSIBL
{
	/// <summary>
	/// Every GUI call the HSIBL sections make goes through here. With no backend set (the original window,
	/// inside OnGUI) each call forwards to the same UIUtils / GUILayout call as before, unchanged. With a
	/// backend set (the HSUIKit panel) the calls go to that backend instead.
	/// </summary>
	internal static class IblUi
	{
		private static IIblBackend _backend;

		internal static bool Classic => _backend == null;

		/// <summary>Runs draw with the given backend active. Exceptions are logged, never thrown into Update.</summary>
		internal static void Run(IIblBackend backend, Action draw)
		{
			IIblBackend previous = _backend;
			_backend = backend;
			try
			{
				draw();
			}
			catch (Exception e)
			{
				Debug.LogError("HSLRE: a settings section failed to draw in the kit panel.\n" + e);
			}
			finally
			{
				_backend = previous;
			}
		}

		#region State
		private static GUIStyle _kitTitleStyle;

		/// <summary>
		/// Style the sections pass for titles. UIUtils.titleStyle2 only exists once the original window has
		/// drawn, so the kit panel gets a marker style of its own to recognise titles by.
		/// </summary>
		internal static GUIStyle TitleStyle
		{
			get
			{
				if (_backend == null)
					return UIUtils.titleStyle2;
				return _kitTitleStyle ?? (_kitTitleStyle = new GUIStyle());
			}
		}

		internal static bool Enabled
		{
			get => _backend != null ? _backend.Enabled : GUI.enabled;
			set
			{
				if (_backend != null)
					_backend.Enabled = value;
				else
					GUI.enabled = value;
			}
		}

		internal static Color Color
		{
			get => _backend != null ? _backend.Color : GUI.color;
			set
			{
				if (_backend != null)
					_backend.Color = value;
				else
					GUI.color = value;
			}
		}
		#endregion

		#region UIUtils
		internal static float SliderGUI(float value, float min, float max, float reset, string label, string tooltip, int decimals, bool hasMinMaxLimit = false, bool hasMinLimit = false)
		{
			if (_backend == null)
				return UIUtils.SliderGUI(value, min, max, reset, label, tooltip, decimals, hasMinMaxLimit, hasMinLimit);
			return _backend.Slider(value, min, max, reset, label, tooltip, decimals, hasMinMaxLimit, hasMinLimit);
		}

		internal static bool ToggleGUI(bool toggle, GUIContent title, string option1 = "Disable", string option2 = "Enable")
		{
			if (_backend == null)
				return UIUtils.ToggleGUI(toggle, title, option1, option2);
			return _backend.Toggle(toggle, title.text, title.tooltip, false);
		}

		internal static bool ToggleGUI(bool toggle, GUIContent title, GUIStyle titleStyle, string option1 = "Disable", string option2 = "Enable")
		{
			if (_backend == null)
				return UIUtils.ToggleGUI(toggle, title, titleStyle, option1, option2);
			return _backend.Toggle(toggle, title.text, title.tooltip, IsTitle(titleStyle));
		}

		internal static bool ToggleGUITitle(bool toggle, GUIContent title, string[] switches, GUIStyle titleStyle, Action onUp, Action onDown)
		{
			if (_backend == null)
				return UIUtils.ToggleGUITitle(toggle, title, switches, titleStyle, onUp, onDown);
			// the kit panel reorders effects by dragging them in the list, so no up / down buttons here
			return _backend.Toggle(toggle, title.text, title.tooltip, true);
		}

		internal static bool ToggleButton(bool toggle, GUIContent label)
		{
			if (_backend == null)
				return UIUtils.ToggleButton(toggle, label);
			return _backend.ToggleButton(toggle, label.text, label.tooltip);
		}

		internal static void ColorPickerGUI(Color value, Color reset, string labeltext, UI_ColorInfo.UpdateColor onSet)
		{
			ColorPickerGUI(value, reset, labeltext, "", onSet);
		}

		internal static void ColorPickerGUI(Color value, Color reset, string labeltext, string tooltip, UI_ColorInfo.UpdateColor onSet)
		{
			if (_backend == null)
			{
				UIUtils.ColorPickerGUI(value, reset, labeltext, tooltip, onSet);
				return;
			}
			if (_backend.ColorField(value, reset, labeltext, tooltip, true, out Color result))
				onSet(result);
		}

		internal static int SelectGUI(int selected, GUIContent title, string[] selections, int xCount = 0)
		{
			if (_backend == null)
				return UIUtils.SelectGUI(selected, title, selections, xCount);
			_backend.Label(title.text, title.tooltip, LabelKind.Body);
			return _backend.Select(selected, selections, xCount, title.tooltip);
		}

		internal static int LayerMaskValue(int value, GUIContent label, SortedList<int, string> layerNames, string option1 = "Disable", string option2 = "Enable", int columns = 2)
		{
			if (_backend == null)
				return UIUtils.LayerMaskValue(value, label, layerNames, option1, option2, columns);
			if (label != null)
				_backend.Label(label.text, label.tooltip, LabelKind.Body);
			int newValue = value;
			foreach (KeyValuePair<int, string> kvp in layerNames)
			{
				if (_backend.Toggle((newValue & (1 << kvp.Key)) != 0, $"{kvp.Key}: {kvp.Value}", "", false))
					newValue |= 1 << kvp.Key;
				else
					newValue &= ~(1 << kvp.Key);
			}
			return newValue;
		}

		/// <summary>Same editing as UIUtils.Gradient, built from the routed controls.</summary>
		internal static void Gradient(Gradient gradient, string labeltext, string tooltip, bool showAlphas = true, Action onChange = null, Texture2D userGeneratedTexture = null)
		{
			if (_backend == null)
			{
				UIUtils.Gradient(gradient, labeltext, tooltip, showAlphas, onChange, userGeneratedTexture);
				return;
			}
			_backend.Label(labeltext, tooltip, LabelKind.Body);
			_backend.Label("Colors", "", LabelKind.Dim);
			for (int i = 0; i < gradient.colorKeys.Length; i++)
			{
				GradientColorKey colorKey = gradient.colorKeys[i];
				if (_backend.ColorField(colorKey.color, Color.white, "Color " + i, "", false, out Color c))
				{
					GradientColorKey[] keys = (GradientColorKey[])gradient.colorKeys.Clone();
					keys[i].color = c;
					gradient.SetKeys(keys, gradient.alphaKeys);
					onChange?.Invoke();
				}
				float newTime = _backend.Slider(colorKey.time, 0f, 1f, colorKey.time, "Position " + i, "", 3, true, false);
				if (Mathf.Approximately(newTime, colorKey.time) == false)
				{
					GradientColorKey[] keys = (GradientColorKey[])gradient.colorKeys.Clone();
					keys[i].time = newTime;
					gradient.SetKeys(keys, gradient.alphaKeys);
					onChange?.Invoke();
				}
			}
			_backend.BeginRow();
			if (_backend.Button("Remove color", "") && gradient.colorKeys.Length > 2)
			{
				gradient.SetKeys(gradient.colorKeys.Take(gradient.colorKeys.Length - 1).ToArray(), gradient.alphaKeys);
				onChange?.Invoke();
			}
			if (_backend.Button("Add color", "") && gradient.colorKeys.Length < 8)
			{
				List<GradientColorKey> keys = gradient.colorKeys.ToList();
				keys.Add(new GradientColorKey(Color.white, 1f));
				gradient.SetKeys(keys.ToArray(), gradient.alphaKeys);
				onChange?.Invoke();
			}
			_backend.EndRow();

			_backend.Label("Alphas", "", LabelKind.Dim);
			for (int i = 0; i < gradient.alphaKeys.Length; i++)
			{
				GradientAlphaKey alphaKey = gradient.alphaKeys[i];
				float newAlpha = _backend.Slider(alphaKey.alpha, 0f, 1f, alphaKey.alpha, "Alpha " + i, "", 3, true, false);
				float newTime = _backend.Slider(alphaKey.time, 0f, 1f, alphaKey.time, "Alpha position " + i, "", 3, true, false);
				if (Mathf.Approximately(newAlpha, alphaKey.alpha) == false || Mathf.Approximately(newTime, alphaKey.time) == false)
				{
					GradientAlphaKey[] keys = (GradientAlphaKey[])gradient.alphaKeys.Clone();
					keys[i].alpha = newAlpha;
					keys[i].time = newTime;
					gradient.SetKeys(gradient.colorKeys, keys);
					onChange?.Invoke();
				}
			}
			_backend.BeginRow();
			if (_backend.Button("Remove alpha", "") && gradient.alphaKeys.Length > 2)
			{
				gradient.SetKeys(gradient.colorKeys, gradient.alphaKeys.Take(gradient.alphaKeys.Length - 1).ToArray());
				onChange?.Invoke();
			}
			if (_backend.Button("Add alpha", "") && gradient.alphaKeys.Length < 8)
			{
				List<GradientAlphaKey> keys = gradient.alphaKeys.ToList();
				keys.Add(new GradientAlphaKey(1f, 1f));
				gradient.SetKeys(gradient.colorKeys, keys.ToArray());
				onChange?.Invoke();
			}
			_backend.EndRow();
		}

		internal static void HorizontalLine()
		{
			if (_backend == null)
				UIUtils.HorizontalLine();
			else
				_backend.Separator();
		}
		#endregion

		#region GUILayout
		internal static void Label(string text, GUIStyle style, params GUILayoutOption[] options)
		{
			if (_backend == null)
				GUILayout.Label(text, style, options);
			else
				_backend.Label(text, "", KindOf(style));
		}

		internal static void Label(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
		{
			if (_backend == null)
				GUILayout.Label(content, style, options);
			else
				_backend.Label(content.text, content.tooltip, KindOf(style));
		}

		internal static bool Button(string text, GUIStyle style, params GUILayoutOption[] options)
		{
			if (_backend == null)
				return GUILayout.Button(text, style, options);
			return _backend.Button(text, "");
		}

		internal static bool Button(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
		{
			if (_backend == null)
				return GUILayout.Button(content, style, options);
			return _backend.Button(content.text, content.tooltip);
		}

		internal static int SelectionGrid(int selected, string[] texts, int xCount, GUIStyle style, params GUILayoutOption[] options)
		{
			if (_backend == null)
				return GUILayout.SelectionGrid(selected, texts, xCount, style, options);
			return _backend.Select(selected, texts, xCount, "");
		}

		/// <summary>Kit panel only (the sections call it behind an IblUi.Classic check).</summary>
		internal static int Dropdown(int selected, string[] options, string tooltip)
		{
			return _backend != null ? _backend.Dropdown(selected, options, tooltip) : selected;
		}

		internal static float HorizontalSlider(float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, params GUILayoutOption[] options)
		{
			if (_backend == null)
				return GUILayout.HorizontalSlider(value, leftValue, rightValue, slider, thumb, options);
			return _backend.CompactSlider(value, leftValue, rightValue);
		}

		internal static string TextField(string text, GUIStyle style, params GUILayoutOption[] options)
		{
			if (_backend == null)
				return GUILayout.TextField(text, style, options);
			return _backend.TextField(text);
		}

		internal static void Space(float pixels)
		{
			if (_backend == null)
				GUILayout.Space(pixels);
			else
				_backend.Space(pixels);
		}

		internal static void FlexibleSpace()
		{
			if (_backend == null)
				GUILayout.FlexibleSpace();
		}

		internal static void BeginHorizontal(params GUILayoutOption[] options)
		{
			if (_backend == null)
				GUILayout.BeginHorizontal(options);
			else
				_backend.BeginRow();
		}

		internal static void BeginHorizontal(GUIStyle style, params GUILayoutOption[] options)
		{
			if (_backend == null)
				GUILayout.BeginHorizontal(style, options);
			else
				_backend.BeginRow();
		}

		internal static void EndHorizontal()
		{
			if (_backend == null)
				GUILayout.EndHorizontal();
			else
				_backend.EndRow();
		}

		internal static void BeginVertical(params GUILayoutOption[] options)
		{
			if (_backend == null)
				GUILayout.BeginVertical(options);
		}

		internal static void BeginVertical(GUIStyle style, params GUILayoutOption[] options)
		{
			if (_backend == null)
				GUILayout.BeginVertical(style, options);
		}

		internal static void EndVertical()
		{
			if (_backend == null)
				GUILayout.EndVertical();
		}
		#endregion

		private static bool IsTitle(GUIStyle style)
		{
			return style != null && (style == UIUtils.titleStyle2 || style == _kitTitleStyle);
		}

		private static LabelKind KindOf(GUIStyle style)
		{
			return IsTitle(style) ? LabelKind.Title : LabelKind.Body;
		}
	}
}
