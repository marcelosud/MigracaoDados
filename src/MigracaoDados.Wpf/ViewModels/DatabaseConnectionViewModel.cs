using MigracaoDados.Application.Database;
using MigracaoDados.Application.UseCases;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MigracaoDados.Wpf.ViewModels;

public sealed class DatabaseConnectionViewModel : INotifyPropertyChanged
{
    private readonly TestarConexaoBancoDadosUseCase _testarConexaoUseCase;
    private readonly SalvarConexaoBancoDadosUseCase _salvarConexaoUseCase;
    private readonly ObterConexaoBancoDadosUseCase _obterConexaoUseCase;
    private readonly ExcluirConexaoBancoDadosUseCase _excluirConexaoUseCase;
    private readonly string _key;

    public DatabaseConnectionViewModel(
        string key,
        string title,
        TestarConexaoBancoDadosUseCase testarConexaoUseCase,
        SalvarConexaoBancoDadosUseCase salvarConexaoUseCase,
        ObterConexaoBancoDadosUseCase obterConexaoUseCase,
        ExcluirConexaoBancoDadosUseCase excluirConexaoUseCase)
    {
        _key = key;
        Title = title;
        _testarConexaoUseCase = testarConexaoUseCase;
        _salvarConexaoUseCase = salvarConexaoUseCase;
        _obterConexaoUseCase = obterConexaoUseCase;
        _excluirConexaoUseCase = excluirConexaoUseCase;
        TestarConexaoCommand = new RelayCommand(async () => await TestarConexaoAsync(), CanTestConnection);
        SalvarCommand = new RelayCommand(async () => await SalvarAsync(), CanSave);
        ExcluirCommand = new RelayCommand(async () => await ExcluirAsync(), CanDelete);
        ResetTestFeedback();
        ResetSaveFeedback();

        _ = CarregarAsync();
    }

    public string Title { get; }
    public ObservableCollection<string> DatabaseTypes { get; } = new(["SQL Server", "Oracle"]);
    public RelayCommand TestarConexaoCommand { get; }
    public RelayCommand SalvarCommand { get; }
    public RelayCommand ExcluirCommand { get; }

    public bool HasRequiredParameters =>
        !string.IsNullOrWhiteSpace(Server)
        && !string.IsNullOrWhiteSpace(User)
        && !string.IsNullOrWhiteSpace(Password);

    private string _selectedDatabaseType = "SQL Server";
    public string SelectedDatabaseType
    {
        get => _selectedDatabaseType;
        set
        {
            if (_selectedDatabaseType == value)
            {
                return;
            }

            _selectedDatabaseType = value;
            OnPropertyChanged();
            InvalidateTestFeedback();
        }
    }

