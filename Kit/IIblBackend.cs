using UnityEngine;

namespace HSIBL
{
	/// <summary>
	/// The controls the HSIBL sections draw, in retained form. The sections stay immediate-mode code: they
	/// run every frame and call these in order; a backend maps each call to a persistent control and
	/// returns either the edited value or the value it was given.
	/// </summary>
	internal interface IIblBackend
	{
		bool Enabled { get; set; }
		Color Color { get; set; }

		void Label(string text, string tooltip, LabelKind kind);
		bool Button(string text, string tooltip);
		bool ToggleButton(bool value, string text, string tooltip);
		float Slider(float value, float min, float max, float reset, string label, string tooltip, int decimals, bool hasMinMaxLimit, bool hasMinLimit);
		float CompactSlider(float value, float min, float max);
		bool Toggle(bool value, string label, string tooltip, bool header);
		/// <summary>Returns true and the new color when it was changed this frame.</summary>
		bool ColorField(Color value, Color reset, string label, string tooltip, bool alpha, out Color result);
		int Select(int selected, string[] options, int columns, string tooltip);
		int Dropdown(int selected, string[] options, string tooltip);
		string TextField(string text);
		void Separator();
		void Space(float height);
		void BeginRow();
		void EndRow();
	}

	internal enum LabelKind { Body, Title, Dim }
}
