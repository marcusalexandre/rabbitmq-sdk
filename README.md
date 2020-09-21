![Rabbitmq](https://miro.medium.com/max/725/1*4-jIqua2I-NmIjy_Tl348g.png)

# Building Blocks: Blocos de Código aceleradores 

O projeto  Building Blocks são um catalogo de facilitadores criados para simplificar a implantação dos recursos e técnologias. 


# Começando

Instale os Blocos de Construção para RabbitMq usando o nuget:

```
dotnet add package RabbitMQ 
```

# Configuração

Em Startup.cs, adicione  seguinte configuração:

```csharp
 public IConfiguration Configuration { get; }

// This method gets called by the runtime. Use this method to add services to the container.
public void ConfigureServices(IServiceCollection services)
{
    //(...)

    services.AddRabbitMq()
        .WithEnviroments(Configuration);

    //(...)
}
```

Onde você precisa configurar as váriaveis de ambiente conforme abaixo:

| Variável  |  Valor padrão |
|---|---|
| RABBITMQ_HOST  |  localhost |
| RABBITMQ_USER  |  guest |
| RABBITMQ_PASSWORD  |  guest | 
| RABBITMQ_VHOST  |  / | 
| RABBITMQ_PORT | 5672 |
| RABBITMQ_RETRYDELAY | 30 |
| RABBITMQ_MAXATTEMPS | 5 |
| RABBITMQ_USE_SSL | false |
| RABBITMQ_NAMEPREFIX | - |
| RABBITMQ_NAMESUFIX | - |
| RABBITMQ_USESCOPE | false |
| RABBITMQ_TTL | - |
| RABBITMQ_PREFETCHCOUNT | 10 |

Você também pode usar o método 'WithParameters' para determinar estes parâmetros manualmente.

Abaixo, um exemplo de parametrização do arquivo ´launchSettings.json´ localizado em Properties, no Visual Studio:

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "Local": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "weatherforecast",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "RABBITMQ_HOST": "localhost",
        "RABBITMQ_VHOST": "/",
        "RABBITMQ_PORT": "5672",
        "RABBITMQ_USER": "guest",
        "RABBITMQ_PASSWORD": "guest",
        "RABBITMQ_USE_SSL": "false"
      }
    }
  }
};
```



# Ciclo de Vida

Todos os subscribers e publisher são criados de maneira interna com AddTransient(), mas opcionamente você pode usar 'WithScope' para mapear os objetos com AddScoped() internamente.

```csharp
    services.AddRabbitMq()
        .WithEnviroments(Configuration)
        .WithScope();
```

# Habilitar o RabbitMq localhost com o Docker

## Via docker comando:

```bash
docker pull heidiks/rabbitmq-delayed-message-exchange:latest
docker run -d --hostname my-rabbit --name some-rabbit heidiks/rabbitmq-delayed-message-exchange:latest
```

## Para rodar com o compose:

```yaml
LOCAL_RABBIT:
    image: heidiks/rabbitmq-delayed-message-exchange:latest
    ports:
      - 15672:15672
      - 5672:5672
```

## Aplicação de Exemplo:

Veja na pasta 'samples' contextos de exemplo para utilização do BuildingBlocks