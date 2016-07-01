using System;
using System.IO;
using System.Xml.Linq;

using Engine;
using Engine.Graphics;
using Game;
using SCModApi;
using System.Collections.Generic;

namespace WorldEditModApi
{
    public class WorldEdit
    {
        public static void Initialize() // Launcher автоматически запускает метод Initialize() в каждой модификации
        {

            Engine.Window.Frame += Load; // Метод загрузки
            Engine.Window.Frame += WorldEditModApi; // Каждый кадр будет вызываться метод WorldEditModApi()
            return;
        }

        internal static string modPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Survivalcraft/SCPlugins/WorldEdit/"; // Стандартная папка мода
        internal static int OldLookControlMode = (int) SettingsManager.LookControlMode; // "Старый" режим управления. Используется для переключения в режим SplitTouch, когда активировано меню WorldEdit
        internal static TerrainRaycastResult? Point1 = null, Point2 = null, Point3 = null; // Точки выделения зоны
        internal static int SelectedBlock; // Выбранный блок, соответствует точке 1
        internal static int ReplaceableBlock; // Заменяемый блок, соответствует точке 2
        internal static int blockCount; // Счетчик блоков в памяти
        internal static List<BlockMem> BlockList = new List<BlockMem>(); // Скопированные блоки в память

        public static void Load()
        {
            if (!(ScreensManager.GetScreenName(ScreensManager.CurrentScreen) == "Game")) return; // Выходим, если мы не в игре

            // Загрузка кнопок при входе в игру            
            LoadButtons(Path.Combine(modPath, "WorldEditButtons.xml"));

            ApplyButtonImage("F1", Path.Combine(modPath, "Button1.png"), Path.Combine(modPath, "Button1_pressed.png"));
            ApplyButtonImage("F2", Path.Combine(modPath, "Button2.png"), Path.Combine(modPath, "Button2_pressed.png"));
            ApplyButtonImage("F3", Path.Combine(modPath, "Button3.png"), Path.Combine(modPath, "Button3_pressed.png"));
            ApplyButtonImage("F5", Path.Combine(modPath, "ButtonPaste.png"), Path.Combine(modPath, "ButtonPaste_pressed.png"));
            ApplyButtonImage("F6", Path.Combine(modPath, "ButtonFill.png"), Path.Combine(modPath, "ButtonFill_pressed.png"));
            ApplyButtonImage("F7", Path.Combine(modPath, "ButtonReplace.png"), Path.Combine(modPath, "ButtonReplace_pressed.png"));
            ApplyButtonImage("F8", Path.Combine(modPath, "ButtonClear.png"), Path.Combine(modPath, "ButtonClear_pressed.png"));
            ApplyButtonImage("F9", Path.Combine(modPath, "ButtonMemCopy.png"), Path.Combine(modPath, "ButtonMemCopy_pressed.png"));
            ApplyButtonImage("F10", Path.Combine(modPath, "ButtonMemPaste.png"), Path.Combine(modPath, "ButtonMemPaste_pressed.png"));
            Engine.Window.Frame -= Load; // Все загружено, убираем вызов метода
            return;            
        }

