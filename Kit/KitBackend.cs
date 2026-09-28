using System.Collections.Generic;
using System.Globalization;
using HSUIKit;
using UnityEngine;
using UnityEngine.UI;

namespace HSIBL
{
	/// <summary>
	/// Draws the HSIBL sections with HSUIKit controls. The section code runs once per frame and calls the
	/// controls in order; each call is matched to the control made for it in an earlier frame (same kind and
	/// label), so controls persist, and anything changed since the last frame is handed back as the
	/// call's return value. Controls a section stops drawing are removed, new ones are inserted in place.
	/// </summary>
	internal sealed class KitBackend : IIblBackend
	{
		private readonly Container _root;
		private readonly Stack<Container> _stack = new Stack<Container>();

		public bool Enabled { get; set; } = true;
		public Color Color { get; set; } = Color.white;

		internal KitBackend(RectTransform content)
		{
			this._root = new Container(content);
		}

		/// <summary>Removes every control, e.g. when a different section is shown.</summary>
		internal void Clear()
		{
			this._root.DestroyFrom(0);
		}

		internal void BeginFrame()
		{
			this.Enabled = true;
			this.Color = Color.white;
			this._stack.Clear();
			this._stack.Push(this._root);
			this._root.Cursor = 0;
		}

		internal void EndFrame()
		{
			while (this._stack.Count > 1)
				this.EndRow();
			this._root.DestroyFrom(this._root.Cursor);
		}

		#region IIblBackend
		public void Label(string text, string tooltip, LabelKind kind)
		{
			if (string.IsNullOrEmpty(text))
				return;
			string k = kind == LabelKind.Title ? "title" : "label";
			LabelNode n = this.Reuse<LabelNode>(k, "") ?? this.Insert(new LabelNode(this.Parent, kind), k, "");
			n.Sync(text, this.Color, tooltip);
			this.Apply(n);
		}

		public bool Button(string text, string tooltip)
		{
			string key = StableKey(text);
			ButtonNode n = this.Reuse<ButtonNode>("button", key) ?? this.Insert(new ButtonNode(this.Parent, text, tooltip), "button", key);
			n.SetText(text);
			this.Apply(n);
			return n.TakeClick();
		}

		public bool ToggleButton(bool value, string text, string tooltip)
		{
			string key = StableKey(text);
			ButtonNode n = this.Reuse<ButtonNode>("togglebutton", key) ?? this.Insert(new ButtonNode(this.Parent, text, tooltip), "togglebutton", key);
			n.SetText(text);
			n.SetStyle(value ? ButtonStyle.Primary : ButtonStyle.Secondary);
			this.Apply(n);
			return n.TakeClick() ? !value : value;
		}

		public float Slider(float value, float min, float max, float reset, string label, string tooltip, int decimals, bool hasMinMaxLimit, bool hasMinLimit)
		{
			string key = StableKey(label);
			SliderNode n = this.Reuse<SliderNode>("slider", key) ?? this.Insert(new SliderNode(this.Parent, SliderTooltip(tooltip, min, max, reset, hasMinMaxLimit, hasMinLimit)), "slider", key);
			bool outOfBounds = (hasMinMaxLimit && (value < min || value > max)) || (hasMinLimit && value < min);
			n.Configure(label + (outOfBounds ? " (!)" : ""), min, max, reset, decimals, hasMinMaxLimit, hasMinLimit);
			this.Apply(n);
			return n.Sync(value);
		}

		public float CompactSlider(float value, float min, float max)
		{
			CompactSliderNode n = this.Reuse<CompactSliderNode>("compactslider", "") ?? this.Insert(new CompactSliderNode(this.Parent, min, max, value), "compactslider", "");
			this.Apply(n);
			return n.Sync(value, min, max);
		}

		public bool Toggle(bool value, string label, string tooltip, bool header)
		{
			string k = header ? "header" : "toggle", key = StableKey(label);
			ToggleNode n = this.Reuse<ToggleNode>(k, key) ?? this.Insert(new ToggleNode(this.Parent, key, value, tooltip, header), k, key);
			this.Apply(n);
			return n.Sync(value);
		}

		public bool ColorField(Color value, Color reset, string label, string tooltip, bool alpha, out Color result)
		{
			string key = StableKey(label);
			ColorNode n = this.Reuse<ColorNode>("color", key) ?? this.Insert(new ColorNode(this.Parent, key, value, reset, alpha, tooltip), "color", key);
			this.Apply(n);
			return n.Sync(value, out result);
		}

