# Arrematch - Leilões em Tempo Real

Aplicação web distribuída de leilões. Vários celulares dão lances no mesmo leilão ao mesmo tempo, e o sistema garante que só um lance vence, que todos veem o preço atualizado e que o leilão encerra sozinho com um vencedor.

## Stack

- **Backend:** .NET 10 (ASP.NET Core Minimal API + SignalR), Clean Architecture, CQRS com MediatR
- **Frontend:** Angular 18 + Tailwind (SPA servida pelo próprio backend em produção)
- **Banco:** PostgreSQL com EF Core (controle otimista via `RowVersion`)
- **Redis:** backplane do SignalR, contagem de espectadores e chaves de idempotência
- **RabbitMQ:** MassTransit, para o evento de "leilão ganho" (fila `email-queue`)
- **Observabilidade:** Prometheus (`/metrics`) e Grafana

## Os três problemas de computação distribuída

### 1. Sincronização e exclusão mútua
Quando vários celulares dão lance no mesmo leilão, só um pode vencer.
- O leilão tem um `RowVersion` (`src/Domain/Entities/Auction.cs`). O `SaveChanges` gera uma versão nova a cada alteração (`src/Infrastructure/Data/ApplicationDbContext.cs`), e o EF faz o UPDATE com `WHERE RowVersion = <versão lida>`.
- Se outro lance gravou antes, nenhuma linha é afetada, o EF lança `DbUpdateConcurrencyException`, a transação inteira é desfeita e a API responde **409**.
- Nenhum lock fica preso entre requisições, então o sistema escala com mais nós.
- Teste: 7 lances simultâneos de valor igual → 1 aceito, 6 com 409.

### 2. Comunicação distribuída
- **Tempo real:** SignalR com um grupo por leilão. O lance é anunciado **depois do commit**, então ninguém vê um lance que não existe no banco.
- **Entre instâncias:** o backplane do Redis faz o evento sair de qualquer nó e chegar a todos os clientes conectados, em qualquer nó (testado com duas instâncias em portas diferentes).
- **Presença:** o número de espectadores fica num conjunto no Redis, então conta conexões de todos os nós.
- **Assíncrona:** ao fim do leilão, o evento `AuctionWonEvent` vai para o RabbitMQ.

### 3. Consistência de dados
- **Forte no banco:** a regra de lance e a gravação são atômicas (um único `SaveChanges`).
- **Eventual nos celulares, com detecção de lacunas:** cada lance recebe um número de sequência por leilão (um relógio lógico). O cliente ignora sequências repetidas ou antigas, e se pular uma (perdeu mensagem) ou reconectar, busca o estado oficial no servidor.
- **Idempotência:** o cabeçalho `X-Idempotency-Key` garante que um reenvio (duplo clique, rede instável) não cria dois lances. A chave é reservada com `SET NX` no Redis, então funciona entre instâncias. Repetir a mesma chave devolve a mesma resposta.
- **Encerramento seguro com várias instâncias:** cada instância roda o worker de abertura/encerramento, mas cada leilão é gravado com `RowVersion`. Só uma consegue fechar o leilão e anunciar o vencedor.

## Como rodar

### Pré-requisitos
- Docker e Docker Compose
- .NET 10 SDK
- Node.js 20+ e npm

### 1. Infraestrutura
```bash
docker compose up -d postgres redis rabbitmq
```

> Se você já rodou uma versão anterior deste projeto, apague o volume do banco antes (`docker compose down -v`). O banco é criado com `EnsureCreated`, que não altera tabelas existentes.

### 2. Frontend (build de produção)
```bash
cd src/Web/ClientApp
npm ci
npx ng build --configuration production
cd ../../..
```

### 3. Backend
```bash
ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://localhost:5000 \
  dotnet run --project src/Web/Web.csproj
```
Abra `http://localhost:5000`. Em desenvolvimento (`dotnet run` sem o `ASPNETCORE_ENVIRONMENT=Production`), o Angular roda pelo proxy de desenvolvimento em `https://localhost:44447`, como antes.

### 4. Celulares na mesma demo
Os celulares precisam acessar o servidor pela rede. Use um túnel HTTPS, por exemplo:
```bash
cloudflared tunnel --url http://localhost:5000
```
Mostre a URL `https://...` gerada num QR code no slide. Cada participante cria a própria conta em **Entrar → Registre-se**.

### 5. Demonstrar duas instâncias (distribuição)
```bash
INSTANCE_NAME=no-A ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://localhost:5000 dotnet run --project src/Web/Web.csproj
INSTANCE_NAME=no-B ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://localhost:5001 dotnet run --project src/Web/Web.csproj
```
Um celular em `:5000` e outro em `:5001` acompanhando o mesmo leilão: o lance feito em um aparece no outro. `GET /api/server-info` mostra qual nó respondeu.

## Testes

```bash
dotnet test tests/Domain.UnitTests
dotnet test tests/Application.UnitTests
# Requer Docker (Testcontainers sobe PostgreSQL, Redis e RabbitMQ descartáveis):
dotnet test tests/Infrastructure.IntegrationTests
dotnet test tests/Application.FunctionalTests
```

## Limitações conhecidas

- **Publicação no RabbitMQ é "melhor esforço":** se o broker cair exatamente no momento do encerramento, o evento de vencedor pode não sair. O encerramento em si é gravado e anunciado normalmente. Para garantia total seria preciso um *transactional outbox*.
- **E-mail ao vencedor é simulado** (o consumidor só registra no log).
- **Espectadores contam conexões**, não pessoas distintas.
- **Usuário admin de exemplo** (`administrator@localhost`) é criado com senha conhecida. Troque antes de expor o sistema na internet.

## Observabilidade

Métricas em `/metrics`:
- `app_bids_total`: lances aceitos
- `app_bid_conflicts_total`: lances perdidos por conflito de concorrência
- `app_bid_amount`: distribuição dos valores

## Licença

MIT
