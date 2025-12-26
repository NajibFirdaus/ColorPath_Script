using UnityEngine;

namespace Connect.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [HideInInspector] public int CurrentStage = 1;
        public int CurrentLevel = 1;

        public RuntimeLevelData CurrentRuntimeLevel;

        public bool IsNewLevel;

        private const string Gameplay = "Gameplay";

        private void Awake()
        {
            IsNewLevel = true;

            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public RuntimeLevelData GetGeneratedLevel()
        {
            if (IsNewLevel)
            {
                CurrentRuntimeLevel =
                    LevelGenerator.Generate(CurrentStage, CurrentLevel);
            }

            return CurrentRuntimeLevel;
        }

        public void RestartLevel()
        {
            IsNewLevel = false;
            GoToGameplay();
        }

        public void NextLevel()
        {
            CurrentLevel++;
            UnlockLevel(CurrentLevel);
           IsNewLevel = true;
            GoToGameplay();
        }

        public bool IsLevelUnlocked(int level)
        {
            string key = $"S{CurrentStage}L{level}";

            if (level == 1)
            {
                PlayerPrefs.SetInt(key, 1);
                return true;
            }

            return PlayerPrefs.GetInt(key, 0) == 1;
        }

        private void UnlockLevel(int level)
        {
            string key = $"S{CurrentStage}L{level}";
            PlayerPrefs.SetInt(key, 1);
        }

        public void GoToGameplay()
        {
            UnityEngine.SceneManagement.SceneManager
                .LoadScene(Gameplay);
        }
    }
}
