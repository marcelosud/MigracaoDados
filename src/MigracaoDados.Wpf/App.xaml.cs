using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MigracaoDados.Application.Interfaces;
using MigracaoDados.Application.Services;
using MigracaoDados.Application.UseCases;
using MigracaoDados.Infrastructure.Csv;
using MigracaoDados.Infrastructure.Database;
using MigracaoDados.Infrastructure.Schemas;
using MigracaoDados.Infrastructure.Security;
using Serilog;
using System.IO;
using System.Windows;

namespace MigracaoDados.Wpf;

public partial class App : System.Windows.Application
{
    public static IHost HostApp { get; private set; } = null!;

    public App()
    {
        HostApp = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false);
            })
            .UseSerilog((context, services, configuration) =>
            {
                var logPath = context.Configuration["Logging:LogFilePath"];
                configuration
                    .WriteTo.File(logPath!, rollingInterval: RollingInterval.Day);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<MainWindow>();
                services.AddSingleton<ViewModels.MainViewModel>();
                services.AddSingleton<ValidarCsvUseCase>();
                services.AddSingleton<TestarConexaoBancoDadosUseCase>();
                services.AddSingleton<SalvarConexaoBancoDadosUseCase>();
                services.AddSingleton<ObterConexaoBancoDadosUseCase>();
                services.AddSingleton<ExcluirConexaoBancoDadosUseCase>();
                services.AddSingleton<ICsvValidationService, CsvValidationService>();
                services.AddSingleton<ICsvFileReader, CsvFileReader>();
                services.AddSingleton<IImportSchemaReader, ExcelLayoutImportSchemaReader>();
                services.AddSingleton<IDatabaseConnectionTester, DatabaseConnectionTester>();
                services.AddSingleton<IDatabaseConnectionRepository, SqliteDatabaseConnectionRepository>();
                services.AddSingleton<ISecretProtector, WindowsDpapiSecretProtector>();
            })
            .Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        HostApp.Start();

        var mainWindow = HostApp.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await HostApp.StopAsync();
        HostApp.Dispose();
        base.OnExit(e);
    }
}
