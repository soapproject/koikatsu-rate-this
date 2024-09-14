using BepInEx.Logging;
using KKAPI.Chara;
using KKAPI.Maker;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KK_Rate_This
{
    public class RateBuffer
    {
        public string SourcePath;
        public string Rating;
        public string CharacterName;

        public RateBuffer(string sourcePath, string rating, string characterName)
        {
            SourcePath = sourcePath;
            Rating = rating;
            CharacterName = characterName;
        }
    }

    public class PanelGUI : MonoBehaviour
    {
        public static PanelGUI Instance;
        private Rect windowRect = new Rect(100, 100, 300, 600);
        private ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("KK_Rate_This");
        private List<string> ratingTypes;
        private volatile bool configChanged = false;
        private List<RateBuffer> rateBuffer = new List<RateBuffer>();
        private Vector2 scrollPosition;

        private void Awake()
        {
            enabled = false;
            LoadConfig();

            // Register configuration change event
            RatingPlugin.RatingTypesConfig.SettingChanged += OnConfigChanged;
        }

        private void OnConfigChanged(object sender, EventArgs e)
        {
            configChanged = true;
        }

        private void Update()
        {
            if (configChanged)
            {
                LoadConfig();
                configChanged = false;
            }
        }

        private void OnGUI()
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            windowRect = GUI.Window(0, windowRect, DrawPanel, "Rate Character Card");
        }

        public static void Show()
        {
            if (Instance != null)
                Instance.enabled = true;
        }

        public static void Hide()
        {
            if (Instance != null)
                Instance.enabled = false;
        }

        private void LoadConfig()
        {
            var ratingTypesConfig = RatingPlugin.RatingTypesConfig.Value;
            var newRatingTypes = new List<string>(ratingTypesConfig.Split(','));

            // Remove leading and trailing spaces from each type
            for (int i = 0; i < newRatingTypes.Count; i++)
            {
                newRatingTypes[i] = newRatingTypes[i].Trim();
            }

            ratingTypes = newRatingTypes;
        }

        private void DrawPanel(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Select a rating:");

            // Copy the ratingTypes list to avoid thread-safety issues
            List<string> localRatingTypes = ratingTypes;

            // draw rating btns
            if (localRatingTypes != null)
            {
                foreach (var rating in localRatingTypes)
                {
                    if (GUILayout.Button(rating))
                    {
                        RateCharacter(rating);
                    }
                }
            }

            // draw buffer
            GUILayout.Label("Buffer:");
            if (rateBuffer.Count > 0)
            {
                GUILayout.Space(10);

                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                for (int i = 0; i < rateBuffer.Count; i++)
                {
                    var move = rateBuffer[i];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{move.CharacterName} -> {move.Rating}");
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        rateBuffer.RemoveAt(i);
                        i--;
                        continue;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();

                // Use empty space
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Move"))
                {
                    MoveBufferedFiles();
                }
            }

            GUILayout.EndVertical();
            GUI.DragWindow();

        }

        private void MoveBufferedFiles()
        {
            foreach (var move in rateBuffer)
            {
                try
                {
                    string fileName = System.IO.Path.GetFileName(move.SourcePath);
                    string destFolder = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, $"UserData/chara/female/{move.Rating}");
                    string destPath = System.IO.Path.Combine(destFolder, fileName);

                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destPath));
                    System.IO.File.Move(move.SourcePath, destPath);
                    Logger.LogInfo($"Moved '{fileName}' to '{destPath}'.");
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"Failed to move '{move.SourcePath}': {ex.Message}");
                }
            }
            Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_l);
            rateBuffer.Clear();
            scrollPosition = Vector2.zero;
        }

        private void RateCharacter(string rating)
        {
            var charFile = MakerAPI.LastLoadedChaFile;
            if (charFile == null)
            {
                Logger.LogWarning("No character file loaded.");
                return;
            }

            string sourcePath = charFile.GetSourceFilePath();
            if (string.IsNullOrEmpty(sourcePath))
            {
                Logger.LogWarning("Character file path not found.");
                return;
            }

            // Upsert rating to buffer
            Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.sel);
            var existingEntry = rateBuffer.Find(instance => instance.SourcePath == sourcePath);
            if (existingEntry != null)
            {
                existingEntry.Rating = rating;
                Logger.LogInfo($"Updated rating of '{System.IO.Path.GetFileName(sourcePath)}' to '{rating}' in buffer.");
            }
            else
            {
                string characterName = charFile.parameter.fullname;
                rateBuffer.Add(new RateBuffer(sourcePath, rating, characterName));
                Logger.LogInfo($"Added '{System.IO.Path.GetFileName(sourcePath)}' to buffer (Rating: {rating}).");
            }
        }

        private void OnDestroy()
        {
            RatingPlugin.RatingTypesConfig.SettingChanged -= OnConfigChanged;
        }
    }
}