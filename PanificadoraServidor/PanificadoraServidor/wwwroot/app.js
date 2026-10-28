const $ = s => document.querySelector(s);
const esc = v => String(v ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const brl = n => Number(n || 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
const dt = s => s ? new Date(s).toLocaleDateString('pt-BR') : '';
const DIAS = ['Domingo', 'Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado'];

const TOKEN = 'token';
const cab = () => ({ 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + (sessionStorage.getItem(TOKEN) || '') });

async function resposta(r) {
  if (r.status === 401) { sessionStorage.removeItem(TOKEN); mostrarLogin(); throw new Error('Sessão expirada. Entre novamente.'); }
  if (!r.ok) {
    let m = r.statusText;
    try { const j = await r.json(); m = j.erro || JSON.stringify(j); } catch { }
    throw new Error(m);
  }
  return r.status === 204 ? null : r.json();
}
const api = async (path, opt = {}) => resposta(await fetch('/api/admin' + path, { ...opt, headers: cab() }));

// ---------- configuração dos cadastros ----------
const ATIVO = { k: 'ativo', l: 'Ativo', t: 'bool' };
const E = {
  estados: { t: 'Estados', f: [{ k: 'sigla', l: 'Sigla' }, { k: 'nome', l: 'Nome' }, ATIVO] },
  cidades: { t: 'Cidades', f: [{ k: 'nome', l: 'Nome' }, { k: 'estadoId', l: 'Estado', ref: 'estados', s: 'sigla' }, ATIVO] },
  grupos: { t: 'Grupos de produtos', f: [{ k: 'descricao', l: 'Descrição' }, ATIVO] },
  produtos: { t: 'Produtos', f: [{ k: 'descricao', l: 'Descrição' }, { k: 'grupoProdutoId', l: 'Grupo', ref: 'grupos', s: 'descricao' }, { k: 'unidade', l: 'Unid.' },
    { k: 'preco', l: 'Preço', t: 'num' }, { k: 'precoPromocional', l: 'Preço promo', t: 'num' }, { k: 'emPromocao', l: 'Promoção', t: 'bool' },
    { k: 'produtoNovo', l: 'Novo', t: 'bool' }, { k: 'estoque', l: 'Estoque', t: 'int' }, ATIVO] },
  vendedores: { t: 'Vendedores', f: [{ k: 'nome', l: 'Nome' }, { k: 'email', l: 'E-mail' }, { k: 'telefone', l: 'Telefone' }, ATIVO] },
  clientes: { t: 'Clientes', f: [{ k: 'razaoSocial', l: 'Razão social' }, { k: 'nomeFantasia', l: 'Fantasia' }, { k: 'cpfCnpj', l: 'CPF/CNPJ' },
    { k: 'telefone', l: 'Telefone' }, { k: 'endereco', l: 'Endereço' }, { k: 'bairro', l: 'Bairro' }, { k: 'cidadeId', l: 'Cidade', ref: 'cidades', s: 'nome' }, ATIVO] },
  formas: { t: 'Formas de pagamento', f: [{ k: 'descricao', l: 'Descrição' }, ATIVO] },
  pracas: { t: 'Praças dos vendedores', f: [{ k: 'vendedorId', l: 'Vendedor', ref: 'vendedores', s: 'nome' }, { k: 'cidadeId', l: 'Cidade (praça)', ref: 'cidades', s: 'nome' }] },
  roteiro: { t: 'Roteiro dos vendedores', f: [{ k: 'vendedorId', l: 'Vendedor', ref: 'vendedores', s: 'nome' }, { k: 'clienteId', l: 'Cliente', ref: 'clientes', s: 'razaoSocial' },
    { k: 'diaSemana', l: 'Dia', t: 'dia' }, { k: 'sequencia', l: 'Sequência', t: 'int' }] },
};
const cache = {};
const lista = async n => (cache[n] = await api('/' + n));
let atual = '';

function valorCelula(f, v) {
  if (f.ref) { const o = (cache[f.ref] || []).find(x => x.id === v); return o ? o[f.s] : v; }
  if (f.t === 'bool') return v ? 'Sim' : 'Não';
  if (f.t === 'dia') return DIAS[v] ?? v;
  if (f.t === 'num') return brl(v);
  return v;
}

async function pgCrud(n) {
  const c = E[n];
  await Promise.all(c.f.filter(f => f.ref).map(f => lista(f.ref)));
  const rows = await lista(n);
  const ordenadas = n === 'roteiro' ? [...rows].sort((a, b) => a.vendedorId - b.vendedorId || a.diaSemana - b.diaSemana || a.sequencia - b.sequencia) : rows;
  $('#conteudo').innerHTML = `<div class="barra"><button class="primario" onclick="edit('${n}')">+ Novo</button></div>
  <table><tr>${c.f.map(f => `<th>${f.l}</th>`).join('')}<th></th></tr>
  ${ordenadas.map(r => `<tr>${c.f.map(f => `<td>${esc(valorCelula(f, r[f.k]))}</td>`).join('')}
    <td><button onclick="edit('${n}',${r.id})">Editar</button> <button class="perigo" onclick="del('${n}',${r.id})">${r.ativo === undefined ? 'Excluir' : 'Inativar'}</button></td></tr>`).join('')}</table>`;
}

function campo(f, v, extra = '') {
  const id = 'f_' + f.k;
  if (f.t === 'bool') return `<label><input type="checkbox" id="${id}" ${v ? 'checked' : ''}> ${f.l}</label>`;
  if (f.ref || f.t === 'dia') {
    const ops = f.t === 'dia' ? DIAS.map((d, i) => ({ id: i, t: d }))
      : (cache[f.ref] || []).filter(x => x.ativo !== false || x.id === v).map(x => ({ id: x.id, t: x[f.s] }));
    return `<label>${f.l}</label><select id="${id}"><option value="">Selecione</option>${ops.map(o => `<option value="${o.id}" ${o.id === v ? 'selected' : ''}>${esc(o.t)}</option>`).join('')}</select>`;
  }
  const tipo = f.t === 'num' || f.t === 'int' ? 'number' : 'text';
  return `<label>${f.l}</label><input type="${tipo}" ${f.t === 'num' ? 'step="0.01"' : ''} id="${id}" value="${esc(v ?? '')}">`;
}

function ler(f) {
  const el = $('#f_' + f.k);
  if (f.t === 'bool') return el.checked;
  if (f.t === 'num') return parseFloat(el.value) || 0;
  if (f.t === 'int' || f.ref || f.t === 'dia') return parseInt(el.value) || 0;
  return el.value;
}

window.edit = (n, id) => {
  const c = E[n];
  const o = id ? cache[n].find(r => r.id === id) : { ativo: true };
  $('#dlgTitle').textContent = c.t;
  $('#dlgBody').innerHTML = c.f.map(f => campo(f, o[f.k])).join('');
  $('#dlgSave').onclick = async e => {
    e.preventDefault();
    const body = { ...o };
    c.f.forEach(f => body[f.k] = ler(f));
    try { await api('/' + n + (id ? '/' + id : ''), { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) }); $('#dlg').close(); go(atual); }
    catch (x) { alert(x.message); }
  };
  $('#dlg').showModal();
};

window.del = async (n, id) => {
  if (!confirm('Confirma?')) return;
  try { await api('/' + n + '/' + id, { method: 'DELETE' }); go(atual); } catch (x) { alert(x.message); }
};

// ---------- condições de pagamento ----------
let parcelasTmp = [];
async function pgCond() {
  await lista('formas');
  const rows = await api('/condicoes'); cache.condicoes = rows;
  $('#conteudo').innerHTML = `<div class="barra"><button class="primario" onclick="editCond()">+ Nova condição</button></div>
  <table><tr><th>Descrição</th><th>Parcelas</th><th>Multa</th><th>Juros</th><th>Desconto</th><th>Ativo</th><th></th></tr>
  ${rows.map(c => `<tr><td>${esc(c.descricao)}</td>
    <td>${c.parcelas.map(p => `${p.numero}ª: ${p.dias}d ${p.percentual}% (${esc((cache.formas.find(f => f.id === p.formaPagamentoId) || {}).descricao)})`).join('<br>')}</td>
    <td>${c.multaPercentual}%</td><td>${c.jurosPercentual}%</td><td>${c.descontoPercentual}%</td><td>${c.ativo ? 'Sim' : 'Não'}</td>
    <td><button onclick="editCond(${c.id})">Editar</button> <button class="perigo" onclick="del('condicoes',${c.id})">Inativar</button></td></tr>`).join('')}</table>`;
}

function desenhaParcelas() {
  const forma = id => (cache.formas || []).filter(f => f.ativo).map(f => `<option value="${f.id}" ${f.id === id ? 'selected' : ''}>${esc(f.descricao)}</option>`).join('');
  $('#parcelas').innerHTML = parcelasTmp.map((p, i) => `<div class="barra"><b>${i + 1}ª</b>
    dias <input type="number" style="width:70px" value="${p.dias}" onchange="parcelasTmp[${i}].dias=+this.value">
    % <input type="number" step="0.01" style="width:80px" value="${p.percentual}" onchange="parcelasTmp[${i}].percentual=+this.value">
    <select onchange="parcelasTmp[${i}].formaPagamentoId=+this.value">${forma(p.formaPagamentoId)}</select>
    <button type="button" onclick="parcelasTmp.splice(${i},1);desenhaParcelas()">✕</button></div>`).join('');
}
window.parcelasTmp = parcelasTmp; window.desenhaParcelas = desenhaParcelas;

window.editCond = id => {
  const o = id ? cache.condicoes.find(c => c.id === id) : { descricao: '', multaPercentual: 5, jurosPercentual: 2, descontoPercentual: 1, ativo: true, parcelas: [] };
  parcelasTmp.length = 0;
  o.parcelas.forEach(p => parcelasTmp.push({ ...p }));
  if (!parcelasTmp.length) parcelasTmp.push({ dias: 7, percentual: 100, formaPagamentoId: (cache.formas.find(f => f.ativo) || {}).id });
  $('#dlgTitle').textContent = 'Condição de pagamento';
  $('#dlgBody').innerHTML = `<label>Descrição</label><input type="text" id="c_desc" value="${esc(o.descricao)}">
    <label>Multa por atraso (%)</label><input type="number" step="0.01" id="c_multa" value="${o.multaPercentual}">
    <label>Juros (% ao mês, proporcional aos dias de atraso)</label><input type="number" step="0.01" id="c_juros" value="${o.jurosPercentual}">
    <label>Desconto por dia de antecipação (%)</label><input type="number" step="0.01" id="c_desc2" value="${o.descontoPercentual}">
    <label><input type="checkbox" id="c_ativo" ${o.ativo ? 'checked' : ''}> Ativo</label>
    <h4>Parcelas</h4><div id="parcelas"></div>
    <button type="button" onclick="parcelasTmp.push({dias:(parcelasTmp.length+1)*7,percentual:0,formaPagamentoId:(cache.formas.find(f=>f.ativo)||{}).id});desenhaParcelas()">+ Parcela</button>`;
  desenhaParcelas();
  $('#dlgSave').onclick = async e => {
    e.preventDefault();
    const body = { descricao: $('#c_desc').value, multaPercentual: +$('#c_multa').value, jurosPercentual: +$('#c_juros').value,
      descontoPercentual: +$('#c_desc2').value, ativo: $('#c_ativo').checked,
      parcelas: parcelasTmp.map((p, i) => ({ numero: i + 1, dias: p.dias, percentual: p.percentual, formaPagamentoId: p.formaPagamentoId })) };
    try { await api('/condicoes' + (id ? '/' + id : ''), { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) }); $('#dlg').close(); go('condicoes'); }
    catch (x) { alert(x.message); }
  };
  $('#dlg').showModal();
};

