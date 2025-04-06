using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Win32;
using StaticCodeAnalysis.Analyzator.Core;

namespace StaticCodeAnalysis.Analyzator.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void PickSolution_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        
        openFileDialog.Filter = "Solution file (*.sln)|*.sln";
        
        openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        
        bool? result = openFileDialog.ShowDialog();
        
        if (result == true)
        {
            string filePath = openFileDialog.FileName;

            SolutionFilePathTextBox.Text = filePath;
        }
    }

    private async void TraceMethodTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }
        
        var codeAnalysis = await CodeAnalysis.Create(SolutionFilePathTextBox.Text);
        var allPaths = codeAnalysis.GetAllCallPaths(TraceMethodTextBox.Text);
        
        foreach (var path in allPaths.Where(x => x.Count > 0).Select(x => new { All = x, Last = x.Last()}))
        {
            EndpointListBox.Items.Add(new ListBoxItem()
            {
                Tag = path.All,
                Content = path.Last
            });
        }

    }

    private void EndpointListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EndpointListBox.SelectedItem != null)
        {
            var item = EndpointListBox.SelectedItem as ListBoxItem;
            var paths = (List<MethodDeclaration>)item.Tag;
            
            CallStackListBox.Items.Clear();
            
            foreach (var path in paths)
            {
                CallStackListBox.Items.Add(new ListBoxItem()
                {
                    Tag = path,
                    Content = path.Key
                });
            }
        }
    }

    private void CallStackListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CallStackListBox.SelectedItem != null)
        {
            var item = CallStackListBox.SelectedItem as ListBoxItem;
            var methodDeclaration = (MethodDeclaration)item.Tag;
            BodyTextBox.Text = methodDeclaration.Body;
        }
    }
}