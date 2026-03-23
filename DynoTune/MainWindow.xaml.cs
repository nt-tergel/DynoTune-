using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

using DynoTune.Services;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DynoTune;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        TestAdlx();
    }

    private void TestAdlx()
    {
        var adlxService = new AmdAdlxService();
        bool ok = adlxService.Initialize();

        Debug.WriteLine($"ADLX init: {ok}");

        if (ok)
        {
            adlxService.Shutdown();
            Debug.WriteLine("ADLX shutdown done.");
        }
    }
}