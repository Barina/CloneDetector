using CloneDetectorCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace CloneDetector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Listen to progress callbacks.
        /// </summary>
        readonly Progress<double> progress;

        /// <summary>
        /// A source for cancellation tokens.
        /// </summary>
        CancellationTokenSource CancellationTokenSource;

        /// <summary>
        /// Whether is data the user currently dragging is valid or not.
        /// </summary>
        private bool isValidData;

        public MainWindow()
        {
            InitializeComponent();

            progress = new Progress<double>();
            progress.ProgressChanged += Progress_ProgressChanged;
            ((INotifyCollectionChanged)fileListBox.Items).CollectionChanged += FilesListBox_CollectionChanged;

            // apply values from settings
            Top = Properties.Settings.Default.Top;
            Left = Properties.Settings.Default.Left;
            Height = Properties.Settings.Default.Height;
            Width = Properties.Settings.Default.Width;
            WindowState = Properties.Settings.Default.WindowState;
        }

        private void FilesListBox_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // get the amount of items currently in the list
            int count = fileListBox.Items.Count;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // calculate the item count before the change 
                int preCount = count - e.NewItems.Count;
                if (preCount <= 1 && count > 1)
                    // count was 0-1 and now it is more than 1
                    (FindResource("ShowStartButton") as Storyboard)?.Begin();
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                // calculate the item count before the change 
                int preCount = count + e.OldItems.Count;
                if (preCount > 1 && count <= 1)
                    // means the last items were removed or only one item left which isn't enough for detection
                    (FindResource("HideStartButton") as Storyboard)?.Begin();
            }

            // enable\disable the clear button according to the amount of items currently presented in the list
            clearFilesButton.IsEnabled = count > 0;

            // update the text of the center message text block according to the amount of files
            if (count > 1)
                centerTextBlock.Text = $"{count} FILES";
            else if (count > 0)
                centerTextBlock.Text = $"{count} FILE";
            else
                centerTextBlock.Text = "DROP FILES HERE";
        }

        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            // enable\disable the remove files button according to the amount of items currently selected in the list
            removeFilesButton.IsEnabled = fileListBox.SelectedItems.Count > 0;

        private void Progress_ProgressChanged(object sender, double progress) =>
            // calculate the progress of the clone detection operation
            startButton.Value = startButton.Maximum * progress;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // if there is a clone detection operation running we need to cancel it before closing the window
            // this will prevent the app to keep being alive after closing it taking resources in the background for nothing
            // as there is nothing now to take care the results..
            CancellationTokenSource?.Cancel();

            // save the appropriate values to the settings
            if (WindowState == WindowState.Maximized)
            {
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
            }
            else
            {
                Properties.Settings.Default.Top = Top;
                Properties.Settings.Default.Left = Left;
                Properties.Settings.Default.Height = Height;
                Properties.Settings.Default.Width = Width;
            }
            Properties.Settings.Default.WindowState = WindowState;
            Properties.Settings.Default.Save();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => RefreshFilesPanel();
        private void Window_StateChanged(object sender, EventArgs e) => RefreshFilesPanel();

        private void AddFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Multiselect = true };
            if (dialog.ShowDialog(this) == true)
            {
                // add the selected files
                var files = GatherFiles(dialog.FileNames);
                Array.ForEach(files, file => fileListBox.Items.Add(file));
            }
        }

        private void RemoveFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var items = fileListBox.SelectedItems.Cast<string>().ToArray();

            if (items.Length <= 0) return;

            // remove selected files
            foreach (var item in items)
                fileListBox.Items.Remove(item);

            // collapse the file list box panel if the file list is now empty
            if (fileListBox.Items.Count <= 0)
                fileListExpander.IsExpanded = false;
        }

        private void ClearFilesButton_Click(object sender, RoutedEventArgs e) => ClearFileList();

        private async void RadialButtonProgressBar_RadialButtonClick(object sender, RoutedEventArgs e)
        {
            // start \ cancel the clone detection operation
            if (startButton.IsWorking)
            {
                startButton.IsEnabled = false;
                CancellationTokenSource?.Cancel();
            }
            else
                await Start();
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            // at first we assume the data is invalid or empty
            isValidData = false;
            e.Effects = DragDropEffects.None;

            // data is empty
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            // data is invalid (not an array of strings)
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files)) return;

            // get all the valid file paths
            files = GatherFiles(files);

            // no valid file path
            if (files.Length <= 0) return;

            // reaching here means there's at least one file path valid
            isValidData = true;
            e.Effects = DragDropEffects.Link;
            // display a tool-tip implying to drop the currently valid files inside the window
            dropMessageTextBlock.Text = $"Drop {files.Length} valid files here.";
            dropMessageBorder.Visibility = Visibility.Visible;
        }

        private void Window_DragLeave(object sender, DragEventArgs e) =>
            // hide the tool-tip
            dropMessageBorder.Visibility = Visibility.Collapsed;

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            // while the cursor hangs over the window we'll update the tool-tip position
            // only if there's valid data
            if (isValidData)
            {
                e.Effects = DragDropEffects.Link;
                var position = e.GetPosition(this);
                dropMessageBorderTransform.X = position.X - dropMessageBorder.ActualWidth * .5;
                dropMessageBorderTransform.Y = position.Y - dropMessageBorder.ActualHeight;
            }
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            // user dropped the data inside the window
            // so we hide the tool-tip
            dropMessageBorder.Visibility = Visibility.Collapsed;

            // check if there is data and if it is valid
            if (!e.Data.GetDataPresent(DataFormats.FileDrop) || !isValidData) return;

            // extract the data from the event args and check again if its empty or not an array of strings
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files)) return;

            // get all the valid paths from the list
            files = GatherFiles(files);

            // add the valid file paths to the list
            Array.ForEach(files, file => fileListBox.Items.Add(file));
        }

        private void FileListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                // delete all selected while the user pressed the delete key
                var items = fileListBox.SelectedItems.Cast<string>().ToArray();
                Array.ForEach(items, i => fileListBox.Items.Remove(i));
            }
        }

        private void FilListExpander_Expanded(object sender, RoutedEventArgs e) => ExpandFilesPanel();
        private void FilListExpander_Collapsed(object sender, RoutedEventArgs e) => CollapseFilesPanel();
        private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseResultsPage();

        private void ResolveButton_Click(object sender, RoutedEventArgs e)
        {
            // count all files marked for deletion
            int deletionCount = 0;
            foreach (var child in resolveControlsStackPanel.Children)
                if (child is ClonesResolver resolver && !resolver.IsResolved)
                    deletionCount += resolver.MarkedForDeletionCount;

            if (deletionCount > 0)
            {
                // prompt the user about the amount of files to be deleted
                if (MessageBox.Show($"Are you sure you want to delete {deletionCount} files from your hard disk?",
                    "Please confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                    == MessageBoxResult.Yes)
                {
                    foreach (var child in resolveControlsStackPanel.Children)
                        if (child is ClonesResolver resolver && !resolver.IsResolved)
                            resolver.Resolve();
                    resolveButton.Prefix = "✔";
                    CloseResultsPage();
                }
            }
            else if (resolveControlsStackPanel.Children.Count > 0)
                // prompt the user about nothing to resolve only if there is files to uncheck
                MessageBox.Show("Nothing to resolve. Please uncheck some files to mark them for deletion.",
                    "Please notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Updates the state of the files panel.
        /// </summary>
        private void RefreshFilesPanel()
        {
            // re position the files panel after the windows size has changed
            // this is a workaround as the panel behavior isn't supporting resizing
            // without this the panel will stay at its position making it looks dumb..
            // and by using the expand\collapse animation it'll be transitioned nicely :)
            if (fileListExpander.IsExpanded)
                ExpandFilesPanel();
            else
                CollapseFilesPanel();
        }

        /// <summary>
        /// Clear all files from the file list.
        /// </summary>
        private void ClearFileList()
        {
            // get all file items
            var items = fileListBox.Items.Cast<string>().ToArray();

            if (items.Length <= 0) return;

            // remove one by one
            foreach (var item in items)
                fileListBox.Items.Remove(item);

            // clear the items (better be safe than sorry..)
            fileListBox.Items.Clear();

            // and collapse the file list panel
            fileListExpander.IsExpanded = false;
        }

        /// <summary>
        /// Start the clone detection with the current file list.
        /// </summary>
        private async Task Start()
        {
            // disable controls
            fileListExpander.IsEnabled = false;

            // initiate values
            startButton.Value = 0;
            startButton.IsWorking = true;
            centerTextBlock.Text = "WORKING...";
            CancellationTokenSource = new CancellationTokenSource();
            string[] items = new string[fileListBox.Items.Count];

            // copy all files into a new list
            fileListBox.Items.CopyTo(items, 0);

            // start to calculate and save the results in a local variable
            var result = await Detector.GetClonedAsync(CancellationTokenSource.Token, progress, items);

            // with the given results build the results page to reflect the new clone list
            BuildResultsPage(result);

            // re-enable controls
            startButton.IsEnabled = true;
            fileListExpander.IsEnabled = true;
            startButton.IsWorking = false;
            centerTextBlock.Text = "DROP FILES HERE";
        }

        /// <summary>
        /// Build and show the results page based on the list of files given. 
        /// Will show 'no clones' where the given list is null or empty.
        /// </summary>
        /// <param name="result">The <see cref="Dictionary{TKey, TValue}"/> containing 
        /// all the clones and their respective MD5 checksum (or hash code).</param>
        private void BuildResultsPage(Dictionary<string, List<string>> result)
        {
            // this method is by far the worst approach for handling control list
            // TODO: let a ListBox \ ListItems handle this and bind a DataSource
            // of CloneResolver list of controls

            // clear current cloneResolver items from the results page
            resolveControlsStackPanel.Children.Clear();

            if (result == null || result.Count <= 0)
            {
                // where no clones found:
                // hide and show appropriate controls
                noClonesTextBlock.Visibility = Visibility.Visible;
                resolveButton.Visibility = Visibility.Collapsed;
                resultsDescription.Visibility = Visibility.Collapsed;
                // set the header message
                resultsHeader.Text = "0 Clones Detected.";
            }
            else
            {
                // where some clones found:
                // hide and show appropriate controls
                noClonesTextBlock.Visibility = Visibility.Collapsed;
                resolveButton.Visibility = Visibility.Visible;
                resultsDescription.Visibility = Visibility.Visible;

                // results comes in pairs of HASH and list of FilePaths for files that shares the same hash
                // so for every pair we'll create a new ClonesResolver control
                // and pass it the corresponding values
                int sumClones = 0;
                foreach (var pair in result)
                {
                    var control = new ClonesResolver() { ClonesHash = pair.Key };
                    resolveControlsStackPanel.Children.Add(control);
                    control.SetListItems(pair.Value);
                    // we'll also count the amount of clones detected
                    sumClones += pair.Value.Count;
                }

                // update the header
                resultsHeader.Text = $"{sumClones} Clones Detected.";
            }
            // begin animate the results page show animation
            (FindResource("ShowResultsPage") as Storyboard)?.Begin();
        }

        /// <summary>
        /// Close the results page.
        /// </summary>
        private void CloseResultsPage()
        {
            // assuming all clones were resolved
            bool allResolved = true;

            // search for unresolved clone
            foreach (var child in resolveControlsStackPanel.Children)
                if (child is ClonesResolver resolver && !resolver.IsResolved)
                {
                    allResolved = false;
                    break;
                }

            // if everything was resolved we can clear the file list as we do not need it anymore
            // if not there's maybe more clones in the list so we let the user to decode
            // whether to start again the detection
            if (allResolved)
                ClearFileList();

            // start to animate the results page hide animation
            (FindResource("HideResultsPage") as Storyboard)?.Begin();
        }

        /// <summary>
        /// Expand the files panel.
        /// </summary>
        private void ExpandFilesPanel()
        {
            fileListExpander.Header = "Close";
            Thickness targetMargin = new Thickness(50, -50, 50, 50);
            SlideFileListPanel(targetMargin);
        }

        /// <summary>
        /// Collapse the files panel.
        /// </summary>
        private void CollapseFilesPanel()
        {
            fileListExpander.Header = "Edit Files";
            Thickness targetMargin = new Thickness(50, -ActualHeight - 50, 50, ActualHeight - 50);
            SlideFileListPanel(targetMargin);
        }

        /// <summary>
        /// Slide the files panel to a new given margins.
        /// </summary>
        /// <param name="targetMargin">The target desired margins for the files list.</param>
        private void SlideFileListPanel(Thickness targetMargin)
        {
            ThicknessAnimation anim = new ThicknessAnimation(targetMargin, TimeSpan.FromSeconds(1))
            {
                EasingFunction = new PowerEase() { EasingMode = EasingMode.EaseOut, Power = 8 }
            };
            filesBorder.BeginAnimation(MarginProperty, anim);
        }

        /// <summary>
        /// Gather all valid files into a new <see cref="string"/> array 
        /// filtering existing files and iterating inside folders too (one level down or non-recursively).
        /// </summary>
        /// <param name="filePaths">The array of file paths to iterate over</param>
        /// <returns>Returns a new filtered array of <see cref="string"/>s.</returns>
        private string[] GatherFiles(string[] filePaths)
        {
            if (filePaths == null || filePaths.Length <= 0) return null;

            // filter already existing paths
            var files = filePaths.Distinct().Where(s => !fileListBox.Items.Contains(s)).ToList();

            // extract directories
            var dirs = filePaths.Where(s => !File.Exists(s) && Directory.Exists(s)).Distinct().ToList();

            // filter the directories
            files = files.Except(dirs).ToList();
            foreach (var dir in dirs)
            {
                // get file list of every directory
                var dirFiles = Directory.GetFiles(dir, "*");
                foreach (var file in dirFiles)
                    // add the path to the files list if it exists (therefore it is not a directory)
                    // and not already exists 
                    if (!files.Contains(file) && File.Exists(file))
                        files.Add(file);
            }
            // lastly filter the list again just to be sure there's no duplicates and return the list as array of string
            return files.Distinct().Where(s => !fileListBox.Items.Contains(s)).ToArray();
        }
    }
}