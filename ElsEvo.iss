; Script gerado a partir do Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "ElsEvo Beta"
#define MyAppVersion "1.0.450"
#define MyAppPublisher "AyameJk"
#define MyAppURL "https://www.example.com/"
#define MyAppExeName "ElsEvo.exe"
#define MyAppAssocName MyAppName + " File"
#define MyAppAssocExt ".myp"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt
#define DoubleAmp(Value) StringChange(Value, "&", "&&")
#define EscapeConstArgument(Value) StringChange(StringChange(StringChange(Value, "%", "%25"), ",", "%2c"), "}", "%7d")

; Pasta real onde "dotnet publish -c Release -r win-x64 --self-contained true
; -p:PublishSingleFile=true -o <PublishDir>" grava os arquivos. Ajuste aqui se você
; publicar em outro lugar — usada em [Files] abaixo, só precisa mudar num lugar só.
#define PublishDir "C:\Users\Victorr\OneDrive\ProjetoElsEvo\ElsEvoBeta\bin\Release\net8.0-windows\win-x64"

[Setup]
; IMPORTANTE: esse AppId precisa ser IDÊNTICO ao AppId do .iss da versão ESTÁVEL — é o
; que permite instalar a build beta "por cima" da estável (e vice-versa) no mesmo lugar,
; em vez do Inno Setup tratar como dois programas diferentes instalados lado a lado.
AppId={{8910440C-BF7A-494D-B5AD-7F0A4DA85D60}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\ElsEvo
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
DisableProgramGroupPage=yes
OutputDir=C:\Users\Victorr\Downloads\OutputBeta

; IMPORTANTE: esse nome precisa bater EXATAMENTE (maiúsculas/minúsculas incluso) com o
; que o atualizar-versao.yml monta na URL do version.json
; (.../releases/download/{tag}/ElsEvo-Setup.exe). Nome de repositório no GitHub é
; case-insensitive, mas nome de asset anexado numa Release é case-sensitive de verdade —
; já quase deu 404 silencioso por causa disso, não mude só de um lado.
OutputBaseFilename=ElsEvo-Setup

; Ícone dentro de Assets\, não solto na raiz do repo (evita duplicar o .ico em dois lugares).
SetupIconFile=C:\Users\Victorr\OneDrive\ProjetoElsEvo\ElsEvoBeta\Assets\icone_app.ico
SolidCompression=yes
WizardStyle=modern dynamic

; ===== Metadados do arquivo — sem isso a aba "Detalhes" do .exe no Windows mostra
; "Versão do arquivo 0.0.0.0" e campos de empresa/copyright vazios. =====
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoCopyright=© {#MyAppPublisher}
VersionInfoDescription=Instalador do ElsEvo (Beta)
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
; Nota: VersionInfoLanguage não existe no Inno Setup 7.1.0 (dá erro de compilação se
; tentar usar) — o idioma dos metadados fica como "Neutro" mesmo, é só cosmético.

; ===== Fechamento automático do ElsEvo durante a atualização =====
; Reforço/rede de segurança pra quando alguém rodar o instalador manualmente com o app
; ainda aberto — o app já se fecha sozinho antes de chamar o instalador no fluxo normal
; (ver MainWindow.BaixarEInstalarAtualizacaoAsync).
AppMutex=ElsEvo_MutexPrincipal
CloseApplications=yes
RestartApplications=yes

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files.

[Registry]
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#DoubleAmp(MyAppName)}}"; Flags: nowait postinstall skipifsilent