		public int Select(int selected, string[] options, int columns, string tooltip)
		{
			string key = options.Length != 0 ? options[0] : "";
			SelectNode n = this.Reuse<SelectNode>("select", key) ?? this.Insert(new SelectNode(this.Parent, options, selected, columns, tooltip, false), "select", key);
			this.Apply(n);
			return n.Sync(selected, options);
		}

		public int Dropdown(int selected, string[] options, string tooltip)
		{
			SelectNode n = this.Reuse<SelectNode>("dropdown", "") ?? this.Insert(new SelectNode(this.Parent, options, selected, 0, tooltip, true), "dropdown", "");
			this.Apply(n);
			return n.Sync(selected, options);
		}

		public string TextField(string text)
		{
			TextNode n = this.Reuse<TextNode>("text", "") ?? this.Insert(new TextNode(this.Parent, text), "text", "");
			this.Apply(n);
			return n.Sync(text);
		}

		public void Separator()
		{
			if (this.Reuse<SeparatorNode>("separator", "") == null)
				this.Insert(new SeparatorNode(this.Parent), "separator", "");
		}

		public void Space(float height)
		{
			// the kit layout already spaces its rows
		}

		public void BeginRow()
		{
			RowNode row = this.Reuse<RowNode>("row", "") ?? this.Insert(new RowNode(this.Parent), "row", "");
			row.Container.Cursor = 0;
			this._stack.Push(row.Container);
		}

		public void EndRow()
		{
			if (this._stack.Count <= 1)
				return;
			Container c = this._stack.Pop();
			c.DestroyFrom(c.Cursor);
		}
		#endregion

		#region Matching
		// Matching is split into Reuse / Insert (instead of passing a factory lambda) so a frame where
		// every control already exists allocates nothing.
		private RectTransform Parent => this._stack.Peek().Rect;

		private T Reuse<T>(string kind, string key) where T : Node
		{
			Container c = this._stack.Peek();
			List<Node> nodes = c.Nodes;
			for (int j = c.Cursor; j < nodes.Count; j++)
			{
				Node candidate = nodes[j];
				if (candidate.Kind == kind && candidate.Key == key && candidate is T match)
				{
					// rows the section no longer draws were between the cursor and the match
					for (int k = c.Cursor; k < j; k++)
						nodes[k].Destroy();
					nodes.RemoveRange(c.Cursor, j - c.Cursor);
					c.Cursor++;
					return match;
				}
			}
			return null;
		}

		private T Insert<T>(T node, string kind, string key) where T : Node
		{
			Container c = this._stack.Peek();
			List<Node> nodes = c.Nodes;
			node.Kind = kind;
			node.Key = key;
			nodes.Insert(c.Cursor, node);
			// relative to the previous control: removed controls stay in the hierarchy until the frame ends
			node.Root.SetSiblingIndex(c.Cursor > 0 ? nodes[c.Cursor - 1].Root.GetSiblingIndex() + 1 : 0);
			c.Cursor++;
			return node;
		}

		private void Apply(Node n)
		{
			n.SetInteractable(this.Enabled);
		}

		/// <summary>The sections number their titles by render order ("3 Bloom", "4.1 Tonemapping"); matching ignores that.</summary>
		private static string StableKey(string label)
		{
			return StripIndex(label);
		}

		private static string StripIndex(string label)
		{
			if (string.IsNullOrEmpty(label))
				return "";
			int i = 0;
			while (i < label.Length && (char.IsDigit(label[i]) || label[i] == '.'))
				i++;
			if (i > 0 && i < label.Length && label[i] == ' ')
				return label.Substring(i + 1);
			return label;
		}

		private static string SliderTooltip(string tooltip, float min, float max, float reset, bool hasMinMaxLimit, bool hasMinLimit)
		{
			string t = tooltip ?? "";
			if (hasMinMaxLimit)
				t += $"\n[Min: {min}    Max: {max}]";
			else if (hasMinLimit)
				t += $"\n[Min: {min}]";
			t += $"\nDefault: {reset}  (right-click the slider to reset)";
			return t.Trim();
		}
		#endregion

		#region Nodes
		private sealed class Container
		{
			internal readonly RectTransform Rect;
			internal readonly List<Node> Nodes = new List<Node>();
			internal int Cursor;

			internal Container(RectTransform rect)
			{
				this.Rect = rect;
			}

