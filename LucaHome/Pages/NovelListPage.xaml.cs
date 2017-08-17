﻿using Common.Common;
using Common.Tools;
using Data.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Common.Dto;
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace LucaHome.Pages
{
    public partial class NovelListPage : Page, INotifyPropertyChanged
    {
        private const string TAG = "MagazinListPage";
        private readonly Logger _logger;

        private readonly NavigationService _navigationService;
        private readonly NovelService _novelService;

        private string _novelListSearchKey = string.Empty;
        private NovelDto _novelDto;
        private IList<string> _bookList = new List<string>();

        public NovelListPage(NavigationService navigationService, NovelDto novelDto)
        {
            _logger = new Logger(TAG, Enables.LOGGING);

            _navigationService = navigationService;
            _novelService = NovelService.Instance;

            _novelDto = novelDto;
            _bookList = _novelDto.Books;

            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string NovelListSearchKey
        {
            get
            {
                return _novelListSearchKey;
            }
            set
            {
                _novelListSearchKey = value;
                OnPropertyChanged("NovelListSearchKey");

                if (_novelListSearchKey != string.Empty)
                {
                    BookList = _novelDto.Books
                        .Where(entry => entry.Contains(_novelListSearchKey))
                        .Select(entry => entry).ToList();
                }
                else
                {
                    BookList = _novelDto.Books;
                }
            }
        }

        public string Author
        {
            get
            {
                return _novelDto.Author;
            }
        }

        public BitmapImage AuthorIcon
        {
            get
            {
                return _novelDto.Icon;
            }
        }

        public IList<string> BookList
        {
            get
            {
                return _bookList;
            }
            set
            {
                _bookList = value;
                OnPropertyChanged("BookList");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            _logger.Debug(string.Format("Received click of sender {0} with arguments {1}", sender, routedEventArgs));
            if (sender is Button)
            {
                Button senderButton = (Button)sender;
                _logger.Debug(string.Format("Tag is {0}", senderButton.Tag));

                string bookTitle = (string)senderButton.Tag;

                string command = string.Format(@"{0}\{1}\{2}", _novelService.NovelDir, _novelDto.Author, bookTitle);
                Process.Start(command);
            }
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            _logger.Debug(string.Format("ButtonBack_Click with sender {0} and routedEventArgs {1}", sender, routedEventArgs));
            _navigationService.GoBack();
        }
    }
}
