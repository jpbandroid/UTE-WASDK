﻿using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using UltraTextEdit.ViewModels;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Runtime.InteropServices;
using WinRT;
using Microsoft.Graphics.Canvas.Text;
using Windows.UI;
using Microsoft.UI.Xaml.Media;

namespace UltraTextEdit.Views;

public sealed partial class MainPage : Page
{
    private string appTitleStr;
    private bool saved;
    private bool _wasOpen;
    private object fileNameWithPath;

    public List<string> Fonts
    {
        get
        {
            return CanvasTextFormat.GetSystemFontFamilies().OrderBy(f => f).ToList();
        }
    }

    public MainViewModel ViewModel
    {
        get;
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
    internal interface IWindowNative
    {
        IntPtr WindowHandle
        {
            get;
        }
    }

    [ComImport]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
        Window window = new Window();
        window.ExtendsContentIntoTitleBar = true;
        window.SetTitleBar(TitleBar);
        appTitleStr= window.Title;

}

    private void RichEditBox_TextChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        editor.Document.GetText(TextGetOptions.UseObjectText, out var textStart);

        //if (textStart == "" || string.IsNullOrWhiteSpace(textStart) || _wasOpen)
        //{
        //    saved = true;
        //}
        //else
        //{
        //    saved = false;
        //}