			internal void DestroyFrom(int index)
			{
				for (int i = index; i < this.Nodes.Count; i++)
					this.Nodes[i].Destroy();
				if (index < this.Nodes.Count)
					this.Nodes.RemoveRange(index, this.Nodes.Count - index);
			}
		}

		private abstract class Node
		{
			internal string Kind, Key;
			internal RectTransform Root;
			private CanvasGroup _group;
			private bool _interactable = true;

			internal void SetInteractable(bool on)
			{
				if (on == this._interactable)
					return;
				this._interactable = on;
				if (this._group == null)
					this._group = this.Root.gameObject.AddComponent<CanvasGroup>();
				this._group.alpha = on ? 1f : 0.45f;
				this._group.interactable = on;
				this._group.blocksRaycasts = on;
			}

			internal virtual void Destroy()
			{
				if (this.Root != null)
				{
					this.Root.gameObject.SetActive(false);
					Object.Destroy(this.Root.gameObject);
				}
			}

			protected static void Tip(GameObject go, string tooltip)
			{
				if (string.IsNullOrEmpty(tooltip) == false)
					KitTooltip.Attach(go, tooltip);
			}
		}

		private sealed class RowNode : Node
		{
			internal readonly Container Container;

			internal RowNode(RectTransform parent)
			{
				this.Root = KitLayout.Row(parent, 8f);
				this.Root.name = "Row";
				this.Container = new Container(this.Root);
			}

			internal override void Destroy()
			{
				this.Container.DestroyFrom(0);
				base.Destroy();
			}
		}

		private sealed class LabelNode : Node
		{
			private readonly KitText _text;
			private readonly LabelKind _kind;
			private string _shown, _tip;
			private Color _shownColor = Color.white;

			internal LabelNode(RectTransform parent, LabelKind kind)
			{
				this._kind = kind;
				this._text = KitText.Create(parent, "", kind == LabelKind.Title ? TextRole.Header : (kind == LabelKind.Dim ? TextRole.Dim : TextRole.Body), true);
				this.Root = (RectTransform)this._text.transform;
				LayoutElement le = this.Root.GetComponent<LayoutElement>() ?? this.Root.gameObject.AddComponent<LayoutElement>();
				le.flexibleWidth = 1f;
			}

			internal void Sync(string text, Color color, string tooltip)
			{
				if (text != this._shown)
				{
					this._shown = text;
					this._text.Value = text.Replace("\r\n", "\n").Replace("\r", "\n");
				}
				if (color != this._shownColor)
				{
					this._shownColor = color;
					// the sections tint warnings (yellow / red); anything else keeps the theme color
					this._text.Text.color = color == Color.white ? (this._kind == LabelKind.Dim ? Theme.TextDim : Theme.Text) : Color.Lerp(color, Theme.Text, 0.15f);
				}
				if (tooltip != this._tip)
				{
					this._tip = tooltip;
					Tip(this.Root.gameObject, tooltip);
				}
			}
		}

		private sealed class ButtonNode : Node
		{
			private readonly KitButton _button;
			private bool _clicked;
			private string _text;

			internal ButtonNode(RectTransform parent, string text, string tooltip)
			{
				this._text = text;
				this._button = KitButton.Create(parent, text, ButtonStyle.Secondary, () => this._clicked = true);
				this.Root = (RectTransform)this._button.transform;
				Tip(this.Root.gameObject, tooltip);
			}

			internal void SetText(string text)
			{
				if (text != this._text)
				{
					this._text = text;
					this._button.Label = text;
				}
			}

			internal void SetStyle(ButtonStyle style)
			{
				if (this._button.Style != style)
					this._button.Style = style;
			}

			internal bool TakeClick()
			{
				bool c = this._clicked;
				this._clicked = false;
				return c;
			}
		}

		private sealed class SliderNode : Node
		{
			private readonly Text _label;
			private readonly KitSlider _slider;
			private readonly KitNumberInput _number;
			private float _reset, _min, _max;
			private bool _limitBoth, _limitMin;
			private string _shownLabel;
			private int _decimals = -1;
			private float? _pending;

