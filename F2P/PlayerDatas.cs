///-----------------------------------------------------------------
/// Author : Tom FALEMPIN
/// Date : 27/04/2020 11:41
///-----------------------------------------------------------------

using Com.IsartDigital.F2P.Managers;
using Com.IsartDigital.F2P.Object.Units.Squad;
using Com.IsartDigital.F2P.Object.Units.Units;
using Com.IsartDigital.F2P.Screens;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Com.IsartDigital.F2P.SessionDatas {

	[System.Serializable]
	public class PlayerDatas {
		public List<World> worlds;
		public RankedLevel rankedLevel;
		public List<Squad> squads;
		public bool noAds;
		public int softCurrency;
		public int hardCurrency;
		public string country;

		public PlayerDatas(List<World> worlds = null, RankedLevel rankedLevel = null, List<Squad> squads = null, bool noAds = false, int softCurrency = 0, int hardCurrency = 0, string Country = null) {
			this.worlds = worlds;
			this.rankedLevel = rankedLevel;
			this.squads = squads;
			this.noAds = noAds;
			this.softCurrency = softCurrency;
			this.hardCurrency = hardCurrency;

			if (rankedLevel != null)
				if (RankedLevel.GetLastStartDay() != rankedLevel.StartDay) AddRankedLevel(GameManager.Credentials.id);

			if (worlds == null || worlds.Count == 0) {
				this.worlds = new List<World>();
				//AddWorld(GameManager.Credentials.id, 0, WorldInfos.worldInfos[0].name);
			}


			if (squads == null || squads.Count == 0) {
				this.squads = new List<Squad>();
				//AddSquadInInventory(Squad.CreateSquad(SquadPattern.GetSquadPattern("forest guardian", 1), GameManager.Credentials.id));
			}

			this.country = Country == null ? System.Globalization.RegionInfo.CurrentRegion.ToString() : Country;
		}

		public void AddRankedLevel(string playerID) {
			rankedLevel = new RankedLevel(playerID, 0, 0, RankedLevel.GetLastStartDay(), "notPlayed");
			ServerManager.Instance.SaveRankedLevel(rankedLevel);
		}

		public void AfterTutosGift(string playerID) {

			AddSquadInInventory(Squad.CreateSquad(SquadPattern.GetSquadPattern("Servant", 1), playerID));
			AddSquadInInventory(Squad.CreateSquad(SquadPattern.GetSquadPattern("Meca", 1), playerID));
			AddSquadInInventory(Squad.CreateSquad(SquadPattern.GetSquadPattern("Archers", 1), playerID));

			ServerManager.Instance.SavePlayerDatas(this);

			AddWorld(playerID, 0, WorldInfos.worldInfos[0].name, 0, false);
			GetWorld(0).AddLevel(playerID, 0, 0, true, true);

		}

		public void ChangeSoftCurrency(int howMany) {
			if (softCurrency + howMany >= 0) {
				softCurrency += howMany;
				MenuHUD.Instance.UpdateSoftCurrency(softCurrency);
			}
			else Debug.LogWarning("[PlayeDatas] Can't remove more softCurrency than the current amount");
		}

		public void ChangeHardCurrency(int howMany) {
			if (hardCurrency + howMany >= 0) {
				hardCurrency += howMany;
				MenuHUD.Instance.UpdateHardCurrency(hardCurrency);
			}
			else Debug.LogWarning("[PlayeDatas] Can't remove more hardCurrency than the current amount");
		}

		public bool CheckIfEnoughCurrency(int howMany, bool isSoft) {
			if (isSoft)
				return softCurrency - howMany >= 0;
			else return hardCurrency - howMany >= 0;
		}

		public void PurchasedNoAds() {
			noAds = true;
			ServerManager.Instance.SaveNoAds(noAds);
		}


		static public string GetUniqueID(string playerID) {
			string uniqueID = string.Concat((string)playerID, (int)Random.Range(0, 1000000000));
			return uniqueID;
		}

		#region SquadManagement

		public void AddSquadInInventory(Squad squad) {
			squads.Add(squad);
			//ServerManager.Instance.SaveSquad(squad);
		}

		public void RemoveSquadFromInventory(string id) {
			int lenght = squads.Count;
			for (int i = 0; i < lenght; i++) {
				if (squads[i].id == id) {
					squads.RemoveAt(i);
					break;
				}
			}
		}

		public void RemoveSquadFromInventory(Squad squad) {
			squads.Remove(squad);
		}

		public Squad FindSquad(string id) {
			int length = squads.Count;
			for (int i = 0; i < length; i++) {
				if (squads[i].id == id) return squads[i];
			}

			return null;
		}

		public void UpgradeSquad(Squad squadtoUpgrade) {
			SquadPattern squadPattern = SquadPattern.GetSquadPattern(squadtoUpgrade.name, squadtoUpgrade.level + 1);
			if (squadPattern != null) {
				squadtoUpgrade.level++;
				ServerManager.Instance.SaveSquad(squadtoUpgrade);
			}
			else Debug.LogError("[PlayeDatas] Can't upgrade, no such squad in Inventory");
		}

		public void UpgradeSquad(string squadtoUpgradeID) {
			Squad squadToUpgrade = FindSquad(squadtoUpgradeID);

			if (squadToUpgrade == null) Debug.LogError(string.Concat("[PlayerDatas] Cant upgrade unexisting squad : ", squadtoUpgradeID));
			UpgradeSquad(squadToUpgrade);
		}

		#endregion

		#region WorldManagement
		public World GetWorld(int worldIndex) {
			int length = worlds.Count;

			for (int i = 0; i < length; i++) {
				if (worldIndex == worlds[i].worldIndex) return worlds[i];
			}

			return null;
		}

		public void AddWorld(string playerID, int worldIndex, string name = null, int nbrOfChests = 0, bool saveWorld = true) {
			if (worlds == null) worlds = new List<World>();

			if (GetWorld(worldIndex) == null) {
				int length = WorldInfos.WorldsInfos.Length;
				if (name == null)
					for (int i = 0; i < length; i++) {
						if (WorldInfos.WorldsInfos[i].worldIndex == worldIndex) name = WorldInfos.WorldsInfos[i].name;
					}

				World world = new World(playerID, name, worldIndex, nbrOfChests);
				if(saveWorld) ServerManager.Instance.SaveWorld(world);
				worlds.Add(world);
			}

		}

		#endregion
	}

	#region LevelsDatas_and_RankedLevelDatas

	[System.Serializable]
	public class Level {
		public int worldIndex;
		public int levelIndex;
		public string playerID;
		public string id;
		public bool firstChest;
		public bool secondChest;

		public Level(string playerID, int worldIndex = 0, int levelIndex = 0, bool firstChest = false, bool secondChest = false, string id = null) {
			this.playerID = playerID;
			this.worldIndex = worldIndex;
			this.levelIndex = levelIndex;
			this.id = id == null ? PlayerDatas.GetUniqueID(playerID) : id;
			this.firstChest = firstChest;
			this.secondChest = secondChest;
		}

		public void UnlockChest(bool isSecondChest = false) {
			firstChest = true;
			if (isSecondChest) secondChest = true;
		}

	}

	[System.Serializable]
	public class RankedLevel {
		public string playerID;
		public string id;

		public int Score;
		public int nbrOfChest;

		public string StartDay;
		public string LastPlayedDate;

		public RankedLevel(string playerID, int score, int nbrOfChest, string StartDay, string LastPlayedDate, string id = null) {
			this.playerID = playerID;
			this.Score = score;
			this.nbrOfChest = nbrOfChest;

			this.StartDay = StartDay;
			this.LastPlayedDate = LastPlayedDate;

			this.id = id == null ? PlayerDatas.GetUniqueID(playerID) : id;
		}

		public void UpdateRankedLevelScore(int score, bool getChest) {
			string date = RankedLevel.GetLastStartDay();
			if (StartDay != date) {
				StartDay = date;
				Score = score;
				nbrOfChest = getChest ? 1 : 0;
			}
			else {
				Score += score;
				nbrOfChest += getChest ? 1 : 0;
			}

			int DayInMonth = (int)System.DateTime.Now.Day;
			int Month = (int)System.DateTime.Now.Month;
			int Year = (int)System.DateTime.Now.Year;

			LastPlayedDate = string.Concat(DayInMonth, "/", Month, "/", Year);

			ServerManager.Instance.SaveRankedLevel(this);

		}

		public void CheckRankedLevel() {
			string date = GetLastStartDay();
			if (StartDay != date) {
				StartDay = date;
				Score = 0;
				nbrOfChest = 0;
			}
		}

		static public string GetLastStartDay() {
			string Date;

			int DayOfWeek = (int)System.DateTime.Now.DayOfWeek;
			int DayInMonth = (int)System.DateTime.Now.Day;
			int Month = (int)System.DateTime.Now.Month;
			int Year = (int)System.DateTime.Now.Year;

			int LastMondayDay = 0;
			int LastMondayMonth = 0;

			if (DayInMonth - DayOfWeek > 0) {
				LastMondayDay = DayInMonth - DayOfWeek;
				Date = string.Concat(LastMondayDay, "/", Month, "/", Year);
			}
			else {
				if (Month - 1 > 0) {
					LastMondayMonth = Month - 1;

					if (Month == 01 || Month == 03 || Month == 05 || Month == 07 || Month == 08 || Month == 10 || Month == 12) LastMondayDay = 31 - (DayInMonth - DayOfWeek);
					else if (Month == 2) LastMondayDay = 28 - (DayInMonth - DayOfWeek);
					else LastMondayDay = 30 - (DayInMonth - DayOfWeek);
					Date = string.Concat(LastMondayDay, "/", LastMondayMonth, "/", Year);
				}
				else {
					Year--;
					Month = 12;
					LastMondayDay = 31 - (DayInMonth - DayOfWeek);
					Date = string.Concat(LastMondayDay, "/", LastMondayMonth, "/", Year);
				}
			}
			return Date;
		}
	}

	[System.Serializable]
	public class LevelsInfosList {
		public LevelsInfos[] levelInfos;
	}

	[System.Serializable]
	public class LevelsInfos {
		static public LevelsInfos[] levelInfos;
		static public LevelsInfos[] LevelInfos => levelInfos;

		public string bestSquadName;

		public int levelIndex;
		public int worldIndex;

		public LevelsInfos(string bestSquadName = null, int levelIndex = 0, int worldIndex = 0) {
			this.bestSquadName = bestSquadName;

			this.worldIndex = worldIndex;
			this.levelIndex = levelIndex;
		}

		static public void Init() {
			TextAsset loadedText = Resources.Load<TextAsset>("LevelInfos");
			string json = "{\"levelInfos\":" + loadedText.text + "}";
			levelInfos = JsonUtility.FromJson<LevelsInfosList>(json).levelInfos;
		}

		static public LevelsInfos getLevelInfos(int worldIndex, int levelIndex) {
			int length = levelInfos.Length;

			for (int i = 0; i < length; i++) {
				if (levelInfos[i].worldIndex == worldIndex && levelInfos[i].levelIndex == levelIndex) return levelInfos[i];
			}

			return null;
		}
	}

	#endregion

	#region WorldsDatas

	[System.Serializable]
	public class World {
		public string name;
		public int worldIndex;
		public int nbrOfChests;
		public string playerID;
		public string id;
		public List<Level> levels;

		public World(string playerID, string name, int worldIndex = 0, int nbrOfChests = 0, List<Level> levels = null, string id = null) {
			this.playerID = playerID;
			this.name = name;
			this.worldIndex = worldIndex;
			this.nbrOfChests = nbrOfChests;
			this.levels = levels == null ? new List<Level>() : levels;
			if (id != null) this.id = id;
			else this.id = PlayerDatas.GetUniqueID(playerID);
		}


		public void addChests(int numberOfChests) {
			nbrOfChests += numberOfChests;
			ServerManager.Instance.SaveWorld(this);
		}

		public void AddLevel(string playerID, int worldIndex, int levelIndex, bool firstChest = false, bool secondChest = false, string id = null) {
			if (FindLevel(levelIndex) == null) {
				Level level = new Level(playerID, worldIndex, levelIndex, firstChest, secondChest, id);
				levels.Add(level);

				ServerManager.Instance.SaveLevel(level);
				int HowManyChests = 0;

				if (firstChest) HowManyChests++;
				if (secondChest) HowManyChests++;
				addChests(HowManyChests);
			}
			else Debug.LogWarning(string.Concat("[PlayerDatas][Level] Trying to add an already existing level ", levelIndex, " at world ", worldIndex, " in the list"));
		}

		public void UpdateLevel(Level levelToUpdate = null, bool firstChest = false, bool secondChest = false) {
			int HowManyChests = 0;

			if (!levelToUpdate.firstChest && firstChest) HowManyChests++;
			if (!levelToUpdate.secondChest && secondChest) HowManyChests++;
			addChests(HowManyChests);

			levelToUpdate.firstChest = firstChest;
			levelToUpdate.secondChest = secondChest;
			ServerManager.Instance.SaveLevel(levelToUpdate);
		}

		public void UpdateLevel(int levelIndex, bool firstChest = false, bool secondChest = false) {
			Level level = FindLevel(levelIndex);

			if (level != null) UpdateLevel(level, firstChest, secondChest);
		}

		public Level FindLevel(int levelIndex) {
			int length = levels.Count;

			for (int i = 0; i < length; i++) {
				if (levels[i].levelIndex == levelIndex) {
					return levels[i];
				}
			}
			return null;
		}
		public Level FindLevel(string id) {
			int length = levels.Count;

			for (int i = 0; i < length; i++) {
				if (levels[i].id == id) {
					return levels[i];
				}
			}
			return null;
		}
	}

	[System.Serializable]
	public class WorldInfosList {
		public WorldInfos[] worldInfos;
	}

	[System.Serializable]
	public class WorldInfos {
		static public WorldInfos[] worldInfos;
		static public WorldInfos[] WorldsInfos => worldInfos;

		public string name;
		public string theme;
		public int worldIndex;

		public WorldInfos(string name, string theme, int worldIndex) {
			this.name = name;
			this.theme = theme;
			this.worldIndex = worldIndex;
		}

		static public void Init() {
			TextAsset loadedText = Resources.Load<TextAsset>("WorldInfos");
			string json = "{\"worldInfos\":" + loadedText.text + "}";
			worldInfos = JsonUtility.FromJson<WorldInfosList>(json).worldInfos;
		}
	}

	#endregion

	#region InventoryDatas

	[System.Serializable]
	public class Squad {
		public string name;
		public int level;

		public string playerID;
		public string id;

		public Squad(string playerID, string name, int level, string id = null) {
			this.name = name;
			this.level = level;

			if (SquadPattern.GetSquadPattern(name, level) == null)
				Debug.LogWarning("[Squad] No such squad in memory");

			this.playerID = playerID;
			this.id = id == null ? PlayerDatas.GetUniqueID(playerID) : id;
		}

		static public Squad CreateSquad(SquadPattern pattern, string playerID) {
			if (pattern == null)
				return null;

			return new Squad(playerID, pattern.name, pattern.level);
		}

		public void UpgradeSquad() {
			SquadPattern squadPattern = SquadPattern.GetSquadPattern(name, level + 1);

			if (squadPattern != null)
				level++;
			else Debug.LogError("[PlayeDatas] Can't upgrade, no such squad in Inventory");
		}
	}

	[System.Serializable]
	public class SquadPatternList {
		public SquadPattern[] allSquads;
	}

	[System.Serializable]
	public class SquadPattern {
		static public SquadPattern[] allySquads;
		static public SquadPattern[] AllSquadPatterns => allySquads;

		public string name;

		public string archetyp;
		public UnitSkin unitSkin;
		public string theme;

		public int nbrofunits;
		public int nunitswonbycagemin;
		public int nunitswonbycagemax;
		public int pv;

		public int damageonstructure;
		public int damageonenemy;
		public float attackspeed;
		public float range;

		public float movementspeed;

		public int level;


		public string description;
		public int pricetoupgrade;

		public delegate void SquadPatternEventHandler();
		static public event SquadPatternEventHandler OnLoaded;

		public SquadPattern(string name, string archetyp, string theme, int nbrofunits, int pv, int damageonstructure, int damageonenemy, float attackspeed, float range, float movementspeed, int level, int nunitswonbycagemin, int nunitswonbycagemax, string description, int pricetoupgrade) {
			this.name = name;

			this.archetyp = archetyp;
			this.theme = theme;

			this.nbrofunits = nbrofunits;
			this.pv = pv;

			this.damageonstructure = damageonstructure;
			this.damageonenemy = damageonenemy;
			this.attackspeed = attackspeed;
			this.range = range;

			this.movementspeed = movementspeed;

			this.level = level;

			this.nunitswonbycagemin = nunitswonbycagemin;
			this.nunitswonbycagemax = nunitswonbycagemax;

			this.description = description;
			this.pricetoupgrade = pricetoupgrade;
		}

		static public void Init() {
			TextAsset loadedText = Resources.Load<TextAsset>("SquadPatterns");
			string json = "{\"allSquads\":" + loadedText.text + "}";
			allySquads = JsonUtility.FromJson<SquadPatternList>(json).allSquads;

			SquadPattern lSquad;
			for (int i = allySquads.Length - 1; i >= 0; i--) {
				lSquad = allySquads[i];
				lSquad.unitSkin = Resources.Load<UnitSkin>(Path.Combine("Skin", lSquad.name));

				if (lSquad.unitSkin == null)
				{
					Debug.LogError("Skin doesn't exist or is not in the skin folder");
					lSquad.unitSkin = Resources.Load<UnitSkin>(Path.Combine("Skin", "FTUE"));
				}
			}

			OnLoaded?.Invoke();
		}

		static public SquadPattern GetSquadPattern(string name, int level) {
			int length = allySquads.Length;

			for (int i = 0; i < length; i++) {
				if (name == allySquads[i].name && level == allySquads[i].level)
					return allySquads[i];
			}

			for (int i = 0; i < length; i++)
			{
				if ("FTUE" == allySquads[i].name)
					return allySquads[i];
			}

			return null;
		}

		static public SquadPattern RandomByWorld(int worldIndex) {
			List<SquadPattern> squadPatternsInSameWorld = new List<SquadPattern>();

			if (worldIndex > WorldInfos.worldInfos.Length) worldIndex = WorldInfos.worldInfos.Length - 1;
			string name = WorldInfos.worldInfos[worldIndex].theme;

			int length = allySquads.Length;

			for (int i = 0; i < length; i++)
				if (name == allySquads[i].theme) squadPatternsInSameWorld.Add(allySquads[i]);

			if (squadPatternsInSameWorld.Count == 0) Debug.LogError("[SquadPattern] No SquadPattern available for this world : " + name);


			return squadPatternsInSameWorld[Random.Range(0, squadPatternsInSameWorld.Count)];
		}
	}
	#endregion
}