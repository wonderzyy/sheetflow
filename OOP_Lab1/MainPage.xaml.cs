
using OOP_Lab1.Models;
using OOP_Lab1.Services;
using Cell = OOP_Lab1.Models.Cell;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Storage;

namespace OOP_Lab1;

public partial class MainPage : ContentPage
{
    private Spreadsheet spreadsheet = new(8, 6);
    private bool showValues;
    private string selectedCellAddress = "A1";

    private readonly Dictionary<string, Entry> cellEntries = new();

    private readonly GoogleDriveService googleDrive = new();

    public MainPage()
    {
        InitializeComponent();
        BuildTable();
    }

    private void BuildTable()
    {
        SheetGrid.Children.Clear();
        cellEntries.Clear();
        SheetGrid.RowDefinitions.Clear();
        SheetGrid.ColumnDefinitions.Clear();

        SheetGrid.RowSpacing = 4;
        SheetGrid.ColumnSpacing = 4;

        SheetGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = new GridLength(50)
            });

        for (int col = 0; col < spreadsheet.ColumnCount; col++)
        {
            SheetGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(145)
                });
        }

        for (int row = 0; row <= spreadsheet.RowCount; row++)
        {
            SheetGrid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(46)
                });
        }

        for (int col = 0; col < spreadsheet.ColumnCount; col++)
        {
            AddElement(
                CreateHeader(Spreadsheet.GetColumnName(col)),
                col + 1,
                0);
        }

        for (int row = 0; row < spreadsheet.RowCount; row++)
        {
            AddElement(
                CreateHeader((row + 1).ToString()),
                0,
                row + 1);

            for (int col = 0; col < spreadsheet.ColumnCount; col++)
            {
                Cell cell = spreadsheet.GetCell(row, col);

                if (showValues)
                {
                    AddElement(
                        CreateValueLabel(cell),
                        col + 1,
                        row + 1);
                }
                else
                {
                    AddElement(
                        CreateCellEntry(cell),
                        col + 1,
                        row + 1);
                }
            }
        }

        SelectedCellLabel.Text = selectedCellAddress;
        FormulaInput.IsEnabled = !showValues;
        FormulaInput.Text = spreadsheet.GetCell(selectedCellAddress).Expression;
    }


    private Entry CreateCellEntry(Cell cell)
    {
        var entry = new Entry
        {
            Text = cell.Expression,
            FontSize = 14,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#383838"),
            Margin = new Thickness(1)
        };

        cellEntries[cell.Address] = entry;

        entry.Focused += (sender, args) =>
        {
            selectedCellAddress = cell.Address;
            SelectedCellLabel.Text = cell.Address;
            FormulaInput.Text = cell.Expression;
        };

        entry.TextChanged += (sender, args) =>
        {
            cell.Expression = args.NewTextValue ?? "";
            cell.Error = null;

            if (selectedCellAddress == cell.Address &&
                FormulaInput.Text != cell.Expression)
            {
                FormulaInput.Text = cell.Expression;
            }
        };

        return entry;
    }


    private Label CreateValueLabel(Cell cell)
    {
        string text = "";
        Color textColor = Colors.White;

        if (!string.IsNullOrWhiteSpace(cell.Expression))
        {
            try
            {
                text = spreadsheet.EvaluateCell(cell.Address).ToString();
                cell.Error = null;
            }
            catch (Exception ex)
            {
                cell.Error = ex.Message;
                text = "Помилка";
                textColor = Color.FromArgb("#383838");
            }
        }

        var label = new Label
        {
            Text = text,
            FontSize = 14,
            TextColor = textColor,
            BackgroundColor = Color.FromArgb("#162238"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        if (cell.Error is string error)
        {
            label.GestureRecognizers.Add(
                new TapGestureRecognizer
                {
                    Command = new Command(async () =>
                    {
                        await DisplayAlert(
                            $"Помилка: {cell.Address}",
                            error,
                            "OK");
                    })
                });
        }

        return label;
    }


    private Label CreateHeader(string text)
    {
        return new Label
        {
            Text = text,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#CBD5E1"),
            BackgroundColor = Color.FromArgb("#505050"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
    }

    private void AddElement(View element, int column, int row)
    {
        Grid.SetColumn(element, column);
        Grid.SetRow(element, row);
        SheetGrid.Children.Add(element);
    }

    private void AddRow_Clicked(object sender, EventArgs e)
    {
        spreadsheet.AddRow();
        BuildTable();
    }

    private void AddColumn_Clicked(object sender, EventArgs e)
    {
        spreadsheet.AddColumn();
        BuildTable();
    }

    private void DeleteRow_Clicked(object sender, EventArgs e)
    {
        spreadsheet.DeleteRow();
        selectedCellAddress = "A1";
        BuildTable();
    }

    private void DeleteColumn_Clicked(object sender, EventArgs e)
    {
        spreadsheet.DeleteColumn();
        selectedCellAddress = "A1";
        BuildTable();
    }

    private void ViewModeButton_Clicked(object sender, EventArgs e)
    {
        showValues = !showValues;

        ViewModeButton.Text = showValues
            ? "Показати вирази"
            : "Показати значення";

        BuildTable();
    }

    private async void ValidateButton_Clicked(object sender, EventArgs e)
    {
        var errors = new List<string>();
        int checkedCells = 0;

        for (int row = 0; row < spreadsheet.RowCount; row++)
        {
            for (int col = 0; col < spreadsheet.ColumnCount; col++)
            {
                Cell cell = spreadsheet.GetCell(row, col);
                cell.Error = null;

                if (string.IsNullOrWhiteSpace(cell.Expression))
                    continue;

                checkedCells++;

                try
                {
                    spreadsheet.ValidateSyntax(cell.Address);
                }
                catch (Exception ex)
                {
                    cell.Error = ex.Message;
                    errors.Add($"{cell.Address}: {ex.Message}");
                }
            }
        }

        string message = errors.Count == 0
            ? $"Перевірено клітинок: {checkedCells}. Синтаксичних помилок немає."
            : $"Знайдено помилок: {errors.Count}\n\n" +
              string.Join("\n", errors.Take(5));

        await DisplayAlert(
            "Перевірка синтаксису",
            message,
            "OK");
    }


    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            string? fileName = await DisplayPromptAsync(
                "Збереження таблиці",
                "Введіть назву файлу:",
                "Зберегти",
                "Скасувати",
                initialValue: "SheetFlow.json");

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            fileName = fileName.Trim();

            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                fileName += ".json";

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                await DisplayAlert(
                    "Помилка",
                    "Назва файлу містить недопустимі символи.",
                    "OK");
                return;
            }

            using var stream = SpreadsheetStorage.CreateStream(spreadsheet);

            var result = await FileSaver.Default.SaveAsync(
                fileName,
                stream,
                CancellationToken.None);

            if (result.IsSuccessful)
            {
                await DisplayAlert(
                    "Успішно",
                    $"Таблицю збережено:\n{result.FilePath}",
                    "OK");
            }
            else if (result.Exception is not OperationCanceledException)
            {
                await DisplayAlert(
                    "Помилка",
                    result.Exception?.Message ?? "Не вдалося зберегти файл.",
                    "OK");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Помилка збереження",
                ex.Message,
                "OK");
        }
    }







    private async void OpenButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            var fileTypes = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                { DevicePlatform.WinUI, new[] { ".json" } }
                });

            var options = new PickOptions
            {
                PickerTitle = "Виберіть файл таблиці",
                FileTypes = fileTypes
            };

            var file = await FilePicker.Default.PickAsync(options);

            if (file is null)
                return;

            using var stream = await file.OpenReadAsync();

            Spreadsheet loadedSpreadsheet =
                await SpreadsheetStorage.LoadAsync(stream);

            spreadsheet = loadedSpreadsheet;
            showValues = false;

            ViewModeButton.Text = "Показати значення";

            selectedCellAddress = "A1";
            BuildTable();

            await DisplayAlert(
                "Успішно",
                $"Завантажено таблицю: {file.FileName}",
                "OK");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Помилка відкриття",
                ex.Message,
                "OK");
        }
    }


    private async void HelpButton_Clicked(object sender, EventArgs e)
    {
        string message =
            "SheetFlow — електронна таблиця.\n\n" +
            "Лабораторна робота №1. Варіант 72.\n\n" +
            "Підтримувані операції:\n" +
            "^, =, <, >, not, and, or\n\n" +
            "Функції:\n" +
            "mmax(x1, ..., xN)\n" +
            "mmin(x1, ..., xN)\n\n" +
            "Приклади:\n" +
            "2^3\n" +
            "mmax(5, 10, 20)\n" +
            "A1 > B1\n" +
            "not (A1 > 10)\n\n" +
            "Введіть вираз у клітинку та натисніть " +
            "«Показати значення» для обчислення.";

        await DisplayAlert(
            "Довідка",
            message,
            "OK");
    }
    private void FormulaInput_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (showValues)
            return;

        Cell cell = spreadsheet.GetCell(selectedCellAddress);

        cell.Expression = e.NewTextValue ?? "";
        cell.Error = null;

        if (cellEntries.TryGetValue(cell.Address, out Entry? entry) &&
            entry.Text != cell.Expression)
        {
            entry.Text = cell.Expression;
        }
    }


    private async void ConnectGoogleDrive_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (googleDrive.IsConnected)
            {
                await DisplayAlert(
                    "Google Drive",
                    "Google Drive вже підключено.",
                    "OK");
                return;
            }

            string credentialsPath = Path.Combine(
                FileSystem.AppDataDirectory,
                "google_client.json");

            if (File.Exists(credentialsPath))
            {
                await googleDrive.ConnectAsync();
            }
            else
            {
                var file = await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle = "Виберіть файл OAuth JSON"
                    });

                if (file is null)
                    return;

                if (!file.FileName.EndsWith(
                    ".json",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException(
                        "Необхідно вибрати JSON-файл");
                }

                using var stream = await file.OpenReadAsync();

                await googleDrive.ConnectAsync(stream);
            }

            await DisplayAlert(
                "Google Drive",
                "Підключення успішне!",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Помилка підключення",
                ex.Message,
                "OK");
        }
    }


    private async void SaveGoogleDrive_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!googleDrive.IsConnected)
            {
                await DisplayAlert(
                    "Google Drive",
                    "Спочатку підключіть Google Drive.",
                    "OK");
                return;
            }

            string? fileName = await DisplayPromptAsync(
                "Збереження в Google Drive",
                "Введіть назву таблиці:",
                "Зберегти",
                "Скасувати",
                initialValue: "SheetFlow.json");

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            fileName = fileName.Trim();

            if (!fileName.EndsWith(
                ".json",
                StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".json";
            }

            string fileId = await googleDrive.UploadTableAsync(
                spreadsheet,
                fileName);

            await DisplayAlert(
                "Успішно",
                $"Файл {fileName} збережено в Google Drive.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Помилка збереження",
                ex.Message,
                "OK");
        }
    }


    private async void OpenGoogleDrive_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!googleDrive.IsConnected)
            {
                await DisplayAlert(
                    "Google Drive",
                    "Спочатку підключіть Google Drive.",
                    "OK");
                return;
            }

            var files = await googleDrive.ListTablesAsync();

            if (files.Count == 0)
            {
                await DisplayAlert(
                    "Google Drive",
                    "Таблиць не знайдено.",
                    "OK");
                return;
            }

            string[] options = files
                .Select((file, index) => $"{index + 1}. {file.Name}")
                .ToArray();

            string? selected = await DisplayActionSheet(
                "Виберіть таблицю",
                "Скасувати",
                null,
                options);

            int index = Array.IndexOf(options, selected);

            if (index < 0)
                return;

            var loadedSpreadsheet =
                await googleDrive.DownloadTableAsync(files[index].Id);

            spreadsheet = loadedSpreadsheet;
            selectedCellAddress = "A1";
            showValues = false;

            ViewModeButton.Text = "Показати значення";

            BuildTable();

            await DisplayAlert(
                "Google Drive",
                "Таблицю успішно відкрито.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Помилка відкриття",
                ex.Message,
                "OK");
        }
    }



}
