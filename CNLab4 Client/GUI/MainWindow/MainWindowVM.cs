using System;
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
        }

        private async void StartListenAsync()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, General.PeerPort);
            listener.Start();
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                await OnClientAcceptedAsync(client);
            }
        }

        private async Task OnClientAcceptedAsync(TcpClient client)
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();

                BasePeerRequest request = await stream.ReadMessageAsync<BasePeerRequest>();
                if (request is GetBlocks getBlocks)
                    await OnRequest(getBlocks, stream);
            }
        }

        private async Task OnRequest(GetBlocks request, NetworkStream stream)
        {
            if (!TryFindTorrent(request.AccessCode, out TorrentVM torrent))
                return;

            int maxBlocksCount = 10;
            int hasSent = 0;

            foreach (var blocks in request.BlocksNeed)
            {
                int fIndex = blocks.FileIndex;
                TorrentFileVM file = torrent.Files[fIndex];
                foreach (int bIndex in blocks.BlocksIndices)
                {
                    if (file.IsDone(bIndex))
                    {
                        byte[] data = null;
                        await Task.Run(() =>
                        {
                            data = file.Read(bIndex);
                            stream.Write(new BlockResponse
                            {
                                Block = new Block
                                {
                                    FileIndex = fIndex,
                                    BlockIndex = bIndex
                                }
                            });
                            stream.Write(data);
                        });
                        torrent.AddBytesSent(data.Length);
                        if (++hasSent >= maxBlocksCount)
                            break;
                    }
                }
                if (hasSent >= maxBlocksCount)
                    break;
            }

            await stream.WriteAsync(new EndResponse());
            General.Log(new string[]
            {
                $"Has sent {hasSent} blocks",
                $"\tTorrent name: {torrent.Name}"
            });
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
            if (dialog.ShowDialog() != true)
                return;

            string accessCode = dialog.AccessCode;
            string directory = dialog.Directory;

            try
            {
                TorrentInfo torrentInfo = await ServerProtocol.GetTorrentInfoAsync(accessCode);
                Torrents.Add(new TorrentVM(directory, torrentInfo));
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

            try
            {
                TorrentInfo torrentInfo = await ServerProtocol.RegisterTorrentDirAsync(torrentDir);
                string baseDir = new DirectoryInfo(torrentDir).Parent.FullName;
                Torrents.Add(new TorrentVM(baseDir, torrentInfo, true));
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
