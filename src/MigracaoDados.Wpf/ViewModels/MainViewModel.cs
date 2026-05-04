using CommunityToolkit.Mvvm.Input;
using MigracaoDados.Application.UseCases;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MigracaoDados.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly GerarMensagemBoasVindasUseCase _useCase;

    public MainViewModel(GerarMensagemBoasVindasUseCase useCase)
    {
        _useCase = useCase;
        AtualizarMensagemCommand = new RelayCommand(AtualizarMensagem);
    }

    private string _mensagem = "Sistema MigracaoDados iniciado.";
    public string Mensagem
    {
        get => _mensagem;
        set
        {
            _mensagem = value;
            OnPropertyChanged();
        }
    }

    public ICommand AtualizarMensagemCommand { get; }

    private void AtualizarMensagem()
    {
        Mensagem = _useCase.Executar();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}