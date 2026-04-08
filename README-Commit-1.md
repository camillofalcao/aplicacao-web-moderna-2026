# 📦 Parte 1 — Criando a Estrutura

## 1.1 Preparação do ambiente

- Crie uma pasta de nome `Agendamentos` e abra-a no VS Code
- Inicialize um repositório Git:

```bash
# Inicia o git na pasta
git init

# (Opcional, mas recomendado) Define a branch principal como 'main'
git branch -M main
```

- Crie o arquivo `.gitignore`:

```bash
dotnet new gitignore
```

## 1.2 Criação dos projetos

Execute os comandos abaixo para criar cada camada da solução:

```bash
# 🔵 Camada de Domínio (Entities)
dotnet new classlib -n Agendamentos.Entities
rm Agendamentos.Entities/Class1.cs

# 🟢 Camada de Infraestrutura
dotnet new classlib -n Agendamentos.Infrastructure
rm Agendamentos.Infrastructure/Class1.cs

# 🟠 Camada da API
dotnet new webapi -n Agendamentos.Api

# 🟣 Camada Web (Blazor WebAssembly)
dotnet new blazorwasm -n Agendamentos.Web

# 🧪 Projeto de Testes
dotnet new xunit -n Agendamentos.Tests
```

> 💡 **Dica:** Após criar os projetos, verifique a estrutura com `ls` ou pelo explorador do VS Code.

## 1.3 Adicionar referências entre projetos

```bash
# Infrastructure referencia Entities
dotnet add Agendamentos.Infrastructure/Agendamentos.Infrastructure.csproj reference Agendamentos.Entities/Agendamentos.Entities.csproj

# Api referencia Entities e Infrastructure
dotnet add Agendamentos.Api/Agendamentos.Api.csproj reference Agendamentos.Entities/Agendamentos.Entities.csproj
dotnet add Agendamentos.Api/Agendamentos.Api.csproj reference Agendamentos.Infrastructure/Agendamentos.Infrastructure.csproj

# Tests referencia Entities e Infrastructure
dotnet add Agendamentos.Tests/Agendamentos.Tests.csproj reference Agendamentos.Entities/Agendamentos.Entities.csproj
dotnet add Agendamentos.Tests/Agendamentos.Tests.csproj reference Agendamentos.Infrastructure/Agendamentos.Infrastructure.csproj
```

## 1.4 Criar o arquivo de solução (Agendamentos.slnx)

```bash
dotnet new sln
dotnet sln Agendamentos.slnx add Agendamentos.Api/Agendamentos.Api.csproj
dotnet sln Agendamentos.slnx add Agendamentos.Entities/Agendamentos.Entities.csproj
dotnet sln Agendamentos.slnx add Agendamentos.Infrastructure/Agendamentos.Infrastructure.csproj
dotnet sln Agendamentos.slnx add Agendamentos.Web/Agendamentos.Web.csproj
dotnet sln Agendamentos.slnx add Agendamentos.Tests/Agendamentos.Tests.csproj
```

## ✅ COMMIT 1 — Estrutura inicial

- Crie um repositório para o seu projeto na sua conta no GitHub e execute os comandos abaixo, após alterar corretamente o comando git remote para o seu repositório:

- Se precisar do meu código para este commit, basta executar no repositório deste curso:

```bash
git checkout f1fbe67
```

E para voltar para a última versão, basta executar:

```bash
git checkout main
```
