using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace TaskManagerWPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Tray
        private NotifyIcon TrayIcon = null;
        private System.Windows.Controls.ContextMenu TrayMenu = null;
        private WindowState fCurrentWindowState = WindowState.Normal;
        private bool fCanClose = false;
        //Tray

        private List<ProcessInfo> blackList = null;
        private ProcessInfo item = null;
        private System.Threading.Timer timer = null;

        public MainWindow()
        {
            this.InitializeComponent();

            this.item = new ProcessInfo();
            this.blackList = new List<ProcessInfo>();

            this.RefreshProcessesList();
            this.WatchProcesses();
        }

        /// <summary>
        /// наблюдение за процессами
        /// </summary>
        private void WatchProcesses()
        {
            this.timer = new System.Threading.Timer(
                obj =>
                {
                    var names = this.blackList
                        .Select(x => x.Name)
                        .ToArray();

                    this.KillProcessByNames(names);

                    System.Windows.Application.Current.Dispatcher.Invoke(() => this.RefreshProcessesList());
                },
                null,
                5000,
                5000);
        }

        /// <summary>
        /// обновление листа процессов
        /// </summary>
        private void RefreshProcessesList()
        {
            this.listView.ItemsSource = this.GetProcessInfo();
        }

        /// <summary>
        /// получение сведений о процессе
        /// </summary>
        /// <returns></returns>
        private IEnumerable<ProcessInfo> GetProcessInfo()
        {
            Process[] processes = Process.GetProcesses();
            List<ProcessInfo> processList = new List<ProcessInfo>();

            foreach (var process in processes)
            {

                processList.Add(
                    new ProcessInfo()
                    {
                        Name = process.ProcessName,
                        Id = process.Id,
                        State = process.Responding ? "Running" : "Not Running",
                        Username = Environment.UserName,
                        Memory = process.WorkingSet64 / 1024 / 1024
                    });
            }

            return processList.OrderBy(x => x.Name).ToArray();
        }

        /// <summary>
        /// отключение процесса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KillButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (Process proc in Process.GetProcesses())
                {
                    ProcessInfo item = this.listView.SelectedItem as ProcessInfo;

                    if (proc.Id == item.Id)
                    {
                        proc.Kill();
                        this.RefreshProcessesList();

                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// запустить выбранный процесс
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
                if (ofd.ShowDialog() == true)
                    Process.Start(ofd.FileName);

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// добавить в черный лист(не запускать больше)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddToBlackListButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (Process proc in Process.GetProcesses())
                {
                    ProcessInfo item = this.listView.SelectedItem as ProcessInfo;

                    if (item == null)
                        return;

                    this.blackList.Add(item);

                    ListBoxItem listBoxItem = new ListBoxItem();

                    if (proc.Id == item.Id)
                    {
                        listBoxItem.Content = proc.ProcessName;

                        this.listBox.Items.Add(listBoxItem);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// отключение процесса по имени
        /// </summary>
        /// <param name="names"></param>
        private void KillProcessByNames(string[] names)
        {
            foreach (Process proc in Process.GetProcesses())
            {
                if (names.Contains(proc.ProcessName))
                {
                    proc.Kill();
                }
            }
        }

        //Tray
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.createTrayIcon();

            this.WindowState = WindowState.Minimized;
            this.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// создание трей иконки
        /// </summary>
        /// <returns></returns>
        private bool createTrayIcon()
        {
            bool result = false;
            if (this.TrayIcon == null)
            {
                this.TrayIcon = new NotifyIcon();
                this.TrayIcon.Icon = new System.Drawing.Icon("setup.ico");

                this.TrayMenu = this.Resources["TrayMenu"] as System.Windows.Controls.ContextMenu;

                this.TrayIcon.Click += delegate (object sender, EventArgs e)
                {
                    if ((e as MouseEventArgs).Button
                    == MouseButtons.Left)
                        this.ShowHideMainWindow(sender, null);
                    else
                    {
                        this.TrayMenu.IsOpen = true;
                        this.Activate();
                    }
                };

                result = true;
            }
            else result = true;

            this.TrayIcon.Visible = true;

            return result;
        }

        /// <summary>
        /// скрытие главного кона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowHideMainWindow(object sender, RoutedEventArgs e)
        {
            this.TrayMenu.IsOpen = false;
            if (this.IsVisible)
            {
                this.Hide();

                (this.TrayMenu.Items[0] as System.Windows.Controls.MenuItem).Header = "Show";
            }
            else
            {
                this.Show();

                (this.TrayMenu.Items[0] as System.Windows.Controls.MenuItem).Header = "Hide";
                this.WindowState = this.CurrentWindowState;
                this.Activate();
            }
        }

        /// <summary>
        /// статус главного окна
        /// </summary>
        public WindowState CurrentWindowState
        {
            get { return this.fCurrentWindowState; }
            set { this.fCurrentWindowState = value; }
        }

        /// <summary>
        /// если статус был измненен
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();

                (this.TrayMenu.Items[0] as System.Windows.Controls.MenuItem).Header = "Show";
            }

            else this.CurrentWindowState = this.WindowState;
        }

        /// <summary>
        /// окно может быть закрыто
        /// </summary>
        public bool CanClose
        {
            get { return this.fCanClose; }
            set { this.fCanClose = value; }
        }

        /// <summary>
        /// если окно было закрыто
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!this.CanClose)
            {
                e.Cancel = true;

                this.CurrentWindowState = this.WindowState;

                (this.TrayMenu.Items[0] as System.Windows.Controls.MenuItem).Header = "Show";

                this.Hide();
            }
            else
            {
                this.TrayIcon.Visible = false;
            }
        }

        /// <summary>
        /// если была нажата кнопка выхода в меню
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuExitClick(object sender, RoutedEventArgs e)
        {
            this.CanClose = true;

            this.Close();
        }
    }
}
/*Написать программу, менеджер процессов, который должен:
1) Отображать максимально подробную информацию о процессах
(смотреть все свойства класса Process в конспекте)
2) Настройки, с помощью которых пользователь может указать процессы, 
которые необходимо всегда завершать. 
При завершении должно выводиться сообщение в правом нижнем углу монитора.
3) Запускать программу
4) Завершать программу
5) Сворачиваться в трей
Должен быть красивый, интуитивно понятный интерфейс.
Предусмотреть на будущее в своей прогамме очистку диска от мусора, 
редактор реестра и оптимизатор реестра, это у нас будет в будущем.
Обязательно соблюдать все правила кодирования.*/