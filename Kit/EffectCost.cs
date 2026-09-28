using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace HSLRE
{
	/// <summary>
	/// Cost per effect (addition): an on-demand measurement of how many milliseconds each effect in HSLRE's
	/// chain takes. For about a second every timed effect is fenced by a GPU sync (a 1-pixel readback, which
	/// waits for all queued rendering), so the time between the fences is that effect's real CPU + GPU cost.
	/// The syncs stall the pipeline, so this only runs when asked for; frame rate dips while it does.
	/// Each effect's result is the MEDIAN of its per-frame times (a mean let single hitches - a GC pause,
	/// another plugin rendering - move the number between runs).
	/// </summary>
	internal static class EffectCost
	{
		private const int WarmupFrames = 5, SampleFrames = 90;

		private static readonly Stopwatch _clock = Stopwatch.StartNew();
		// per effect: its time in each sampled frame (summed if it renders more than once in a frame)
		private static readonly Dictionary<object, List<double>> _frames = new Dictionary<object, List<double>>();
		private static readonly Dictionary<object, float> _results = new Dictionary<object, float>();
		private static int _framesLeft, _lastTickFrame = -1, _syncFrame = -1;
		private static double _syncCost; // ms one fence costs by itself, measured each sampled frame
		private static object _pending;
		private static readonly Dictionary<object, int> _lastSampleFrame = new Dictionary<object, int>();
		private static double _pendingStart;
		private static RenderTexture _fenceTarget;
		private static Texture2D _fenceRead;

		/// <summary>Bumped whenever new results are in (for the UI to refresh).</summary>
		internal static int Version { get; private set; }

		internal static bool Measuring => _framesLeft > 0;

		/// <summary>Sum of the last results, in ms.</summary>
		internal static float Total { get; private set; }

		internal static void Begin()
		{
			_frames.Clear();
			_pending = null;
			_framesLeft = WarmupFrames + SampleFrames;
		}

		/// <summary>Last measured cost of an effect in ms; false when it wasn't measured (off, or not timeable).</summary>
		internal static bool TryGet(object effect, out float ms)
		{
			return _results.TryGetValue(effect, out ms);
		}

		/// <summary>Once per frame (from Update): counts the measurement down and publishes the averages.</summary>
		internal static void Tick()
		{
			if (_framesLeft <= 0 || Time.frameCount == _lastTickFrame)
				return;
			_lastTickFrame = Time.frameCount;
			if (--_framesLeft > 0)
				return;
			_results.Clear();
			float total = 0f;
			foreach (KeyValuePair<object, List<double>> pair in _frames)
			{
				List<double> times = pair.Value;
				if (times.Count == 0)
					continue;
				times.Sort();
				int mid = times.Count / 2;
				float ms = (float)(times.Count % 2 == 1 ? times[mid] : (times[mid - 1] + times[mid]) * 0.5);
				_results[pair.Key] = ms;
				total += ms;
			}
			Total = total;
			_pending = null;
			Version++;
		}

		private static bool Sampling => _framesLeft > 0 && _framesLeft <= SampleFrames;

		/// <summary>Right before an effect renders.</summary>
		internal static void Start(object effect)
		{
			if (Sampling == false)
				return;
			if (_syncFrame != Time.frameCount)
			{
				// what a fence costs on its own this frame, to take out of every sample
				_syncFrame = Time.frameCount;
				Fence();
				double a = Now();
				Fence();
				_syncCost = Now() - a;
			}
			Fence();
			_pending = effect;
			_pendingStart = Now();
		}

		/// <summary>Right after that effect rendered. Ignored unless it is the one Start was called for.</summary>
		internal static void Stop(object effect)
		{
			if (_pending == null || ReferenceEquals(_pending, effect) == false)
				return;
			_pending = null;
			if (Sampling == false)
				return;
			Fence();
			double ms = Now() - _pendingStart - _syncCost;
			if (ms < 0)
				ms = 0;
			List<double> times;
			if (_frames.TryGetValue(effect, out times) == false)
				_frames[effect] = times = new List<double>(SampleFrames);
			// one entry per frame: a second run in the same frame adds to that frame's entry
			if (times.Count != 0 && _lastSampleFrame.ContainsKey(effect) && _lastSampleFrame[effect] == Time.frameCount)
				times[times.Count - 1] += ms;
			else
				times.Add(ms);
			_lastSampleFrame[effect] = Time.frameCount;
		}

		private static double Now()
		{
			return _clock.Elapsed.TotalMilliseconds;
		}

		/// <summary>Waits until the GPU has done everything queued so far (reading a pixel back has to).</summary>
		private static void Fence()
		{
			if (_fenceTarget == null)
			{
				_fenceTarget = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32) { hideFlags = HideFlags.DontSave };
				_fenceTarget.Create();
				_fenceRead = new Texture2D(1, 1, TextureFormat.ARGB32, false) { hideFlags = HideFlags.DontSave };
			}
			RenderTexture previous = RenderTexture.active;
			RenderTexture.active = _fenceTarget;
			GL.Clear(false, true, Color.clear);
			_fenceRead.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
			RenderTexture.active = previous;
		}
	}
}
