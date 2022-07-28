﻿using BlockEditor.Helpers;
using BlockEditor.Models;
using DataAccess;
using Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockEditor.Views
{


    public partial class LoadMapWindow : Window
    {
        private enum SearchBy { Username, ID, MyLevels }

        public int SelectedLevelID { get; private set; }

        private SearchBy _searchBy;

        private int _page = 1;

        public LoadMapWindow()
        {
            InitializeComponent();
            AddSearchByItems();
            UpdateButtons();
        }

        private void AddSearchByItems()
        {
            SearchByComboBox.Items.Clear();

            foreach (var type in Enum.GetValues(typeof(SearchBy)))
            {
                var item = new ComboBoxItem();
                item.Content = InsertSpaceBeforeCapitalLetter(type.ToString());
                SearchByComboBox.Items.Add(item);
            }
        }

        private string InsertSpaceBeforeCapitalLetter(string input)
        {
            if(string.IsNullOrWhiteSpace(input))
                return string.Empty;

            if(string.Equals("ID", input, StringComparison.InvariantCultureIgnoreCase))
                return input;

            return string.Concat(input.ToString().Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
        }

        private void AddSearchResults(IEnumerable<SearchResult> results)
        {
            if (results == null)
                return;

            foreach (var item in results)
            {
                if (item == null)
                    continue;

                var control = new SearchResultControl(item);
                control.OnSelectedLevel += OnSelectedLevel;

                SearchResultPanel.Children.Add(control);
            }
        }

        private void OnSelectedLevel(int id)
        {
            SelectedLevelID = id;
            DialogResult = true;
            Close();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            using(new TempCursor(Cursors.Wait)) 
            { 

                SearchResultPanel.Children.Clear();
                var search = searchTextbox.Text;

                switch (_searchBy)
                {
                    case SearchBy.Username:
                        AddSearchResults(SearchByUsername(search));
                        break;

                    case SearchBy.ID:
                        AddSearchResults(SearchByLevelId(search));
                        break;

                    case SearchBy.MyLevels:
                        AddSearchResults(SearchMyLevels());
                        break;

                    default: throw new Exception("Something is wrong...");
                }
            }
        }

        private IEnumerable<SearchResult> SearchByUsername(string username)
        {
            if(string.IsNullOrWhiteSpace(username))
                yield break;

            var data   = PR2Accessor.Search(username, _page);
            var levels = PR2Parser.SearchResult(data);

            foreach (var l in levels)
            {
                if(l == null)
                    continue;

                yield return new SearchResult(l.LevelID, l.Title);
            }
        }

        private IEnumerable<SearchResult> SearchMyLevels()
        {
            if(!CurrentUser.IsLoggedIn())
            {
                MessageUtil.ShowError("Requires user to login");
                yield break;
            }

            var data = PR2Accessor.LoadMyLevels(CurrentUser.Token);
            var levels = PR2Parser.LoadResult(data);

            foreach (var l in levels)
            {
                if (l == null)
                    continue;

                yield return new SearchResult(l.LevelID, l.Title);
            }
        }

        private IEnumerable<SearchResult> SearchByLevelId(string levelID)
        {
            SearchResult result = null;

            try
            {
                if (string.IsNullOrWhiteSpace(levelID))
                    yield break;

                if (!MyConverters.TryParse(levelID, out var id))
                {
                    MessageUtil.ShowError("Invalid Level ID");
                    yield break;
                }

                var data = PR2Accessor.Download(id);

                if (string.IsNullOrWhiteSpace(data))
                {
                    MessageUtil.ShowError("Failed to download level");
                    yield break;
                }

                var levelInfo = PR2Parser.Level(data);

                if (levelInfo?.Level?.Title == null)
                {
                    MessageUtil.ShowError("Failed to parse level");
                    yield break;
                }

                if (levelInfo.Messages == null || levelInfo.Messages.Any())
                {
                    MessageUtil.ShowMessages(levelInfo.Messages);
                    yield break;
                }

                result = new SearchResult(levelInfo.Level.LevelID, levelInfo.Level.Title);
            }
            catch (WebException ex)
            {
                var r = ex.Response as HttpWebResponse;

                if (r != null && r.StatusCode == HttpStatusCode.NotFound)
                    MessageUtil.ShowInfo("Level not found");
            }

            if(result != null)
                yield return result;
        }

        private void SearchBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _searchBy = (SearchBy)SearchByComboBox.SelectedIndex;
            }
            catch (Exception ex)
            {
                MessageUtil.ShowError(ex.Message);
            }

            UpdateButtons();
        }

        private bool IsOKToSearch()
        {
            return !string.IsNullOrWhiteSpace(searchTextbox.Text) || _searchBy == SearchBy.MyLevels;
        }

        private void UpdateButtons()
        {
            btnSearch.IsEnabled = IsOKToSearch();
        }

        private void searchTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateButtons();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(!IsOKToSearch())
                return;

            if(e.Key == Key.Enter)
                Search_Click(null , null);

            UpdateButtons();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            searchTextbox.Focus();
        }

        private void OnPreviousPage(object sender, RoutedEventArgs e)
        {

        }

        private void OnNextPage(object sender, RoutedEventArgs e)
        {

        }
    }
}