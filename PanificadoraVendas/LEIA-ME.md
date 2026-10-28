# Panificação B2B — App do Vendedor (.NET MAUI, .NET 10, Visual Studio 2026)

## Como abrir no Visual Studio 2026
1. Visual Studio 2026 com a carga de trabalho **".NET Multi-platform App UI development"**.
2. **Novo projeto → .NET MAUI App**, nome **`PanificadoraVendas`** (o nome precisa ser igual, por causa dos namespaces), framework **.NET 10**.
3. Apague do projeto novo: `MainPage.xaml` e `MainPage.xaml.cs`.
4. Copie desta pasta para o projeto (substituindo quando perguntar):
   `App.xaml`, `App.xaml.cs`, `AppShell.xaml`, `AppShell.xaml.cs`, `MauiProgram.cs`
   e as pastas `Models`, `Services`, `ViewModels`, `Views`.
5. Compile e execute (Android Emulator ou Windows Machine). Não precisa de pacotes NuGet extras.

> API em HTTP no Android: em `Platforms/Android/AndroidManifest.xml` adicione `android:usesCleartextTraffic="true"` na tag `<application>`.
> No emulador Android, o PC (localhost) é `http://10.0.2.2:PORTA`.

## Login
- Ao abrir, o app mostra a **tela de login** (usuário, senha e URL do servidor). A sessão fica salva: nas próximas vezes entra direto, até tocar em **Sincronizar > Sair**.
- **Com servidor:** valida no servidor e recebe um token (guardado no `SecureStorage`). Só perfil *Vendedor* entra; o administrador usa o site.
- **Sem internet:** entra com um usuário/senha que já tenha entrado neste aparelho (hash PBKDF2 guardado no `SecureStorage`). Nesse modo dá para tirar pedidos, mas para baixar/enviar é preciso entrar de novo com internet.
- **Demonstração (URL vazia):** usuário `demo`, senha `demo`.
- **Um aparelho, vários vendedores:** ao entrar outro vendedor, os dados do anterior são apagados do aparelho; se ele tiver pedidos ainda não enviados, a troca é bloqueada até ele enviar.
- O vendedor das chamadas à API é o do token, não mais um campo digitado.

## O que o vídeo pede e onde está no app
| Requisito do vídeo | Onde |
|---|---|
| Cada vendedor tem uma carteira/roteiro por dia da semana | Aba **Roteiro** (seletor de dia) |
| Sequência de visita em 10, 20, 30 para poder inserir 15 | `RoteiroService.CalcularSequencia` (renumera se não houver brecha) |
| Cadastrar cliente novo na rua e encaixá-lo no roteiro | **Novo cliente** (campo "Posição na rota"); vai junto com os pedidos |
| Ao terminar um cliente, ir ao próximo | Botão **Próximo cliente da rota** + status de visita |
| Ao abrir o cliente, listar produtos já comprados com a **quantidade da última compra** (e a data, que pode não ser a da semana anterior) | **PedidoPage** (`UltimaTexto`) |
| Preço visível | Em cada produto |
| Mostrar **quanto o cliente está devendo** | Roteiro, Clientes e Pedido (vermelho se deve) |
| Produto novo e em promoção destacados por **cor** | Laranja = promoção, azul = novo; aparecem no topo mesmo que o cliente nunca tenha comprado |
| Pedido **não é enviado na hora** (internet ruim) | Pedido fica salvo offline no aparelho |
| Envio em 2 horários: 12h (manhã) e 19h (tarde) | Aba **Envio**: lotes 12h/19h, próximo envio e botão **Enviar pedidos agora**; falhou = continua pendente |
| Botão para **baixar** lista de preços e clientes | Aba **Sincronizar** |
| Condição de pagamento (7/14, 7/14/21, entrada Pix...) com forma de pagamento, multa, juros, desconto | Escolhida no pedido, com prévia das parcelas (contas a receber) |
| Sem saldo de estoque no app (vende o que tem) | Não há controle de estoque no app |

## Regras que ficam no servidor/site (fora do app)
- Dois vendedores da mesma empresa **não** podem atender a mesma praça.
- Cadastros com status ativo/inativo: clientes, produtos, grupos, vendedores, cidades, estados, formas e condições de pagamento.
- Recebimento dos pedidos, **corte** (quantidade pedida x liberada), venda sem estoque negativo, geração do **contas a receber** (nota/parcela, ex.: 123/1, 123/2) e faturamento.

## Contrato da API (JSON)
- `GET  /api/app/dados?vendedorId=1` → `{ clientes[], produtos[], ultimasCompras[], condicoes[] }`
- `POST /api/app/pedidos` ← `{ vendedorId, clientesNovos[], pedidos[] }` (200 = recebido)
  Clientes novos têm `id` negativo; os pedidos deles usam esse mesmo `clienteId` temporário.

Sem URL configurada, o app roda em **modo demonstração** com dados de exemplo.
