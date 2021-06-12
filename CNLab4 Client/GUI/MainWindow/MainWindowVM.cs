using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CNLab4;
using CNLab4.Messages;
using CNLab4.Messages.Peer;
using CNLab4.Messages.Peer.Requests;
using CNLab4.Messages.Peer.Responses;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json.Linq;


namespace CNLab4_Client.GUI
{
    public class MainWindowVM : BaseViewModel
    {
        private Window _owner;

        #region Bindings

        public string PeerAddressStrRepr => General.PeerAddress.ToString();

        public string ServerAddressStrRepr => General.ServerAddress.ToString();

        private RelayCommand _addTorrentCmd;
        public RelayCommand AddTorrentCmd
            => _addTorrentCmd ?? (_addTorrentCmd = new RelayCommand(_ => AddTorrent()));

        private RelayCommand _registerTorrentDirCmd;
        public RelayCommand RegisterTorrentDirCmd
            => _registerTorrentDirCmd ?? (_registerTorrentDirCmd = new RelayCommand(_ => RegisterTorrentDir()));

        private RelayCommand _registerTorrentFileCmd;
        public RelayCommand RegisterTorrentFileCmd
            => _registerTorrentFileCmd ?? (_registerTorrentFileCmd = new RelayCommand(_ => RegisterTorrentFile()));

        private ObservableCollection<TorrentVM> _torrents;
        public ObservableCollection<TorrentVM> Torrents
            => _torrents ?? (_torrents = new ObservableCollection<TorrentVM>());

        private TorrentVM _selectedTorrent;
        public TorrentVM SelectedTorrent
        {
            get => _selectedTorrent;
            set
            {
                _selectedTorrent = value;
                NotifyPropChanged(nameof(SelectedTorrent));
            }
        }

        #endregion

        public MainWindowVM(Window owner)
        {
            _owner = owner;
            StartListenAsync();
            UpdateSpeedsLoopAsync();

#if TASKRUN
            General.Log("TASKRUN defined");
#else
            General.Log("TASKRUN not defined");
#endif

#if DEBUG
            MakeTestAsync();
#endif
        }
#if DEBUG
        private async void MakeTestAsync()
        {
            await Task.Delay(1000);
            if (General.PeerPort == 59001)
            {
                await RegisterTorrentDir(@"D:\Downloads\_UnplayedOsuMaps");
            }
            else
            {
                Func<string, bool> validator = (value) => value.Length > 0;
                var dialog = new InputDialog
                {
                    TitleText = "Access code:",
                    InputValidator = validator,
                    Owner = _owner
                };
                if (dialog.ShowDialog() == true)
                {
                    string dir;
                    switch (General.PeerPort)
                    {
                        case 59002:
                            dir = @"D:\Downloads\__2";
                            break;
                        case 59003:
                            dir = @"D:\Downloads\__3";
                            break;
                        default:
                            dir = @"D:\Downloads\__4";
                            break;
                    }
                    await AddTorrent(dialog.InputText, dir);
                }
            }
        }
#endif
        private async void UpdateSpeedsLoopAsync()
        {
            while (true)
            {
                foreach (var torrent in Torrents)
                    torrent.UpdateSpeeds();
                await Task.Delay(1_000);
            }
        }

        private async void StartListenAsync()
        {
            int maxClientConnections = 10;
            int connectedCount = 0;

            TcpListener listener = new TcpListener(IPAddress.Any, General.PeerPort);
            listener.Start();
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                if (connectedCount >= maxClientConnections)
                {
                    client.Close();
                    continue;
                }
                else
                {
                    connectedCount++;
                    OnClientAcceptedAsync(client).ContinueWith(task => connectedCount--);
                }
            }
        }