    private string _server = string.Empty;
    public string Server
    {
        get => _server;
        set
        {
            if (_server == value)
            {
                return;
            }

            _server = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasRequiredParameters));
            InvalidateTestFeedback();
        }
    }

    private string _database = string.Empty;
    public string Database
    {
        get => _database;
        set
        {
            if (_database == value)
            {
                return;
            }

            _database = value;
            OnPropertyChanged();
            InvalidateTestFeedback();
        }
    }

    private string _user = string.Empty;
    public string User
    {
        get => _user;
        set
        {
            if (_user == value)
            {
                return;
            }

            _user = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasRequiredParameters));
            InvalidateTestFeedback();
        }
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set
        {
            if (_password == value)
            {
                return;
            }

            _password = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasRequiredParameters));
            InvalidateTestFeedback();
        }
    }

    private string _testResult = "Informe os parametros e clique em Testar conexao.";
    public string TestResult
    {
        get => _testResult;
        set
        {
            _testResult = value;
            OnPropertyChanged();
        }
    }

    private bool _isTesting;
    public bool IsTesting
    {
        get => _isTesting;
        set
        {
            _isTesting = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBusy));
            TestarConexaoCommand.RaiseCanExecuteChanged();
            SalvarCommand.RaiseCanExecuteChanged();
            ExcluirCommand.RaiseCanExecuteChanged();
        }
    }

    private bool _isSaving;
    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            _isSaving = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBusy));
            TestarConexaoCommand.RaiseCanExecuteChanged();
            SalvarCommand.RaiseCanExecuteChanged();
            ExcluirCommand.RaiseCanExecuteChanged();
        }
    }

    private bool _isDeleting;
    public bool IsDeleting
    {
        get => _isDeleting;
        set
        {
            _isDeleting = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBusy));
            TestarConexaoCommand.RaiseCanExecuteChanged();
            SalvarCommand.RaiseCanExecuteChanged();
            ExcluirCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsBusy => IsTesting || IsSaving || IsDeleting;

    private string _saveResult = "Parametros ainda nao salvos.";
    public string SaveResult
    {
        get => _saveResult;
        set
        {
            _saveResult = value;
            OnPropertyChanged();
        }
    }

    private bool _lastTestSucceeded;
    public bool LastTestSucceeded
    {
        get => _lastTestSucceeded;
        set
        {
            _lastTestSucceeded = value;
            OnPropertyChanged();
        }
    }

    private string _testStatus = string.Empty;
    public string TestStatus
    {
        get => _testStatus;
        set
        {
            _testStatus = value;
            OnPropertyChanged();
        }
    }

    private string _testIcon = string.Empty;
    public string TestIcon
    {
        get => _testIcon;
        set
        {
            _testIcon = value;
            OnPropertyChanged();
        }
    }

    private string _saveStatus = string.Empty;
    public string SaveStatus
    {
        get => _saveStatus;
        set
        {
            _saveStatus = value;
            OnPropertyChanged();
        }
    }

    private string _saveIcon = string.Empty;
    public string SaveIcon
    {
        get => _saveIcon;
        set
        {
            _saveIcon = value;
            OnPropertyChanged();
        }
    }

    private bool CanTestConnection()
    {
        return !IsBusy;
    }

    private bool CanSave()
    {
        return !IsBusy;
    }

    private bool CanDelete()
    {
        return !IsBusy;
    }

    public async Task<bool> TestarConexaoAsync()
    {
        IsTesting = true;
        LastTestSucceeded = false;
        TestResult = "Testando conexao...";
        TestStatus = "Processando";
        TestIcon = "...";

        try
        {
            var result = await _testarConexaoUseCase.ExecutarAsync(ToParameters());
            LastTestSucceeded = result.Success;
            TestResult = result.Message;

            if (result.Success)
            {
                MarkTestSuccess();
            }
            else
            {
                MarkTestError();
            }

            return result.Success;
        }
        finally
        {
            IsTesting = false;
        }
    }

    public async Task<bool> SalvarAsync()
    {
        IsSaving = true;
        SaveResult = "Salvando parametros...";
        SaveStatus = "Processando";
        SaveIcon = "...";

        try
        {
            var result = await _salvarConexaoUseCase.ExecutarAsync(ToProfile());
            SaveResult = result.Message;

            if (result.Success)
            {
                MarkSaveSuccess();
            }
            else
            {
                MarkSaveError();
            }

            return result.Success;
        }
        finally
        {
            IsSaving = false;
        }
    }

    public async Task<bool> ExcluirAsync()
    {
        var confirmation = MessageBox.Show(
            $"Deseja excluir os parametros da conexao \"{Title}\"?",
            "Confirmar exclusao",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            return false;
        }

        IsDeleting = true;
        SaveResult = "Excluindo parametros...";
        SaveStatus = "Processando";
        SaveIcon = "...";

        try
        {
            var result = await _excluirConexaoUseCase.ExecutarAsync(_key);
            SaveResult = result.Message;

            if (result.Success)
            {
                LimparParametros();
                LastTestSucceeded = false;
                TestResult = "Informe os parametros e clique em Testar conexao.";
                ResetTestFeedback();
                MarkSaveSuccess();
            }
            else
            {
                MarkSaveError();
            }

            return result.Success;
        }
        finally
        {
            IsDeleting = false;
        }
    }

    public async Task CarregarAsync()
    {
        var profile = await _obterConexaoUseCase.ExecutarAsync(_key);

        if (profile is null)
        {
            return;
        }

        SelectedDatabaseType = profile.Provider == DatabaseProviderType.Oracle ? "Oracle" : "SQL Server";
        Server = profile.Server;
        Database = profile.Database;
        User = profile.User;
        Password = profile.Password;
        SaveResult = "Parametros carregados do banco local.";
        MarkSaveSuccess();
    }

    private DatabaseConnectionParameters ToParameters()
    {
        return new DatabaseConnectionParameters(
            SelectedDatabaseType == "Oracle" ? DatabaseProviderType.Oracle : DatabaseProviderType.SqlServer,
            Server,
            Database,
            User,
            Password);
    }

    private DatabaseConnectionProfile ToProfile()
    {
        return new DatabaseConnectionProfile(
            _key,
            SelectedDatabaseType == "Oracle" ? DatabaseProviderType.Oracle : DatabaseProviderType.SqlServer,
            Server,
            Database,
            User,
            Password);
    }

    private void LimparParametros()
    {
        SelectedDatabaseType = "SQL Server";
        Server = string.Empty;
        Database = string.Empty;
        User = string.Empty;
        Password = string.Empty;
    }

    private void ResetTestFeedback()
    {
        TestStatus = "Pendente";
        TestIcon = "-";
    }

    private void InvalidateTestFeedback()
    {
        if (IsTesting)
        {
            return;
        }

        LastTestSucceeded = false;
        TestResult = "Parametros alterados. Teste a conexao novamente.";
        ResetTestFeedback();
    }

    private void ResetSaveFeedback()
    {
        SaveStatus = "Pendente";
        SaveIcon = "-";
    }

    private void MarkTestSuccess()
    {
        TestStatus = "Sucesso";
        TestIcon = "OK";
    }

    private void MarkTestError()
    {
        TestStatus = "Erro";
        TestIcon = "X";
    }

    private void MarkSaveSuccess()
    {
        SaveStatus = "Sucesso";
        SaveIcon = "OK";
    }

    private void MarkSaveError()
    {
        SaveStatus = "Erro";
        SaveIcon = "X";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
