# NLog Targets for Azure

![logo](src/icon.png)

| Package Names                         | NuGet                 | Description |
| ------------------------------------- | :-------------------: | ----------- |
| **NLog.Extensions.AzureTable**  | [![NuGet](https://img.shields.io/nuget/v/NLog.Extensions.AzureTable.svg)](https://www.nuget.org/packagesNLog.Extensions.AzureTable/) | Azure Table Storage or Azure CosmosDb Tables |

NLog AzureTableTarget to Azure Table Storage or Azure CosmosDB Tables
- connectionString is required.
- tableName is optional. The default value will be from the process name, or the  assembly name passed to "dotnet".

## nlog.config

```xml
<extensions>
  <add assembly="NLog.Extensions.AzureTable" /> 
</extensions>

<targets>
  <target xsi:type="AzureTable"
          name="table"
          connectionString="String"
          tableName="String" >
    <contextproperty name="PartitionKey" layout="${logger}" />
    <contextproperty name="RowKey" layout="${date:format=O}_${guid}" />
    <contextproperty name="logger" layout="${logger}" />
    <contextproperty name="Level" layout="${level}" />
    <contextproperty name="eventId" layout="${event-properties:item=EventId_Id}" />
    <contextproperty name="machinename" layout="${machinename}" />
    <contextproperty name="message" layout="${message}" />
    <contextproperty name="exception" layout="${exception:format=tostring}" />
  </target>
</targets>
```
## IConfiguration
### appsettings.json

```json
{
  "NLog": {
    "extensions": [
      { "assembly": "NLog.Extensions.AzureTable" }
    ],
    "targets": {
      "console": {
        "type": "ColoredConsole",
        "useDefaultRowHighlightingRules": "true",
        "layout": "${date:format=HH\\:mm}|${level}|${logger}|${message}"
      },
      "table": {
        "type": "AzureTable",
        "tableName": "Sample",
        "connectionString":"{Input your connetion string here}",
        "contextProperties": [
          { "name": "PartitionKey", "layout": "${logger}" },
          { "name": "RowKey", "layout": "${date:format=O}_${guid}" },
          { "name": "logger", "layout": "${logger}" },
          { "name": "level", "layout": "${level}" },
          { "name": "eventId", "layout": "${event-properties:item=EventId_Id}" },
          { "name": "machinename", "layout": "${machinename}" },
          { "name": "message", "layout": "${message}" },
          { "name": "exception", "layout": "${exception:tostring}" }
        ]
      }
    },
    "rules": [{"logger": "*", "minLevel": "Trace", "writeTo": "table,console" }]
  }
}
```

### Environment varaibles
```
NLog__targets__table__connectionString=xxx
```
### Azure Key Vault secrets:
```
NLog--targets--table--ConnectionString=xxx
```
### Command line
```
--nlog:targets:table:connectionstring "xxx"
```
### startup.cs
```C#
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services) {
            services.AddLogging(ConfigLogging);
            ...
        }
        private void ConfigLogging(ILoggingBuilder builder) {
            LogManager.Configuration = new NLogLoggingConfiguration(Configuration.GetSection("NLog"));
            builder.ClearProviders()
                .AddConfiguration(Configuration.GetSection("Logging"))
                .AddNLog(Configuration);
        }
```

## References
- [Configuration in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1)