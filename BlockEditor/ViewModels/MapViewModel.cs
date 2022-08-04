﻿using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using BlockEditor.Helpers;
using BlockEditor.Models;
using BlockEditor.Utils;
using BlockEditor.Views.Windows;
using static BlockEditor.Models.BlockImages;

namespace BlockEditor.ViewModels
{
    public class MapViewModel : NotificationObject
    {
        public Game Game { get; }

        private MyPoint? _mousePosition;
        private UserMode _mode;

        public UserMode Mode { 
            get
            {
                return _mode;
            }
            set
            {
                _mode = value;
                RaisePropertyChanged(nameof(IsSelectionMode));
                RaisePropertyChanged(nameof(IsFillMode));
            }
        }

        public BitmapImage MapContent
        {
            get => Game.GameImage?.GetImage(); 
        }

        public BlockSelection BlockSelection { get; }

        private Action _cleanBlockSelection { get; }

        public bool IsSelectionMode => Mode == UserMode.Selection;
        public bool IsFillMode {
            get { return Mode == UserMode.Fill; }
            set { Mode = value ? UserMode.Fill : UserMode.None; }
        }


        public bool IsOverwrite {
            get { return Game.Map?.Blocks?.Overwrite ?? false; }
            set { Game.Map.Blocks.Overwrite = value; RaisePropertyChanged(); }
        }


        public RelayCommand StartPositionCommand { get; }
        public RelayCommand FillCommand { get; }


        public MapViewModel(Action cleanBlockSelection)
        {
            Game = new Game();
            Mode = UserMode.None;

            _cleanBlockSelection = cleanBlockSelection;
            BlockSelection       = new BlockSelection();
            StartPositionCommand = new RelayCommand((_) => Game.GoToStartPosition());
            FillCommand          = new RelayCommand((_) => IsFillMode = !IsFillMode);
            BlockSelection.OnSelectionClick += OnSelectionClick;
            Game.Engine.OnFrame += OnFrameUpdate;
        }


        #region Events
       
        private void OnSelectionClick()
        {
            Mode = UserMode.Selection;
            _cleanBlockSelection?.Invoke();
        }

        public void OnCleanUserMode()
        {
            _cleanBlockSelection?.Invoke();
            BlockSelection?.Clean();
            Mode = UserMode.None;
        }

        public void OnFrameUpdate()
        {
            new FrameUpdate(Game, _mousePosition, BlockSelection);

            RaisePropertyChanged(nameof(MapContent));
        }

        internal void OnSelectedBlockID(int? id)
        {
            if(Mode != UserMode.Selection && Mode != UserMode.Fill)
            {
                BlockSelection.Clean();
                BlockSelection.SelectedBlock = id;
            }

            if(id != null && Mode != UserMode.Fill)
                Mode = UserMode.AddBlock;
        }

        public void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (Mode)
            {
                case UserMode.AddBlock:
                    var p1 = MyUtils.GetPosition(sender as IInputElement, e);

                    if (e.ChangedButton == MouseButton.Right)
                        Game.DeleteBlock(p1);
                    else if (e.ChangedButton == MouseButton.Left)
                        Game.AddBlock(p1, BlockSelection.SelectedBlock);
                    break;

                case UserMode.Selection:
     
                    var p2 = MyUtils.GetPosition(sender as IInputElement, e);

                    if (p2 == null)
                        break;

                    if (e.LeftButton == MouseButtonState.Pressed) 
                    { 
                        BlockSelection.UserSelection.OnMouseDown(p2, Game.GetMapIndex(p2));
                    }
                    else if (e.RightButton == MouseButtonState.Pressed)
                    { 
                        var click = Game.GetMapIndex(_mousePosition);
                        var start = BlockSelection.UserSelection.MapRegion.Start;
                        var end   = BlockSelection.UserSelection.MapRegion.End;

                        if(BlockSelection.UserSelection.MapRegion.IsInside(click))
                            break;

                        Game.DeleteSelection(start, end);
                        OnCleanUserMode();
                    }

                    break;

                case UserMode.AddSelection:
                    if (e.ChangedButton != MouseButton.Left)
                        break;

                    var p3 = MyUtils.GetPosition(sender as IInputElement, e);
                    Game.AddSelection(p3, BlockSelection.SelectedBlocks);
                    break;

                case UserMode.Fill:
                    if (e.ChangedButton != MouseButton.Left)
                        break;

                    var id = BlockSelection.SelectedBlock;
                    
                    if(id == null)
                        throw new Exception("Select a block to flood fill.");

                    var index = Game.GetMapIndex(MyUtils.GetPosition(sender as IInputElement, e));

                    var click4 = Game.GetMapIndex(_mousePosition);

                    Mode = UserMode.None;

                    using(new TempCursor(Cursors.Wait)) 
                    {
                        if (BlockSelection.UserSelection.MapRegion.IsInside(click4))
                            Game.AddBlocks(MapUtil.GetRectangleFill(Game.Map, id.Value, BlockSelection.UserSelection.MapRegion));
                        else
                            Game.AddBlocks(MapUtil.GetFloodFill(Game.Map, index, id.Value));
                    }
                    break;

                case UserMode.None:
                    var p4 = MyUtils.GetPosition(sender as IInputElement, e);

                    if (e.ChangedButton == MouseButton.Right)
                        Game.DeleteBlock(p4);

                    break;
            }
        }

