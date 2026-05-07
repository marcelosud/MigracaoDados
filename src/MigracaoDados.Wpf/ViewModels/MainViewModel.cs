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
        ExcluirConexaoBancoDadosUseCase excluirConexaoBancoDadosUseCase,
        IConfiguration configuration)
    {
        _validarCsvUseCase = validarCsvUseCase;
        _layoutTemplatesPath = Path.GetFullPath(configuration["AppSettings:LayoutTemplatesPath"] ?? "LayoutTemplates");

        AbrirValidacaoCommand = new RelayCommand(AbrirValidacao);
        AbrirConexoesBancoDadosCommand = new RelayCommand(AbrirConexoesBancoDados);
        SelecionarPastaCommand = new RelayCommand(SelecionarPasta);
        ValidarArquivosCommand = new RelayCommand(async () => await ValidarArquivosAsync(), PodeValidarArquivos);
        IniciarConexaoDestinoCommand = new RelayCommand(async () => await IniciarConexaoDestinoAsync(), PodeIniciarConexaoDestino);
        ConfirmarConexaoDestinoPopupCommand = new RelayCommand(async () => await ConfirmarConexaoDestinoPopupAsync(), PodeConfirmarConexaoDestinoPopup);
        CancelarConexaoDestinoPopupCommand = new RelayCommand(CancelarConexaoDestinoPopup);
        ExecutarStoredProceduresCommand = new RelayCommand(PrepararStoredProcedures, PodeExecutarStoredProcedures);
        DestinoConnection = new DatabaseConnectionViewModel(
            "Destino",
            "Banco de Dados de Destino",
            testarConexaoBancoDadosUseCase,
            salvarConexaoBancoDadosUseCase,
            obterConexaoBancoDadosUseCase,
            excluirConexaoBancoDadosUseCase);
        DestinoConnection.PropertyChanged += OnDestinoConnectionPropertyChanged;
        OrigemConnection = new DatabaseConnectionViewModel(
            "Origem",
            "Banco de Dados de Origem",
            testarConexaoBancoDadosUseCase,
            salvarConexaoBancoDadosUseCase,
            obterConexaoBancoDadosUseCase,
            excluirConexaoBancoDadosUseCase);

        Arquivos.Add(new CsvValidationFileItemViewModel("Contrato.csv", "Contrato.xlsx"));
        Arquivos.Add(new CsvValidationFileItemViewModel("Prestacao.csv", "Prestacoes.xlsx"));
        Arquivos.Add(new CsvValidationFileItemViewModel("Pagamento.csv", "Pagamentos.xlsx"));
        Arquivos.Add(new CsvValidationFileItemViewModel("Liberacao.csv", "Liberacoes.xlsx"));

        ProgressMaximum = Arquivos.Count;
        SelectedArquivo = Arquivos.FirstOrDefault();
        ResetarEtapas();
        AbrirValidacao();
    }

    private string _mensagem =
    "Siga as orientações abaixo do assistente de migração de dados.\n" +
    "Etapa 1: Identificação e Validação dos arquivos CSVs.\n" +
    "Etapa 2: Conexão com o Banco de Dados de Destino.\n" +
    "Etapa 3: Migração para o Banco de Dados de Destino.";

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
    public RelayCommand IniciarConexaoDestinoCommand { get; }
    public RelayCommand ConfirmarConexaoDestinoPopupCommand { get; }
    public RelayCommand CancelarConexaoDestinoPopupCommand { get; }
    public RelayCommand ExecutarStoredProceduresCommand { get; }
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

    private Visibility _destinationConnectionPopupVisibility = Visibility.Collapsed;
    public Visibility DestinationConnectionPopupVisibility
    {
        get => _destinationConnectionPopupVisibility;
        set
        {
            _destinationConnectionPopupVisibility = value;
            OnPropertyChanged();
        }
    }

    private bool _isConnectingDestination;
    public bool IsConnectingDestination
    {
        get => _isConnectingDestination;
        set
        {
            _isConnectingDestination = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDatabaseConnectionStepEnabled));
            IniciarConexaoDestinoCommand.RaiseCanExecuteChanged();
            ConfirmarConexaoDestinoPopupCommand.RaiseCanExecuteChanged();
        }
    }

    private bool _destinationConnectionReady;
    public bool DestinationConnectionReady
    {
        get => _destinationConnectionReady;
        set
        {
            _destinationConnectionReady = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsStoredProceduresStepEnabled));
            ExecutarStoredProceduresCommand.RaiseCanExecuteChanged();
        }
    }

    public bool AreCsvFilesValid => Arquivos.Count > 0 && Arquivos.All(arquivo => arquivo.IsValid);
    public bool IsDatabaseConnectionStepEnabled => AreCsvFilesValid && !DestinationConnectionReady && !IsConnectingDestination;
    public bool IsStoredProceduresStepEnabled => DestinationConnectionReady;

    public string DatabaseConnectionStepTitle =>
        string.IsNullOrWhiteSpace(DestinoConnection.Server)
            ? "Etapa 2 - Conexão com o Banco de Destino"
            : $"Etapa 2 - Conexão com o Banco de Destino ({DestinoConnection.Server})";

    private string _csvValidationStepState = "Pendente";
    public string CsvValidationStepState
    {
        get => _csvValidationStepState;
        set
        {
            _csvValidationStepState = value;
            OnPropertyChanged();
        }
    }

    private string _csvValidationStepIcon = "-";
    public string CsvValidationStepIcon
    {
        get => _csvValidationStepIcon;
        set
        {
            _csvValidationStepIcon = value;
            OnPropertyChanged();
        }
    }

    private string _csvValidationStepDetail = "Aguardando validação dos arquivos CSV.";
    public string CsvValidationStepDetail
    {
        get => _csvValidationStepDetail;
        set
        {
            _csvValidationStepDetail = value;
            OnPropertyChanged();
        }
    }

    private string _databaseConnectionStepState = "Pendente";
    public string DatabaseConnectionStepState
    {
        get => _databaseConnectionStepState;
        set
        {
            _databaseConnectionStepState = value;
            OnPropertyChanged();
        }
    }

    private string _databaseConnectionStepIcon = "-";
    public string DatabaseConnectionStepIcon
    {
        get => _databaseConnectionStepIcon;
        set
        {
            _databaseConnectionStepIcon = value;
            OnPropertyChanged();
        }
    }

    private string _databaseConnectionStepDetail = "Conclua a validação dos CSVs para habilitar a conexão.";
    public string DatabaseConnectionStepDetail
    {
        get => _databaseConnectionStepDetail;
        set
        {
            _databaseConnectionStepDetail = value;
            OnPropertyChanged();
        }
    }

    private string _storedProceduresStepState = "Pendente";
    public string StoredProceduresStepState
    {
        get => _storedProceduresStepState;
        set
        {
            _storedProceduresStepState = value;
            OnPropertyChanged();
        }
    }

    private string _storedProceduresStepIcon = "-";
    public string StoredProceduresStepIcon
    {
        get => _storedProceduresStepIcon;
        set
        {
            _storedProceduresStepIcon = value;
            OnPropertyChanged();
        }
    }

    private string _storedProceduresStepDetail = "Aguardando conexão com o banco de destino.";
    public string StoredProceduresStepDetail
    {
        get => _storedProceduresStepDetail;
        set
        {
            _storedProceduresStepDetail = value;
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

    private void OnDestinoConnectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DatabaseConnectionViewModel.Server))
        {
            OnPropertyChanged(nameof(DatabaseConnectionStepTitle));
        }

        if (DestinationConnectionReady
            && (e.PropertyName is nameof(DatabaseConnectionViewModel.SelectedDatabaseType)
                or nameof(DatabaseConnectionViewModel.Server)
                or nameof(DatabaseConnectionViewModel.Database)
                or nameof(DatabaseConnectionViewModel.User)
                or nameof(DatabaseConnectionViewModel.Password)))
        {
            MarcarEtapaConexaoPendente("Parâmetros do banco de destino alterados. Teste a conexão novamente para prosseguir.");
        }

        if (e.PropertyName is nameof(DatabaseConnectionViewModel.IsBusy)
            or nameof(DatabaseConnectionViewModel.HasRequiredParameters)
            or nameof(DatabaseConnectionViewModel.LastTestSucceeded))
        {
            IniciarConexaoDestinoCommand.RaiseCanExecuteChanged();
            ConfirmarConexaoDestinoPopupCommand.RaiseCanExecuteChanged();
        }
    }

    private bool PodeIniciarConexaoDestino()
    {
        return IsDatabaseConnectionStepEnabled && !DestinoConnection.IsBusy;
    }

    private bool PodeConfirmarConexaoDestinoPopup()
    {
        return DestinationConnectionPopupVisibility == Visibility.Visible
            && !IsConnectingDestination
            && !DestinoConnection.IsBusy;
    }

    private bool PodeExecutarStoredProcedures()
    {
        return IsStoredProceduresStepEnabled;
    }

    private async Task IniciarConexaoDestinoAsync()
    {
        if (!PodeIniciarConexaoDestino())
        {
            return;
        }

        IsConnectingDestination = true;
        MarcarEtapaConexaoProcessando("Verificando parâmetros do banco de destino...");

        try
        {
            await DestinoConnection.CarregarAsync();
            OnPropertyChanged(nameof(DatabaseConnectionStepTitle));

            if (!DestinoConnection.HasRequiredParameters)
            {
                MarcarEtapaConexaoPendente("Informe os parâmetros do banco de destino para prosseguir.");
                DestinationConnectionPopupVisibility = Visibility.Visible;
                return;
            }

            var connectionSucceeded = await DestinoConnection.TestarConexaoAsync();
            if (!connectionSucceeded)
            {
                MarcarEtapaConexaoErro("Não foi possível conectar com os parâmetros salvos. Revise os dados para prosseguir.");
                DestinationConnectionPopupVisibility = Visibility.Visible;
                return;
            }

            MarcarEtapaConexaoSucesso();
        }
        finally
        {
            IsConnectingDestination = false;
        }
    }

    private async Task ConfirmarConexaoDestinoPopupAsync()
    {
        if (!PodeConfirmarConexaoDestinoPopup())
        {
            return;
        }

        IsConnectingDestination = true;
        MarcarEtapaConexaoProcessando("Testando e salvando a conexão com o banco de destino...");

        try
        {
            var connectionSucceeded = await DestinoConnection.TestarConexaoAsync();
            if (!connectionSucceeded)
            {
                MarcarEtapaConexaoErro("A conexão não funcionou. Corrija os parâmetros antes de prosseguir.");
                return;
            }

            var saveSucceeded = await DestinoConnection.SalvarAsync();
            if (!saveSucceeded)
            {
                MarcarEtapaConexaoErro("A conexão funcionou, mas os parâmetros não foram salvos.");
                return;
            }

            DestinationConnectionPopupVisibility = Visibility.Collapsed;
            MarcarEtapaConexaoSucesso();
        }
        finally
        {
            IsConnectingDestination = false;
        }
    }

    private void CancelarConexaoDestinoPopup()
    {
        DestinationConnectionPopupVisibility = Visibility.Collapsed;

        if (!DestinationConnectionReady && AreCsvFilesValid)
        {
            MarcarEtapaConexaoPendente("Conexão com o banco de destino pendente.");
        }
    }

    private void PrepararStoredProcedures()
    {
        StoredProceduresStepDetail = "Banco de destino conectado. A execução das stored procedures será configurada na próxima etapa.";
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
        MarcarEtapaValidacaoProcessando();

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
                    MarcarEtapaValidacaoErro($"Validação interrompida em {arquivo.CsvFileName}: arquivo não encontrado.");
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
                    MarcarEtapaValidacaoErro($"Validação interrompida em {arquivo.CsvFileName}: {result.Errors.Count} erro(s) encontrado(s).");
                    return;
                }

                arquivo.MarcarSucesso(result.TotalRows);
                ProgressValue = index + 1;
                ProgressText = $"{ProgressValue} de {ProgressMaximum} arquivo(s) validado(s).";
            }

            Status = "Todos os arquivos CSV foram validados com sucesso.";
            MarcarEtapaValidacaoSucesso();
            MarcarEtapaConexaoPendente("Validação concluída. Inicie a conexão com o banco de destino.");
        }
        catch (Exception exception)
        {
            if (SelectedArquivo is not null)
            {
                SelectedArquivo.MarcarErro("Falha na validação.", SelectedArquivo.TotalRows);
            }

            Status = $"Falha ao validar CSV: {exception.Message}";
            MarcarEtapaValidacaoErro($"Falha ao validar CSV: {exception.Message}");
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
        ResetarEtapas();
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

    private void ResetarEtapas()
    {
        DestinationConnectionReady = false;
        DestinationConnectionPopupVisibility = Visibility.Collapsed;

        CsvValidationStepState = "Pendente";
        CsvValidationStepIcon = "-";
        CsvValidationStepDetail = "Aguardando validação dos arquivos CSV.";

        DatabaseConnectionStepState = "Pendente";
        DatabaseConnectionStepIcon = "-";
        DatabaseConnectionStepDetail = "Conclua a validação dos CSVs para habilitar a conexão.";

        StoredProceduresStepState = "Pendente";
        StoredProceduresStepIcon = "-";
        StoredProceduresStepDetail = "Aguardando conexão com o banco de destino.";

        AtualizarDisponibilidadeEtapas();
    }

    private void MarcarEtapaValidacaoProcessando()
    {
        CsvValidationStepState = "Processando";
        CsvValidationStepIcon = "...";
        CsvValidationStepDetail = "Validando arquivos CSV obrigatórios.";
        AtualizarDisponibilidadeEtapas();
    }

    private void MarcarEtapaValidacaoSucesso()
    {
        CsvValidationStepState = "Sucesso";
        CsvValidationStepIcon = "OK";
        CsvValidationStepDetail = "Todos os arquivos CSV obrigatórios foram validados com sucesso.";
        AtualizarDisponibilidadeEtapas();
    }

    private void MarcarEtapaValidacaoErro(string detail)
    {
        CsvValidationStepState = "Erro";
        CsvValidationStepIcon = "X";
        CsvValidationStepDetail = detail;
        MarcarEtapaConexaoPendente("Corrija a validação dos CSVs para habilitar a conexão.");
        AtualizarDisponibilidadeEtapas();
    }

    private void MarcarEtapaConexaoPendente(string detail)
    {
        DestinationConnectionReady = false;
        DatabaseConnectionStepState = "Pendente";
        DatabaseConnectionStepIcon = "-";
        DatabaseConnectionStepDetail = detail;
        StoredProceduresStepState = "Pendente";
        StoredProceduresStepIcon = "-";
        StoredProceduresStepDetail = "Aguardando conexão com o banco de destino.";
        AtualizarDisponibilidadeEtapas();
    }

    private void MarcarEtapaConexaoProcessando(string detail)
    {
        DatabaseConnectionStepState = "Processando";
        DatabaseConnectionStepIcon = "...";
        DatabaseConnectionStepDetail = detail;
        AtualizarDisponibilidadeEtapas();
    }

    private void MarcarEtapaConexaoErro(string detail)
    {
        DestinationConnectionReady = false;
        DatabaseConnectionStepState = "Erro";
        DatabaseConnectionStepIcon = "X";
        DatabaseConnectionStepDetail = detail;
        StoredProceduresStepState = "Pendente";
        StoredProceduresStepIcon = "-";
        StoredProceduresStepDetail = "Aguardando conexão com o banco de destino.";
        AtualizarDisponibilidadeEtapas();
    }

    private void MarcarEtapaConexaoSucesso()
    {
        DestinationConnectionReady = true;
        DatabaseConnectionStepState = "Sucesso";
        DatabaseConnectionStepIcon = "OK";
        DatabaseConnectionStepDetail = $"Conexão realizada com sucesso em {DestinoConnection.Server}.";
        StoredProceduresStepState = "Pendente";
        StoredProceduresStepIcon = "-";
        StoredProceduresStepDetail = "Banco conectado. Próxima etapa: configurar a execução das stored procedures de migração.";
        AtualizarDisponibilidadeEtapas();
    }

    private void AtualizarDisponibilidadeEtapas()
    {
        OnPropertyChanged(nameof(AreCsvFilesValid));
        OnPropertyChanged(nameof(IsDatabaseConnectionStepEnabled));
        OnPropertyChanged(nameof(IsStoredProceduresStepEnabled));
        OnPropertyChanged(nameof(DatabaseConnectionStepTitle));
        IniciarConexaoDestinoCommand.RaiseCanExecuteChanged();
        ConfirmarConexaoDestinoPopupCommand.RaiseCanExecuteChanged();
        ExecutarStoredProceduresCommand.RaiseCanExecuteChanged();
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

    private bool _isValid;
    public bool IsValid
    {
        get => _isValid;
        private set
        {
            _isValid = value;
            OnPropertyChanged();
        }
    }

    public void Resetar()
    {
        Errors.Clear();
        TotalRows = 0;
        IsValid = false;
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
        IsValid = true;
        Icone = "✓";
        Status = "Concluído";
        Detalhe = $"{totalRows} linha(s) lida(s)";
    }

    public void MarcarErro(string detalhe, int totalRows)
    {
        TotalRows = totalRows;
        IsValid = false;
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
