using Microsoft.UI.Text;
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
using UltraTextEdit.Dialogs;
using Microsoft.UI.Xaml.Markup;

namespace UltraTextEdit.Views;

public sealed partial class MainPage : Page
{
    public string appTitleStr;
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
        //MainWindow window = new MainWindow();
        //appTitleStr= window.Title;

}

    private void RichEditBox_TextChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MainWindow window = new MainWindow();
        editor.Document.GetText(TextGetOptions.UseObjectText, out var textStart);

        if (textStart == "" || string.IsNullOrWhiteSpace(textStart) || _wasOpen)
        {
            saved = true;
        }
        else
        {
            saved = false;
        }

        if (!saved) window.UnsavedTextBlock.Visibility = Visibility.Visible;
        else window.UnsavedTextBlock.Visibility = Visibility.Collapsed;
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
        MainWindow window = new MainWindow();
        string fileName = window.AppTitle.Text.Replace(" - " + appTitleStr, "");
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
                //FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                //if (status != FileUpdateStatus.Complete)
                //{
                //    Windows.UI.Popups.MessageDialog errorBox = new("File " + file.Name + " couldn't be saved.");
                //    await errorBox.ShowAsync();
                //}
                saved = true;
                fileNameWithPath = file.Path;
                window.AppTitle.Text = file.Name + " - " + appTitleStr;
                //Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
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
                    window.AppTitle.Text = file.Name + " - " + appTitleStr;
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
        MainWindow window = new MainWindow();

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
                window.AppTitle.Text = file.Name + " - " + appTitleStr;
                fileNameWithPath = file.Path;
            }
            saved = true;
            _wasOpen = true;
            //Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
            //Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("CurrentlyOpenFile", file);
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

    private void SubscriptButton_Click(object sender, RoutedEventArgs e)
    {
        ITextSelection selectedText = editor.Document.Selection;
        ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
        charFormatting.Subscript = FormatEffect.Toggle;
    }

    private void SuperScriptButton_Click(object sender, RoutedEventArgs e)
    {
        ITextSelection selectedText = editor.Document.Selection;
        ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
        charFormatting.Subscript = FormatEffect.Toggle;
    }

    private void FindButton2_Click(object sender, RoutedEventArgs e)
    {
        textsplitview.IsPaneOpen = true;
    }

    private void FindBoxRemoveHighlights()
    {
        ITextRange documentRange = editor.Document.GetRange(0, TextConstants.MaxUnitCount);
        SolidColorBrush defaultBackground = editor.Background as SolidColorBrush;
        SolidColorBrush defaultForeground = editor.Foreground as SolidColorBrush;

        documentRange.CharacterFormat.BackgroundColor = defaultBackground.Color;
        documentRange.CharacterFormat.ForegroundColor = defaultForeground.Color;
    }

    private void FindBoxHighlightMatches()
    {
        FindBoxRemoveHighlights();

        Color highlightBackgroundColor = (Color)Application.Current.Resources["SystemColorHighlightColor"];
        Color highlightForegroundColor = (Color)Application.Current.Resources["SystemColorHighlightTextColor"];

        string textToFind = findBox.Text;
        if (textToFind != null)
        {
            ITextRange searchRange = editor.Document.GetRange(0, 0);
            while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, FindOptions.None) > 0)
            {
                searchRange.CharacterFormat.BackgroundColor = highlightBackgroundColor;
                searchRange.CharacterFormat.ForegroundColor = highlightForegroundColor;
            }
        }
    }

    private void FindButton_Click(object sender, RoutedEventArgs e)
    {
        FindBoxHighlightMatches();
    }

    private void RemoveHighlightButton_Click(object sender, RoutedEventArgs e)
    {
        FindBoxRemoveHighlights();
    }

    private void ReplaceSelected_Click(object sender, RoutedEventArgs e)
    {
        editor.Document.Selection.SetText(TextSetOptions.None, replaceBox.Text);
    }

    private void ReplaceAll_Click(object sender, RoutedEventArgs e)
    {
        editor.Document.GetText(TextGetOptions.FormatRtf, out string value);
        if (!(string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(findBox.Text) && string.IsNullOrWhiteSpace(replaceBox.Text)))
        {
            editor.Document.SetText(TextSetOptions.FormatRtf, value.Replace(findBox.Text, replaceBox.Text));
        }
    }

    private void closepane(object sender, RoutedEventArgs e)
    {
        textsplitview.IsPaneOpen = false;
    }

    private void StrikeButton_Click(object sender, RoutedEventArgs e)
    {
        ITextSelection selectedText = editor.Document.Selection;

        if (selectedText != null)
        {
            ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
            charFormatting.Strikethrough = charFormatting.Strikethrough == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
        }
    }

    private async void vraopen(object sender, RoutedEventArgs e)
    {
        VRAWindow window = new VRAWindow();


        window.Activate();
    }

    private async void AddImageButton_Click(object sender, RoutedEventArgs e)
    {
        // Open an image file.
        FileOpenPicker open = new();
        open.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        open.FileTypeFilter.Add(".png");
        open.FileTypeFilter.Add(".jpg");
        open.FileTypeFilter.Add(".jpeg");

        StorageFile file = await open.PickSingleFileAsync();

        if (file != null)
        {
            using IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.Read);
            var properties = await file.Properties.GetImagePropertiesAsync();
            int width = (int)properties.Width;
            int height = (int)properties.Height;

            iOd dialog = new()
            {
                DefaultWidth = width,
                DefaultHeight = height
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                editor.Document.Selection.InsertImage((int)dialog.DefaultWidth, (int)dialog.DefaultHeight, 0, VerticalCharacterAlignment.Baseline, string.IsNullOrWhiteSpace(dialog.Tag) ? "Image" : dialog.Tag, randAccStream);
                return;
            }

            // Insert an image
            editor.Document.Selection.InsertImage(width, height, 0, VerticalCharacterAlignment.Baseline, "Image", randAccStream);
        }
    }
    private void AddLinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.UI.Xaml.FrameworkElement", "AllowFocusOnInteraction"))
            hyperlinkText.AllowFocusOnInteraction = true;
        editor.Document.Selection.Link = $"\"{hyperlinkText.Text}\"";
        editor.Document.Selection.CharacterFormat.ForegroundColor = (Color)XamlBindingHelper.ConvertValue(typeof(Color), "#6194c7");
        AddLinkButton.Flyout.Hide();
    }
}