			internal SliderNode(RectTransform parent, string tooltip)
			{
				this.Root = Kit.Rect(parent, "SliderRow");
				VerticalLayoutGroup v = this.Root.gameObject.AddComponent<VerticalLayoutGroup>();
				v.spacing = 0f;
				v.childForceExpandWidth = true;
				v.childForceExpandHeight = false;
				this.Root.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

				RectTransform top = KitLayout.Row(this.Root, 6f);
				Kit.Layout(top.gameObject, -1f, 28f);
				this._label = Kit.MakeText(top, "Label", "", Theme.FontSize, Theme.Text, TextAnchor.MiddleLeft);
				this._label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
				Tip(this._label.gameObject, tooltip);
				Kit.HitArea(this._label.gameObject);

				this._number = KitNumberInput.Create(top, 0f, false, v2 => this._pending = this.Limit(v2));
				Kit.Layout(this._number.gameObject, 82f, 26f);

				KitIconButton reset = KitIconButton.Create(top, Sprites.Reset, 24f, false, () => this._pending = this._reset);
				KitTooltip.Attach(reset.gameObject, "Reset to default");

				this._slider = KitSlider.Create(this.Root, null, 0f, 1f, 0f, false, v2 => this._pending = v2);
				KitContextMenu.Attach(this._slider.transform.parent.gameObject, () => new KitMenu().Item("Reset to default", () => this._pending = this._reset));
			}

			internal void Configure(string label, float min, float max, float reset, int decimals, bool limitBoth, bool limitMin)
			{
				if (label != this._shownLabel)
				{
					this._shownLabel = label;
					this._label.text = label;
				}
				this._reset = reset;
				this._limitBoth = limitBoth;
				this._limitMin = limitMin;
				if (min != this._min || max != this._max)
				{
					this._min = min;
					this._max = max;
					this._slider.SetRange(min, max);
					this._number.Step = Mathf.Max((max - min) / 400f, Mathf.Pow(10f, -Mathf.Max(0, decimals)));
				}
				if (decimals != this._decimals)
				{
					this._decimals = decimals;
					string format = decimals <= 0 ? "0" : "0." + new string('#', decimals);
					this._number.Format = format;
					this._slider.Format = decimals <= 0 ? "0" : "0." + new string('0', Mathf.Min(decimals, 3));
				}
			}

			private float Limit(float v)
			{
				if (this._limitBoth)
					return Mathf.Clamp(v, this._min, this._max);
				if (this._limitMin)
					return Mathf.Max(v, this._min);
				return v;
			}

			internal float Sync(float value)
			{
				if (this._pending.HasValue)
				{
					value = this._pending.Value;
					this._pending = null;
				}
				if (this._slider.Dragging == false && Mathf.Approximately(this._slider.Value, Mathf.Clamp(value, this._min, this._max)) == false)
					this._slider.SetValue(value, false, false);
				if (this._number.Input.Field.isFocused == false && Mathf.Approximately(this._number.Value, value) == false)
					this._number.SetValue(value, false);
				return value;
			}
		}

		private sealed class CompactSliderNode : Node
		{
			private readonly KitSlider _slider;
			private float _min, _max;
			private float? _pending;

			internal CompactSliderNode(RectTransform parent, float min, float max, float value)
			{
				this._min = min;
				this._max = max;
				this._slider = KitSlider.Create(parent, null, min, max, value, false, v => this._pending = v);
				this.Root = (RectTransform)this._slider.transform.parent;
			}

			internal float Sync(float value, float min, float max)
			{
				if (min != this._min || max != this._max)
				{
					this._min = min;
					this._max = max;
					this._slider.SetRange(min, max);
				}
				if (this._pending.HasValue)
				{
					value = this._pending.Value;
					this._pending = null;
				}
				if (this._slider.Dragging == false && Mathf.Approximately(this._slider.Value, Mathf.Clamp(value, this._min, this._max)) == false)
					this._slider.SetValue(value, false, false);
				return value;
			}
		}

		private sealed class ToggleNode : Node
		{
			private readonly KitSwitch _switch;
			private bool? _pending;

			internal ToggleNode(RectTransform parent, string label, bool value, string tooltip, bool header)
			{
				this._switch = KitSwitch.Create(parent, label, value, v => this._pending = v);
				this.Root = (RectTransform)this._switch.transform;
				if (header)
				{
					Kit.Layout(this.Root.gameObject, -1f, 36f);
					Text t = this.Root.Find("Label")?.GetComponent<Text>();
					if (t != null)
					{
						t.fontStyle = FontStyle.Bold;
						t.fontSize = Theme.FontSize + 2;
					}
				}
				Tip(this.Root.gameObject, tooltip);
			}

