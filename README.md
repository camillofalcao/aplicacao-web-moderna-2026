<div align="center">

# 📅 Sistema de Agendamentos

### Recriando o Projeto do Zero com TDD e Clean Architecture

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-3.x-06B6D4?logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)
[![SQLite](https://img.shields.io/badge/SQLite-3-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

Este tutorial guia você passo a passo na criação completa do sistema de agendamentos,
desde a estrutura inicial até o frontend. Ao seguir cada etapa, você terá o projeto
completo funcionando.

</div>

---

## 📑 Índice

- [Tecnologias](#-tecnologias)
- [Pré-requisitos](#-pré-requisitos)
- [Arquitetura do Projeto](#-arquitetura-do-projeto)
- [Commit 1 — Criando a Estrutura](/README-Commit-1.md)

---

## 🛠 Tecnologias

| Camada         | Tecnologia                          | Função                            |
| -------------- | ----------------------------------- | --------------------------------- |
| **API**        | ASP.NET Core Minimal APIs (.NET 10) | Endpoints REST                    |
| **Domínio**    | C# / ErrorOr                        | Entidades e regras de negócio     |
| **Infra**      | Entity Framework Core + SQLite      | Persistência de dados             |
| **Frontend**   | Blazor WebAssembly                  | SPA interativa                    |
| **Estilo**     | Tailwind CSS                        | Estilização utilitária            |
| **Segurança**  | JWT (JSON Web Tokens)               | Autenticação e autorização        |
| **Desempenho**  | Redis                              | Cache
| **Testes**     | xUnit                               | Testes unitários e de integração  |

---

## ✅ Pré-requisitos

| Requisito                | Verificação                |
| ------------------------- | -------------------------- |
| .NET 10 SDK               | `dotnet --version` → 10.x |
| Editor de código          | VS Code, Rider ou Visual Studio |
| Git                       | `git --version`            |

---

## 🏗 Arquitetura do Projeto

```
Agendamentos/
├── Agendamentos.Entities/        # 🔵 Camada de Domínio
│   └── (Entidades e regras de negócio)
│
├── Agendamentos.Infrastructure/  # 🟢 Camada de Infraestrutura
│   └── (DbContext, repositórios, migrations)
│
├── Agendamentos.Api/             # 🟠 Camada da API
│   └── (Endpoints, configuração, middleware)
│
├── Agendamentos.Web/             # 🟣 Frontend Blazor WASM
│   └── (Componentes, páginas, serviços)
│
├── Agendamentos.Tests/           # 🧪 Testes
│   └── (Unitários e integração)
│
└── README.md                     # 📄 Este tutorial
```

---

<div align="center">

*Tutorial em construção — novas partes serão adicionadas em breve.* 🚧

</div>
