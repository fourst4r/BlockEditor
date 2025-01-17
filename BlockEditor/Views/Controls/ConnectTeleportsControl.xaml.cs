﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlockEditor.Helpers;
using BlockEditor.Models;

using static BlockEditor.Models.UserMode;

namespace BlockEditor.Views.Controls
{
    public partial class ConnectTeleportsControl : UserControl
    {


        private readonly ConnectTeleports _data;

        private readonly Game _game;
        private readonly Cursor _connectCursor;


        public ConnectTeleportsControl(Game game)
        {
            InitializeComponent();
            _game = game;
            _data = new ConnectTeleports();
            _connectCursor = Mouse.OverrideCursor;
            Init(99);
        }



        private void Init(int count)
        {
            var culture = CultureInfo.InvariantCulture;

            MyColorPicker.SetColor(string.Empty);
            MyColorPicker.OnNewColor += MyColorPicker_OnNewColor;

            _data.Start();
            UpdateGui();
        }

        private void UpdateGui()
        {
            tbCount.Content = "Selected Block Count:  " + _data.Count;
            btnOK.IsEnabled = _data.Count > 0;
            btnReset.IsEnabled = _data.Count > 0 || !string.IsNullOrEmpty(_data.Options);
        }

        public void Add(SimpleBlock point)
        {
            _data.Add(point);

            UpdateGui(); 
        }


        private void Close(bool addBlocks)
        {
            App.MyMainWindow?.CurrentMap?.ClearSidePanel();

            if(_game == null)
                return;

            _game.Mode.Value = UserModes.None;

            if(!addBlocks)
                return;

            using (new TempCursor(Cursors.Wait))
                _game.AddBlocks(_data.GetAddedBlocks());

        }


        private void MyColorPicker_OnNewColor(string text)
        {
            try
            {
                if (!string.IsNullOrEmpty(text))
                    text = Convert.ToInt32(text, 16).ToString();
            }
            catch
            {
                MessageUtil.ShowError("Failed to convert color to PR2 block option format.");
            }

            _data.Options = text;
            UpdateGui();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close(false);
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            _data.Start();
            MyColorPicker.SetColor(string.Empty);
            UpdateGui();
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = null;
            }
            catch { }
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                if(_game.Mode.Value == UserModes.ConnectTeleports)
                    Mouse.OverrideCursor = _connectCursor;
            }
            catch {}
        }
    }
}
