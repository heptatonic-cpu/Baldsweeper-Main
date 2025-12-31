using System;
using System.Collections;
using System.Collections.Generic;
using MTM101BaldAPI;
using MTM101BaldAPI.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BALDsweeper
{
	// i lost this script so i decompiled it to dnspy just to get it back //
	public class MinesweeperManager : MonoBehaviour
	{
		public static MinesweeperManager Instance { get; private set; }

		public void Init()
		{
			MinesweeperManager.Instance = this;
			audMan = Instance.gameObject.AddComponent<AudioManager>();
			audMan.audioDevice = Instance.gameObject.AddComponent<AudioSource>();
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			msObj = new GameObject("MainGame");
			InitCamera();
		}

		public void InitCamera()
		{
			cameraTransform = new GameObject("CameraTransform").transform;
			cameraTransform.position += Vector3.up * 5f;
			if (gameCamera != null)
			{
				gameCamera.UpdateTargets(cameraTransform, 30);
			}
		}
		
		public void RemoveAllCells()
		{
			for (int x = 0; x < gridCells.GetLength(0); x++)
			{
				for (int y = 0; y < gridCells.GetLength(1); y++)
				{
					Destroy(gridCells[x, y].gameObject);
					
				}
			}
			gridCells = null;
		}

		public void InitUI()
		{
			flagsRemainCounter = UIHelpers.CreateText<TextMeshProUGUI>(BaldiFonts.ComicSans24, "flags remain", canvas.transform, Vector2.zero, false);
			flagsRemainCounter.color = Color.black;
			flagsRemainCounter.enableWordWrapping = false;
			dexIndicator = UIHelpers.CreateImage(BasePlugin.Assets.Get<Sprite>("ms_dexneutral"), canvas.transform, new Vector3(0f, 50f, 0f), true, 0.5f);
			dexIndicator.transform.SetAsFirstSibling();
			dexIndicator.rectTransform.anchorMin = Vector2.zero;
			dexIndicator.rectTransform.anchorMax = Vector2.zero;
			dexIndicator.rectTransform.anchoredPosition = new Vector2(320f, 300);
			StandardMenuButton button = dexIndicator.gameObject.ConvertToButton<StandardMenuButton>(true);
			button.OnPress.AddListener(delegate()
			                           {
			                           	RemoveAllCells();
			                           	BuildCells(gridRows, gridColumns, gridBombs);
			                           });
			
		}

		private void UpdateUI()
		{
			if (flagsRemainCounter != null)
			{
				flagsRemainCounter.text = string.Format("{0} / flagging = {1}", flags.ToString(), flagging.ToString());
			}
			UpdateDex();
		}
		
		private void UpdateDex()
		{
			if (dexIndicator != null)
			{
				if (gameover)
				{
					dexIndicator.sprite = BasePlugin.Assets.Get<Sprite>("ms_dexgameover");
				}
				else if (win)
				{
					dexIndicator.sprite = BasePlugin.Assets.Get<Sprite>("ms_dexcool");
				}
			}
		}
		private void DoGameOver()
		{
			if (gridCells != null)
			{
				for (int x = 0; x < gridCells.GetLength(0); x++)
				{
					for (int y = 0; y < gridCells.GetLength(1); y++)
					{
						GridCell cell = gridCells[x, y];
						if ((cell.bomb && cell.type == CellType.Mine) || (cell.flagged))
						{
							cell.revealed = true;
							UpdateCell(cell);
						}
					}
				}
			}
		}

		public void MoveCamera()
		{
			if (gameCamera != null)
			{
				gameCamera.UpdateTargets(cameraTransform, 30);
				if (Singleton<InputManager>.Instance.GetDigitalInput("UseItem", false))
				{
					Vector2 mov = CursorController.Movement;
					cameraRot += new Vector3(-mov.y, mov.x, 0f) * 1.15f;
					cameraRot.x = Mathf.Clamp(cameraRot.x, -89f, 89f);
					cameraTransform.eulerAngles = cameraRot;
				}
				Vector2 analog;
				Singleton<InputManager>.Instance.GetAnalogInput(movementData, out analogMove, out analog);
				float d = Singleton<InputManager>.Instance.GetDigitalInput("Run", false) ? 130f : 50f;
				cameraTransform.position += cameraTransform.forward * analogMove.y * Time.deltaTime * d;
				cameraTransform.position += cameraTransform.right * analogMove.x * Time.deltaTime * d;
			}
		}

		private void UpdateCursorRay()
		{
			Vector3 pos = new Vector3(CursorController.Instance.LocalPosition.x / cursorBounds.x * (float)Screen.width, (float)Screen.height + CursorController.Instance.LocalPosition.y / cursorBounds.y * (float)Screen.height);
			ray = gameCamera.camCom.ScreenPointToRay(pos);
			rayHit = Physics.Raycast(ray, out raycastHit, 1000f, 2363401);
			if (rayHit && (!gameover || !win) && Singleton<InputManager>.Instance.GetDigitalInput("Interact", false))
			{
				dexIndicator.sprite = BasePlugin.Assets.Get<Sprite>("ms_dexsurprised");
			}
			else
			{
				dexIndicator.sprite = BasePlugin.Assets.Get<Sprite>("ms_dexneutral");
			}
			if (rayHit && Singleton<InputManager>.Instance.GetDigitalInput("Interact", true) &&  (!gameover || !win))
			{
				GridCell hit = raycastHit.transform.GetComponent<GridCell>();
				
				if (!flagging)
				{
					audMan.PlaySingle(BasePlugin.Assets.Get<SoundObject>("ItemPickup"));
					if (hit.type == CellType.Empty)
					{
						Flood(hit);
					}
					else
					{
						RevealCell(hit);
					}
					if (hit.type == CellType.Mine)
					{
						DoGameOver();
					}
				}
				else
				{
					audMan.PlaySingle(BasePlugin.Assets.Get<SoundObject>("Boink"));
					FlagCell(hit);
				}
			}
		}

		public void FlagCell(GridCell cell)
		{
			if (cell != null && !cell.revealed)
			{
				if (!cell.flagged)
				{
					flags--;
					cell.flagged = true;
					UpdateCell(cell);
				}
				else
				{
					flags++;
					cell.flagged = false;
					UpdateCell(cell);
				}
			}
		}
		
		public void Flood(GridCell cell)
		{
			if (!cell.revealed && cell.type != CellType.Mine && !cell.flagged)
			{
				RevealCell(cell);
				if (cell.type == CellType.Empty)
				{
					for (int i = cell.row - 1; i <= cell.row + 1; i++)
					{
						for (int j = cell.column - 1; j <= cell.column + 1; j++)
						{
							if (i != cell.row || j != cell.column)
							{
								if (InBounds(i, j))
								{
									Flood(gridCells[i, j]);
								}
							}
						}
					}
				}
			}
			
		}

		public void BuildCells(int r, int c, int b)
		{
			gridRows = r;
			gridColumns = c;
			gridBombs = b;
			gridCells = new GridCell[r, c];
			List<Vector2Int> coordinates = new List<Vector2Int>();
			checked
			{
				for (int x = 0; x < r; x++)
				{
					for (int y = 0; y < c; y++)
					{
						coordinates.Add(new Vector2Int(x, y));
						BuildSingleCell(x, y);
					}
				}
				int totalCells = r * c;
				if (b >= totalCells)
				{
					Debug.LogWarning("Bomb count exceeded or is equal to cell count. This will set all cells as bombs!");
					b = totalCells;
				}
				for (int a = 0; a < b; a++)
				{
					int rnd = UnityEngine.Random.Range(0, coordinates.Count);
					Vector2Int bombPos = coordinates[rnd];
					MakeAsBomb(bombPos.x, bombPos.y);
					coordinates.RemoveAt(rnd);
				}
				CalculateBombsNearby();
			}
		}

		public void Update()
		{
			if (gameCamera != null)
			{
				MoveCamera();
				UpdateCursorRay();
			}
			if (Input.GetKeyDown(KeyCode.F))
			{
				flagging = !flagging;
			}
			UpdateUI();
		}

		public void BuildSingleCell(int x, int y)
		{
			GameObject tile = new GameObject("GridTile");
			gridCells[x, y] = tile.AddComponent<GridCell>();
			gridCells[x, y].type = CellType.Empty;
			gridCells[x, y].row = x;
			gridCells[x, y].column = y;
			tile.transform.SetParent(msObj.transform, true);
			tile.AddComponent<MeshFilter>().mesh = BasePlugin.Assets.Get<Mesh>("Quad");
			tile.AddComponent<MeshCollider>().sharedMesh = BasePlugin.Assets.Get<Mesh>("Quad");
			MeshRenderer meshRenderer = tile.AddComponent<MeshRenderer>();
			meshRenderer.material = new Material(BasePlugin.tileStandardShader);
			meshRenderer.material.SetTexture("_LightMap", Texture2D.whiteTexture);
			meshRenderer.material.SetMainTexture(GridCell.baseSprite);
			tile.transform.localPosition = new Vector3((float)x * 10f, -0.1f, (float)y * 10f);
			tile.transform.localScale = new Vector3(10f, 10f, 1f);
			tile.transform.eulerAngles = new Vector3(90f, 0f, 0f);
		}

		public void MakeAsBomb(int r, int c)
		{
			checked
			{
				if (gridCells[r, c] != null)
				{
					gridCells[r, c].type = CellType.Mine;
					gridCells[r, c].bomb = true;
					flags++;
				}
			}
		}

		public void RevealCell(GridCell curCell)
		{
			if (curCell != null)
			{
				if (!curCell.revealed && (!gameover || !win))
				{
					if (curCell.type == CellType.Mine)
					{
						gameover = true;
						audMan.PlaySingle(BasePlugin.Assets.Get<SoundObject>("BAL_Ohh"));
						audMan.PlaySingle(BasePlugin.Assets.Get<SoundObject>("Explosion"));
					}
					if (!curCell.flagged)
					{
						curCell.revealed = true;
						UpdateCell(curCell);
					}
				}
			}
		}
		
		

		public void UpdateCell(GridCell curCell)
		{
			if (curCell != null)
			{
				Material i = curCell.gameObject.GetComponent<MeshRenderer>().material;
				if (curCell.revealed)
				{
					if (curCell.type == CellType.Mine)
					{
						i.SetMainTexture(GridCell.bombSprite);
					}
					else if (curCell.type == CellType.Number)
					{
						i.SetMainTexture(BasePlugin.Assets.Get<Texture2D>("tile_" + curCell.bombsNearby));
					}
					else if (curCell.type == CellType.Empty)
					{
						i.SetMainTexture(BasePlugin.Assets.Get<Texture2D>("tile_0"));
					}
					if (curCell.flagged && curCell.type != CellType.Mine && gameover)
					{
						i.SetMainTexture(BasePlugin.Assets.Get<Texture2D>("tile_wf"));
					}
				}
				else if (curCell.flagged)
				{
					i.SetMainTexture(GridCell.flaggedSprite);
				}
				else if (!curCell.flagged)
				{
					i.SetMainTexture(GridCell.baseSprite);
				}
				
			}
		}

		public bool InBounds(int x, int y)
		{
			return x >= 0 && x < gridCells.GetLength(0) && y >= 0 && y < gridCells.GetLength(1);
		}

		public void CalculateBombsNearby()
		{
			int r = gridCells.GetLength(0);
			int c = gridCells.GetLength(1);
			checked
			{
				for (int x = 0; x < r; x++)
				{
					for (int y = 0; y < c; y++)
					{
						GridCell curCell = gridCells[x, y];
						if (!curCell.bomb)
						{
							int b = 0;
							for (int i = x - 1; i <= x + 1; i++)
							{
								for (int j = y - 1; j <= y + 1; j++)
								{
									if (i != x || j != y)
									{
										if (InBounds(i, j))
										{
											GridCell nearbyCell = gridCells[i, j];
											if (nearbyCell.bomb)
											{
												b++;
											}
										}
									}
								}
							}
							curCell.bombsNearby = b;
							if (curCell.bombsNearby > 0)
							{
								curCell.type = CellType.Number;
							}
						}
					}
				}
			}
		}

		public GameObject msObj;

		public CursorController cursor;

		public Vector2 cursorBounds;

		private Ray ray;

		private RaycastHit raycastHit;

		private bool rayHit;

		public GameCamera gameCamera;

		public Transform cameraTransform;

		private Vector3 cameraRot;

		private Vector2 analogMove;

		private AnalogInputData movementData = new AnalogInputData
		{
			steamAnalogId = "Movement",
			xAnalogId = "MovementX",
			yAnalogId = "MovementY",
			steamDeltaId = "",
			xDeltaId = "",
			yDeltaId = ""
		};

		public bool flagging;
		
		public bool gameover;

		public bool win;

		public int flags;

		public Canvas canvas;

		private TextMeshProUGUI flagsRemainCounter;

		private TextMeshProUGUI timeCounter;
		
		private Image dexIndicator;
		
		public int gridRows;
		public int gridColumns;
		public int gridBombs;
		
		public GridCell[,] gridCells;
		
		private AudioManager audMan;
	}
}
