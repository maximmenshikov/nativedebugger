using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace InteropSvc
{
    internal class InteropLib
    {
        public const uint HKEY_CLASSES_ROOT = 0x80000000;
        public const uint HKEY_CURRENT_USER = 0x80000001;
        public const uint HKEY_LOCAL_MACHINE = 0x80000002;
        public const uint HKEY_USERS = 0x80000003;

        public enum RadioDeviceType : uint
        {
            RADIODEVICES_MANAGED = 1,
            RADIODEVICES_PHONE,
            RADIODEVICES_BLUETOOTH,
            RADIODEVICES_WIFI
        }

        private static IInteropClass _instance = null;

        public static IInteropClass Instance
        {
            get
            {
                if (_instance == null)
                    Initialize();
                return _instance;
            }
        }

        public static bool Initialize()
        {
            if (_instance == null)
            {
                try
                {
                    uint retval = Microsoft.Phone.InteropServices.ComBridge.RegisterComDll("InteropLibWL.dll", new Guid("070E61BC-473F-4215-8A31-02206D9D15F2"));
                }
                catch (Exception ex)
                {
                    return false;
                }
                _instance = (IInteropClass)new CInteropClass();
                return (_instance != null) ? true : false;
            }
            return true;
        }

        /// <summary>
        /// Returns TRUE if application has root permissions
        /// </summary>
        /// <returns></returns>
        public static bool HasRootAccess()
        {
            if (_checkInstance() == false)
            {
                return false;
            }
            return Instance.HasRootAccess();
        }

        /// <summary>
        /// Checking if instance is up and running, creating new instance if it isn't.
        /// </summary>
        private static bool _checkInstance()
        {
            if (_instance == null)
            {
                if (Initialize() == false)
                {
                    throw new Exception("Cannot instantiate Interop Library");
                }
            }
            return true;
        }


        [ComImport, ClassInterface(ClassInterfaceType.None), Guid("070E61BC-473F-4215-8A31-02206D9D15F2")]
        public class CInteropClass
        {
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            public int dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public int nFileSizeHigh;
            public int nFileSizeLow;
            public int dwOID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }


        public class FileEntry
        {
            private string _fileName = "";
            private bool _isDirectory = false;

            public string FileName
            {
                get
                {
                    if (_fileName == null)
                        return "";
                    return _fileName;
                }
                set
                {
                    _fileName = value;
                }
            }
            public bool IsDirectory
            {
                get
                {
                    return _isDirectory;
                }
                set
                {
                    _isDirectory = value;
                }
            }
        }

        public class FileEntryComparer : IComparer<FileEntry>
        {

            public int Compare(FileEntry x, FileEntry y)
            {
                if (x.IsDirectory == true && y.IsDirectory == false)
                    return -1;
                else if (x.IsDirectory == false && y.IsDirectory == true)
                    return 1;
                else
                {
                    return x.FileName.CompareTo(y.FileName);
                }
            }
        }

        private static Object getContentLock = new Object();

        /// <summary>
        /// Returns folder content.
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static List<FileEntry> GetContent(string folder)
        {
            lock (getContentLock)
            {
                if (_instance == null)
                {
                    return null;
                }
                var list = new List<FileEntry>();

                string n = _instance.GetFolder(folder);
                if (n != null)
                {
                    if (n.Length > 0)
                    {
                        string[] fnames = n.Split('|');
                        foreach (var fname in fnames)
                        {
                            var fe = new FileEntry();
                            fe.FileName = fname.Substring(0, fname.IndexOf(":"));
                            fe.IsDirectory = (fname.Substring(fname.IndexOf(":") + 1) == "dir") ? true : false;
                            list.Add(fe);
                        }
                    }
                }
                list.Sort(new FileEntryComparer());
                return list;
            }
        }

        [ComImport, Guid("3B57C022-D53F-4CCA-8DAD-3E12235BE8AE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IInteropClass
        {
            void Align();

            [return: MarshalAs(UnmanagedType.Bool)]
            bool HasRootAccess();

            [return: MarshalAs(UnmanagedType.Bool)]
	        bool CreateProcess([MarshalAs(UnmanagedType.BStr)] string path, [MarshalAs(UnmanagedType.BStr)] string arguments, [MarshalAs(UnmanagedType.BStr)] string accountName, out uint handle);
	        
            void WaitForSingleObject7(uint handle, uint timeout);
	        void CloseHandle7(uint handle);
	        uint CreateFile7([MarshalAs(UnmanagedType.BStr)] string fileName, uint dwDesiredAccess, uint dwShareMode, 
			        uint lpSecurityAttributes /*null*/, uint dwCreationDisposition, 
			        uint dwFlagsAndAttributes, uint hTemplateFile);

            [return: MarshalAs(UnmanagedType.Bool)]
	        bool CreateDirectory7([MarshalAs(UnmanagedType.BStr)] string src);

            [return: MarshalAs(UnmanagedType.Bool)]
	        bool DeleteDirectory7([MarshalAs(UnmanagedType.BStr)] string src);
	        
            [return: MarshalAs(UnmanagedType.Bool)]
            bool CopyFile7([MarshalAs(UnmanagedType.BStr)] string src, [MarshalAs(UnmanagedType.BStr)] string dest, [MarshalAs(UnmanagedType.Bool)]bool failIfExists);
	        
            [return: MarshalAs(UnmanagedType.Bool)]
            bool DeleteFile7([MarshalAs(UnmanagedType.BStr)] string src);

            [return: MarshalAs(UnmanagedType.U4)]
	        uint GetFileAttributes7([MarshalAs(UnmanagedType.BStr)] string path);

	        void SetFileAttributes7([MarshalAs(UnmanagedType.BStr)] string path, uint attributes);

	        [return: MarshalAs(UnmanagedType.Bool)]
            bool DeviceIoControl7(uint hDevice, uint dwIoControlCode, 
											 uint lpInBuf, uint nInBufSize, 
											 uint lpOutBuf, uint nOutBufSize, 
											 uint lpBytesReturned, uint lpOverlapped);
            [return: MarshalAs(UnmanagedType.BStr)]
            string GetFolder([MarshalAs(UnmanagedType.BStr)] string path); 
            [return: MarshalAs(UnmanagedType.U4)]
            uint FindFirstFile7([MarshalAs(UnmanagedType.BStr)] string dir, out WIN32_FIND_DATA data);
	        [return: MarshalAs(UnmanagedType.Bool)]
            bool FindNextFile7(uint handle, out WIN32_FIND_DATA data);
	        void FindClose7(uint handle);
	        
            [return: MarshalAs(UnmanagedType.U4)]
            uint FindWindow7([MarshalAs(UnmanagedType.BStr)] string className, [MarshalAs(UnmanagedType.BStr)] string windowName);
	        
            [return: MarshalAs(UnmanagedType.Bool)]
            bool DestroyWindow7(uint hWnd);

	        void RegistryGetDWORD7(uint hKey,
                                    [MarshalAs(UnmanagedType.BStr)]string pszSubKey,
                                    [MarshalAs(UnmanagedType.BStr)]string pszValueName,
                                    out int pdwData);
            void RegistryGetString7(uint hKey,
                                     [MarshalAs(UnmanagedType.BStr)]string pszSubKey,
                                     [MarshalAs(UnmanagedType.BStr)]string pszValueName,
                                     System.Text.StringBuilder pszData,
                                     int cchData);
            void RegistrySetDWORD7(uint hKey,
                            [MarshalAs(UnmanagedType.BStr)]string pszSubKey,
                            [MarshalAs(UnmanagedType.BStr)]string pszValueName,
                            int dwData);
            void RegistrySetString7(uint hKey,
                                     [MarshalAs(UnmanagedType.BStr)]string pszSubKey,
                                     [MarshalAs(UnmanagedType.BStr)]string pszValueName,
                                     [MarshalAs(UnmanagedType.BStr)]string pszData);
	        [return: MarshalAs(UnmanagedType.Bool)]
            bool IsProcessRunning([MarshalAs(UnmanagedType.BStr)] string processName);
            [return: MarshalAs(UnmanagedType.Bool)]
	        bool TerminateProcess7([MarshalAs(UnmanagedType.BStr)] string processName);
            
            [return: MarshalAs(UnmanagedType.U4)]
	        uint AllocMem(uint amount);
	        void FreeMem(uint mem);

	        uint GetBatterySavingsMode();
	        void SetBatterySavingsMode(uint mode);
            [return: MarshalAs(UnmanagedType.U4)]
	        uint GetDataEnabled();
	        void SetDataEnabled(uint mode);
	        void GetRadioStates(out uint bWifi, out uint bPhone, out uint bBT);
	        void SetRadioState(uint dwDevice, uint dwState, uint sync);

            void ApplySkSettings();

            void Unk1();
            void Unk2();
            void Unk3();
            void Unk4();

            [return: MarshalAs(UnmanagedType.U4)]
            uint GetPower([MarshalAs(UnmanagedType.BStr)]string device, uint flags);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool SetPower([MarshalAs(UnmanagedType.BStr)]string device, uint flags, uint state);

            uint File_CreateFile([MarshalAs(UnmanagedType.BStr)] string processName, uint dwDesiredAccess, uint dwShareMode, uint dwCreationDisposition);
            uint File_GetSize(uint hFile);
            void File_SetFilePointer(uint hFile, uint offset, uint dwMoveMethod);
            uint File_ReadFile(uint hFile, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] array, uint bytesToRead);
            void File_Close(uint hFile);

            uint ZMediaLibrary_GetSongs();
            uint ZMediaList_GetHash(uint hMediaList, int index);
            void ZMediaList_Release(uint hMediaList);
            bool PlaySoundFile(string fileName, uint flags);
            void AddRingtoneFile(string fileName, string name, int isProtected, int setAsRingtone);
            void AddMusicFile(
                 [MarshalAs(UnmanagedType.BStr)] string pwszPathToFile,
                 int iTrackNumber,
                 [MarshalAs(UnmanagedType.BStr)] string pwszTrackTitle,
                 int iTrackDurationMSec,
                 [MarshalAs(UnmanagedType.BStr)] string pwszTrackArtistName,
                 [MarshalAs(UnmanagedType.BStr)] string pwszTrackGenreTitle,
                 [MarshalAs(UnmanagedType.BStr)] string pwszAlbumTitle,
                 [MarshalAs(UnmanagedType.BStr)] string pwszAlbumArtistName,
                 [MarshalAs(UnmanagedType.BStr)] string pwszAlbumReleaseDateISO8601,
                 [MarshalAs(UnmanagedType.BStr)] string pwszPathToAlbumCoverArtImage,
                 [MarshalAs(UnmanagedType.BStr)] string pwszPathToTrackArtistBackgroundImage,
                 [MarshalAs(UnmanagedType.BStr)] string pwszPathToAlbumArtistBackgroundImage
                );
           void RemoveAllDummyMusicFiles();
           void HideAllDummyMusicFiles();
           void FlushMediaDatabase();
           [return: MarshalAs(UnmanagedType.Bool)]
           bool MoveFile7([MarshalAs(UnmanagedType.BStr)] string oldFileName, [MarshalAs(UnmanagedType.BStr)] string newFileName);
           void DeleteRegVal(uint hKey, [MarshalAs(UnmanagedType.BStr)] string key, [MarshalAs(UnmanagedType.BStr)] string value);
           void ReloadMarketplaceConfigs();
           void EnableUiOrientationChange([MarshalAs(UnmanagedType.Bool)] bool mode);
        }

    }
}
