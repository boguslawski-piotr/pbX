﻿using System;
namespace pbXForms
{
	public class Metrics
	{
#if __IOS__
		public static int StatusBarHeight = 20;
#endif
#if __ANDROID__
		public static int StatusBarHeight = 24;
#endif
		public static int ScreenEdgeMargin = 16;

		public static int AppBarHeightPortrait = 56;
		public static int AppBarHeightLandscape = 48;

		public static int ToolBarHeightPortrait = 48;
		public static int ToolBarHeightLandscape = 48;
		public static int ToolBarItemsSpacing = 24;

		public static int TouchTargetHeight = 48;
		public static int IconHeight = 24;

		public static int ButtonItemsSpacing = 8;
	}
}