        //if (!saved) UnsavedTextBlock.Visibility = Visibility.Visible;
        //else UnsavedTextBlock.Visibility = Visibility.Collapsed;
    }

    private void OnKeyboardAcceleratorInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        switch (sender.Key)
        {
            case Windows.System.VirtualKey.B:
                //editor.FormatSelected(RichEditHelpers.FormattingMode.Bold);
                //BoldButton.IsChecked = editor.Document.Selection.CharacterFormat.Bold == FormatEffect.On;
                args.Handled = true;
                break;
            case Windows.System.VirtualKey.I:
                //editor.FormatSelected(RichEditHelpers.FormattingMode.Italic);
                //ItalicButton.IsChecked = editor.Document.Selection.CharacterFormat.Italic == FormatEffect.On;
                args.Handled = true;
                break;
            case Windows.System.VirtualKey.U:
                //editor.FormatSelected(RichEditHelpers.FormattingMode.Underline);
                //UnderlineButton.IsChecked = editor.Document.Selection.CharacterFormat.Underline == UnderlineType.Single;
                args.Handled = true;
                break;
            case Windows.System.VirtualKey.S:
                //SaveFile(false);
                break;
        }
    }

    private void showinsiderinfo(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ToggleThemeTeachingTip1.IsOpen = true;
    }

    private void BoldButton_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.UI.Text.ITextSelection selectedText = editor.Document.Selection;
        if (selectedText != null)
        {
            Microsoft.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
            charFormatting.Bold = Microsoft.UI.Text.FormatEffect.Toggle;
            selectedText.CharacterFormat = charFormatting;
        }
    }

    private void ItalicButton_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.UI.Text.ITextSelection selectedText = editor.Document.Selection;
        if (selectedText != null)
        {
            Microsoft.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
            charFormatting.Italic = Microsoft.UI.Text.FormatEffect.Toggle;
            selectedText.CharacterFormat = charFormatting;
        }
    }

    private void UnderlineButton_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.UI.Text.ITextSelection selectedText = editor.Document.Selection;
        if (selectedText != null)
        {
            Microsoft.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
            if (charFormatting.Underline == Microsoft.UI.Text.UnderlineType.None)
            {
                charFormatting.Underline = Microsoft.UI.Text.UnderlineType.Single;
            }
            else
            {
                charFormatting.Underline = Microsoft.UI.Text.UnderlineType.None;
            }
            selectedText.CharacterFormat = charFormatting;
        }
    }

    private void AlignCenterButton_Click(object sender, RoutedEventArgs e)
    {
        ITextSelection selectedText = editor.Document.Selection;
        selectedText.ParagraphFormat.Alignment = ParagraphAlignment.Center;
    }

    private void AlignLeftButton_Click(object sender, RoutedEventArgs e)
    {
        ITextSelection selectedText = editor.Document.Selection;
        selectedText.ParagraphFormat.Alignment = ParagraphAlignment.Left;
    }

    private void AlignRightButton_Click(object sender, RoutedEventArgs e)
    {
        ITextSelection selectedText = editor.Document.Selection;
        selectedText.ParagraphFormat.Alignment = ParagraphAlignment.Right;
    }

    private void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        SaveFile(true);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveFile(false);
    }

    private async void SaveFile(bool isCopy)
    {
        string fileName = AppTitle.Text.Replace(" - " + appTitleStr, "");
        if (isCopy || fileName == "Untitled")
        {
            FileSavePicker savePicker = App.MainWindow.CreateSaveFilePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Rich Text", new List<string>() { ".rtf" });
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "New Document";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we
                // finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                using (IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    if (file.Name.EndsWith(".txt"))
                    {
                        editor.Document.SaveToStream(Microsoft.UI.Text.TextGetOptions.None, randAccStream);
                    }
                    else
                    {
                        editor.Document.SaveToStream(Microsoft.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                    }

                // Let Windows know that we're finished changing the file so the
                // other app can update the remote version of the file.
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status != FileUpdateStatus.Complete)
                {
                    Windows.UI.Popups.MessageDialog errorBox = new("File " + file.Name + " couldn't be saved.");
                    await errorBox.ShowAsync();
                }
                saved = true;
                fileNameWithPath = file.Path;
                AppTitle.Text = file.Name + " - " + appTitleStr;
                Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
            }
        }
        else if (!isCopy || fileName != "Untitled")
        {
            //string path = fileNameWithPath.Replace("\\" + fileName, "");
            try
            {
                StorageFile file = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync("CurrentlyOpenFile");
                if (file != null)
                {
                    // Prevent updates to the remote version of the file until we
                    // finish making changes and call CompleteUpdatesAsync.
                    CachedFileManager.DeferUpdates(file);
                    // write to file
                    using (IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                        if (file.Name.EndsWith(".txt"))
                        {
                            editor.Document.SaveToStream(TextGetOptions.None, randAccStream);
                        }
                        else
                        {
                            editor.Document.SaveToStream(TextGetOptions.FormatRtf, randAccStream);
                        }


                    // Let Windows know that we're finished changing the file so the
                    // other app can update the remote version of the file.
                    FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (status != FileUpdateStatus.Complete)
                    {
                        Windows.UI.Popups.MessageDialog errorBox = new("File " + file.Name + " couldn't be saved.");
                        await errorBox.ShowAsync();
                    }
                    saved = true;
                    AppTitle.Text = file.Name + " - " + appTitleStr;
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove("CurrentlyOpenFile");
                }
            }
            catch (Exception)
            {
                SaveFile(true);
            }
        }
    }

    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        // Open a text file.
        Windows.Storage.Pickers.FileOpenPicker open = App.MainWindow.CreateOpenFilePicker();
        open.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        open.FileTypeFilter.Add(".rtf");
        open.FileTypeFilter.Add(".txt");

        Windows.Storage.StorageFile file = await open.PickSingleFileAsync();

        if (file != null)
        {
            using (IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                IBuffer buffer = await FileIO.ReadBufferAsync(file);
                var reader = DataReader.FromBuffer(buffer);
                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                string text = reader.ReadString(buffer.Length);
                // Load the file into the Document property of the RichEditBox.
                editor.Document.LoadFromStream(TextSetOptions.FormatRtf, randAccStream);
                //editor.Document.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, text);
                AppTitle.Text = file.Name + " - " + appTitleStr;
                fileNameWithPath = file.Path;
            }
            saved = true;
            _wasOpen = true;
            Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("CurrentlyOpenFile", file);
        }
    }

    private void FontsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (editor.Document.Selection != null)
        {
            editor.Document.Selection.CharacterFormat.Name = FontsCombo.SelectedValue.ToString();
        }
    }

    private void FontSizeBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (editor != null && editor.Document.Selection != null)
        {
            ITextSelection selectedText = editor.Document.Selection;
            ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
            charFormatting.Size = (float)sender.Value;
            selectedText.CharacterFormat = charFormatting;
        }
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        // Extract the color of the button that was clicked.
        Button clickedColor = (Button)sender;
        var rectangle = (Microsoft.UI.Xaml.Shapes.Rectangle)clickedColor.Content;
        var color = (rectangle.Fill as SolidColorBrush).Color;

        editor.Document.Selection.CharacterFormat.ForegroundColor = color;

        fontColorButton.Flyout.Hide();
        editor.Focus(FocusState.Keyboard);
    }

    private void ConfirmColor_Click(object sender, RoutedEventArgs e)
    {
        // Confirm color picker choice and apply color to text
        Color color = myColorPicker.Color;
        editor.Document.Selection.CharacterFormat.ForegroundColor = color;

        // Hide flyout
        colorPickerButton.Flyout.Hide();
    }

    private void CancelColor_Click(object sender, RoutedEventArgs e)
    {
        // Cancel flyout
        colorPickerButton.Flyout.Hide();
    }
}
