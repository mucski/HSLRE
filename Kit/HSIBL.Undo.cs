using System.Collections.Generic;
using Harmony;
using UnityEngine;

namespace HSIBL
{
	public partial class HSIBL
	{
		// Undo / redo (addition): a history of whole-settings states (the same SaveConfig XML the snapshot uses).
		// A state is recorded after each finished interaction with the panel (mouse release, Enter), so a slider
		// drag is one step. Undo and redo load a state back through LoadConfig.
		private const int UndoLimit = 50;
		private readonly List<string> _undo = new List<string>();
		private int _undoIndex = -1;

		internal bool HasUndoHistory => this._undoIndex >= 0;
		internal bool CanUndo => this._undoIndex > 0;
		internal bool CanRedo => this._undoIndex >= 0 && this._undoIndex < this._undo.Count - 1;

		/// <summary>Starts a fresh history at the current settings (window first opened, scene loaded).</summary>
		internal void ResetUndo()
		{
			this._undo.Clear();
			this._undoIndex = -1;
			string xml = this.SettingsToXml();
			if (xml == null)
				return;
			this._undo.Add(xml);
			this._undoIndex = 0;
		}

		/// <summary>Adds the current settings as a step if they differ from the current one.</summary>
		internal void RecordUndo()
		{
			if (this.ShowingSnapshot)
				return;
			if (this._undoIndex < 0)
			{
				this.ResetUndo();
				return;
			}
			string xml = this.SettingsToXml();
			if (xml == null || xml == this._undo[this._undoIndex])
				return;
			// a new change drops whatever could still be redone
			this._undo.RemoveRange(this._undoIndex + 1, this._undo.Count - this._undoIndex - 1);
			this._undo.Add(xml);
			if (this._undo.Count > UndoLimit)
				this._undo.RemoveAt(0);
			this._undoIndex = this._undo.Count - 1;
		}

		internal bool Undo()
		{
			if (this.ShowingSnapshot)
				return false;
			this.RecordUndo(); // a change not recorded yet becomes the step we come back to with redo
			if (this.CanUndo == false)
				return false;
			this._undoIndex--;
			this.ApplyUndoState();
			return true;
		}

		internal bool Redo()
		{
			if (this.ShowingSnapshot)
				return false;
			this.RecordUndo(); // anything changed since the last undo replaces the redo steps
			if (this.CanRedo == false)
				return false;
			this._undoIndex++;
			this.ApplyUndoState();
			return true;
		}

		private void ApplyUndoState()
		{
			this.LoadSettingsXml(this._undo[this._undoIndex]);
			// loading can round values slightly; keep the history in step with what is actually set now
			string now = this.SettingsToXml();
			if (now != null)
				this._undo[this._undoIndex] = now;
		}

		/// <summary>
		/// Studio undoes / redoes its own scene edits on Z / Shift+Z without checking Ctrl, so Ctrl+Z / Ctrl+Y
		/// over the HSLRE window would also undo the last pose or object move. Studio's shortcuts skip that frame.
		/// </summary>
		[HarmonyPatch(typeof(Studio.ShortcutKeyCtrl), "Update")]
		private static class ShortcutKeyCtrl_Update_Patches
		{
			private static bool Prefix()
			{
				return KitPanel.UndoShortcutPressed() == false;
			}
		}
	}
}
