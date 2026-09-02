# Desktop Splash Screen Together

Complemento de compatibilidade do **Desktop Splash Screen** para o Skyrim
Together Reborn. O projeto mantém a versão `1.2.0.0`.

## Links

- Mod na Nexus: https://www.nexusmods.com/skyrimspecialedition/mods/190395
- Dependência original: https://www.nexusmods.com/skyrimspecialedition/mods/83470
- Código original MIT: https://github.com/Exit-9B/SplashScreen

## Funcionamento

Este projeto é somente um complemento. Ele exige que o Desktop Splash Screen
original esteja instalado e verifica a presença de `_SplashScreen.dll` e
`_SplashScreen_preload.txt` antes de iniciar.

O launcher:

- mostra `Data/Interface/DesktopSplashTogether/splash.gif` ou `splash.png`;
- usa `Data/Interface/splash.png` do mod original somente como alternativa;
- desenha o texto animado e a barra de carregamento;
- inicia o `SkyrimTogether.exe` já instalado pelo usuário;
- fecha a splash quando detecta a janela visível do jogo.

Ele não inclui, substitui ou modifica o Desktop Splash Screen original nem o
`SkyrimTogether.exe`. Os arquivos personalizados ficam em uma pasta isolada,
evitando conflitos com a dependência.

## Instalação

1. Instale e ative o Desktop Splash Screen original.
2. Instale o complemento pelo Vortex ou MO2.
3. Adicione `Data/SkyrimTogetherReborn/DesktopSplashTogetherLauncher.exe` como
   executável no gerenciador.
4. Inicie o Skyrim Together por essa nova entrada.

No MO2, execute o launcher dentro do próprio MO2 para que ele veja os arquivos
virtuais da dependência.

## Segurança e compilação

O código é C# legível e não realiza acesso à rede, injeção em processos,
alteração do Registro, elevação de privilégios ou modificação do
`SkyrimTogether.exe`. Consulte `BUILDING.md` para reproduzir a compilação.

## Licenças

- `LICENSE`: licença MIT do launcher criado por RuanFCatarino.
- `ORIGINAL-MOD-LICENSE`: licença MIT integral do projeto de Parapets / Exit-9B.
- `THIRD-PARTY-NOTICES.md`: créditos e avisos sobre terceiros.

---

## English summary for reviewers

This repository contains the auditable C# source for an unofficial dependency
add-on to the original Desktop Splash Screen. It requires the original mod at
runtime but does not include, replace or modify its DLL, preload marker or image.
It starts the user's existing `SkyrimTogether.exe` and does not perform network
access, process injection, registry changes, privilege elevation or executable
modification. Reproducible build instructions are available in `BUILDING.md`.

