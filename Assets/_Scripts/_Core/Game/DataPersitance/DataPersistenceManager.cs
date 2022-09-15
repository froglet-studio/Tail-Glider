using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TailGlider.Utility.Singleton;
using System;

namespace StarWriter.Core 
{
    public class DataPersistenceManager : SingletonPersistent<DataPersistenceManager>
    {
        [Header("File Storage Config")]

        [SerializeField] private string gameFileName = "gamedata.json";
        [SerializeField] private string hangerFileName = "hangerdata.json";
        [SerializeField] private string playerDataFileName = "playerdata.dat";

        private GameData gameData;

        private HangerData hangerData;

        private PlayerData playerData;

        private List<IDataPersistence> dataPersistenceObjects;

        private FileDataHandler dataHandler;
        //public static DataPersistenceManager Instance { get; private set; }

        public override void Awake()
        {
            base.Awake(); 
            //if (Instance != null)
            //{
            //    Debug.Log("Duplicate DataPersistanceManager Instance found!");

            //}
            //Instance = this;
            this.dataHandler = new FileDataHandler(Application.persistentDataPath, gameFileName, hangerFileName, playerDataFileName);
        }

        private void Start()
        {

            this.dataPersistenceObjects = FindAllDataPersistenceObjects();
            LoadGame();
            LoadHanger();
            LoadCurrentPlayer();
        }
        /// <summary>
        /// Sets HangerData to default values
        /// </summary>
        public void NewHanger()
        {
            this.hangerData = new HangerData();
        }
        /// <summary>
        /// Sends HangerData to be saved to DataHandler
        /// </summary>
        internal void SaveHanger()
        {
            // Push Loaded gamedata out to scripts to update it
            foreach (IDataPersistence Obj in dataPersistenceObjects)
            {
                Obj.SaveData(ref hangerData);
            }

            // Save data to disk using the file data handler
            dataHandler.SaveHanger(hangerData);

            Debug.Log("Game Saved. " + gameData.testNumber);
        }
        /// <summary>
        /// Gets HangerData to be loaded from DataHandler, if HangerData is null Creates a new default HangerData
        /// </summary>
        internal void LoadHanger()
        {
            if (dataPersistenceObjects == null)
            {
                this.dataPersistenceObjects = FindAllDataPersistenceObjects();
            }
            // Load saved data from disk using the file data handler
            this.hangerData = dataHandler.LoadHanger();

            // Create default values if GameData is null
            if (this.hangerData == null)
            {
                Debug.Log("HangerData not found while attempting to load.  Created a new HangerData file.");
                NewHanger();
            }

            // Push Loaded gamedata out to scripts requiring it
            foreach (IDataPersistence Obj in dataPersistenceObjects)
            {
                Obj.LoadData(hangerData);
            }
            //Debug.Log("Pilot in Bay001 is " + hangerData.Bay001Pilot); ;
        }

        /// <summary>
        /// Sets GameData and HangerData to default values
        /// </summary>
        public void NewGame()
        {
            this.gameData = new GameData();
        }
        /// <summary>
        /// Gets GameData to be loaded from DataHandler, if GameData is null Creates a new default GameData
        /// </summary>
        public void LoadGame()
        {
            // Load saved data from disk using the file data handler
            this.gameData = dataHandler.LoadGame();

            // Create default values if GameData is null
            if (this.gameData == null)
            {
                Debug.Log("GameData not found while attempting to load.  Created a new GameData file.");
                NewGame();
            }

            // Push Loaded gamedata out to scripts requiring it
            foreach (IDataPersistence Obj in dataPersistenceObjects)
            {
                Obj.LoadData(gameData);
            }
            Debug.Log("Loaded Game. " + gameData.testNumber);
        }
        /// <summary>
        /// Sends GameData to be saved to DataHandler
        /// </summary>
        public void SaveGame()
        {
            // Push Loaded gamedata out to scripts to update it
            foreach (IDataPersistence Obj in dataPersistenceObjects)
            {
                Obj.SaveData(ref gameData);
            }

            // Save data to disk using the file data handler
            dataHandler.SaveGame(gameData);

            Debug.Log("Game Saved. " + gameData.testNumber);
        }
        /// <summary>
        /// Saves GamaData on shutdown
        /// </summary>
        private void OnApplicationQuit()
        {
            SaveGame();
        }

        public void NewPlayer()
        {
            this.playerData = new PlayerData();
        }

        public void LoadCurrentPlayer()
        {
            // Load saved data from disk using the file data handler
            this.playerData = dataHandler.LoadCurrentPlayer();

            // Create default values if GameData is null
            if (this.playerData == null)
            {
                Debug.Log("PlayerData not found while attempting to load.  Created a new PlayerData file.");
                NewPlayer();
            }

            // Push Loaded PlayerData out to scripts requiring it

            //**********************************************************************************************************
            //TODO push data to the Player and player stats and the hanger for favorite build to display first
            //foreach (IDataPersistence Obj in dataPersistenceObjects)
            //{
            //    Obj.LoadData(gameData);
            //}
            Debug.Log("Loaded Player. " + playerData.playerName);
        }

        public void SaveCurrentPlayer()
        {
            // Push Loaded gamedata out to scripts to update it
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            //player.GetComponent<Player>().SaveData(ref playerData);

            // Save data to disk using the file data handler
            dataHandler.SaveGame(gameData);

            Debug.Log("Game Saved. " + gameData.testNumber);
        }
        public void ResetAllSaveFilesToDefault()
        {
            NewGame();
            SaveGame();
            NewPlayer();
            SaveCurrentPlayer();
            NewHanger();
            SaveHanger();
        }

        /// <summary>
        /// Finds all IDataPersistence components located on Monobehaviors
        /// </summary>
        /// <returns>List<IDataPersistence></returns>
        public List<IDataPersistence> FindAllDataPersistenceObjects()
        {
            IEnumerable<IDataPersistence> dataPersistenceObjects = FindObjectsOfType<MonoBehaviour>().OfType<IDataPersistence>();

            return new List<IDataPersistence>(dataPersistenceObjects);
        }
    }
}


