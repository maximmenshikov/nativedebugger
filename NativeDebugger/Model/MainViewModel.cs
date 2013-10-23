using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using InteropSvc;
using System.Collections.Generic;

namespace WmdcLauncher
{
    public class MainViewModel : BaseViewModel
    {
        public enum WmdcState
        {
            NotRunning = 0,
            RunInProgress = 1,
            Running = 2,
            Failed = 3
        }

        public enum DbgState
        {
            NotRunning = 0,
            ChangeStateInProgress = 1,
            Running = 2
        }

        public bool GetState()
        {
            if (InteropSvc.InteropLib.Instance.IsProcessRunning("rapiclnt.exe") ||
                InteropSvc.InteropLib.Instance.IsProcessRunning("rapiworker.exe") ||
                InteropSvc.InteropLib.Instance.IsProcessRunning("ConManClient2.exe"))
                return true;
            return false;
        }

        public bool GetDebuggerState()
        {
            if (InteropSvc.InteropLib.Instance.IsProcessRunning("edm2.exe") ||
                InteropSvc.InteropLib.Instance.IsProcessRunning("edm3.exe"))
                return true;
            return false;
        }

        public void RefreshState()
        {
            if (GetState())
                State = WmdcState.Running;
            else
                State = WmdcState.NotRunning;
        }