// ---------- pedidos: corte e faturamento ----------
async function pgPedidos() {
  const peds = await api('/pedidos?status=Recebido');
  const m = {};
  peds.forEach(p => p.itens.forEach(i => {
    const r = (m[i.produtoId] ??= { d: i.descricao, est: i.estoque, ped: 0, lib: 0 });
    r.ped += i.qtdPedida; r.lib += i.qtdLiberada;
  }));
  const resumo = Object.values(m);
  $('#conteudo').innerHTML = `<div class="barra">
    <button class="primario" onclick="corte()">Aplicar corte automático</button>
    <button class="primario" onclick="faturarTodos()">Faturar todos</button>
    <span class="mini">Se a soma pedida passa do estoque, o estoque é dividido proporcionalmente (clientes sem débito recebem a sobra primeiro). Você pode ajustar a quantidade liberada.</span></div>
    ${resumo.length ? `<table><tr><th>Produto</th><th>Estoque</th><th>Total pedido</th><th>Total liberado</th><th>Situação</th></tr>
    ${resumo.map(r => `<tr class="${r.ped > r.est ? 'alerta' : ''}"><td>${esc(r.d)}</td><td>${r.est}</td><td>${r.ped}</td><td>${r.lib}</td>
      <td>${r.ped > r.est ? `FALTA ${r.ped - r.est} → fazer corte` : 'Estoque suficiente'}</td></tr>`).join('')}</table><br>` : '<p>Nenhum pedido recebido.</p>'}
    ${peds.map(p => `<div class="card"><h3>#${p.id} • ${esc(p.cliente)} <span class="mini">(vendedor ${esc(p.vendedor)} • ${new Date(p.dataHora).toLocaleString('pt-BR')} • ${esc(p.condicao)})</span></h3>
      <div class="${p.clienteDeve > 0 ? 'ruim' : 'ok'}">${p.clienteDeve > 0 ? 'Cliente deve ' + brl(p.clienteDeve) : 'Cliente sem débitos'}</div>
      <table><tr><th>Produto</th><th>Estoque</th><th>Pedido</th><th>Liberado</th><th>Preço</th></tr>
      ${p.itens.map(i => `<tr><td>${esc(i.descricao)}</td><td>${i.estoque}</td><td>${i.qtdPedida}</td>
        <td><input class="liberado" type="number" min="0" max="${i.qtdPedida}" data-p="${p.id}" data-i="${i.id}" value="${i.qtdLiberada}"></td><td>${brl(i.precoUnitario)}</td></tr>`).join('')}</table>
      <div class="barra" style="margin-top:8px"><button onclick="salvarLib(${p.id})">Salvar liberado</button>
        <button class="primario" onclick="faturar(${p.id})">Faturar</button><button class="perigo" onclick="cancelar(${p.id})">Cancelar pedido</button></div></div>`).join('')}`;
}

