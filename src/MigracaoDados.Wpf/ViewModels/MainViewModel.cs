using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using MigracaoDados.Application.Csv;
using MigracaoDados.Application.Database;
using MigracaoDados.Application.Session;
using MigracaoDados.Application.UseCases;
using MigracaoDados.Domain.Importacao;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MigracaoDados.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ValidarCsvUseCase _validarCsvUseCase;
    private readonly ExecutarStoredProceduresMigracaoUseCase _executarStoredProceduresMigracaoUseCase;
    private readonly MigrationSessionState _migrationSessionState;
    private readonly string _layoutTemplatesPath;

    public MainViewModel(
        ValidarCsvUseCase validarCsvUseCase,
        TestarConexaoBancoDadosUseCase testarConexaoBancoDadosUseCase,
        SalvarConexaoBancoDadosUseCase salvarConexaoBancoDadosUseCase,
        ObterConexaoBancoDadosUseCase obterConexaoBancoDadosUseCase,
        ExcluirConexaoBancoDadosUseCase excluirConexaoBancoDadosUseCase,
        ExecutarStoredProceduresMigracaoUseCase executarStoredProceduresMigracaoUseCase,
        MigrationSessionState migrationSessionState,
        IConfiguration configuration)
    {
        _validarCsvUseCase = validarCsvUseCase;
        _executarStoredProceduresMigracaoUseCase = executarStoredProceduresMigracaoUseCase;
        _migrationSessionState = migrationSessionState;
        _layoutTemplatesPath = Path.GetFullPath(configuration["AppSettings:LayoutTemplatesPath"] ?? "LayoutTemplates");

        AbrirValidacaoCommand = new RelayCommand(AbrirValidacao);
        AbrirConexoesBancoDadosCommand = new RelayCommand(AbrirConexoesBancoDados);
        SelecionarPastaCommand = new RelayCommand(SelecionarPasta);
        ValidarArquivosCommand = new RelayCommand(async () => await ValidarArquivosAsync(), PodeValidarArquivos);
        IniciarConexaoDestinoCommand = new RelayCommand(async () => await IniciarConexaoDestinoAsync(), PodeIniciarConexaoDestino);
        ConfirmarConexaoDestinoPopupCommand = new RelayCommand(async () => await ConfirmarConexaoDestinoPopupAsync(), PodeConfirmarConexaoDestinoPopup);
        CancelarConexaoDestinoPopupCommand = new RelayCommand(CancelarConexaoDestinoPopup);
        ExecutarStoredProceduresCommand = new RelayCommand(async () => await ExecutarStoredProceduresAsync(), PodeExecutarStoredProcedures);
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
    public bool IsStoredProceduresStepEnabled =>
        DestinationConnectionReady
        && !IsExecutingStoredProcedures
        && StoredProceduresStepState != "Sucesso";

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

    private Visibility _validationParametersVisibility = Visibility.Collapsed;
    public Visibility ValidationParametersVisibility
    {
        get => _validationParametersVisibility;
        set
        {
            _validationParametersVisibility = value;
            OnPropertyChanged();
        }
    }

    private string _currentMovementDateDisplay = "-";
    public string CurrentMovementDateDisplay
    {
        get => _currentMovementDateDisplay;
        set
        {
            _currentMovementDateDisplay = value;
            OnPropertyChanged();
        }
    }

    private string _previousMovementDateDisplay = "-";
    public string PreviousMovementDateDisplay
    {
        get => _previousMovementDateDisplay;
        set
        {
            _previousMovementDateDisplay = value;
            OnPropertyChanged();
        }
    }

    private string _nextMovementDateDisplay = "-";
    public string NextMovementDateDisplay
    {
        get => _nextMovementDateDisplay;
        set
        {
            _nextMovementDateDisplay = value;
            OnPropertyChanged();
        }
    }

    private string _institutionNumberDisplay = "-";
    public string InstitutionNumberDisplay
    {
        get => _institutionNumberDisplay;
        set
        {
            _institutionNumberDisplay = value;
            OnPropertyChanged();
        }
    }

    private string _csvPathDisplay = "-";
    public string CsvPathDisplay
    {
        get => _csvPathDisplay;
        set
        {
            _csvPathDisplay = value;
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
            OnPropertyChanged(nameof(IsStoredProceduresStepEnabled));
            ExecutarStoredProceduresCommand.RaiseCanExecuteChanged();
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

    private bool _isExecutingStoredProcedures;
    public bool IsExecutingStoredProcedures
    {
        get => _isExecutingStoredProcedures;
        set
        {
            _isExecutingStoredProcedures = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsStoredProceduresStepEnabled));
            ExecutarStoredProceduresCommand.RaiseCanExecuteChanged();
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

    private async Task ExecutarStoredProceduresAsync()
    {
        if (!PodeExecutarStoredProcedures())
        {
            return;
        }

        IsExecutingStoredProcedures = true;
        MarcarEtapaStoredProceduresProcessando("Executando stored procedures de migracao no banco de destino...");

        try
        {
            var result = await _executarStoredProceduresMigracaoUseCase.ExecutarAsync(
                CriarParametrosConexaoDestino(),
                _migrationSessionState.Parameters);

            if (result.Success)
            {
                MarcarEtapaStoredProceduresSucesso(result.Message);
            }
            else
            {
                MarcarEtapaStoredProceduresErro(result.Message);
            }
        }
        catch (Exception exception)
        {
            MarcarEtapaStoredProceduresErro($"Falha ao executar a Etapa 3: {exception.Message}");
        }
        finally
        {
            IsExecutingStoredProcedures = false;
        }
    }

    private DatabaseConnectionParameters CriarParametrosConexaoDestino()
    {
        return new DatabaseConnectionParameters(
            DestinoConnection.SelectedDatabaseType == "Oracle"
                ? DatabaseProviderType.Oracle
                : DatabaseProviderType.SqlServer,
            DestinoConnection.Server,
            DestinoConnection.Database,
            DestinoConnection.User,
            DestinoConnection.Password);
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
            var packageParameterValues = new List<CsvFileParameterValues>();

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

                var executionResult = await _validarCsvUseCase.ExecutarComDadosAsync(csvPath, schemaPath);
                var result = executionResult.ValidationResult;
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

                var parameterValues = ValidarParametrosTransversais(arquivo, executionResult.CsvFile, packageParameterValues);
                AtualizarErrosSelecionados();

                if (arquivo.Errors.Count > 0 || parameterValues is null)
                {
                    arquivo.MarcarErro($"{arquivo.Errors.Count} erro(s) encontrado(s).", result.TotalRows);
                    ProgressText = $"{ProgressValue} de {ProgressMaximum} arquivo(s) validado(s).";
                    Status = $"{arquivo.CsvFileName} inválido. Corrija o arquivo antes de validar os próximos.";
                    MarcarEtapaValidacaoErro($"Validação interrompida em {arquivo.CsvFileName}: {arquivo.Errors.Count} erro(s) encontrado(s).");
                    return;
                }

                packageParameterValues.Add(parameterValues);

                arquivo.MarcarSucesso(result.TotalRows);
                ProgressValue = index + 1;
                ProgressText = $"{ProgressValue} de {ProgressMaximum} arquivo(s) validado(s).";
            }

            Status = "Todos os arquivos CSV foram validados com sucesso.";
            AtualizarParametrosSessao(packageParameterValues[0]);
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

    private CsvFileParameterValues? ValidarParametrosTransversais(
        CsvValidationFileItemViewModel arquivo,
        CsvDataFile csvFile,
        IReadOnlyList<CsvFileParameterValues> previousFiles)
    {
        const string movementDateColumn = "ENT_DT_MOVTO";
        const string institutionColumn = "ENT_NR_INST";

        DateOnly? fileMovementDate = null;
        string? fileInstitutionNumber = null;
        var referenceValues = previousFiles.FirstOrDefault();

        if (csvFile.Rows.Count == 0)
        {
            arquivo.Errors.Add(CsvValidationErrorItemViewModel.From(new CsvValidationError(
                0,
                string.Empty,
                string.Empty,
                "O arquivo CSV nao possui linhas para definir os parametros da migracao.",
                ValidationErrorType.EmptyFile)));

            return null;
        }

        foreach (var row in csvFile.Rows)
        {
            var movementDateValue = ReadColumnValue(row, movementDateColumn);
            var institutionNumberValue = ReadColumnValue(row, institutionColumn);

            if (string.IsNullOrWhiteSpace(movementDateValue))
            {
                arquivo.Errors.Add(CsvValidationErrorItemViewModel.From(new CsvValidationError(
                    row.RowNumber,
                    movementDateColumn,
                    string.Empty,
                    $"A coluna '{movementDateColumn}' é obrigatória para definir a data de movimento.",
                    ValidationErrorType.RequiredValue)));
            }
            else if (!TryParseMovementDate(movementDateValue, out var rowMovementDate))
            {
                arquivo.Errors.Add(CsvValidationErrorItemViewModel.From(new CsvValidationError(
                    row.RowNumber,
                    movementDateColumn,
                    movementDateValue,
                    $"O valor '{movementDateValue}' não é uma data de movimento válida.",
                    ValidationErrorType.InvalidType)));
            }
            else
            {
                fileMovementDate ??= rowMovementDate;

                if (referenceValues is not null)
                {
                    if (referenceValues.MovementDate != rowMovementDate)
                    {
                        arquivo.Errors.Add(CsvValidationErrorItemViewModel.From(new CsvValidationError(
                            row.RowNumber,
                            movementDateColumn,
                            movementDateValue,
                            $"A data de movimento '{movementDateColumn}' esta divergente da data '{movementDateColumn}' informada no arquivo {referenceValues.SourceCsvFileName}. Valor esperado '{FormatParameterDate(referenceValues.MovementDate)}'.",
                            ValidationErrorType.InconsistentValue)));
                    }
                }
                else if (fileMovementDate.Value != rowMovementDate)
                {
                    arquivo.Errors.Add(CsvValidationErrorItemViewModel.From(new CsvValidationError(
                        row.RowNumber,
                        movementDateColumn,
                        movementDateValue,
                        $"A data de movimento '{movementDateColumn}' esta divergente da primeira data '{movementDateColumn}' informada no arquivo {arquivo.CsvFileName}. Valor esperado '{FormatParameterDate(fileMovementDate.Value)}'.",
                        ValidationErrorType.InconsistentValue)));
                }
            }

            if (string.IsNullOrWhiteSpace(institutionNumberValue))
            {
                arquivo.Errors.Add(CsvValidationErrorItemViewModel.From(new CsvValidationError(
                    row.RowNumber,
                    institutionColumn,
                    string.Empty,
                    $"A coluna '{institutionColumn}' é obrigatória para definir a instituição.",
                    ValidationErrorType.RequiredValue)));
            }
            else
            {
                fileInstitutionNumber ??= institutionNumberValue;

                if (referenceValues is not null)
                {
                    if (!string.Equals(referenceValues.InstitutionNumber, institutionNumberValue, StringComparison.OrdinalIgnoreCase))
                    {
                        arquivo.Errors.Add(CsvValidationErrorItemViewModel.From(new CsvValidationError(
                            row.RowNumber,
                            institutionColumn,
                            institutionNumberValue,
                            $"A coluna '{institutionColumn}' esta divergente do valor '{institutionColumn}' informado no arquivo {referenceValues.SourceCsvFileName}. Valor esperado '{referenceValues.InstitutionNumber}'.",
                            ValidationErrorType.InconsistentValue)));
                    }
                }
                else if (!string.Equals(fileInstitutionNumber, institutionNumberValue, StringComparison.OrdinalIgnoreCase))
                {
                    arquivo.Errors.Add(CsvValidationErrorItemViewModel.From(new CsvValidationError(
                        row.RowNumber,
                        institutionColumn,
                        institutionNumberValue,
                        $"A coluna '{institutionColumn}' esta divergente do primeiro valor '{institutionColumn}' informado no arquivo {arquivo.CsvFileName}. Valor esperado '{fileInstitutionNumber}'.",
                        ValidationErrorType.InconsistentValue)));
                }
            }
        }

        if (arquivo.Errors.Count > 0 || fileMovementDate is null || string.IsNullOrWhiteSpace(fileInstitutionNumber))
        {
            return null;
        }

        return new CsvFileParameterValues(fileMovementDate.Value, fileInstitutionNumber, arquivo.CsvFileName);
    }

    private void AtualizarParametrosSessao(CsvFileParameterValues values)
    {
        var previousMovementDate = GetPreviousBusinessDay(values.MovementDate);
        var nextMovementDate = GetNextBusinessDay(values.MovementDate);

        var parameters = new MigrationSessionParameters(
            FormatParameterDate(values.MovementDate),
            FormatParameterDate(previousMovementDate),
            FormatParameterDate(nextMovementDate),
            values.InstitutionNumber,
            CsvFolderPath);

        _migrationSessionState.SetParameters(parameters);

        CurrentMovementDateDisplay = FormatDisplayDate(values.MovementDate);
        PreviousMovementDateDisplay = FormatDisplayDate(previousMovementDate);
        NextMovementDateDisplay = FormatDisplayDate(nextMovementDate);
        InstitutionNumberDisplay = values.InstitutionNumber;
        CsvPathDisplay = CsvFolderPath;
        ValidationParametersVisibility = Visibility.Visible;
    }

    private static string ReadColumnValue(CsvDataRow row, string columnName)
    {
        return row.Values.TryGetValue(columnName, out var value)
            ? value.Trim()
            : string.Empty;
    }

    private static bool TryParseMovementDate(string value, out DateOnly movementDate)
    {
        var trimmedValue = value.Trim();
        string[] formats = ["yyyyMMdd", "yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy"];

        if (DateOnly.TryParseExact(
            trimmedValue,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out movementDate))
        {
            return true;
        }

        if (DateTime.TryParse(trimmedValue, new CultureInfo("pt-BR"), DateTimeStyles.None, out var ptBrDate)
            || DateTime.TryParse(trimmedValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out ptBrDate))
        {
            movementDate = DateOnly.FromDateTime(ptBrDate);
            return true;
        }

        movementDate = default;
        return false;
    }

    private static DateOnly GetPreviousBusinessDay(DateOnly date)
    {
        var previousDate = date.AddDays(-1);

        while (previousDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            previousDate = previousDate.AddDays(-1);
        }

        return previousDate;
    }

    private static DateOnly GetNextBusinessDay(DateOnly date)
    {
        var nextDate = date.AddDays(1);

        while (nextDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            nextDate = nextDate.AddDays(1);
        }

        return nextDate;
    }

    private static string FormatParameterDate(DateOnly date)
    {
        return date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }

    private static string FormatDisplayDate(DateOnly date)
    {
        return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }

    private void ResetarEtapas()
    {
        IsExecutingStoredProcedures = false;
        DestinationConnectionReady = false;
        DestinationConnectionPopupVisibility = Visibility.Collapsed;
        _migrationSessionState.Clear();
        ValidationParametersVisibility = Visibility.Collapsed;
        CurrentMovementDateDisplay = "-";
        PreviousMovementDateDisplay = "-";
        NextMovementDateDisplay = "-";
        InstitutionNumberDisplay = "-";
        CsvPathDisplay = "-";

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
        StoredProceduresStepDetail = "Banco conectado. Proxima etapa: executar as stored procedures de migracao.";
        AtualizarDisponibilidadeEtapas();
    }

    private void MarcarEtapaStoredProceduresProcessando(string detail)
    {
        StoredProceduresStepState = "Processando";
        StoredProceduresStepIcon = "...";
        StoredProceduresStepDetail = detail;
        AtualizarDisponibilidadeEtapas();
    }

    private void MarcarEtapaStoredProceduresSucesso(string detail)
    {
        StoredProceduresStepState = "Sucesso";
        StoredProceduresStepIcon = "OK";
        StoredProceduresStepDetail = detail;
        AtualizarDisponibilidadeEtapas();
    }

    private void MarcarEtapaStoredProceduresErro(string detail)
    {
        StoredProceduresStepState = "Erro";
        StoredProceduresStepIcon = "X";
        StoredProceduresStepDetail = detail;
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

public sealed record CsvFileParameterValues(
    DateOnly MovementDate,
    string InstitutionNumber,
    string SourceCsvFileName);

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