        public void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            switch (Mode)
            {
                case UserMode.AddBlock:
                    _mousePosition = MyUtils.GetPosition(sender as IInputElement, e);

                    if (e.RightButton == MouseButtonState.Pressed)
                        Game.DeleteBlock(_mousePosition);
                    else if (e.LeftButton == MouseButtonState.Pressed)
                        Game.AddBlock(_mousePosition, BlockSelection.SelectedBlock);
                    break;

                case UserMode.Selection:
                    _mousePosition = MyUtils.GetPosition(sender as IInputElement, e);
                    break;

                case UserMode.Fill:
                    _mousePosition = MyUtils.GetPosition(sender as IInputElement, e);
                    break;

                case UserMode.AddSelection:
                    _mousePosition = MyUtils.GetPosition(sender as IInputElement, e);

                    if (e.LeftButton != MouseButtonState.Pressed)
                        break;

                    var p3 = MyUtils.GetPosition(sender as IInputElement, e);
                    Game.AddSelection(p3, BlockSelection.SelectedBlocks);
                    break;
            }
        }

        internal void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            switch (Mode)
            {
                case UserMode.Selection:

                    if (e.ChangedButton != MouseButton.Left)
                        break;

                    var pos   = MyUtils.GetPosition(sender as IInputElement, e);
                    var index = Game.GetMapIndex(pos);

                    BlockSelection.UserSelection.OnMouseUp(pos, index);
                    break;
            }
        }

        public void OnSizeChanged(int width, int height)
        {
            if(Game.GameImage != null)
                Game.GameImage.Dispose();

           Game.Camera.ScreenSize = new MyPoint(width, height);
            Game.GameImage = new GameImage(width, height);  // thread safe?
        }
      
        public void OnLoadMap(Map map)
        {
            if (map == null)
                return;

            Game.Engine.Pause = true;
            Thread.Sleep(GameEngine.FPS * 5); // make sure engine has been stopped

            var size = Game.Map.BlockSize;
            Game.Map = map;
            Game.Map.BlockSize = size;

            Game.UserOperations.Clear();

            Game.GoToStartPosition();

            Game.Engine.Pause = false;
        }

        public void OnZoomChanged(BlockSize size)
        {
            var halfScreenX = Game.GameImage.Width  / 2;
            var halfScreenY = Game.GameImage.Height / 2;

            var cameraPosition = new MyPoint(Game.Camera.Position.X, Game.Camera.Position.Y);
            var middleOfScreen = new MyPoint(cameraPosition.X + halfScreenX, cameraPosition.Y + halfScreenY);

            var currentIndex = Game.Map.GetMapIndex(middleOfScreen);
            var currentSize  = Game.Map.BlockSize;

            Game.Map.BlockSize = size;

            var x = currentIndex.X * Game.Map.BlockPixelSize - halfScreenX;
            var y = currentIndex.Y * Game.Map.BlockPixelSize - halfScreenY;

            Game.Camera.Position = new MyPoint(x, y);
        }

        #endregion

    }
}