        private async Task OnClientAcceptedAsync(TcpClient client)
        {
            try
            {
                using (client)
                {
                    NetworkStream stream = client.GetStream();
                    //stream.ReadTimeout = 1000;// TODO delete
                    //stream.WriteTimeout = 1000;// TODO delete

                    while (true)
                    {
#if TASKRUN
                        BasePeerRequest request = await stream.ReadMessageTask<BasePeerRequest>();
#else
                        BasePeerRequest request = await stream.ReadMessageAsync<BasePeerRequest>();
#endif
                        if (request is GetDoneMasks getDoneMasks)
                            await OnRequestAsync(getDoneMasks, stream);
                        if (request is GetBlock getBlock)
                            await OnRequestAsync(getBlock, stream);
                    }
                }
            }
            catch (EndOfStreamException)
            {
                General.Log($"Client disconnected because {nameof(EndOfStreamException)} was thrown.");
            }
            catch (Exception e)
            {
                General.Log("TcpClient exception",
                    $"\tMessage: {e.Message}");
            }
        }

        private async Task OnRequestAsync(GetDoneMasks request, NetworkStream stream)
        {
            if (TryFindTorrent(request.AccessCode, out TorrentVM torrent))
            {
                BitArray[] doneMasks = torrent.GetDoneMasks();
#if TASKRUN
                await stream.WriteTask(new DoneMasksResponse { DoneMasks = doneMasks });
#else
                await stream.WriteAsync(new DoneMasksResponse { DoneMasks = doneMasks });
#endif
            }
            else
            {
#if TASKRUN
                await stream.WriteTask(new Error { Text = "Wrong access key." });
#else
                await stream.WriteAsync(new Error { Text = "Wrong access key." });
#endif
            }
        }

        private async Task OnRequestAsync(GetBlock request, NetworkStream stream)
        {
            if (IsValid(request, out TorrentVM torrent, out TorrentFileVM file))
            {
                byte[] data = await file.TryReadAsync(request.BlockIndex);
                if (data is object)
                {
#if TASKRUN
                    await stream.WriteTask(new BlockSentResponse());
                    await stream.WriteTask(data);
#else
                    await stream.WriteAsync(new BlockSentResponse());
                    await stream.WriteAsync(data);
#endif
                    torrent.OnBlockSent(data.Length);
                }
                else
                {
#if TASKRUN
                    await stream.WriteTask(new Error { Text = "Block unavailable." });
#else
                    await stream.WriteAsync(new Error { Text = "Block unavailable." });
#endif
                }
            }
            else
            {
#if TASKRUN
                await stream.WriteTask(new Error { Text = "Wrong access key." });
#else
                await stream.WriteAsync(new Error { Text = "Wrong access key." });
#endif
            }
        }

        private bool IsValid(GetBlock request, out TorrentVM torrent, out TorrentFileVM file)
        {
            if (TryFindTorrent(request.AccessCode, out torrent))
            {
                if (request.FileIndex >= 0 && request.FileIndex < torrent.Files.Count)
                {
                    file = torrent.Files[request.FileIndex];
                    if (request.BlockIndex >= 0 && request.BlockIndex < file.BlocksCount)
                        return true;
                }
            }

            file = null;
            return false;
        }

