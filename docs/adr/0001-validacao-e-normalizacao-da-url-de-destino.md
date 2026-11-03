# 0001 — Validação e normalização da URL de destino

- Status: aceito
- Data: 2026-11-03

## Contexto

O LinkGuardiao armazena uma URL de destino por link e responde `302` com ela no header `Location`.
A validação anterior (`UrlSafety.IsSafeHttpUrl`) só checava esquema `http`/`https`, presença de host e
tamanho. Com isso eram aceitos:

- credenciais embutidas (`https://confiavel.com@malicioso.com`), úteis para phishing;
- destinos internos: `localhost`, `169.254.169.254` (metadata de cloud), redes privadas e as mesmas
  faixas escritas de forma alternativa (`2130706433`, `0x7f000001`, `127.1`, `[::ffff:127.0.0.1]`);
- caracteres de controle e Unicode cru, porque a URL era persistida e redirecionada via `Uri.ToString()`,
  que devolve a forma *não escapada* (CR/LF e host IDN em Unicode chegavam ao header `Location`).

O encurtador não pode virar um trampolim para a rede interna de quem clica nem para endpoints de
metadata, e o servidor não deve emitir headers inválidos.

## Decisão

Centralizar a política em `Application/Security`:

- `UrlSafety.TryNormalize` valida e devolve a forma canônica ASCII (`Uri.AbsoluteUri` com host em
  punycode via `Uri.IdnHost`). Rejeita: esquema diferente de http/https, userinfo, caracteres de
  controle e `\`, porta inválida, URL normalizada > 2000 caracteres (tamanho da coluna
  `OriginalUrl`; o limite antigo de 2048 deixava passar URLs que quebravam o INSERT com 500), rótulos DNS inválidos, punycode inválido, hosts
  internos (`localhost`, `*.local`, `*.internal`, `*.arpa`, rótulo único).
- Hosts cujo último rótulo é numérico são interpretados pelo algoritmo IPv4 do WHATWG URL Standard
  (`IpAddressPolicy.TryParseWhatwgIpv4`), o mesmo dos navegadores, para que a decisão bata com o IP que o
  navegador realmente usaria. A checagem roda sobre `IdnHost`, depois do mapeamento IDNA (dígitos
  fullwidth e `。` viram ASCII).
- `IpAddressPolicy.IsPublic` usa lista de bloqueio para IPv4 e lista de permissão para IPv6
  (somente `2000::/3`, menos 6to4, Teredo/`2001::/23` e documentação). IPv4-mapped é avaliado pelo IPv4 embutido.
- A mesma função é usada no validador FluentValidation (mensagem específica por motivo), no `LinkService`
  (antes de consumir o limite diário) e no `RedirectController` (revalida links legados; se falhar, `410`
  sem enfileirar analytics).

## Consequências

- Links existentes que apontem para destinos agora proibidos deixam de redirecionar (`410`).
- URLs passam a ser gravadas em forma escapada/punycode; a exibição no dashboard mostra `xn--...` para IDN.
- Não há resolução DNS: domínios públicos que resolvem para IP privado continuam aceitos (ver ADR 0002).
- A validação de IDN depende de ICU no runtime (`InvariantGlobalization=false` já era o padrão do projeto).
