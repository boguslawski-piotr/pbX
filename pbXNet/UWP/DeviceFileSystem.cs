﻿#if WINDOWS_UWP

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace pbXNet
{
	public partial class DeviceFileSystem : IFileSystem, IDisposable
	{
        public readonly IEnumerable<DeviceFileSystemRoot> AvailableRootsForEndUser = new List<DeviceFileSystemRoot>() 
        { 
            DeviceFileSystemRoot.Personal,
        };

        protected void Initialize(string dirname)
        {
            // TODO: UWP DeviceFileSystem obsluzyc wiecej typow DeviceFileSystemRoot Root
            _root = ApplicationData.Current.LocalFolder;
            SetCurrentDirectory(dirname);
        }

        public void Dispose()
        {
            _current = null;
            _root = null;
        }

        private StorageFolder _root;
        private StorageFolder _current;

        async public Task<IFileSystem> MakeCopyAsync()
        {
            DeviceFileSystem cp = new DeviceFileSystem();
            cp.SetType(_type);
            if (_root != null)
                cp._root = await StorageFolder.GetFolderFromPathAsync(_root.Path);
            if (_current != null)
                cp._current = await StorageFolder.GetFolderFromPathAsync(_current.Path);
            return cp;
        }

        async public Task SetCurrentDirectoryAsync(string dirname)
        {
            if (dirname == null || dirname == "")
            {
                _current = _root;
            }
            else if (dirname == "..")
            {
                _current = await _current.GetParentAsync();
            }
            else
            {
                _current = await _current.GetFolderAsync(dirname);
            }
        }

        //

        async public Task<bool> DirectoryExistsAsync(string dirname)
        {
            return await _current.TryGetItemAsync(dirname) != null;
        }

        async public Task<bool> FileExistsAsync(string filename)
        {
            return await _current.TryGetItemAsync(filename) != null;
        }

        async public Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "")
        {
            IEnumerable<string> dirnames =
                from storageDir in await _current.GetFoldersAsync()
                where Regex.IsMatch(storageDir.Name, pattern)                
                select storageDir.Name;
            return dirnames;
        }

        async public Task<IEnumerable<string>> GetFilesAsync(string pattern = "")
        {
            IEnumerable<string> filenames =
                from storageFile in await _current.GetFilesAsync()
                where Regex.IsMatch(storageFile.Name, pattern)                
                select storageFile.Name;
            return filenames;
        }

        async public Task CreateDirectoryAsync(string dirname)
        {
            _current = await _current.CreateFolderAsync(dirname, CreationCollisionOption.OpenIfExists);
        }

        async public Task WriteTextAsync(string filename, string text)
        {
            IStorageFile storageFile = await _current.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(storageFile, text);
        }

        async public Task<string> ReadTextAsync(string filename)
        {
            IStorageFile storageFile = await _current.GetFileAsync(filename);
            return await FileIO.ReadTextAsync(storageFile);
        }

        public Task DeleteDirectoryAsync(string dirname)
        {
            throw new NotImplementedException();
        }

        public Task DeleteFileAsync(string filename)
        {
            throw new NotImplementedException();
        }
    }
}

#endif
