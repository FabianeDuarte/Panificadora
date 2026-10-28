# Distribuidora de Panificação — Site + API (ASP.NET Core, .NET 10)

Complemento do app MAUI **PanificadoraVendas**. Um único projeto contém:
- **Site de gestão** (`wwwroot`) — cadastros, pedidos recebidos com corte, vendas e contas a receber;
- **API do site** (`/api/admin/*`);
- **API do aplicativo** (`/api/app/*`) — usada pelos botões "Baixar" e "Enviar pedidos" do app.

Banco: **SQLite** (arquivo `panificadora.db`, criado e populado com dados de exemplo na primeira execução).

## ⚠ Atualizando de uma versão anterior (sem login)
Apague o arquivo `panificadora.db` (na pasta do projeto) antes de executar. O banco usa `EnsureCreated()`,
que não acrescenta as novas tabelas (`Usuarios`, `Sessoes`) a um banco já existente.

## Login
| Usuário | Senha | Perfil | Acessa |
|---|---|---|---|
| `admin` | `admin123` | Admin | Site de gestão |
| `henrique` | `123456` | Vendedor (Henrique) | Aplicativo |
| `marcos` | `123456` | Vendedor (Marcos) | Aplicativo |

**Troque as senhas no primeiro acesso** (menu "Trocar senha" no site; cadastro em *Sistema > Usuários de acesso*).

Como funciona:
- `POST /api/auth/login` devolve um **token** (guardado só como hash SHA-256 no banco). Admin vale 12 h; vendedor vale 30 dias.
- `/api/admin/*` só aceita perfil **Admin**; `/api/app/*` só perfil **Vendedor**. O vendedor das chamadas do app é **sempre o do token** (o app não consegue pedir dados de outro vendedor).
- Senhas com **PBKDF2-SHA256** (100 mil iterações, salt aleatório). Mínimo de 6 caracteres.
- 5 senhas erradas bloqueiam o usuário por 5 minutos.
- Trocar/redefinir a senha ou inativar o usuário derruba as sessões dele.
- Gestão de usuários (criar, redefinir senha, inativar, vincular a vendedor) no site, menu **Sistema > Usuários de acesso**.
- Sem HTTPS o token trafega em texto puro: **para uso real, publique com HTTPS**.

## Como abrir
1. Visual Studio 2026 com a carga **ASP.NET e desenvolvimento para a Web**.
2. Abra a pasta/projeto `PanificadoraServidor.csproj` (ou crie um projeto "ASP.NET Core Vazio" e substitua pelos arquivos).
3. Execute (perfil **http**, porta 5080). O site abre em `http://localhost:5080`.
   O pacote `Microsoft.EntityFrameworkCore.Sqlite` é restaurado automaticamente.

## Ligar o app MAUI
Na tela de **login** do app informe usuário (`henrique` / `123456`) e a URL da API:
- Emulador Android: `http://10.0.2.2:5080`
- Windows: `http://localhost:5080`
- Celular real: `http://IP-DO-PC:5080` (libere a porta 5080 no firewall)

## O que o vídeo pede e onde está
| Requisito | Onde |
|---|---|
| Cadastros com status ativo/inativo (excluir = inativar): clientes, produtos, vendedores, grupos de produtos, cidades, estados | Menu **Cadastros** |
| Forma de pagamento (boleto, Pix) e condição de pagamento (7/14, 7/14/21...) com multa, juros, desconto e parcelas em % | **Formas** e **Condições de pagamento** |
| Praça do vendedor; **não pode haver dois vendedores concorrendo na mesma praça** | **Praças** (validação no cadastro) e no cliente novo vindo do app |
| Roteiro do vendedor com dia da semana e sequência (10, 20, 30...) | **Roteiro** |
| Receber pedidos do app (12h e 19h) e clientes novos | `POST /api/app/pedidos` (reenvio não duplica) |
| **Corte**: quantidade pedida x quantidade liberada quando falta estoque | **Pedidos recebidos** → "Aplicar corte" + ajuste manual |
| Pedido vira venda; **não vende com estoque negativo** | "Faturar" / "Faturar todos" |
| Venda gera **contas a receber** (duplicata `123/1`, `123/2`) conforme a condição | Automático ao faturar |
| Saldo devedor do cliente aparece no app | Soma das contas em aberto |
| Última compra de cada produto por cliente no app | Calculada das vendas faturadas |
| Sem limite de crédito (empresa generosa) | Não implementado de propósito |

## Premissas que adotei (o vídeo não fecha o cálculo)
- **Multa** = % fixo sobre o valor, se pagar após o vencimento.
- **Juros** = % **ao mês**, proporcional aos dias de atraso.
- **Desconto** = % **por dia de antecipação** (1% ao dia, como dito no vídeo), limitado a 100%.
- Corte automático: divide o estoque proporcionalmente ao pedido; a sobra do arredondamento vai primeiro para clientes sem débito. O gestor sempre pode editar o "liberado".
- Cliente novo do app numa cidade sem vendedor: a praça passa a ser do vendedor que cadastrou. Se a praça for de outro vendedor, o cliente é criado, mas sem roteiro, e o aviso volta na resposta da API.

## Observações técnicas
- O banco é criado com `EnsureCreated()` (sem migrations). Se mudar as entidades, apague `panificadora.db`.
- Login por token próprio (sem pacotes extras). Para uso real, use HTTPS e troque as senhas iniciais.