const lerLib = id => [...document.querySelectorAll(`input[data-p="${id}"]`)].map(e => ({ itemId: +e.dataset.i, qtdLiberada: parseInt(e.value) || 0 }));
const tenta = async fn => { try { await fn(); } catch (x) { alert(x.message); go(atual); } };
window.corte = () => tenta(async () => { const r = await api('/pedidos/corte', { method: 'POST' }); alert(r.itensCortados + ' item(ns) com corte.'); go('pedidos'); });
window.salvarLib = id => tenta(async () => { await api(`/pedidos/${id}/liberado`, { method: 'PUT', body: JSON.stringify(lerLib(id)) }); go('pedidos'); });
window.faturar = id => tenta(async () => {
  await api(`/pedidos/${id}/liberado`, { method: 'PUT', body: JSON.stringify(lerLib(id)) });
  const r = await api(`/pedidos/${id}/faturar`, { method: 'POST' });
  alert(r.venda ? `Venda/nota ${r.venda} gerada: ${brl(r.total)}. Contas a receber criadas.` : 'Nada liberado: pedido cancelado.');
  go('pedidos');
});
window.cancelar = id => confirm('Cancelar pedido?') && tenta(async () => { await api(`/pedidos/${id}/cancelar`, { method: 'POST' }); go('pedidos'); });
window.faturarTodos = () => confirm('Faturar todos os pedidos recebidos com as quantidades liberadas atuais?') && tenta(async () => {
  const r = await api('/pedidos/faturar-todos', { method: 'POST' });
  alert(`${r.faturados} pedido(s) faturado(s).` + (r.erros.length ? '\n\nProblemas:\n' + r.erros.join('\n') : ''));
  go('pedidos');
});

