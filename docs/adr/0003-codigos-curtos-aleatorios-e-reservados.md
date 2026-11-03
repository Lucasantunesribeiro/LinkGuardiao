# 0003 — Códigos curtos aleatórios, formato restrito e lista de reservados

- Status: aceito
- Data: 2026-11-03

## Contexto

Os códigos são gerados com `RandomNumberGenerator` (6 caracteres alfanuméricos) e expostos em duas rotas:
`/r/{codigo}` (qualquer tamanho) e `/{codigo:length(6)}` na raiz, que compartilha o espaço de nomes com
rotas fixas da API (`/health` tem exatamente 6 caracteres) e do frontend (`/login`, `/dashboard`). Não há
aliases escolhidos pelo usuário. As rotas públicas aceitavam qualquer string como código e a repassavam
ao repositório/cache; o frontend interpolava o parâmetro da rota direto nas URLs da API.

## Decisão

- `ShortCodePolicy.IsReserved`: lista (case-insensitive) de nomes de rotas e palavras sensíveis; o gerador
  descarta candidatos reservados.
- `ShortCodePolicy.IsWellFormed`: rotas públicas aceitam apenas `[A-Za-z0-9_-]{1,32}` (32 para tolerar
  códigos legados migrados do SQLite); fora disso respondem `404` sem consultar o banco.
- O frontend aplica a mesma regex e usa `encodeURIComponent` antes de chamar a API ou redirecionar.
- Continuamos sem aliases personalizados; se forem adicionados, devem passar por `IsWellFormed` e `IsReserved`.

## Consequências

- Um link nunca recebe código que colida com `/health` ou com rotas do SPA.
- Entradas malformadas não chegam ao DynamoDB/Redis.
- Adicionar uma nova rota fixa de 6 caracteres na raiz exige incluí-la na lista de reservados.
