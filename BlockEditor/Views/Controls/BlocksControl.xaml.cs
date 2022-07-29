﻿using BlockEditor.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BlockEditor.Views.Controls
{
    public partial class BlocksControl : UserControl
    {

        public ImageBlock SelectedBlock { get; private set; }

        private Border _selectedBorder { get; set; }


        public BlocksControl()
        {
            InitializeComponent();

            AddBlocks();
        }


        private void AddBlocks()
        {
            foreach (var image in BlockImages.Images)
            {
                if (image.Value == null)
                    continue;

                BlockContainer.Children.Add(CreateBorder(image.Value));
            }
        }

        private Border CreateBorder(ImageBlock block)
        {
            var border = new Border();
            border.Margin = new Thickness(3, 6, 3, 6);
            border.MouseDown += Border_MouseDown;
            border.BorderThickness = new Thickness(3);
            border.Child = block;

            return border;
        }

        private void ToggleBorder(Border border)
        {
            if (border == null)
                return;

            if (border.BorderBrush == null)
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            else
                _selectedBorder.BorderBrush = null;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            var border  = sender as Border;
            var block = border?.Child as ImageBlock;

            if (block == null)
                return;

            ToggleBorder(_selectedBorder);

            _selectedBorder = border;
            SelectedBlock   = block;

            ToggleBorder(_selectedBorder);
        }

    }
}