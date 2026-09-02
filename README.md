# ✦ ElsEvo Beta

**Canal Beta do patcher de mods para Elsword, feito do zero em C# / WPF (.NET 10).**

> ⚠️ Este é o canal **Beta** do ElsEvo — versões de teste, podem conter bugs. Para a versão estável, veja o repositório [`ElsEvo`](https://github.com/AyameJk/ElsEvo).

ElsEvo automatiza o processo de aplicar mods no Elsword: seleciona os packs, faz backup dos arquivos originais, aplica os mods, abre o jogo e restaura tudo sozinho quando você fecha. Compatível com mods criados pelos próprios jogadores — vozes, texturas, BGM, vídeos e mais.

> Criado para trazer de volta a experiência do Elsword brasileiro com uma ferramenta moderna, rápida e mais segura de usar.

---

## 🖥️ Tecnologias

<p align="left">
  <img src="https://skillicons.dev/icons?i=cs,dotnet,git,github,vscode,windows" />
</p>

- **C# / WPF** — .NET 10
- **Inno Setup** — instalador Windows
- **GitHub Actions** — build e release automatizados

---

## ✨ Funcionalidades

- 🎨 **Tema Claro/Escuro**, aplicado em tempo real
- 🌐 **Multilíngue** — Português, Inglês e Chinês
- 📦 **Gerenciador de Mods** — importe pastas ou `.zip`, organize por categoria (Geral / BGM / Vídeo)
- 🎙️ **Download de dublagens** — baixa e instala packs de voz direto pelo app, com fila, progresso e limite de velocidade configurável
- 🔄 **Atualização automática** — verifica, baixa e instala novas versões sozinho, sem precisar abrir o navegador
- 🛡️ **Backup e restauração seguros** — arquivos originais nunca são perdidos, mesmo se algo falhar no meio do processo
- 🌐 **Configurações de rede** — proxy manual, limite de velocidade, timeouts e tentativas automáticas
- 🔔 **Ícone na bandeja**, inicialização com o Windows, minimizar automático
- 🖼️ Interface 100% customizada, sem depender do chrome nativo do Windows

---

## 📥 Instalação

1. Baixe o instalador mais recente na aba [**Releases**](https://github.com/AyameJk/ElsEvoBeta/releases)
2. Execute o `ElsEvo-Setup.exe`
3. Abra o ElsEvo e configure a pasta de instalação do Elsword em **Configurações → Elsword**

> ⚠️ O instalador não é assinado digitalmente (projeto pessoal, sem certificado EV). O Windows SmartScreen pode avisar sobre "editor desconhecido" — clique em **Mais informações → Executar assim mesmo**.

---

## 🎮 Como usar

1. Clique em **Gerenciar Mods**
2. Importe uma pasta ou `.zip` com os arquivos do mod (`.kom`, `.ogg`, `.avi`, `general.ess`)
3. Escolha qual pack usar para cada arquivo
4. Clique em **Aplicar e Jogar** — o ElsEvo cuida do resto

---

## 🔀 Canais de distribuição

O ElsEvo tem dois canais **independentes**, cada um em seu próprio repositório:

| Canal | Repositório | Descrição |
|---|---|---|
| **Estável** | [`ElsEvo`](https://github.com/AyameJk/ElsEvo) | Versões testadas e recomendadas |
| **Beta** | [`ElsEvoBeta`](https://github.com/AyameJk/ElsEvoBeta) | Versões de teste, podem conter bugs |

Você pode alternar entre os canais em **Configurações → Beta apenas** — os dois compartilham o mesmo `AppId`, então instalar um substitui o outro automaticamente, sem duplicar o programa.

---

## 🛠️ Compilando localmente

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) e [Inno Setup 6](https://jrsoftware.org/isinfo.php).

```bash
# Publicar o executável (self-contained, single-file)
dotnet publish ElsEvo.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Compilar o instalador
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" ElsEvo.iss
```

O instalador final fica em `Output\ElsEvo-Setup.exe`.

### CI/CD

Todo push na `main` roda uma build de verificação. Toda **Release** publicada no GitHub dispara o pipeline completo: compila, gera o instalador, anexa na Release e atualiza o `version.json` automaticamente — veja `.github/workflows/compilar-e-publicar.yml`.

---

## 📄 Licença

Uso não comercial. Este é um projeto pessoal, sem vínculo oficial com a KOG Games, Nexon ou a Elsword IP.

---

## 🎨 Créditos

Alguns ícones usados na interface do ElsEvo são de terceiros, via [Flaticon](https://www.flaticon.com/):

- Ícone de download por [Magnific](https://www.flaticon.com/authors/magnific) — [Flaticon](https://www.flaticon.com/)

---

## 👤 Autor

Desenvolvido por **[AyameJk](https://github.com/AyameJk)**

<p align="center">
  <img width="422" height="422" alt="ElsEvo_Beta_logo" src="https://github.com/user-attachments/assets/02239044-35bc-4814-bfd7-ab76e7198eda" />
</p>

<p align="center">
  <i>「 "A natureza nos guia, e a esperança nos mantém firmes..." - Rena 」</i>
</p>
