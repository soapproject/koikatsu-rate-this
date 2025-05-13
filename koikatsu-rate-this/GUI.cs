using BepInEx.Logging;
using KKAPI.Chara;
using KKAPI.Maker;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly Vector2 MinSize = new Vector2(200f, 300f);
        private readonly float ResizeHandleSize = 15f;

        private ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("KK_Rate_This");

        private List<string> ratingTypes;
        private volatile bool configChanged = false;
        private List<RateBuffer> rateBuffer = new List<RateBuffer>();
        private Vector2 scrollPosition;

        private bool isResizing = false;
        private Vector2 resizeStartPos;
        private Rect originalWindowRect;

        private void Awake()
        {
            enabled = false;
            LoadConfig();
            RatingPlugin.RatingTypesConfig.SettingChanged += OnConfigChanged;
        }

        private void OnDestroy()
        {
            RatingPlugin.RatingTypesConfig.SettingChanged -= OnConfigChanged;
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
            HandleResize();
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

            for (int i = 0; i < newRatingTypes.Count; i++)
                newRatingTypes[i] = newRatingTypes[i].Trim();

            ratingTypes = newRatingTypes;
        }

        private void DrawPanel(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Select a rating:");

            var localRatingTypes = ratingTypes;

            if (localRatingTypes != null)
            {
                foreach (var rating in localRatingTypes)
                {
                    if (GUILayout.Button(rating))
                        RateCharacter(rating);
                }
            }

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

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Move"))
                    MoveBufferedFiles();
            }

            GUILayout.EndVertical();

            // 避開右下角的 Resize 區域
            GUI.DragWindow(new Rect(0, 0, windowRect.width - ResizeHandleSize, 20));
        }

        private void HandleResize()
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            // 計算右下角 handle 的區域
            Rect handleRect = new Rect(
                windowRect.xMax - ResizeHandleSize,
                windowRect.yMax - ResizeHandleSize,
                ResizeHandleSize,
                ResizeHandleSize
            );

            // 畫出亮灰色的 Resize Handle
            var prevColor = GUI.color;
            GUI.color = new Color(0.9f, 0.9f, 0.9f);
            GUI.DrawTexture(handleRect, Texture2D.whiteTexture);
            GUI.color = prevColor;

            var e = Event.current;
            if (e.button != 0) return;

            if (e.type == EventType.MouseDown && handleRect.Contains(e.mousePosition))
            {
                isResizing = true;
                GUIUtility.hotControl = controlID;
                e.Use();   // 吃掉事件，避免被其他 GUI 元件消化
                return;
            }

            if (GUIUtility.hotControl != controlID) return;

            if (e.type == EventType.MouseDrag)
            {
                windowRect.size = Vector2.Max(windowRect.size + e.delta, MinSize);
                e.Use();
                return;
            }

            if (e.type == EventType.MouseUp)
            {
                isResizing = false;
                GUIUtility.hotControl = 0;
                e.Use();
            }
        }

        private void MoveBufferedFiles()
        {
            foreach (var move in rateBuffer)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(move.SourcePath);
                    string extension = Path.GetExtension(move.SourcePath);
                    string destFolder = Path.Combine(BepInEx.Paths.GameRootPath, $"UserData/chara/female/{move.Rating}");
                    string destPath = Path.Combine(destFolder, fileName + extension);

                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));

                    int index = 1;
                    while (File.Exists(destPath))
                    {
                        destPath = Path.Combine(destFolder, $"{fileName}({index}){extension}");
                        index++;
                    }

                    File.Move(move.SourcePath, destPath);
                    Logger.LogInfo($"Moved '{fileName + extension}' to '{destPath}'.");
                }
                catch (Exception ex)
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

            Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.sel);
            var existingEntry = rateBuffer.Find(x => x.SourcePath == sourcePath);
            if (existingEntry != null)
            {
                existingEntry.Rating = rating;
                Logger.LogInfo($"Updated rating of '{Path.GetFileName(sourcePath)}' to '{rating}' in buffer.");
            }
            else
            {
                string characterName = charFile.parameter.fullname;
                rateBuffer.Add(new RateBuffer(sourcePath, rating, characterName));
                Logger.LogInfo($"Added '{Path.GetFileName(sourcePath)}' to buffer (Rating: {rating}).");
            }
        }
    }
}
