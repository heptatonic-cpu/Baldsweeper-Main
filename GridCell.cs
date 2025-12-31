
using System;
using UnityEngine;

namespace BALDsweeper
{
	
	public class GridCell : MonoBehaviour
	{
		public GridCell(int r = 0, int c = 0)
		{
			row = r;
			column = c;
		}
		
		public static Texture baseSprite
		{
			get
			{
				return BasePlugin.Assets.Get<Texture>("tile_unk");
			}
		}

		public static Texture2D bombSprite
		{
			get
			{
				return BasePlugin.Assets.Get<Texture2D>("tile_bomb");
			}
		}
		
		public static Texture2D flaggedSprite
		{
			get
			{
				return BasePlugin.Assets.Get<Texture2D>("tile_flag");
			}
		}
		
		public int row;
		
		public int column;
		
		public int bombsNearby;
		
		public CellType type;
		
		public bool bomb;
		
		public bool flagged;
		
		public bool revealed;
	}
}
