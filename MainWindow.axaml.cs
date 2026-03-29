using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkPortScanner
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<string> _openPorts = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();
            Start.Click += StartButton;
            List.ItemsSource = _openPorts;
        }

        private async void StartButton(object? sender, RoutedEventArgs e)
        {
            string host = IP.Text;
            if (!int.TryParse(DiapozonOt.Text, out int Ot) ||
                  !int.TryParse(DiapozonDo.Text, out int Do) ||
                    !int.TryParse(ColPotok.Text, out int Potoc))
            {
                return;
            }

            int totalPor = Do - Ot + 1;
            int checkedPorts = 0;

            SemaphoreSlim semaphore = new SemaphoreSlim(Potoc); 
            List<Task> tasks = new List<Task>();


            for (int port = Ot; port <= Do; port++)
            {
                await semaphore.WaitAsync(); 

                int currentPort = port;

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (await IsPortOpen(host, currentPort, 200)) 
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                _openPorts.Add($"Порт {currentPort} открыт");
                            });
                        }
                    }
                    catch { }

                    Interlocked.Increment(ref checkedPorts);

                    await Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        ScanProgress.Value = (double)checkedPorts / totalPor * 100;
                        StatusText.Text = $"Сканирование... Найдено: {_openPorts.Count}";
                    });
          
                         semaphore.Release();  
                }));
            }

            await Task.WhenAll(tasks);
            StatusText.Text = $"Завершено. Найдено: {_openPorts.Count}";
        }


        private async Task<bool> IsPortOpen(string host, int port, int timeout)
        {
            try
            {
                using TcpClient client = new TcpClient();

                var task = client.ConnectAsync(host, port);
                var delay = Task.Delay(timeout);

                var completed = await Task.WhenAny(task, delay);

                return completed == task && client.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}