// ---------- vendas e contas a receber ----------
async function pgVendas() {
  const v = await api('/vendas');
  $('#conteudo').innerHTML = `<table><tr><th>Nota</th><th>Data</th><th>Cliente</th><th>Pedido</th><th>Total</th></tr>
  ${v.map(x => `<tr><td>${x.numero}</td><td>${dt(x.dataVenda)}</td><td>${esc(x.cliente)}</td><td>#${x.pedidoId}</td><td>${brl(x.total)}</td></tr>`).join('')}</table>`;
}

let filtroContas = 'aberto';
async function pgContas() {
  const c = await api('/contas?status=' + filtroContas);
  $('#conteudo').innerHTML = `<div class="barra">Mostrar:
    <select onchange="filtroContas=this.value;go('contas')"><option value="aberto" ${filtroContas === 'aberto' ? 'selected' : ''}>Em aberto</option>
    <option value="pago" ${filtroContas === 'pago' ? 'selected' : ''}>Pagas</option><option value="todas" ${filtroContas === 'todas' ? 'selected' : ''}>Todas</option></select></div>
  <table><tr><th>Documento</th><th>Cliente</th><th>Vencimento</th><th>Forma</th><th>Valor</th><th>Situação</th><th></th></tr>
  ${c.map(x => `<tr><td>${esc(x.documento)}</td><td>${esc(x.cliente)}</td><td>${dt(x.vencimento)}</td><td>${esc(x.forma)}</td><td>${brl(x.valor)}</td>
    <td class="${x.pago ? 'ok' : x.atrasada ? 'ruim' : ''}">${x.pago ? 'Pago ' + dt(x.dataPagamento) + ' (' + brl(x.valorPago) + ')' : x.atrasada ? 'Atrasada' : 'A vencer'}</td>
    <td>${x.pago ? '' : `<button onclick="baixar(${x.id})">Receber</button>`}</td></tr>`).join('')}</table>`;
}
window.filtroContas = filtroContas;
window.baixar = id => tenta(async () => {
  const hoje = new Date().toISOString().slice(0, 10);
  const data = prompt('Data do recebimento (AAAA-MM-DD):', hoje);
  if (!data) return;
  const r = await api(`/contas/${id}/calcular?data=${data}`);
  const txt = `Valor: ${brl(r.valor)}\nMulta: ${brl(r.multa)}\nJuros: ${brl(r.juros)}\nDesconto: ${brl(r.desconto)}\n\nTOTAL A RECEBER: ${brl(r.total)}\n\nConfirmar recebimento?`;
  if (!confirm(txt)) return;
  await api(`/contas/${id}/baixar`, { method: 'POST', body: JSON.stringify({ data }) });
  go('contas');
});

