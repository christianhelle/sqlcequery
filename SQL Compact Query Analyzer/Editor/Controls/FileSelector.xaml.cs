using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Hardcodet.Wpf.Toolbox.Controls.Files
{
  /// <summary>
  /// A simple file selector which supports save-as and
  /// open-file dialogs.
  /// </summary>
  public partial class FileSelector : UserControl
  {
    #region Mode dependency property

    /// <summary>
    /// Defines whether a file needs to be opened or saved.
    /// </summary>
    public static readonly DependencyProperty ModeProperty;

    /// <summary>
    /// A property wrapper for the <see cref="ModeProperty"/>
    /// dependency property:<br/>
    /// Defines whether a file needs to be opened or saved.
    /// </summary>
    public FileSelectorMode Mode
    {
      get { return (FileSelectorMode) GetValue(ModeProperty); }
      set { SetValue(ModeProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="ModeProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="Mode"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void ModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
    }

    #endregion


    #region FileFilter dependency property

    /// <summary>
    /// Defines a file filter for the file selection dialog.
    /// </summary>
    public static readonly DependencyProperty FileFilterProperty;

    /// <summary>
    /// A property wrapper for the <see cref="FileFilterProperty"/>
    /// dependency property:<br/>
    /// Defines a file filter for the file selection dialog.
    /// </summary>
    public string FileFilter
    {
      get { return (string) GetValue(FileFilterProperty); }
      set { SetValue(FileFilterProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="FileFilterProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="FileFilter"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void FileFilterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
    }

    #endregion


    #region FileName dependency property

    /// <summary>
    /// Gets or sets the file path to be displayed.
    /// </summary>
    public static readonly DependencyProperty FileNameProperty;

    /// <summary>
    /// A property wrapper for the <see cref="FileNameProperty"/>
    /// dependency property:<br/>
    /// Gets or sets the file path to be displayed.
    /// </summary>
    public string FileName
    {
      get { return (string) GetValue(FileNameProperty); }
      set { SetValue(FileNameProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="FileNameProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="FileName"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void FileNamePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      FileSelector owner = (FileSelector) d;
      owner.UpdateFileNameDisplay();
    }

    #endregion


    #region MaxDisplayLength dependency property

    /// <summary>
    /// The maximum number of characters of the file path to be displayed.
    /// If the path is longer than this value, it will be shortened.
    /// Set to 0 in order to always show the full path.
    /// </summary>
    public static readonly DependencyProperty MaxDisplayLengthProperty;

    /// <summary>
    /// A property wrapper for the <see cref="MaxDisplayLengthProperty"/>
    /// dependency property:<br/>
    /// The maximum number of characters of the file path to be displayed.
    /// If the path is longer than this value, it will be shortened.
    /// Set to 0 in order to always show the full path.
    /// </summary>
    public int MaxDisplayLength
    {
      get { return (int) GetValue(MaxDisplayLengthProperty); }
      set { SetValue(MaxDisplayLengthProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="MaxDisplayLengthProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="MaxDisplayLength"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void MaxDisplayLengthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      FileSelector owner = (FileSelector) d;
      owner.UpdateFileNameDisplay();
    }

    #endregion


    #region construction

    /// <summary>
    /// Inits the control's dependency properties.
    /// </summary>
    static FileSelector()
    {
      //register dependency properties
      FrameworkPropertyMetadata md = new FrameworkPropertyMetadata(0, MaxDisplayLengthPropertyChanged);
      MaxDisplayLengthProperty =
        DependencyProperty.Register("MaxDisplayLength", typeof (int), typeof (FileSelector), md);

      md = new FrameworkPropertyMetadata("", FileNamePropertyChanged);
      FileNameProperty = DependencyProperty.Register("FileName", typeof (string), typeof (FileSelector), md);

      md = new FrameworkPropertyMetadata(FileSelectorMode.Open, ModePropertyChanged);
      ModeProperty = DependencyProperty.Register("Mode", typeof (FileSelectorMode), typeof (FileSelector), md);

      md = new FrameworkPropertyMetadata(null, FileFilterPropertyChanged);
      FileFilterProperty = DependencyProperty.Register("FileFilter", typeof (string), typeof (FileSelector), md);
    }


    public FileSelector()
    {
      InitializeComponent();
    }

    #endregion


    #region update displayed file name

    /// <summary>
    /// Updates the displayed control according to the
    /// <see cref="FileName"/> and <see cref="MaxDisplayLength"/>
    /// properties.
    /// </summary>
    private void UpdateFileNameDisplay()
    {
      string fileName = FileName;

      if (String.IsNullOrEmpty(fileName))
      {
        txtFileName.Text = String.Empty;
        return;
      }

      int length = fileName.Length;
      int maxLength = MaxDisplayLength;
      if (maxLength > 0 && length > maxLength + 1)
      {
        fileName = "~" + fileName.Substring(length - maxLength, maxLength);
      }

      txtFileName.Text = fileName;
    }

    #endregion


    #region browse file

    /// <summary>
    /// Displays a file dialog and assign the selected
    /// file to the <see cref="FileName"/> property.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnBrowse_Click(object sender, RoutedEventArgs e)
    {
      FileDialog dlg;
      if (Mode == FileSelectorMode.Open)
        dlg = new OpenFileDialog();
      else
        dlg = new SaveFileDialog();

      dlg.Filter = FileFilter;
      bool? result = dlg.ShowDialog();
      if (result == true)
      {
        FileName = dlg.FileName;
      }
    }

    #endregion
  }

  /// <summary>
  /// Defines whether the selector displays a file-open or
  /// file-save dialog.
  /// </summary>
  public enum FileSelectorMode
  {
    Open = 0,
    Save = 1
  }
}