        private bool TryFindTorrent(string accessCode, out TorrentVM result)
        {
            foreach (TorrentVM viewModel in _torrents)
            {
                if (viewModel.AccessCode == accessCode)
                {
                    result = viewModel;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private async void AddTorrent()
        {
            var dialog = new AddTorrentDialog();
            dialog.Owner = _owner;
            if (dialog.ShowDialog() != true)
                return;

            string accessCode = dialog.AccessCode;
            string directory = dialog.Directory;

            await AddTorrent(accessCode, directory);
        }

        private async Task AddTorrent(string accessCode, string directory)
        {
            if (TryFindTorrent(accessCode, out var _))
            {
                MessageBox.Show($"Torrent with this access code alreay added.");
                return;
            }

            try
            {
                TorrentInfo torrentInfo = await ServerProtocol.GetTorrentInfoAsync(accessCode);
                var torrent = new TorrentVM(directory, torrentInfo);
                Torrents.Add(torrent);
                if (Torrents.Count == 1)
                    SelectedTorrent = torrent;

            }
            catch (ErrorResponseException e)
            {
                General.Log(new string[]
                {
                    $"Server error response on retrieve torrent info",
                    $"\tAccess code: {accessCode}",
                    $"\tMessage: {e.Message}"
                });
            }
            catch (UnknownResponseException)
            {
                General.Log(new string[]
                {
                    "Server unknown response on retrieve torrent info",
                    $"\tAccess code: {accessCode}"
                });
            }
            catch
            {
                General.Log(new string[]
                {
                    $"Unknown exception on retrieve torrent info",
                    $"\tAccess code: {accessCode}"
                });
            }
        }

        private async void RegisterTorrentDir()
        {
            if (!TrySelectDirectory(out string torrentDir))
                return;

            await RegisterTorrentDir(torrentDir);
        }

        private async Task RegisterTorrentDir(string torrentDir)
        {
            try
            {
                TorrentInfo torrentInfo = await ServerProtocol.RegisterTorrentDirAsync(torrentDir);
                string baseDir = new DirectoryInfo(torrentDir).Parent.FullName;
                var torrent = new TorrentVM(baseDir, torrentInfo, true);
                Torrents.Add(torrent);
                if (Torrents.Count == 1)
                    SelectedTorrent = torrent;
                General.Log(new string[]
                {
                    "Torrent registered successfully",
                    $"\tTorrent directory: {torrentDir}"
                });
            }
            catch (ErrorResponseException e)
            {
                General.Log(new string[]
                {
                    $"Server error response on register torrent directory",
                    $"\tTorrent directory: {torrentDir}",
                    $"\tMessage: {e.Message}"
                });
            }
            catch (UnknownResponseException)
            {
                General.Log(new string[]
                {
                    "Server unknown response on register torrent directory",
                    $"\tTorrent directory: {torrentDir}"
                });
            }
            catch
            {
                General.Log(new string[]
                {
                    $"Unknown exception on register torrent directory",
                    $"\tTorrent directory: {torrentDir}"
                });
            }
        }

        private async void RegisterTorrentFile()
        {
            if (!TrySelectFile(out string filePath))
                return;

            try
            {
                TorrentInfo torrentInfo = await ServerProtocol.RegisterTorrentFileAsync(filePath);
                string directory = new FileInfo(filePath).DirectoryName;
                Torrents.Add(new TorrentVM(directory, torrentInfo, true));
                General.Log(new string[]
                {
                    "Torrent registered successfully",
                    $"\tFile path: {filePath}"
                });
            }
            catch (ErrorResponseException e)
            {
                General.Log(new string[]
                {
                    $"Server error response on register torrent file",
                    $"\tFile path: {filePath}",
                    $"\tMessage: {e.Message}"
                });
            }
            catch (UnknownResponseException)
            {
                General.Log(new string[]
                {
                    "Server unknown response on register torrent file",
                    $"\tFile path: {filePath}"
                });
            }
            catch
            {
                General.Log(new string[]
                {
                    $"Unknown exception on register torrent file",
                    $"\tFile path: {filePath}"
                });
                General.Log();
                General.Log();
            }
        }
        
        private bool TrySelectDirectory(out string dirPath)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.EnsureFileExists = true;
                if (dialog.ShowDialog(_owner) == CommonFileDialogResult.Ok)
                {
                    dirPath = dialog.FileName;
                    return true;
                }
                else
                {
                    dirPath = "";
                    return false;
                }
            }
        }

        private bool TrySelectFile(out string filePath)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.EnsureFileExists = true;
                if (dialog.ShowDialog(_owner) == CommonFileDialogResult.Ok)
                {
                    filePath = dialog.FileName;
                    return true;
                }
                else
                {
                    filePath = "";
                    return false;
                }
            }
        }

    }
}
