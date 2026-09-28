using HSUIKit;
using UnityEngine;

namespace HSIBL
{
	/// <summary>HSLRE's procedurally drawn icons (white shapes, tinted by the Image).</summary>
	internal static class KitPanelIcons
	{
		/// <summary>Studio toolbar icon: a sun, a disc with short rays.</summary>
		public static Sprite Toolbar => Sprites.Custom("lre_toolbar", 32, (x, y) =>
		{
			float dx = x - 16f, dy = y - 16f;
			float r = Mathf.Sqrt(dx * dx + dy * dy);
			float disc = Mathf.Clamp01(6.5f - r + 0.5f);
			float angle = Mathf.Atan2(dy, dx);
			float rayMask = Mathf.Clamp01(Mathf.Cos(angle * 8f) * 4f - 2.6f);
			float rays = r > 9f && r < 13.5f ? rayMask : 0f;
			return Mathf.Max(disc, rays);
		});

		/// <summary>Overwrite / save: an arrow pointing down into a tray.</summary>
		public static Sprite Save => Sprites.Custom("lre_save", 32, (x, y) =>
		{
			float d = Mathf.Min(
				Mathf.Min(Sprites.SegmentDistance(x, y, 16f, 25f, 16f, 12f), Mathf.Min(Sprites.SegmentDistance(x, y, 16f, 11f, 10.5f, 16.5f), Sprites.SegmentDistance(x, y, 16f, 11f, 21.5f, 16.5f))),
				Mathf.Min(Sprites.SegmentDistance(x, y, 7f, 13f, 7f, 7f), Mathf.Min(Sprites.SegmentDistance(x, y, 7f, 7f, 25f, 7f), Sprites.SegmentDistance(x, y, 25f, 7f, 25f, 13f))));
			return Mathf.Clamp01(1.7f - d);
		});

		/// <summary>Delete: a bin with a lid.</summary>
		public static Sprite Trash => Sprites.Custom("lre_trash", 32, (x, y) =>
		{
			float lid = Mathf.Min(Sprites.SegmentDistance(x, y, 8f, 23f, 24f, 23f), Sprites.SegmentDistance(x, y, 13.5f, 26f, 18.5f, 26f));
			float body = Mathf.Min(Mathf.Min(Sprites.SegmentDistance(x, y, 10f, 20f, 11f, 7f), Sprites.SegmentDistance(x, y, 22f, 20f, 21f, 7f)), Sprites.SegmentDistance(x, y, 11f, 7f, 21f, 7f));
			float ribs = Mathf.Min(Sprites.SegmentDistance(x, y, 14f, 17f, 14.3f, 10f), Sprites.SegmentDistance(x, y, 18f, 17f, 17.7f, 10f));
			return Mathf.Clamp01(1.6f - Mathf.Min(Mathf.Min(lid, body), ribs * 1.15f));
		});

		/// <summary>Measure cost: a stopwatch (ring, crown on top, one hand).</summary>
		public static Sprite Stopwatch => Sprites.Custom("lre_stopwatch", 32, (x, y) =>
		{
			float dx = x - 16f, dy = y - 14.5f;
			float ring = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 9.5f);
			float crown = Mathf.Min(Sprites.SegmentDistance(x, y, 16f, 24f, 16f, 27f), Sprites.SegmentDistance(x, y, 13f, 28f, 19f, 28f));
			float hand = Sprites.SegmentDistance(x, y, 16f, 14.5f, 19.5f, 19.5f);
			return Mathf.Clamp01(1.6f - Mathf.Min(Mathf.Min(ring, crown), hand));
		});

		/// <summary>Pin to favourites: a five-pointed star.</summary>
		public static Sprite Star => Sprites.Custom("lre_star", 32, (x, y) =>
		{
			float dx = x - 16f, dy = y - 15f;
			float r = Mathf.Sqrt(dx * dx + dy * dy);
			// angle from straight up, folded into one point's 72 degree sector: 0 at a tip, 1 between two tips
			float a = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
			float sector = Mathf.Repeat(a + 36f, 72f) - 36f;
			float t = Mathf.Abs(sector) / 36f;
			float edge = Mathf.Lerp(12.5f, 5.2f, Mathf.Pow(t, 0.8f));
			return Mathf.Clamp01(edge - r + 0.5f);
		});

		/// <summary>Drag handle: two columns of dots.</summary>
		public static Sprite Grip => Sprites.Custom("lre_grip", 16, (x, y) =>
		{
			float best = 99f;
			for (int cx = 0; cx < 2; cx++)
				for (int cy = 0; cy < 3; cy++)
				{
					float px = 5.5f + cx * 5f, py = 3.5f + cy * 4.5f;
					best = Mathf.Min(best, Mathf.Sqrt((x - px) * (x - px) + (y - py) * (y - py)));
				}
			return Mathf.Clamp01(1.6f - best);
		});
	}
}
