using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using MigracaoDados.Application.UseCases;
using MigracaoDados.Domain.Importacao;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MigracaoDados.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ValidarCsvUseCase _validarCsvUseCase;
    private readonly string _layoutTemplatesPath;

    public MainViewModel(
        ValidarCsvUseCase validarCsvUseCase,
        TestarConexaoBancoDadosUseCase testarConexaoBancoDadosUseCase,
        SalvarConexaoBancoDadosUseCase salvarConexaoBancoDadosUseCase,
        ObterConexaoBancoDadosUseCase obterConexaoBancoDadosUseCase,
        IConfiguration configuration)
    {
        _validarCsvUseCase = validarCsvUseCase;
        _layoutTemplatesPath = Path.GetFullPath(configuration["AppSettings:LayoutTemplatesPath"] ?? "LayoutTemplates");

        AbrirValidacaoCommand = new RelayCommand(AbrirValidacao);
        AbrirConexoesBancoDadosCommand = new RelayCommand(AbrirConexoesBancoDados);
        SelecionarPastaCommand = new RelayCommand(SelecionarPasta);
        ValidarArquivosCommand = new RelayCommand(async () => await ValidarArquivosAsync(), PodeValidarArquivos);
        DestinoConnection = new DatabaseConnectionViewModel(
            "Destino",
            "Banco de Dados de Destino",
            testarConexaoBancoDadosUseCase,
            salvarConexaoBancoDadosUseCase,
            obterConexaoBancoDadosUseCase);
        OrigemConnection = new DatabaseConnectionViewModel(
            "Origem",
            "Banco de Dados de Origem",
            testarConexaoBancoDadosUseCase,
            salvarConexaoBancoDadosUseCase,
            obterConexaoBancoDadosUseCase);

        Arquivos.Add(new CsvValidationFileItemViewModel("Contrato.csv", "Contrato.xlsx"));
        Arquivos.Add(new CsvValidationFileItemViewModel("Prestacao.csv", "Prestacoes.xlsx"));
        Arquivos.Add(new CsvValidationFileItemViewModel("Pagamento.csv", "Pagamentos.xlsx"));
        Arquivos.Add(new CsvValidationFileItemViewModel("Liberacao.csv", "Liberacoes.xlsx"));

        ProgressMaximum = Arquivos.Count;
        SelectedArquivo = Arquivos.FirstOrDefault();
        AbrirValidacao();
    }

    private string _mensagem = "Selecione a pasta onde estão os arquivos CSV para iniciar a validação sequencial.";
    public string Mensagem
    {
        get => _mensagem;
        set
        {
            _mensagem = value;
            OnPropertyChanged();
        }
    }

    private string _csvFolderPath = string.Empty;
    public string CsvFolderPath
    {
        get => _csvFolderPath;
        set
        {
            _csvFolderPath = value;
            OnPropertyChanged();
            ValidarArquivosCommand.RaiseCanExecuteChanged();
        }
    }

    private string _status = "Aguardando pasta dos CSVs.";
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

    private bool _isValidating;
    public bool IsValidating
    {
        get => _isValidating;
        set
        {
            _isValidating = value;
            OnPropertyChanged();
            ValidarArquivosCommand.RaiseCanExecuteChanged();
        }
    }

    private int _progressValue;
    public int ProgressValue
    {
        get => _progressValue;
        set
        {
            _progressValue = value;
            OnPropertyChanged();
        }
    }

    private int _progressMaximum;
    public int ProgressMaximum
    {
        get => _progressMaximum;
        set
        {
            _progressMaximum = value;
            OnPropertyChanged();
        }
    }

    private string _progressText = "0 de 4 arquivo(s) validado(s).";
    public string ProgressText
    {
        get => _progressText;
        set
        {
            _progressText = value;
            OnPropertyChanged();
        }
    }

    private CsvValidationFileItemViewModel? _selectedArquivo;
    public CsvValidationFileItemViewModel? SelectedArquivo
    {
        get => _selectedArquivo;
        set
        {
            _selectedArquivo = value;
            OnPropertyChanged();
            AtualizarErrosSelecionados();
        }
    }

    public RelayCommand SelecionarPastaCommand { get; }
    public RelayCommand ValidarArquivosCommand { get; }
    public RelayCommand AbrirValidacaoCommand { get; }
    public RelayCommand AbrirConexoesBancoDadosCommand { get; }
    public DatabaseConnectionViewModel DestinoConnection { get; }
    public DatabaseConnectionViewModel OrigemConnection { get; }
    public ObservableCollection<CsvValidationFileItemViewModel> Arquivos { get; } = new();
    public ObservableCollection<CsvValidationErrorItemViewModel> Erros { get; } = new();

    private Visibility _validationViewVisibility;
    public Visibility ValidationViewVisibility
    {
        get => _validationViewVisibility;
        set
        {
            _validationViewVisibility = value;
            OnPropertyChanged();
        }
    }

    private Visibility _databaseConnectionsViewVisibility;
    public Visibility DatabaseConnectionsViewVisibility
    {
        get => _databaseConnectionsViewVisibility;
        set
        {
            _databaseConnectionsViewVisibility = value;
            OnPropertyChanged();
        }
    }

    private void AbrirValidacao()
    {
        ValidationViewVisibility = Visibility.Visible;
        DatabaseConnectionsViewVisibility = Visibility.Collapsed;
    }

    private void AbrirConexoesBancoDados()
    {
        ValidationViewVisibility = Visibility.Collapsed;
        DatabaseConnectionsViewVisibility = Visibility.Visible;
    }

    private void SelecionarPasta()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Selecionar pasta dos arquivos CSV"
        };

        if (dialog.ShowDialog() == true)
        {
            CsvFolderPath = dialog.FolderName;
            Status = "Pasta selecionada. Pronto para validar os arquivos.";
            ResetarValidacao();
        }
    }

    private bool PodeValidarArquivos()
    {
        return !IsValidating
            && !string.IsNullOrWhiteSpace(CsvFolderPath)
            && Directory.Exists(CsvFolderPath);
    }

    private async Task ValidarArquivosAsync()
    {
        if (!PodeValidarArquivos())
        {
            return;
        }

        IsValidating = true;
        ResetarValidacao();

        try
        {
            for (var index = 0; index < Arquivos.Count; index++)
            {
                var arquivo = Arquivos[index];
                SelectedArquivo = arquivo;
                ProgressValue = index;
                ProgressText = $"Validando {index + 1} de {Arquivos.Count}: {arquivo.CsvFileName}";

                var csvPath = Path.Combine(CsvFolderPath, arquivo.CsvFileName);
                var schemaPath = Path.Combine(_layoutTemplatesPath, arquivo.SchemaFileName);

                if (!File.Exists(csvPath))
                {
                    arquivo.MarcarErro("Arquivo nao encontrado.", 0);
                    ProgressText = $"{ProgressValue} de {ProgressMaximum} arquivo(s) validado(s).";
                    Status = $"{arquivo.CsvFileName} nao encontrado. A validação foi interrompida.";
                    return;
                }

                arquivo.MarcarValidando();
                Status = $"Validando {arquivo.CsvFileName}...";

                var result = await _validarCsvUseCase.ExecutarAsync(csvPath, schemaPath);
                arquivo.TotalRows = result.TotalRows;
                arquivo.Errors.Clear();

                foreach (var error in result.Errors)
                {
                    arquivo.Errors.Add(CsvValidationErrorItemViewModel.From(error));
                }

                AtualizarErrosSelecionados();

                if (!result.IsValid)
                {
                    arquivo.MarcarErro($"{result.Errors.Count} erro(s) encontrado(s).", result.TotalRows);
                    ProgressText = $"{ProgressValue} de {ProgressMaximum} arquivo(s) validado(s).";
                    Status = $"{arquivo.CsvFileName} inválido. Corrija o arquivo antes de validar os próximos.";
                    return;
                }

                arquivo.MarcarSucesso(result.TotalRows);
                ProgressValue = index + 1;
                ProgressText = $"{ProgressValue} de {ProgressMaximum} arquivo(s) validado(s).";
            }

            Status = "Todos os arquivos CSV foram validados com sucesso.";
        }
        catch (Exception exception)
        {
            if (SelectedArquivo is not null)
            {
                SelectedArquivo.MarcarErro("Falha na validação.", SelectedArquivo.TotalRows);
            }

            Status = $"Falha ao validar CSV: {exception.Message}";
        }
        finally
        {
            IsValidating = false;
        }
    }

    private void ResetarValidacao()
    {
        foreach (var arquivo in Arquivos)
        {
            arquivo.Resetar();
        }

        SelectedArquivo = Arquivos.FirstOrDefault();
        Erros.Clear();
        TotalRows = 0;
        ProgressValue = 0;
        ProgressText = $"0 de {ProgressMaximum} arquivo(s) validado(s).";
    }

    private void AtualizarErrosSelecionados()
    {
        Erros.Clear();

        if (SelectedArquivo is null)
        {
            TotalRows = 0;
            return;
        }

        TotalRows = SelectedArquivo.TotalRows;

        foreach (var error in SelectedArquivo.Errors)
        {
            Erros.Add(error);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public sealed class CsvValidationFileItemViewModel : INotifyPropertyChanged
{
    public CsvValidationFileItemViewModel(string csvFileName, string schemaFileName)
    {
        CsvFileName = csvFileName;
        SchemaFileName = schemaFileName;
        Nome = Path.GetFileNameWithoutExtension(csvFileName);
        Resetar();
    }

    public string Nome { get; }
    public string CsvFileName { get; }
    public string SchemaFileName { get; }
    public ObservableCollection<CsvValidationErrorItemViewModel> Errors { get; } = new();

    private string _icone = string.Empty;
    public string Icone
    {
        get => _icone;
        private set
        {
            _icone = value;
            OnPropertyChanged();
        }
    }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        private set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    private string _detalhe = string.Empty;
    public string Detalhe
    {
        get => _detalhe;
        private set
        {
            _detalhe = value;
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

    public void Resetar()
    {
        Errors.Clear();
        TotalRows = 0;
        Icone = "-";
        Status = "Pendente";
        Detalhe = "Aguardando validação";
    }

    public void MarcarValidando()
    {
        Icone = "...";
        Status = "Validando";
        Detalhe = "Em andamento";
    }

    public void MarcarSucesso(int totalRows)
    {
        TotalRows = totalRows;
        Icone = "✓";
        Status = "Concluído";
        Detalhe = $"{totalRows} linha(s) lida(s)";
    }

    public void MarcarErro(string detalhe, int totalRows)
    {
        TotalRows = totalRows;
        Icone = "X";
        Status = "Com erro";
        Detalhe = detalhe;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
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
            Tipo = error.Type.GetDescription()
        };
    }
}
