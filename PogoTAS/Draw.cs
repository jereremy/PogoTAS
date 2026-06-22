using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PogoTAS
{
	public class Draw // from a dif project
	{
		static List<GUIStyle> Fonts = new List<GUIStyle>();
		static Color StaticColor = new Color();
		static Color StaticOutlineColor = new Color();

		public Draw()
		{
			Fonts = new List<GUIStyle>();
		}
		public static void Reset()
		{
			Fonts = new List<GUIStyle>();
			StaticColor = new Color();
		}

	/*	public static int CreateFont(string OSFontName, int Size, int Style)
		{

			if (Fonts.Count > 0)
			{
				for (int i = 0; i < Fonts.Count; i++)
				{
					if (Fonts[i] == null)
					{
						Fonts.RemoveAt(i);
						i -= 1;
					}
					else if (Fonts[i].font.name == OSFontName && Fonts[i].font.fontSize == Size && Fonts[i].fontStyle == (FontStyle)Style)
						return i;
				}
			}

			GUIStyle TextStyle = new GUIStyle();
			TextStyle.font = Font.CreateDynamicFontFromOSFont(OSFontName, Size);
			TextStyle.fontStyle = (FontStyle)Style;
			TextStyle.contentOffset = new Vector2(0f, -5f);
			TextStyle.alignment = TextAnchor.UpperLeft;
			Fonts.Add(TextStyle);
			return Fonts.Count - 1;
		}*/

		public static Vector3 WorldToScreen(Vector3 WorldPosition)
		{
			Camera mainCamera = Camera.main;
			if (mainCamera == null)
			{
				return Vector3.zero;
			}
			Vector3 ScreenPoint = mainCamera.WorldToScreenPoint(WorldPosition, Camera.MonoOrStereoscopicEye.Mono);

			return new Vector3(ScreenPoint.x, Screen.height - ScreenPoint.y, ScreenPoint.z);
		}

		public static void Rect(float x, float y, float Width, float Height, Color32 rgba)
		{
			GL.Begin(GL.LINE_STRIP);

			StaticColor.r = ((float)rgba.r) * 0.003921568627f;
			StaticColor.g = ((float)rgba.g) * 0.003921568627f;
			StaticColor.b = ((float)rgba.b) * 0.003921568627f;
			StaticColor.a = ((float)rgba.a) * 0.003921568627f;

			GL.Color(StaticColor);

			x = (int)x;
			y = (int)y;
			Width = (int)Width;
			Height = (int)Height;

			x += .5f;
			y += .5f;
			Width -= 1f;
			Height -= 1f;

			GL.Vertex3(x, y, 0f);
			GL.Vertex3(x, y + Height, 0f);
			GL.Vertex3(x + Width, y + Height, 0f);
			GL.Vertex3(x + Width, y, 0f);
			GL.Vertex3(x, y, 0f);

			GL.End();


			//RectDrawQueue.Add(new DrawDesc(DrawQStack, x, y, Width, Height, rgba));
			// DrawQStack += 1;
		}

		public static void FilledRect(float x, float y, float Width, float Height, Color32 rgba)
		{
			GL.Begin(GL.QUADS);

			StaticColor.r = ((float)rgba.r) * 0.003921568627f;
			StaticColor.g = ((float)rgba.g) * 0.003921568627f;
			StaticColor.b = ((float)rgba.b) * 0.003921568627f;
			StaticColor.a = ((float)rgba.a) * 0.003921568627f;

			GL.Color(StaticColor);

			x = (int)x;
			y = (int)y;
			Width = (int)Width;
			Height = (int)Height;

			if (Width < 0 || Height < 0) // bec backface culling
			{
				GL.Vertex3(x, y, 0f);
				GL.Vertex3(x, y + Height, 0f);
				GL.Vertex3(x + Width, y + Height, 0f);
				GL.Vertex3(x + Width, y, 0f);
			}
			else
			{
				GL.Vertex3(x, y, 0f);
				GL.Vertex3(x + Width, y, 0f);
				GL.Vertex3(x + Width, y + Height, 0f);
				GL.Vertex3(x, y + Height, 0f);
			}

			GL.End();
			//FilledRectDrawQueue.Add(new DrawDesc(DrawQStack, x, y, Width, Height, rgba));
			//DrawQStack += 1;
		}

		public static void Line(float x1, float y1, float x2, float y2, Color32 rgba)
		{
			GL.Begin(GL.LINES);

			StaticColor.r = ((float)rgba.r) * 0.003921568627f;
			StaticColor.g = ((float)rgba.g) * 0.003921568627f;
			StaticColor.b = ((float)rgba.b) * 0.003921568627f;
			StaticColor.a = ((float)rgba.a) * 0.003921568627f;

			GL.Color(StaticColor);

			x1 = (int)x1;
			y1 = (int)y1;
			x2 = (int)x2;
			y2 = (int)y2;

			x1 += .5f;
			y1 += .5f;
			x2 += .5f;
			y2 += .5f;

			GL.Vertex3(x1, y1, 0f);
			GL.Vertex3(x2, y2, 0f);

			GL.End();

			//LineDrawQueue.Add(new DrawDesc(DrawQStack, x1, y1, x2, y2, rgba));
			//DrawQStack += 1;
		}


	/*	public static void Text(float x, float y, string Text, int FontIndex, TextAnchor Alignment, ProxyColor32 rgba)
		{
			x = (int)x;
			y = (int)y;
			if (Fonts == null || FontIndex < 0 || Fonts.Count <= FontIndex)
			{
				GUI.Label(new Rect(x, y, 0, 0), Text);
			}
			else
			{
				if (rgba == null)
				{
					rgba = ProxyColor32.white;
				}
				StaticColor.r = ((float)rgba.r) * 0.003921568627f;
				StaticColor.g = ((float)rgba.g) * 0.003921568627f;
				StaticColor.b = ((float)rgba.b) * 0.003921568627f;
				StaticColor.a = ((float)rgba.a) * 0.003921568627f;

				Fonts[FontIndex].normal.textColor = StaticColor;
				Fonts[FontIndex].alignment = Alignment;
				GUI.Label(new Rect(x, y, 0, 0), Text, Fonts[FontIndex]);
			}

			if (GameHandler.Instance.PaintMaterial != null)
				GameHandler.Instance.PaintMaterial.SetPass(0);


			//TextDrawQueue.Add(new DrawTextDesc(x, y, Text, FontIndex, Alignment, rgba));

		}*/

	/*	public static void OutlinedText(float x, float y, float OutlineWidth, float OutlineLayerCount, string Text, int FontIndex, TextAnchor Alignment, ProxyColor32 rgba, ProxyColor32 OutlineRGBA)
		{
			if (OutlineWidth != 0f && OutlineWidth < 1f)
				OutlineWidth = 1;

			if (OutlineLayerCount != 0f && OutlineLayerCount < 1f)
				OutlineLayerCount = 1;

			bool DoOutline = (OutlineWidth > 0f && OutlineLayerCount > 0f);

			OutlineWidth = Mathf.Floor(OutlineWidth);
			OutlineLayerCount = Mathf.Floor(OutlineLayerCount);

			float OutlineStepSize = OutlineWidth / OutlineLayerCount;

			x = (int)x;
			y = (int)y;
			if (Fonts == null || FontIndex < 0 || Fonts.Count <= FontIndex)
			{
				Color OgColor = GUI.color;
				GUI.color = Color.black;
				if (DoOutline)
				{
					for (float OutStep = OutlineStepSize; OutStep <= OutlineWidth; OutStep += OutlineStepSize)
					{
						GUI.Label(new Rect(x + OutStep, y + OutStep, 0, 0), Text);
						GUI.Label(new Rect(x - OutStep, y + OutStep, 0, 0), Text);
						GUI.Label(new Rect(x + OutStep, y - OutStep, 0, 0), Text);
						GUI.Label(new Rect(x - OutStep, y - OutStep, 0, 0), Text);
					}
				}

				GUI.color = Color.white;
				GUI.Label(new Rect(x, y, 0, 0), Text);
				GUI.color = OgColor;
			}
			else
			{
				if (rgba == null)
				{
					rgba = ProxyColor32.white;
				}

				StaticColor.r = ((float)rgba.r) * 0.003921568627f;
				StaticColor.g = ((float)rgba.g) * 0.003921568627f;
				StaticColor.b = ((float)rgba.b) * 0.003921568627f;
				StaticColor.a = ((float)rgba.a) * 0.003921568627f;

				if (DoOutline)
				{
					if (OutlineRGBA == null)
					{
						OutlineRGBA = ProxyColor32.black;
					}


					StaticOutlineColor.r = ((float)OutlineRGBA.r) * 0.003921568627f;
					StaticOutlineColor.g = ((float)OutlineRGBA.g) * 0.003921568627f;
					StaticOutlineColor.b = ((float)OutlineRGBA.b) * 0.003921568627f;
					StaticOutlineColor.a = ((float)OutlineRGBA.a) * 0.003921568627f;

					Fonts[FontIndex].normal.textColor = StaticOutlineColor;
					Fonts[FontIndex].alignment = Alignment;
					for (float OutStep = OutlineStepSize; OutStep <= OutlineWidth; OutStep += OutlineStepSize)
					{
						GUI.Label(new Rect(x + OutStep, y + OutStep, 0, 0), Text, Fonts[FontIndex]);
						GUI.Label(new Rect(x - OutStep, y + OutStep, 0, 0), Text, Fonts[FontIndex]);
						GUI.Label(new Rect(x + OutStep, y - OutStep, 0, 0), Text, Fonts[FontIndex]);
						GUI.Label(new Rect(x - OutStep, y - OutStep, 0, 0), Text, Fonts[FontIndex]);
					}
				}


				Fonts[FontIndex].normal.textColor = StaticColor;
				Fonts[FontIndex].alignment = Alignment;
				GUI.Label(new Rect(x, y, 0, 0), Text, Fonts[FontIndex]);
			}

			if (GameHandler.Instance.PaintMaterial != null)
				GameHandler.Instance.PaintMaterial.SetPass(0);


			//TextDrawQueue.Add(new DrawTextDesc(x, y, Text, FontIndex, Alignment, rgba));

		}*/

	/*	public static float[] GetTextSize(string Text, int FontIndex)
		{
			if (Fonts == null || FontIndex < 0 || Fonts.Count <= FontIndex)
				return new float[] { 0f, 0f };

			if (Text == "")
				return new float[] { 0f, Fonts[FontIndex].font.fontSize };

			float[] WH = new float[2];

			WH[1] = Fonts[FontIndex].font.fontSize;
			foreach (char c in Text)
			{
				Fonts[FontIndex].font.GetCharacterInfo(c, out CharacterInfo charInfo, Fonts[FontIndex].font.fontSize);
				WH[0] += charInfo.advance;
			}

			return WH;
		}*/
	}
}
