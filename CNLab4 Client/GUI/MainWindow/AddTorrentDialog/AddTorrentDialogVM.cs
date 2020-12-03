using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CNLab4_Client.GUI
{
    class AddTorrentDialogVM : BaseViewModel
    {
        private Window _owner;

        #region Bindings

        private string _accessCode;
        public string AccessCode
        {
            get => _accessCode;
            set
            {
                _accessCode = value;
                NotifyPropChanged(nameof(AccessCode));
            }
        }

        private string _directory;
        public string Directory
        {
            get => _directory;
            set
            {
                _directory = value;
                NotifyPropChanged(nameof(Directory));
            }
        }

        private RelayCommand _changeDirectoryCmd;
        public RelayCommand ChangeDirectoryCmd
            => _changeDirectoryCmd ?? (_changeDirectoryCmd = new RelayCommand(_ => ChangeDirectory()));

        #endregion

        public AddTorrentDialogVM(Window owner)
        {
            _owner = owner;
        }

        private void ChangeDirectory()
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.EnsureFileExists = true;
                if (dialog.ShowDialog(_owner) == CommonFileDialogResult.Ok)
                {
                    Directory = dialog.FileName;
                }
            }
        }
    }
}