// ---------- menu ----------
const MENU = [
  ['sep', 'Operação'], ['pedidos', 'Pedidos recebidos', pgPedidos], ['vendas', 'Vendas', pgVendas], ['contas', 'Contas a receber', pgContas],
  ['sep', 'Sistema'], ['usuarios', 'Usuários de acesso', pgUsuarios],
  ['sep', 'Cadastros'], ['clientes', 'Clientes'], ['produtos', 'Produtos'], ['grupos', 'Grupos de produtos'], ['vendedores', 'Vendedores'],
  ['pracas', 'Praças'], ['roteiro', 'Roteiro'], ['condicoes', 'Condições de pagamento', pgCond], ['formas', 'Formas de pagamento'],
  ['cidades', 'Cidades'], ['estados', 'Estados'],
];
$('#menu').innerHTML = MENU.map(([k, l]) => k === 'sep' ? `<div class="sep">${l}</div>` : `<button data-k="${k}" onclick="go('${k}')">${l}</button>`).join('');

window.go = async k => {
  atual = k;
  document.querySelectorAll('#menu button').forEach(b => b.classList.toggle('ativo', b.dataset.k === k));
  const item = MENU.find(m => m[0] === k);
  $('#titulo').textContent = item[1];
  try { await (item[2] ? item[2]() : pgCrud(k)); } catch (x) { $('#conteudo').innerHTML = `<p class="ruim">${esc(x.message)}</p>`; }
};

// ---------- usuários de acesso ----------
async function pgUsuarios() {
  await lista('vendedores');
  const rows = await lista('usuarios');
  $('#conteudo').innerHTML = `<div class="barra"><button class="primario" onclick="editUsuario()">+ Novo usuário</button>
    <span class="mini">Admin acessa este site. Vendedor acessa o aplicativo e precisa estar vinculado a um vendedor.</span></div>
  <table><tr><th>Nome</th><th>Login</th><th>Perfil</th><th>Vendedor</th><th>Ativo</th><th></th></tr>
  ${rows.map(u => `<tr><td>${esc(u.nome)}</td><td>${esc(u.login)}</td><td>${esc(u.perfil)}</td>
    <td>${esc((cache.vendedores.find(v => v.id === u.vendedorId) || {}).nome || '')}</td><td>${u.ativo ? 'Sim' : 'Não'}</td>
    <td><button onclick="editUsuario(${u.id})">Editar</button> <button class="perigo" onclick="del('usuarios',${u.id})">Inativar</button></td></tr>`).join('')}</table>`;
}