        internal static void WorldEditModApi()
        {
            if (!(ScreensManager.GetScreenName(ScreensManager.CurrentScreen) == "Game")) // Выходим, если мы не в игре
            {
                return;
            }

            // Показазывеем контейнер с кнопками, если активирована кнопка WorldEditMenu
            ScreensManager.CurrentScreen.ScreenWidget.FindWidget<StackPanelWidget>("WorldEditMenuContainerTop", true).IsVisible = ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("WorldEditMenu", true).IsChecked;
            ScreensManager.CurrentScreen.ScreenWidget.FindWidget<StackPanelWidget>("WorldEditMenuContainerBottom", true).IsVisible = ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("WorldEditMenu", true).IsChecked;

            // Смена режима управления
            if (ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("WorldEditMenu", true).IsTapped)
            {
               if (ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("WorldEditMenu", true).IsChecked)
                {
                    OldLookControlMode = (int) SettingsManager.LookControlMode;
                    SettingsManager.LookControlMode = LookControlMode.SplitTouch;
                }
                else
                {
                    SettingsManager.LookControlMode = (LookControlMode) OldLookControlMode;
                }
            }

            if ((Engine.Input.Keyboard.IsKeyDown(Engine.Input.Key.F1)) || (ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("F1", true).IsTapped)) // Выделение 1 точки
            {
                ComponentMiner componentMiner = Subsystems.Player.ComponentPlayer.ComponentMiner;
                Point1 = componentMiner.PickTerrainForDigging(Subsystems.Drawing.ViewPosition, Subsystems.Drawing.ViewDirection);
                
                if (Point1.HasValue)
                {
                    SCModApi.Gui.DisplayMessage("Set position 1 on: " + Point1.Value.CellFace.X + ", " + Point1.Value.CellFace.Y + ", " + Point1.Value.CellFace.Z, false);
                    SelectedBlock = World.GetBlock(Point1.Value.CellFace.X, Point1.Value.CellFace.Y, Point1.Value.CellFace.Z);                    
                    return;
                }
            }

            if ((Engine.Input.Keyboard.IsKeyDown(Engine.Input.Key.F2)) || (ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("F2", true).IsTapped)) // Выделение 2 точки
            {
                ComponentMiner componentMiner = Subsystems.Player.ComponentPlayer.ComponentMiner;
                Point2 = componentMiner.PickTerrainForDigging(Subsystems.Drawing.ViewPosition, Subsystems.Drawing.ViewDirection);
                if (Point2.HasValue)
                {
                    SCModApi.Gui.DisplayMessage("Set position 2 on: " + Point1.Value.CellFace.X + ", " + Point1.Value.CellFace.Y + ", " + Point1.Value.CellFace.Z, false);
                    ReplaceableBlock = Subsystems.Terrain.TerrainData.GetCellValue(Point2.Value.CellFace.X, Point2.Value.CellFace.Y, Point2.Value.CellFace.Z);
                    return;
                }
            }

            if ((Engine.Input.Keyboard.IsKeyDown(Engine.Input.Key.F3)) || (ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("F3", true).IsTapped)) // Выделение 3 точки
            {
                ComponentMiner componentMiner = Subsystems.Player.ComponentPlayer.ComponentMiner;
                Point3 = componentMiner.PickTerrainForDigging(Subsystems.Drawing.ViewPosition, Subsystems.Drawing.ViewDirection);
                if (Point3.HasValue)
                {
                    SCModApi.Gui.DisplayMessage("Set position 3 on: " + Point1.Value.CellFace.X + ", " + Point1.Value.CellFace.Y + ", " + Point1.Value.CellFace.Z, false);
                    return;
                }
            }

            if (Point1.HasValue && Point2.HasValue && ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("WorldEditMenu", true).IsChecked) // Выделение зоны между точками 1 и 2
            {
                int startX = Math.Min(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                int endX = Math.Max(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                int startY = Math.Min(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                int endY = Math.Max(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                int startZ = Math.Min(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);
                int endZ = Math.Max(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);

                PrimitivesRenderer3D PrimitivesRenderer3D = new PrimitivesRenderer3D();
                Vector3 pointStart = new Vector3(startX, startY, startZ);
                Vector3 pointEnd = new Vector3(endX + 1, endY + 1, endZ + 1);
                BoundingBox boundingBox = new BoundingBox(pointStart, pointEnd);
                PrimitivesRenderer3D.FlatBatch(-1, DepthStencilState.None, (RasterizerState)null, (BlendState)null).QueueBoundingBox(boundingBox, Color.Green);
                PrimitivesRenderer3D.Flush(Subsystems.Drawing.ViewProjectionMatrix, true);
            }

            if (Point3.HasValue && ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("WorldEditMenu", true).IsChecked) // Выделение зоны вставки
            {
                
                int startX = Math.Min(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                int endX = Math.Max(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                int startY = Math.Min(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                int endY = Math.Max(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                int startZ = Math.Min(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);
                int endZ = Math.Max(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);

                startX += Point3.Value.CellFace.Point.X - Point1.Value.CellFace.Point.X;
                startY += Point3.Value.CellFace.Point.Y - Point1.Value.CellFace.Point.Y;
                startZ += Point3.Value.CellFace.Point.Z - Point1.Value.CellFace.Point.Z;
                endX += Point3.Value.CellFace.Point.X - Point1.Value.CellFace.Point.X;
                endY += Point3.Value.CellFace.Point.Y - Point1.Value.CellFace.Point.Y;
                endZ += Point3.Value.CellFace.Point.Z - Point1.Value.CellFace.Point.Z;
                
                PrimitivesRenderer3D primitivesRenderer3D = new PrimitivesRenderer3D();
                Vector3 pointStart = new Vector3(startX, startY, startZ);
                Vector3 pointEnd = new Vector3(endX + 1, endY + 1, endZ + 1);
                BoundingBox boundingBox = new BoundingBox(pointStart, pointEnd);
                primitivesRenderer3D.FlatBatch(-1, DepthStencilState.None, (RasterizerState)null, (BlendState)null).QueueBoundingBox(boundingBox, Color.Red);
                primitivesRenderer3D.Flush(Subsystems.Drawing.ViewProjectionMatrix, true);
            }

            if ((Engine.Input.Keyboard.IsKeyDownOnce(Engine.Input.Key.F5)) || (ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("F5", true).IsTapped)) // Копирование выделенной зоны
            {
                if (Point1 == null)
                {
                    Gui.DisplayMessage("You have not selected point 1", false);
                }
                else if (Point2 == null)
                {
                    Gui.DisplayMessage("You have not selected point 2", false);
                }
                else if (Point3 == null)
                {
                    Gui.DisplayMessage("You have not selected point 3", false);
                }
                else
                {
                    int startX = Math.Min(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                    int endX = Math.Max(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                    int startY = Math.Min(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                    int endY = Math.Max(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                    int startZ = Math.Min(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);
                    int endZ = Math.Max(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);

                    for (int y = 0; y <= endY - startY; y++)
                    {
                        for (int z = 0; z <= endZ - startZ; z++)
                        {
                            for (int x = 0; x <= endX - startX; x++)
                            {
                                int targetX, targetY, targetZ;
                                int PlaceX, PlaceY, PlaceZ;
                                if (Point1.Value.CellFace.Point.X > Point2.Value.CellFace.Point.X)
                                {
                                    targetX = Point1.Value.CellFace.Point.X - x;
                                    PlaceX = Point3.Value.CellFace.Point.X - x;
                                }
                                else
                                {
                                    targetX = Point1.Value.CellFace.Point.X + x;
                                    PlaceX = Point3.Value.CellFace.Point.X + x;
                                }

                                if (Point1.Value.CellFace.Point.Y > Point2.Value.CellFace.Point.Y)
                                {
                                    targetY = Point1.Value.CellFace.Point.Y - y;
                                    PlaceY = Point3.Value.CellFace.Point.Y - y;
                                }
                                else
                                {
                                    targetY = Point1.Value.CellFace.Point.Y + y;
                                    PlaceY = Point3.Value.CellFace.Point.Y + y;
                                }

                                if (Point1.Value.CellFace.Point.Z > Point2.Value.CellFace.Point.Z)
                                {
                                    targetZ = Point1.Value.CellFace.Point.Z - z;
                                    PlaceZ = Point3.Value.CellFace.Point.Z - z;
                                }
                                else
                                {
                                    targetZ = Point1.Value.CellFace.Point.Z + z;
                                    PlaceZ = Point3.Value.CellFace.Point.Z + z;
                                }
                                
                                int block = World.GetBlock(targetX, targetY, targetZ);
                                // 15360 - Air
                                if (block != 15360) World.SetBlock(PlaceX, PlaceY, PlaceZ, block);
                            }
                        }
                    }
                }
                return;
            }

            if ((Engine.Input.Keyboard.IsKeyDownOnce(Engine.Input.Key.F6)) || (ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("F6", true).IsTapped)) // Заполнение зоны
            {
                if (Point1 == null)
                {
                    Gui.DisplayMessage("You have not selected point 1", false);
                }
                else if (Point2 == null)
                {
                    Gui.DisplayMessage("You have not selected point 2", false);
                }
                else
                {
                    int startX = Math.Min(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                    int endX = Math.Max(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                    int startY = Math.Min(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                    int endY = Math.Max(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                    int startZ = Math.Min(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);
                    int endZ = Math.Max(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);

                    for (int x = startX; x <= endX; x++)
                    {
                        for (int y = startY; y <= endY; y++)
                        {
                            for (int z = startZ; z <= endZ; z++)
                            {
                                World.SetBlock(x, y, z, SelectedBlock);
                            }
                        }
                    }
                }
                return;
            }

            if ((Engine.Input.Keyboard.IsKeyDownOnce(Engine.Input.Key.F7)) || (ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("F7", true).IsTapped)) // Замена зоны
            {
                if (Point1 == null)
                {
                    Gui.DisplayMessage("You have not selected point 1", false);
                }
                else if (Point2 == null)
                {
                    Gui.DisplayMessage("You have not selected point 2", false);
                }
                else
                {
                    int startX = Math.Min(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                    int endX = Math.Max(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                    int startY = Math.Min(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                    int endY = Math.Max(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                    int startZ = Math.Min(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);
                    int endZ = Math.Max(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);
                    

                    for (int x = startX; x <= endX; x++)
                    {
                        for (int y = startY; y <= endY; y++)
                        {
                            for (int z = startZ; z <= endZ; z++)
                            {
                                if (World.GetBlock(x, y, z) == ReplaceableBlock) World.SetBlock(x, y, z, SelectedBlock);
                            }
                        }
                    }
                }
                return;
            }

            if ((Engine.Input.Keyboard.IsKeyDownOnce(Engine.Input.Key.F8)) || (ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("F8", true).IsTapped)) // Очистка зоны
            {
                if (Point1 == null)
                {
                    Gui.DisplayMessage("You have not selected point 1", false);
                }
                else if (Point2 == null)
                {
                    Gui.DisplayMessage("You have not selected point 2", false);
                }
                else
                {
                    int startX = Math.Min(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                    int endX = Math.Max(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                    int startY = Math.Min(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                    int endY = Math.Max(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                    int startZ = Math.Min(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);
                    int endZ = Math.Max(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);

                    for (int x = startX; x <= endX; x++)
                    {
                        for (int y = startY; y <= endY; y++)
                        {
                            for (int z = startZ; z <= endZ; z++)
                            {
                                World.SetBlock(x, y, z, 0);
                            }
                        }
                    }
                }
                return;
            }

            if ((Engine.Input.Keyboard.IsKeyDownOnce(Engine.Input.Key.F9)) || (ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("F9", true).IsTapped)) // Копирование зоны в память
            {
                if (Point1 == null)
                {
                    Gui.DisplayMessage("You have not selected point 1", false);
                }
                else if (Point2 == null)
                {
                    Gui.DisplayMessage("You have not selected point 2", false);
                }
                else
                {
                    int startX = Math.Min(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                    int endX = Math.Max(Point1.Value.CellFace.Point.X, Point2.Value.CellFace.Point.X);
                    int startY = Math.Min(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                    int endY = Math.Max(Point1.Value.CellFace.Point.Y, Point2.Value.CellFace.Point.Y);
                    int startZ = Math.Min(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);
                    int endZ = Math.Max(Point1.Value.CellFace.Point.Z, Point2.Value.CellFace.Point.Z);

                    blockCount = 0;
                    BlockList.Clear();

                    for (int x = 0; x <= endX - startX; x++)
                    {
                        for (int y = 0; y <= endY - startY; y++)
                        {
                            for (int z = 0; z <= endZ - startZ; z++)
                            {
                                BlockMem blmem = new BlockMem();
                                int X, Y, Z;
                                if (Point1.Value.CellFace.Point.X > Point2.Value.CellFace.Point.X)
                                {
                                    blmem.x = - x;
                                    X = Point1.Value.CellFace.Point.X - x;
                                }
                                else
                                {
                                    blmem.x = x;
                                    X = Point1.Value.CellFace.Point.X + x;
                                }

                                if (Point1.Value.CellFace.Point.Y > Point2.Value.CellFace.Point.Y)
                                {
                                    blmem.y = - y;
                                    Y = Point1.Value.CellFace.Point.Y - y;
                                }
                                else
                                {
                                    blmem.y = y;
                                    Y = Point1.Value.CellFace.Point.Y + y;
                                }

                                if (Point1.Value.CellFace.Point.Z > Point2.Value.CellFace.Point.Z)
                                {
                                    blmem.z = - z;
                                    Z = Point1.Value.CellFace.Point.Z - z;
                                }
                                else
                                {
                                    blmem.z = z;
                                    Z = Point1.Value.CellFace.Point.Z + z;
                                }
                                
                                blmem.id = World.GetBlock(X, Y, Z);
                                BlockList.Add(blmem);
                                blockCount++;
                            }
                        }
                    }
                }
                Gui.DisplayMessage("Copied " + blockCount + " blocks", false);
                return;
            }

            if ((Engine.Input.Keyboard.IsKeyDownOnce(Engine.Input.Key.F10)) || (ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>("F10", true).IsTapped))  // Вставка зоны из памяти
            {
                if (Point3 == null) Gui.DisplayMessage("You have not selected point 3", false);
                else
                {
                    for (var i = 0; i < blockCount; i++)
                    {                        
                        var xPos = Point3.Value.CellFace.X + BlockList[i].x;
                        var yPos = Point3.Value.CellFace.Y + BlockList[i].y;
                        var zPos = Point3.Value.CellFace.Z + BlockList[i].z;
                        
                        World.SetBlock(xPos, yPos, zPos, BlockList[i].id);
                    }
                    Gui.DisplayMessage("Pasted " + blockCount + " blocks", false);
                }
            }

        }



        internal class BlockMem
        {
            internal int x;
            internal int y;
            internal int z;
            internal int id;
        }

        public static void ApplyButtonImage(string widgetName, string normalSubtexture, string clickedSubtexture) // Метод применения текстуры кнопокам из файлов
        {
            if (File.Exists(normalSubtexture))
            {
                using (FileStream fs = new FileStream(normalSubtexture, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Subtexture subtexture = new Subtexture(Texture2D.Load(fs), Vector2.Zero, Vector2.One);
                    ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>(widgetName, true).NormalSubtexture = subtexture;
                    fs.Close();
                }
            }
            else Log.Warning("Subtexture " + normalSubtexture + " not found!");

            if (File.Exists(clickedSubtexture))
            {
                using (FileStream fs = new FileStream(clickedSubtexture, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Subtexture subtexture = new Subtexture(Texture2D.Load(fs), Vector2.Zero, Vector2.One);
                    ScreensManager.CurrentScreen.ScreenWidget.FindWidget<BitmapButtonWidget>(widgetName, true).ClickedSubtexture = subtexture;
                    fs.Close();
                }
            }
            else Log.Warning("Subtexture " + clickedSubtexture + " not found!");
        }

        public static void LoadButtons(string XmlPath) // Загрузка кнопок из XML файла
        {
            using (FileStream fs = new FileStream(XmlPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                WidgetsManager.LoadWidgetContents(ScreensManager.CurrentScreen.ScreenWidget, ScreensManager.CurrentScreen, XElement.Load(fs));
                fs.Close();
            }
        }
        
    }
}