        public Visibility StateDirectVisibility
        {
            get
            {
                return (State == WmdcState.Running) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility StateReverseVisibility
        {
            get
            {
                return (State == WmdcState.Failed || State == WmdcState.NotRunning) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void RefreshDebuggerState()
        {
            if (GetDebuggerState())
                DebuggerState = DbgState.Running;
            else
                DebuggerState = DbgState.NotRunning;
        }

        private WmdcState _state = WmdcState.NotRunning;
        public WmdcState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                OnChange("State");
                OnChange("StateString");
                OnChange("StateDirectVisibility");
                OnChange("StateReverseVisibility");
            }
        }

        public string StateString
        {
            get
            {
                switch (State)
                {
                    case WmdcState.NotRunning:
                        return LocalizedResources.StateDisabled;
                    case WmdcState.Running:
                        return LocalizedResources.StateEnabled;
                    case WmdcState.RunInProgress:
                        return LocalizedResources.StateEnabling;
                    case WmdcState.Failed:
                        return LocalizedResources.StateFailed;
                }
                return "";
            }
        }

        private DbgState _dbgState = DbgState.NotRunning;
        public DbgState DebuggerState
        {
            get
            {
                return _dbgState;
            }
            set
            {
                _dbgState = value;
                OnChange("DebuggerState");
                OnChange("DebuggerStateString");
                OnChange("DebuggerStateDirectVisibility");
                OnChange("DebuggerStateReverseVisibility");
            }
        }

        public string DebuggerStateString
        {
            get
            {
                switch (DebuggerState)
                {
                    case DbgState.ChangeStateInProgress:
                        return LocalizedResources.StateDebuggerDisabling;
                    case DbgState.NotRunning:
                        return LocalizedResources.StateDebuggerDisabled;
                    case DbgState.Running:
                        return LocalizedResources.StateDebuggerEnabled;
                }
                return "";
            }
        }

        public Visibility DebuggerStateDirectVisibility
        {
            get
            {
                return (DebuggerState == DbgState.Running) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility DebuggerStateReverseVisibility
        {
            get
            {
                return (DebuggerState == DbgState.NotRunning || DebuggerState == DbgState.ChangeStateInProgress) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public enum StateChangedType
        {
            Logging = 0,
            IndicatorShow = 1,
        }
        public class StateChangedEventArgs : EventArgs
        {
            public StateChangedType Type;
            public string Text = null;
            public bool ShowIndicator = false;
        }

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public void AddToLog(string text)
        {
            Deployment.Current.Dispatcher.BeginInvoke(delegate()
            {
                var e = new StateChangedEventArgs();
                e.Text = text;
                e.Type = StateChangedType.Logging;
                StateChanged(this, e);
            }
            );
        }

        public void ShowIndicator(bool show)
        {
            Deployment.Current.Dispatcher.BeginInvoke(delegate()
            {
                var e = new StateChangedEventArgs();
                e.ShowIndicator = show;
                e.Type = StateChangedType.IndicatorShow;
                StateChanged(this, e);
            }
            );
        }


        public void SetState(WmdcState state)
        {
            Deployment.Current.Dispatcher.BeginInvoke(delegate()
            {
                State = state;
            }
            );
        }

        public void SetDebuggerState(DbgState state)
        {
            Deployment.Current.Dispatcher.BeginInvoke(delegate()
            {
                DebuggerState = state;
            }
            );
        }

        #region "File functions"

        private class EnumFile
        {
            public string FileName;
            public bool isFolder;
        }

        EnumFile[] EnumerateFiles(string folder)
        {
            InteropLib.WIN32_FIND_DATA data;
            uint handle = InteropLib.Instance.FindFirstFile7(folder + "\\*", out data);
            var list = new List<EnumFile>();

            if (handle != 0xFFFFFFFFU)
            {
                bool result = false;
                do
                {
                    if (data.cFileName != "." && data.cFileName != "..")
                    {
                        EnumFile ef = new EnumFile();
                        ef.FileName = data.cFileName;
                        bool t = ((data.dwFileAttributes & 0x10) == 0x10) ? true : false;
                        ef.isFolder = t ? true : false;
                        list.Add(ef);
                    }
                    result = InteropLib.Instance.FindNextFile7(handle, out data);
                } while (result != false);
                InteropLib.Instance.FindClose7(handle);
            }
            return list.ToArray();
        }

        void CopyDirectory(string src, string dest)
        {
            EnumFile[] files = EnumerateFiles(src);
            InteropLib.Instance.CreateDirectory7(dest);
            if (files.Length > 0)
            {
                foreach (EnumFile ef in files)
                {
                    if (ef.isFolder == true)
                    {
                        CopyDirectory(src + "\\" + ef.FileName, dest + "\\" + ef.FileName);
                    }
                    else
                    {
                        InteropLib.Instance.DeleteFile7(dest + "\\" + ef.FileName);
                        InteropLib.Instance.CopyFile7(src + "\\" + ef.FileName, dest + "\\" + ef.FileName, false);
                    }
                }
            }
        }

        #endregion

        private Object RunThreadLock = new Object();
        private void RunThread()
        {
            lock (RunThreadLock)
            {
                if (InteropLib.Instance == null)
                {
                    SetState(WmdcState.Failed);
                    return;
                }
                AddToLog("<NO>");
                SetState(WmdcState.RunInProgress);
                ShowIndicator(true);
                InteropLib.Instance.CreateDirectory7("\\Windows\\CoreCon1.1");
                AddToLog(LocalizedResources.TerminatingProcesses + " ");

                uint handle;
                InteropLib.Instance.CreateProcess("\\Windows\\ClientShutdown2.exe", "", "standard", out handle);
                InteropLib.Instance.WaitForSingleObject7(handle, 5000);
                InteropLib.Instance.CloseHandle7(handle);

                while (InteropLib.Instance.TerminateProcess7("rapiclnt.exe")) ;
                while (InteropLib.Instance.TerminateProcess7("rapiworker.exe")) ;
                System.Threading.Thread.Sleep(3000);
                AddToLog(LocalizedResources.Ok + ".\n" + LocalizedResources.CopyingFiles + " ");
                CopyDirectory("\\Applications\\Install\\1bb49e6d-518f-4a10-9a34-7a63041282a9\\Install\\WindowsFolder", "\\Windows");
                CopyDirectory("\\Applications\\Install\\1bb49e6d-518f-4a10-9a34-7a63041282a9\\Install\\WindowsFolder", "\\Windows\\CoreCon1.1");
                AddToLog(LocalizedResources.Ok + ".\n" + LocalizedResources.ApplyingSecuritySettings + " ");

                int configured = 0;
                InteropLib.Instance.RegistryGetDWORD7(InteropLib.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\ActiveSync", "DeviceConfigured", out configured);
                if (configured == 0)
                {
                    InteropLib.Instance.RegistrySetString7(InteropLib.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\ActiveSync", "DeviceManufacturer", "Microsoft");
                    InteropLib.Instance.RegistrySetString7(InteropLib.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\ActiveSync", "DeviceModel", "Windows Phone");
                    InteropLib.Instance.RegistrySetString7(InteropLib.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\ActiveSync", "DeviceIcon", "\\Windows\\sync_generic.ico");
                    InteropLib.Instance.RegistrySetDWORD7(InteropLib.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\ActiveSync", "DeviceConfigured", 1);
                }
                InteropLib.Instance.RegistrySetString7(InteropLib.HKEY_LOCAL_MACHINE, "System\\OOM\\DoNotKillApps", "\\windows\\edm2p.exe", "");
                InteropLib.Instance.RegistrySetString7(InteropLib.HKEY_LOCAL_MACHINE, "System\\OOM\\DoNotKillApps", "\\windows\\ConManClient2.exe", "");
                InteropLib.Instance.RegistrySetString7(InteropLib.HKEY_LOCAL_MACHINE, "System\\OOM\\DoNotKillApps", "\\windows\\CMAccept.exe", "");
                InteropLib.Instance.RegistrySetString7(InteropLib.HKEY_LOCAL_MACHINE, "System\\OOM\\DoNotKillApps", "\\windows\\rapiclnt.exe", "");
                InteropLib.Instance.RegistrySetString7(InteropLib.HKEY_LOCAL_MACHINE, "System\\OOM\\DoNotKillApps", "\\windows\\rapiworker.exe", "");
                InteropLib.Instance.RegistrySetDWORD7(InteropLib.HKEY_LOCAL_MACHINE, "System", "CoreConOverrideSecurity", 1);

                InteropLib.Instance.RegistrySetDWORD7(InteropLib.HKEY_LOCAL_MACHINE, "Security\\Policies\\Policies", "00001001", 1);

                AddToLog(LocalizedResources.Ok + "\n" + LocalizedResources.StartingConnectionManager + " ");
                InteropLib.Instance.CreateProcess("\\Windows\\ConManClient2.exe", "", "standard", out handle);
                if (handle == 0)
                {
                    AddToLog("\n" + LocalizedResources.StartingConnectionManagerProblem + " \n");
                    ShowIndicator(false);
                    SetState(WmdcState.Failed);
                    return;
                }
                InteropLib.Instance.CloseHandle7(handle);

                AddToLog(LocalizedResources.Ok + "\n" + LocalizedResources.StartingRapiWorker + " ");
                InteropLib.Instance.CreateProcess("\\Windows\\rapiworker.exe", "-now", "standard", out handle);

                if (handle == 0)
                {
                    AddToLog("\n" + LocalizedResources.StartingRapiWorkerProblem + "\n");
                    ShowIndicator(false);
                    SetState(WmdcState.Failed);
                    return;
                }
                InteropLib.Instance.CloseHandle7(handle);
                AddToLog(LocalizedResources.Ok + ".\n" + LocalizedResources.WaitingForIpResolving + ".");
                for (int x = 0; x < 2; x++)
                {
                    System.Threading.Thread.Sleep(1000);
                    AddToLog(".");
                }
                System.Threading.Thread.Sleep(1000);
                AddToLog(" " + LocalizedResources.Ok + ".\n");
                try
                {
                    System.Text.StringBuilder str = new System.Text.StringBuilder(1000);
                    InteropLib.Instance.RegistryGetString7(InteropLib.HKEY_CURRENT_USER, "Software\\Microsoft\\CoreCon", "IPAddress", str, 1000);
                    string[] ips = str.ToString().Split(';');
                    AddToLog(LocalizedResources.CoreConIPs + " \n");
                    for (int x = 0; x < ips.Length; x++)
                    {
                        AddToLog("   " + ips[x] + "\n");
                    }
                }
                catch (Exception ex)
                {
                    AddToLog(LocalizedResources.CoreConIPs + ": \n   " + LocalizedResources.NoIPs + "\n");
                }
                ShowIndicator(false);
                SetState(WmdcState.Running);
            }
        }

        public void Run()
        {
            System.Threading.Thread thread = new System.Threading.Thread(RunThread);
            thread.Start();
        }

        private Object DisableDebuggersLock = new Object();
        private void DisableDebuggersThread()
        {
            lock (DisableDebuggersLock)
            {
                ShowIndicator(true);
                
                uint handle;
                SetDebuggerState(DbgState.ChangeStateInProgress);
                InteropLib.Instance.CreateProcess("\\Application Data\\Phone Tools\\10.0\\CoreCon\\bin\\ClientShutdown3.exe", "", "standard", out handle);
                InteropLib.Instance.WaitForSingleObject7(handle, 5000);
                InteropLib.Instance.CloseHandle7(handle);

                InteropLib.Instance.CreateProcess("\\Windows\\ClientShutdown2.exe", "", "standard", out handle);
                InteropLib.Instance.WaitForSingleObject7(handle, 5000);
                InteropLib.Instance.CloseHandle7(handle);

                for (int i = 0; i < 5; i++)
                    InteropLib.Instance.TerminateProcess7("edm2.exe") ;
                for (int i = 0; i < 5; i++)
                    InteropLib.Instance.TerminateProcess7("edm3.exe") ;
                for (int i = 0; i < 5; i++)
                    InteropLib.Instance.TerminateProcess7("rapiclnt.exe") ;
                for (int i = 0; i < 5; i++)
                    InteropLib.Instance.TerminateProcess7("rapiworker.exe") ;
                for (int i = 0; i < 5; i++)
                    InteropLib.Instance.TerminateProcess7("ConManClient2.exe") ;
                for (int i = 0; i < 5; i++) 
                    InteropLib.Instance.TerminateProcess7("ConManClient3.exe");
                for (int i = 0; i < 5; i++)
                    InteropLib.Instance.TerminateProcess7("CMAccept3.exe");
                for (int i = 0; i < 5; i++)
                    InteropLib.Instance.TerminateProcess7("CMAccept2.exe");

                Deployment.Current.Dispatcher.BeginInvoke(delegate()
                {
                    DebuggerState = DbgState.NotRunning;
                    State = WmdcState.NotRunning;
                });
                SetDebuggerState(DbgState.NotRunning);
                ShowIndicator(false);
            }
        }

        public void DisableDebuggers()
        {
            System.Threading.Thread thread = new System.Threading.Thread(DisableDebuggersThread);
            thread.Start();
        }

        #region "Checkboxes"

        public bool RunRapi
        {
            get
            {
                int val = 1;
                InteropLib.Instance.RegistryGetDWORD7(InteropLib.HKEY_LOCAL_MACHINE, "Software\\OEM\\RemoteAccess", "RunRapi", out val);
                return (val > 0) ? true : false;
            }
            set
            {
                InteropLib.Instance.RegistrySetDWORD7(InteropLib.HKEY_LOCAL_MACHINE, "Software\\OEM\\RemoteAccess", "RunRapi", value ? 1 : 0);
                OnChange("RunRapi");
            }
        }

        public bool RunDebugger
        {
            get
            {
                int val = 0;
                InteropLib.Instance.RegistryGetDWORD7(InteropLib.HKEY_LOCAL_MACHINE, "Software\\OEM\\RemoteAccess", "RunDebugger", out val);
                return (val > 0) ? true : false;
            }
            set
            {
                InteropLib.Instance.RegistrySetDWORD7(InteropLib.HKEY_LOCAL_MACHINE, "Software\\OEM\\RemoteAccess", "RunDebugger", value ? 1 : 0);
                OnChange("RunDebugger");
            }
        }

        public bool UsePcDebugger
        {
            get
            {
                int val = 0;
                InteropLib.Instance.RegistryGetDWORD7(InteropLib.HKEY_LOCAL_MACHINE, "Software\\OEM\\RemoteAccess", "UsePcDebugger", out val);
                return (val > 0) ? true : false;
            }
            set
            {
                InteropLib.Instance.RegistrySetDWORD7(InteropLib.HKEY_LOCAL_MACHINE, "Software\\OEM\\RemoteAccess", "UsePcDebugger", value ? 1 : 0);
                OnChange("UsePcDebugger");
            }
        }

        public string SystemIntegrationButtonText
        {
            get
            {
                if (SystemIntegrationEnabled)
                    return LocalizedResources.Disable;
                else
                    return LocalizedResources.Enable;
            }
        }
        public bool SystemIntegrationEnabled
        {
            get
            {
                var text = new System.Text.StringBuilder(500);
                try
                {

                    InteropLib.Instance.RegistryGetString7(InteropLib.HKEY_LOCAL_MACHINE, "Init", "Launch160", text, 500);
                    if (text.ToString().ToLower() == "rapiworker.exe")
                        return true;
                }
                catch (Exception ex)
                {
                }
                return false;
            }
            set
            {
                if (value == true)
                    InteropLib.Instance.RegistrySetString7(InteropLib.HKEY_LOCAL_MACHINE, "Init", "Launch160", "rapiworker.exe");
                else
                    InteropLib.Instance.RegistrySetString7(InteropLib.HKEY_LOCAL_MACHINE, "Init", "Launch160", "");
                OnChange("SystemIntegrationEnabled");
                OnChange("SystemIntegrationButtonText");
            }
        }
        #endregion
    }
}