window.editUsuario = id => {
  const o = id ? cache.usuarios.find(u => u.id === id) : { nome: '', login: '', perfil: 'Vendedor', vendedorId: null, ativo: true };
  $('#dlgTitle').textContent = id ? 'Editar usuário' : 'Novo usuário';
  const vops = cache.vendedores.filter(v => v.ativo).map(v => `<option value="${v.id}" ${v.id === o.vendedorId ? 'selected' : ''}>${esc(v.nome)}</option>`).join('');
  $('#dlgBody').innerHTML = `<label>Nome</label><input type="text" id="u_nome" value="${esc(o.nome)}">
    <label>Login</label><input type="text" id="u_login" value="${esc(o.login)}" autocomplete="off">
    <label>Perfil</label><select id="u_perfil"><option ${o.perfil === 'Admin' ? 'selected' : ''}>Admin</option><option ${o.perfil === 'Vendedor' ? 'selected' : ''}>Vendedor</option></select>
    <label>Vendedor vinculado (perfil Vendedor)</label><select id="u_vend"><option value="">Selecione</option>${vops}</select>
    <label>${id ? 'Nova senha (deixe em branco para manter)' : 'Senha (mín. 6 caracteres)'}</label><input type="password" id="u_senha" autocomplete="new-password">
    <label><input type="checkbox" id="u_ativo" ${o.ativo ? 'checked' : ''}> Ativo</label>`;
  $('#dlgSave').onclick = async e => {
    e.preventDefault();
    const body = { nome: $('#u_nome').value, login: $('#u_login').value, senha: $('#u_senha').value || null, perfil: $('#u_perfil').value,
      vendedorId: parseInt($('#u_vend').value) || null, ativo: $('#u_ativo').checked };
    try { await api('/usuarios' + (id ? '/' + id : ''), { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) }); $('#dlg').close(); go('usuarios'); }
    catch (x) { alert(x.message); }
  };
  $('#dlg').showModal();
};

// ---------- login / sessão ----------
function mostrarLogin() {
  $('#app').style.display = 'none';
  $('#login').style.display = 'flex';
  $('#lgSenha').value = '';
}
function mostrarApp(nome) {
  $('#login').style.display = 'none';
  $('#app').style.display = 'flex';
  $('#usuarioNome').textContent = nome;
  go('pedidos');
}

$('#loginForm').onsubmit = async e => {
  e.preventDefault();
  $('#lgErro').textContent = '';
  try {
    const r = await fetch('/api/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ login: $('#lgLogin').value, senha: $('#lgSenha').value }) });
    const j = await r.json().catch(() => ({}));
    if (!r.ok) throw new Error(j.erro || 'Falha no login.');
    if (j.perfil !== 'Admin') {
      fetch('/api/auth/logout', { method: 'POST', headers: { 'Authorization': 'Bearer ' + j.token } });
      throw new Error('Este usuário não tem acesso ao site. Use o aplicativo.');
    }
    sessionStorage.setItem(TOKEN, j.token);
    mostrarApp(j.nome);
  } catch (x) { $('#lgErro').textContent = x.message; }
};

window.sair = async () => {
  try { await fetch('/api/auth/logout', { method: 'POST', headers: cab() }); } catch { }
  sessionStorage.removeItem(TOKEN);
  mostrarLogin();
};

window.trocarSenha = () => {
  $('#dlgTitle').textContent = 'Trocar minha senha';
  $('#dlgBody').innerHTML = `<label>Senha atual</label><input type="password" id="s_atual" autocomplete="current-password">
    <label>Nova senha (mín. 6 caracteres)</label><input type="password" id="s_nova" autocomplete="new-password">
    <label>Repita a nova senha</label><input type="password" id="s_rep" autocomplete="new-password">`;
  $('#dlgSave').onclick = async e => {
    e.preventDefault();
    if ($('#s_nova').value !== $('#s_rep').value) { alert('As senhas não conferem.'); return; }
    try {
      const r = await fetch('/api/auth/trocar-senha', { method: 'POST', headers: cab(), body: JSON.stringify({ senhaAtual: $('#s_atual').value, novaSenha: $('#s_nova').value }) });
      if (!r.ok) { const j = await r.json().catch(() => ({})); throw new Error(j.erro || 'Não foi possível trocar a senha.'); }
      $('#dlg').close(); alert('Senha alterada com sucesso.');
    } catch (x) { alert(x.message); }
  };
  $('#dlg').showModal();
};

// ao abrir: se já há sessão válida entra direto, senão mostra o login
(async () => {
  if (!sessionStorage.getItem(TOKEN)) return mostrarLogin();
  try {
    const me = await resposta(await fetch('/api/auth/me', { headers: cab() }));
    if (me.perfil !== 'Admin') throw new Error();
    mostrarApp(me.nome);
  } catch { sessionStorage.removeItem(TOKEN); mostrarLogin(); }
})();
