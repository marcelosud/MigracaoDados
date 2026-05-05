using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using MigracaoDados.Application.UseCases;
using MigracaoDados.Domain.Importacao;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MigracaoDados.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ValidarCsvUseCase _validarCsvUseCase;
    private readonly string _schemaPath;

    public MainViewModel(
        ValidarCsvUseCase validarCsvUseCase,
        IConfiguration configuration)
    {
        _validarCsvUseCase = validarCsvUseCase;
        _schemaPath = Path.GetFullPath(configuration["AppSettings:DefaultSchemaPath"] ?? "LayoutTemplates/Contrato.xlsx");

        SelecionarArquivoCommand = new RelayCommand(SelecionarArquivo);
        ValidarCsvCommand = new RelayCommand(async () => await ValidarCsvAsync(), PodeValidarCsv);
    }

    private string _mensagem = "Selecione o arquivo Contrato.csv para validar contra o layout Contrato.xlsx.";
    public string Mensagem
    {
        get => _mensagem;
        set
        {
            _mensagem = value;
            OnPropertyChanged();
        }
    }

    private string _csvFilePath = string.Empty;
    public string CsvFilePath
    {
        get => _csvFilePath;
        set
        {
            _csvFilePath = value;
            OnPropertyChanged();
            ValidarCsvCommand.RaiseCanExecuteChanged();
        }
    }

    private string _status = "Aguardando arquivo.";
    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    private int _totalRows;
    public int TotalRows
    {
        get => _totalRows;
        set
        {
            _totalRows = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand SelecionarArquivoCommand { get; }
    public RelayCommand ValidarCsvCommand { get; }
    public ObservableCollection<CsvValidationErrorItemViewModel> Erros { get; } = new();

    private void SelecionarArquivo()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Arquivos CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*",
            Title = "Selecionar arquivo CSV"
        };

        if (dialog.ShowDialog() == true)
        {
            CsvFilePath = dialog.FileName;
            Status = "Arquivo selecionado. Pronto para validar.";
            Erros.Clear();
            TotalRows = 0;
        }
    }

    private bool PodeValidarCsv()
    {
        return !string.IsNullOrWhiteSpace(CsvFilePath) && File.Exists(CsvFilePath);
    }

    private async Task ValidarCsvAsync()
    {
        try
        {
            Status = "Validando CSV...";
            Erros.Clear();

            var result = await _validarCsvUseCase.ExecutarAsync(CsvFilePath, _schemaPath);
            TotalRows = result.TotalRows;

            foreach (var error in result.Errors)
            {
                Erros.Add(CsvValidationErrorItemViewModel.From(error));
            }

            Status = result.IsValid
                ? $"CSV valido. {result.TotalRows} linha(s) pronta(s) para a proxima etapa."
                : $"CSV invalido. {result.Errors.Count} erro(s) encontrado(s).";
        }
        catch (Exception exception)
        {
            Status = $"Falha ao validar CSV: {exception.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public sealed class CsvValidationErrorItemViewModel
{
    public required int Linha { get; init; }
    public required string Coluna { get; init; }
    public required string ValorInformado { get; init; }
    public required string Mensagem { get; init; }
    public required string Tipo { get; init; }

    public static CsvValidationErrorItemViewModel From(CsvValidationError error)
    {
        return new CsvValidationErrorItemViewModel
        {
            Linha = error.RowNumber,
            Coluna = error.ColumnName,
            ValorInformado = error.ProvidedValue,
            Mensagem = error.Message,
            Tipo = error.Type.ToString()
        };
    }
}
