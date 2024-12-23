using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ryba
{
    public partial class MainWindow : Window
    {
        string FilePath;
        int[] temperatures;

        public MainWindow()
        {
            InitializeComponent();
            FishKindSel.Items.Add("Сёмга");
            FishKindSel.Items.Add("Минтай");
        }

        private void LoadDataBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл",
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                FilePath = openFileDialog.FileName;
            }
            ReadData();
        }

        public void ReadData()
        {
            DateTime startDateTime;
            if (!File.Exists(FilePath))
            {
                MessageBox.Show("Ошибка: файл не найден.");
                return;
            }
            string[] fileLines = File.ReadAllLines(FilePath);
            if (fileLines.Length != 2)
            {
                MessageBox.Show("Ошибка: файл должен содержать ровно две строки.");
                return;
            }
            string[] temperatureStrings = fileLines[1].Split(',');
            try
            {
                temperatures = temperatureStrings.Select(temp => int.Parse(temp.Trim())).ToArray();
            }
            catch (FormatException)
            {
                MessageBox.Show("Ошибка: вторая строка должна содержать целые числа");
                return;
            }

            startDateTime = DateTime.Parse(fileLines[0]);
            ShipmentDateTxt.Text = startDateTime.ToString("dd.MM.yyyy HH:mm");
            TempChangeTxt.Text = string.Join(", ", temperatures);
        }

        private void FishKindSel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FishKindSel.SelectedIndex == 0) // Сёмга
            {
                MaxTempTxt.Text = "5";
                MinTempTxt.Text = "-3";
                OvercoldTimeTxt.Text = "60"; // минуты
                OverheatTimeTxt.Text = "20"; // минуты
            }
            if (FishKindSel.SelectedIndex == 1) // Минтай
            {
                MaxTempTxt.Text = "-5";
                MinTempTxt.Text = "-180";
                OvercoldTimeTxt.Text = "5"; // минуты
                OverheatTimeTxt.Text = "120"; // минуты
            }
        }

        private void CheckBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int maxTemp = int.Parse(MaxTempTxt.Text);
                int minTemp = int.Parse(MinTempTxt.Text);
                int maxTempTime = int.Parse(OverheatTimeTxt.Text);
                int minTempTime = int.Parse(OvercoldTimeTxt.Text);
                DateTime shipmentDt = DateTime.Parse(ShipmentDateTxt.Text);
                int OverheatTime = 0;
                int OvercoldTime = 0;
                DateTime curTime = shipmentDt;

                if (maxTemp < minTemp)
                {
                    int buffer = maxTemp;
                    maxTemp = minTemp;
                    minTemp = buffer;
                    buffer = maxTempTime;
                    maxTempTime = minTempTime;
                    minTempTime = buffer;
                }

                string res = $"Дата отгрузки: {shipmentDt.ToString("dd.MM.yyyy HH:mm")}\n";
                foreach (int t in temperatures)
                {
                    curTime = curTime.AddMinutes(10);
                    if (t > maxTemp)
                    {
                        OverheatTime += 10;
                        res += $"Текущее время: {curTime.ToString("dd.MM.yyyy HH:mm")}; Превышение максимальной температуры: {t - maxTemp}\n";
                    }
                    else if (t < minTemp)
                    {
                        OvercoldTime += 10;
                        res += $"Текущее время: {curTime.ToString("dd.MM.yyyy HH:mm")}; Понижение минимальной температуры: {minTemp - t}\n";
                    }
                }

                if (OverheatTime > maxTempTime)
                {
                    res += $"Внимание: Превышение максимальной температуры произошло более чем на {maxTempTime} минут!\n";
                }

                if (OvercoldTime > minTempTime)
                {
                    res += $"Внимание: Понижение минимальной температуры произошло более чем на {minTempTime} минут!\n";
                }

                // Обновляем текст в TextBlock для отчета
                ReportTextBlock.Text = res;
            }
            catch (Exception ex)
            {
                ReportTextBlock.Text = "Ошибка: " + ex.Message;
            }
        }

        private void ReportTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Копируем текст отчета в буфер обмена
            Clipboard.SetText(ReportTextBlock.Text);
            MessageBox.Show("Текст отчета скопирован в буфер обмена.");
        }

        private void ReportTextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            // Изменяем цвет текста, чтобы он выглядел как ссылка
            ReportTextBlock.TextDecorations = TextDecorations.Underline;
            ReportTextBlock.Foreground = new SolidColorBrush(Colors.Blue);
        }

        private void ReportTextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            // Возвращаем цвет текста и отменяем подчеркивание
            ReportTextBlock.TextDecorations = null;
            ReportTextBlock.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Проверка, что введенный текст является числом
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static readonly char[] allowedChars = "0123456789-".ToCharArray();

        private static bool IsTextAllowed(string text)
        {
            foreach (char c in text)
            {
                if (!allowedChars.Contains(c))
                    return false;
            }
            return true;
        }

        private void SaveReportBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Сохранить отчет",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = "Отчет.txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, ReportTextBlock.Text);
                    MessageBox.Show("Отчет успешно сохранен!", "Сохранение отчета", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}