			internal bool Sync(bool value)
			{
				if (this._pending.HasValue)
				{
					value = this._pending.Value;
					this._pending = null;
				}
				if (this._switch.Value != value)
					this._switch.SetValue(value, true, false);
				return value;
			}
		}

		private sealed class ColorNode : Node
		{
			private readonly KitColorField _field;
			private readonly Color _reset;
			private Color? _pending;

			internal ColorNode(RectTransform parent, string label, Color value, Color reset, bool alpha, string tooltip)
			{
				this._reset = reset;
				this.Root = KitLayout.Row(parent, 6f);
				this.Root.name = "ColorRow";
				this._field = KitColorField.Create(this.Root, label.TrimEnd(':', ' '), value, alpha, c => this._pending = c);
				this._field.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
				Tip(this._field.gameObject, tooltip);
				KitIconButton resetButton = KitIconButton.Create(this.Root, Sprites.Reset, 24f, false, () => this._pending = this._reset);
				KitTooltip.Attach(resetButton.gameObject, "Reset to default");
			}

			internal bool Sync(Color value, out Color result)
			{
				if (this._pending.HasValue)
				{
					result = this._pending.Value;
					this._pending = null;
					this._field.SetValue(result, false);
					return true;
				}
				if (this._field.Value != value)
					this._field.SetValue(value, false);
				result = value;
				return false;
			}
		}

		private sealed class SelectNode : Node
		{
			private readonly KitSegmented _segmented;
			private readonly KitDropdown _dropdown;
			private int? _pending;
			private int _shown;
			private string[] _options;

			internal SelectNode(RectTransform parent, string[] options, int selected, int columns, string tooltip, bool dropdown)
			{
				this._shown = selected;
				this._options = options;
				int totalLength = 0;
				foreach (string o in options)
					totalLength += o.Length;
				if (dropdown == false && columns != 1 && options.Length <= 5 && totalLength <= 42 && selected >= 0 && selected < options.Length)
				{
					this._segmented = KitSegmented.Create(parent, options, Mathf.Clamp(selected, 0, options.Length - 1), i => this._pending = i);
					this.Root = (RectTransform)this._segmented.transform;
				}
				else
				{
					this._dropdown = KitDropdown.Create(parent, options, selected, i => this._pending = i);
					this.Root = (RectTransform)this._dropdown.transform;
				}
				Tip(this.Root.gameObject, tooltip);
			}

			internal int Sync(int selected, string[] options)
			{
				// Some sections build their option array every frame (SEGI's "Visualize GBuffer": new[] { "None", ... }).
				// Compared by reference, that looked like new options each frame, and SetOptions closes the dropdown -
				// it snapped shut as soon as it opened. Compare the names instead.
				if (this._dropdown != null && ReferenceEquals(options, this._options) == false)
				{
					bool same = this._options != null && this._options.Length == options.Length;
					for (int i = 0; same && i < options.Length; i++)
						same = options[i] == this._options[i];
					this._options = options;
					if (same == false)
					{
						this._dropdown.SetOptions(options, selected);
						this._shown = selected;
					}
				}
				if (this._pending.HasValue)
				{
					selected = this._pending.Value;
					this._pending = null;
				}
				if (selected != this._shown)
				{
					this._shown = selected;
					if (this._segmented != null)
					{
						if (selected >= 0 && selected < options.Length)
							this._segmented.Select(selected, false);
					}
					else
						this._dropdown.Selected = selected;
				}
				return selected;
			}
		}

		private sealed class TextNode : Node
		{
			private readonly KitInput _input;
			private string _pending;

			internal TextNode(RectTransform parent, string text)
			{
				this._input = KitInput.Create(parent, "", text, s => this._pending = s);
				this.Root = (RectTransform)this._input.transform;
				LayoutElement le = this.Root.GetComponent<LayoutElement>() ?? this.Root.gameObject.AddComponent<LayoutElement>();
				le.flexibleWidth = 1f;
			}

			internal string Sync(string text)
			{
				if (this._pending != null)
				{
					text = this._pending;
					this._pending = null;
				}
				if (this._input.Field.isFocused == false && this._input.Text != text)
					this._input.Text = text;
				return text;
			}
		}

		private sealed class SeparatorNode : Node
		{
			internal SeparatorNode(RectTransform parent)
			{
				Image line = Kit.Image(parent, "Separator", null, Theme.Border);
				this.Root = line.rectTransform;
				Kit.Layout(line.gameObject, -1f, 1f);
			}
		}
		#endregion
	}
}
