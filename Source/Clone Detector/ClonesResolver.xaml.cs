using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CloneDetector
{
    /// <summary>
    /// Interaction logic for ClonesResolver.xaml
    /// </summary>
    public partial class ClonesResolver : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="IsResolved"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsResolvedProperty =
            DependencyProperty.Register("IsResolved",
                typeof(bool),
                typeof(ClonesResolver),
                new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="CloneCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloneCountProperty =
            DependencyProperty.Register("CloneCount",
                typeof(int),
                typeof(ClonesResolver),
                new PropertyMetadata(0));

        /// <summary>
        /// Identifies the <see cref="ClonesHash"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClonesHashProperty =
            DependencyProperty.Register("ClonesHash",
                typeof(string),
                typeof(ClonesResolver),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="CloneList"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloneListProperty =
            DependencyProperty.Register("CloneList",
                typeof(ObservableCollection<FilePathCheckBoxListItem>),
                typeof(ClonesResolver),
                new PropertyMetadata(OnCloneListPropertyChanged));

        private static void OnCloneListPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            (d as ClonesResolver)?.UpdateCloneList();

        /// <summary>
        /// Gets or sets whether the detected clones were resolved or not.
        /// </summary>
        public bool IsResolved
        {
            get => (bool)GetValue(IsResolvedProperty);
            set => SetValue(IsResolvedProperty, value);
        }

        /// <summary>
        /// Gets or sets the amount of clones.
        /// </summary>
        public int CloneCount
        {
            get => (int)GetValue(CloneCountProperty);
            set => SetValue(CloneCountProperty, value);
        }

        /// <summary>
        /// Gets or sets the MD5 checksum hash code shared by all the associated clones.
        /// </summary>
        public string ClonesHash
        {
            get => (string)GetValue(ClonesHashProperty);
            set => SetValue(ClonesHashProperty, value);
        }

        /// <summary>
        /// Gets or sets the clones list.
        /// </summary>
        public ObservableCollection<FilePathCheckBoxListItem> CloneList
        {
            get => (ObservableCollection<FilePathCheckBoxListItem>)GetValue(CloneListProperty);
            set => SetValue(CloneListProperty, value);
        }

        /// <summary>
        /// Gets the amount of files that are currently marked for deletion.
        /// </summary>
        public int MarkedForDeletionCount => CloneList.Where(file => !file.IsSelected)?.Count() ?? 0;

        public ClonesResolver() => InitializeComponent();

        /// <summary>
        /// Refresh the clone list with the new\current items. Good after changing <see cref="CloneList"/>.
        /// </summary>
        private void UpdateCloneList()
        {
            // set the new clone list (nothing should happen if its the same object)
            fileList.ItemsSource = CloneList;

            // select the first item to mark it for keeping
            fileList.SelectedIndex = 0;
            if (CloneList.Count > 0)
            {
                var item = fileList.Items.GetItemAt(0) as FilePathCheckBoxListItem;
                item.IsSelected = true;
            }

            // update the amount of clones
            CloneCount = CloneList.Count;

            // refresh the comment text
            commentTextBlock.Text = $"{MarkedForDeletionCount} files marked for deletion.";
        }

        private void FileCheckBox_CheckedChanged(object sender, RoutedEventArgs e) =>
            commentTextBlock.Text = $"{MarkedForDeletionCount} files marked for deletion.";

        private void ResolveButton_Click(object sender, RoutedEventArgs e)
        {
            // count the files that are marked foe deletion
            var count = MarkedForDeletionCount;
            if (count > 0)
            {
                // notify the user about the deletion operation
                if (MessageBox.Show($"{count} files will be deleted. Proceed?",
                "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                == MessageBoxResult.Yes)
                    // delete the files after the user approves
                    Resolve();
            }
            else
                // if nothing marked for deletion just update the comment text block with this message
                commentTextBlock.Text = "Nothing to resolve.";
        }

        /// <summary>
        /// Iterate over all marked files and delete them.
        /// This operation could only fire once.
        /// </summary>
        public void Resolve()
        {
            // ignore if already resolved
            if (IsResolved) return;
            // disable the control for the operation
            controlContentGrid.IsEnabled = false;

            // count the files marked for deletion
            var count = MarkedForDeletionCount;
            if (count > 0)
            {
                // get the files to delete as a list
                var filesToDelete = CloneList.Where(item => !item.IsSelected).ToList();
                // instantiate a list for error message
                var errorList = new List<(string path, string msg)>();
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        // for each file check if exists and try to delete it
                        if (File.Exists(file.FilePath))
                            File.Delete(file.FilePath);
                    }
                    catch (Exception e)
                    {
                        // every error should be logged in the error list created above
                        errorList.Add((file.FilePath, e.Message));
                    }
                }
                // if there were errors 
                // display error message indicating how many errors we encountered
                if (errorList.Count > 5)
                    MessageBox.Show($"Error deleting {errorList.Count} files.",
                        "Error Deleting Files", MessageBoxButton.OK, MessageBoxImage.Error);
                // if there were only up to 5 errors
                // display them in detail
                else if (errorList.Count > 0)
                {
                    string message = $"Error deleting {errorList.Count} files:";
                    var nl = Environment.NewLine;
                    foreach (var (path, msg) in errorList)
                        message += $"{nl}File: {path}{nl}Error message: {msg}{nl}";
                    MessageBox.Show(message, "Error Deleting Files", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // after finishing the operation
                // update the comment with the last message about how many files were removed \ resolved
                commentTextBlock.Text = $"{count} files have been resolved!";
                // update the prefix of the grow able button to represent the end of the process
                resolveButton.Prefix = "✔";
                // mark this as resolved
                IsResolved = true;
                // collapse the control
                rootExpander.IsExpanded = false;
            }
            else
            {
                // if there is nothing to do display this message in the comment
                commentTextBlock.Text = "Nothing to resolve.";
                // and re-enable the control
                controlContentGrid.IsEnabled = true;
            }
        }

        internal void SetListItems(List<string> items) =>
            CloneList = new ObservableCollection<FilePathCheckBoxListItem>(items.Select(item =>
            new FilePathCheckBoxListItem() { FilePath = item }));
    }

    /// <summary>
    /// CheckBoxListItem representing a file path and whether is selected or not.
    /// </summary>
    public class FilePathCheckBoxListItem
    {
        /// <summary>
        /// The path to the file on the system.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Whether this item is selected (is checked) or not.
        /// </summary>
        public bool IsSelected { get; set; }
    }
}