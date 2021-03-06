﻿using Common.Dto;
using Common.Tools;
using Data.Controller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows.Media.Imaging;

namespace Data.Services
{
    public delegate void NovelListDownloadEventHandler(IList<NovelDto> novelList, bool success, string response);
    public delegate void NovelServiceErrorEventHandler(string error);

    public class NovelService
    {
        private const string TAG = "NovelService";
        private const int TIMEOUT = 6 * 60 * 60 * 1000;

        private readonly LocalDriveController _localDriveController;

        private static NovelService _instance = null;
        private static readonly object _padlock = new object();

        private DriveInfo _libraryDrive;
        private string _novelDir = string.Empty;
        private IList<NovelDto> _novelList = new List<NovelDto>();

        private Timer _reloadTimer;

        NovelService()
        {
            _localDriveController = new LocalDriveController();
            _libraryDrive = _localDriveController.GetLibraryDrive();
            if (_libraryDrive == null)
            {
                Logger.Instance.Error(TAG, "Found no library drive!");
            }
            else
            {
                _novelDir = _libraryDrive.Name + "Books\\Romane";
            }

            _reloadTimer = new Timer(TIMEOUT);
            _reloadTimer.Elapsed += _reloadTimer_Elapsed;
            _reloadTimer.AutoReset = true;
            _reloadTimer.Start();
        }

        public event NovelListDownloadEventHandler OnNovelListDownloadFinished;
        private void publishOnNovelListDownloadFinished(IList<NovelDto> novelList, bool success, string response)
        {
            OnNovelListDownloadFinished?.Invoke(novelList, success, response);
        }

        public event NovelServiceErrorEventHandler OnNovelServiceError;
        private void publishOnNovelServiceError(string error)
        {
            OnNovelServiceError?.Invoke(error);
        }

        public static NovelService Instance
        {
            get
            {
                lock (_padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new NovelService();
                    }

                    return _instance;
                }
            }
        }

        public string NovelDir
        {
            get
            {
                return _novelDir;
            }
        }

        public IList<NovelDto> NovelList
        {
            get
            {
                return _novelList;
            }
        }

        public NovelDto GetByAuthor(string author)
        {
            NovelDto foundNovelDtos = _novelList
                        .Where(entry => entry.Author.Contains(author))
                        .Select(entry => entry)
                        .FirstOrDefault();

            return foundNovelDtos;
        }

        public IList<NovelDto> FoundNovelDtos(string searchKey)
        {
            if (searchKey == string.Empty)
            {
                return _novelList;
            }

            List<NovelDto> foundNovelList = _novelList
                        .Where(novel => novel.ToString().Contains(searchKey))
                        .Select(novel => novel)
                        .OrderBy(novel => novel.Author)
                        .ToList();
            return foundNovelList;
        }

        public void StartReading(string directory, string title)
        {
            if (!directoryAvailable())
            {
                return;
            }

            if (directory == null || title == null
                || directory == string.Empty || title == string.Empty)
            {
                Logger.Instance.Error(TAG, "Diretory or title is null or empty!");
                publishOnNovelServiceError("Diretory or title is null or empty!");
                return;
            }

            string command = string.Format(@"{0}\{1}\{2}", _novelDir, directory, title);
            Process.Start(command);
        }

        public void OpenExplorer(string directory)
        {
            if (!directoryAvailable())
            {
                return;
            }

            if (directory == null || directory == string.Empty)
            {
                Logger.Instance.Error(TAG, "Diretory is null or empty!");
                publishOnNovelServiceError("Diretory is null or empty!");
                return;
            }

            string command = string.Format(@"{0}\{1}", _novelDir, directory);
            Process.Start(command);
        }

        public void LoadNovelList()
        {
            if (!directoryAvailable())
            {
                publishOnNovelListDownloadFinished(null, false, "Novel directory is not available!");
                return;
            }

            string[] extensionArray = new string[] { ".pdf", ".epub" };
            _novelList.Clear();
            string[] authorList = _localDriveController.ReadDirInDir(_novelDir);

            foreach (string authorDir in authorList)
            {
                string[] bookList = _localDriveController.ReadFileNamesInDir(authorDir, extensionArray);
                Uri iconUri = new Uri(string.Format("{0}\\_icon.jpg", authorDir), UriKind.Absolute);

                bookList = bookList
                    .Select(fileName => fileName.Replace(authorDir, "").Replace("\\", ""))
                    .OrderBy(fileName => fileName)
                    .ToArray();
                string author = authorDir.Replace(_novelDir, "").Replace("\\", "");
                BitmapImage icon = new BitmapImage(iconUri);

                NovelDto newEntry = new NovelDto(
                    author,
                    bookList,
                    icon);
                _novelList.Add(newEntry);
            }

            _novelList = _novelList.OrderBy(x => x.Author).ToList();
            publishOnNovelListDownloadFinished(_novelList, true, "Success");
        }

        private void _reloadTimer_Elapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            LoadNovelList();
        }

        private bool directoryAvailable()
        {
            if (_novelDir == string.Empty)
            {
                Logger.Instance.Error(TAG, "No directory for novels! Trying to read again...");
                _libraryDrive = _localDriveController.GetLibraryDrive();

                if (_libraryDrive == null)
                {
                    Logger.Instance.Error(TAG, "Found no library drive!");
                    publishOnNovelServiceError("Found no library drive! Please check your attached storages!");
                    return false;
                }
                else
                {
                    _novelDir = _libraryDrive.Name + "Books\\Romane";
                }
            }

            return true;
        }

        public void Dispose()
        {
            Logger.Instance.Debug(TAG, "Dispose");

            _reloadTimer.Elapsed -= _reloadTimer_Elapsed;
            _reloadTimer.AutoReset = false;
            _reloadTimer.Stop();
        }
    }
}
