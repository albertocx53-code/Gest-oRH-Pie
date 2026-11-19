// Telas da versão web do Gestão de RH.
// Cada tela só mostra os dados e chama o Servico (regras.js), igual ao sistema desktop.

const HOJE = "2026-11-13"; // data de referência dos dados de exemplo (a mesma do sistema desktop)
const DIAS_SEMANA = ["dom", "seg", "ter", "qua", "qui", "sex", "sáb"];
const MESES = ["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"];

let servico = null;
let telaAtual = "painel";
const estadoInicial = () => ({
  diaPonto: HOJE, mesFolha: HOJE.slice(0, 7), busca: "", filtroDep: "", filtroSit: "",
  modoPonto: "dia", // "dia" ou "periodo" (Consultar período, parte do Clóvis)
  periodo: { de: HOJE.slice(0, 8) + "01", ate: HOJE, pessoa: "", ocorrencia: "" },
});
let estado = estadoInicial();

const $ = (id) => document.getElementById(id);
const esc = (t) => String(t ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const dataBR = (iso) => (iso ? `${iso.slice(8, 10)}/${iso.slice(5, 7)}/${iso.slice(0, 4)}` : "");
const dinheiro = (v) => Number(v).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
const etiqueta = (t) => `<span class="etiqueta e-${esc(String(t).split(" ")[0])}">${esc(t)}</span>`;
const mesPorExtenso = (iso) => `${MESES[Number(iso.slice(5, 7)) - 1]} de ${iso.slice(0, 4)}`;

function diaDaSemana(iso) {
  const [a, m, d] = iso.split("-").map(Number);
  return new Date(a, m - 1, d).getDay();
}

function somarDias(iso, n) {
  const [a, m, d] = iso.split("-").map(Number);
  const dt = new Date(a, m - 1, d + n);
  return `${dt.getFullYear()}-${String(dt.getMonth() + 1).padStart(2, "0")}-${String(dt.getDate()).padStart(2, "0")}`;
}

function lerValor(texto) {
  texto = String(texto || "").trim().replace(/\s|R\$/g, "");
  if (texto.includes(",")) texto = texto.replace(/\./g, "").replace(",", ".");
  return texto === "" ? 0 : Number(texto);
}

function aviso(mensagem, ruim = false) {
  const el = $("aviso");
  el.textContent = mensagem;
  el.classList.toggle("ruim", ruim);
  el.hidden = false;
  clearTimeout(aviso.t);
  aviso.t = setTimeout(() => (el.hidden = true), 3200);
}

// ---------- início ----------
window.addEventListener("DOMContentLoaded", async () => {
  try {
    await abrirBanco();
  } catch (erro) {
    $("carregando").textContent = "Não foi possível abrir o banco de dados. Recarregue a página.";
    return;
  }
  $("carregando").hidden = true;
  mostrarLogin();

  $("form-login").addEventListener("submit", async (e) => {
    e.preventDefault();
    const dados = new FormData(e.target);
    const botao = $("btn-entrar");
    botao.disabled = true;
    botao.textContent = "Entrando…";
    $("erro-login").hidden = true;
    let sessao = null;
    try {
      if (!window.crypto || !crypto.subtle) throw new Error("este navegador não liberou a criptografia. Abra pelo endereço https://gestao-rh-teal.vercel.app/sistema/");
      sessao = await entrar(dados.get("usuario"), dados.get("senha"));
    } catch (erro) {
      botao.disabled = false;
      botao.textContent = "Entrar";
      $("erro-login").textContent = "Não foi possível entrar: " + erro.message;
      $("erro-login").hidden = false;
      return;
    }
    botao.disabled = false;
    botao.textContent = "Entrar";
    if (!sessao) {
      // senha com espaço sobrando no fim (comum no celular) também cai aqui
      $("erro-login").textContent = "Usuário ou senha incorretos. Confira a senha: Aurora.demo26 (com A maiúsculo e sem espaço no fim).";
      $("erro-login").hidden = false;
      return;
    }
    e.target.reset();
    servico = new Servico(sessao);
    abrirSistema();
  });

  document.querySelectorAll("#menu button").forEach((b) => b.addEventListener("click", () => irPara(b.dataset.tela)));

  $("btn-sair").addEventListener("click", () => {
    servico.sessao.ativa = false;
    servico = null;
    mostrarLogin();
  });

  $("btn-restaurar").addEventListener("click", () =>
    abrirJanela("Restaurar dados de exemplo?", [
      { tipo: "aviso", texto: "Tudo o que foi cadastrado neste navegador será apagado e os dados de exemplo voltam." },
    ], async () => {
      await restaurarDadosDeExemplo();
      aviso("Dados de exemplo restaurados.");
    }, "Restaurar"));

  $("janela-cancelar").addEventListener("click", () => $("janela").close());
});

function mostrarLogin() {
  $("tela-sistema").hidden = true;
  $("tela-login").hidden = false;
  document.querySelector("#form-login input[name=usuario]").focus();
}

function abrirSistema() {
  estado = estadoInicial(); // cada login começa com os filtros padrão
  $("tela-login").hidden = true;
  $("tela-sistema").hidden = false;
  $("avatar").textContent = servico.sessao.nome.charAt(0).toUpperCase();
  $("avatar").title = `${servico.sessao.nome} · ${servico.sessao.perfil}`;
  $("selo-consulta").hidden = servico.podeEditar;
  $("btn-restaurar").hidden = !servico.podeEditar;
  irPara("painel");
}

const TITULOS = {
  painel: "Painel", funcionarios: "Funcionários", ponto: "Ponto e frequência",
  recrutamento: "Recrutamento", agenda: "Agenda", salarios: "Salários",
};

function irPara(tela) {
  telaAtual = tela;
  document.querySelectorAll("#menu button").forEach((b) => b.classList.toggle("ativo", b.dataset.tela === tela));
  $("titulo-tela").textContent = tela === "painel" ? `Olá, ${servico.sessao.nome}` : TITULOS[tela];
  const [a, m, d] = HOJE.split("-").map(Number);
  const hoje = new Date(a, m - 1, d).toLocaleDateString("pt-BR", { weekday: "long", day: "numeric", month: "long" });
  $("subtitulo").textContent = `${servico.sessao.perfil} · ${hoje}`;
  desenhar();
}

function desenhar() {
  try {
    $("area").innerHTML = TELAS[telaAtual]();
    LIGACOES[telaAtual]?.();
  } catch (erro) {
    if (erro instanceof ErroRegra) $("area").innerHTML = `<div class="cartao"><p>${esc(erro.message)}</p></div>`;
    else throw erro;
  }
}

// ---------- janela de formulário ----------
function abrirJanela(titulo, campos, aoSalvar, textoBotao = "Salvar") {
  $("janela-titulo").textContent = titulo;
  $("janela-ok").textContent = textoBotao;
  $("janela-erro").hidden = true;
  $("janela-cancelar").hidden = false;
  $("janela-campos").innerHTML = campos.map(htmlCampo).join("");
  const form = $("form-janela");
  form.onsubmit = async (e) => {
    e.preventDefault();
    const valores = {};
    campos.forEach((c) => { if (c.nome) valores[c.nome] = form.elements[c.nome].value; });
    try {
      await aoSalvar(valores);
      $("janela").close();
      desenhar();
      if (textoBotao === "Salvar") aviso("Salvo com sucesso.");
    } catch (erro) {
      if (!(erro instanceof ErroRegra)) throw erro;
      $("janela-erro").textContent = erro.message;
      $("janela-erro").hidden = false;
    }
  };
  campos.forEach((c) => c.aoMudar && form.elements[c.nome].addEventListener("change", () => c.aoMudar(form)));
  campos.forEach((c) => c.aoMudar && c.aoMudar(form));
  $("janela").showModal();
}

function htmlCampo(c) {
  if (c.tipo === "aviso") return `<p class="muted">${esc(c.texto)}</p>`;
  if (c.tipo === "html") return c.html;
  const valor = esc(c.valor ?? "");
  let entrada;
  if (c.tipo === "select") {
    entrada = `<select name="${c.nome}">${c.opcoes.map((o) => {
      const [v, t] = Array.isArray(o) ? o : [o, o];
      return `<option value="${esc(v)}" ${String(v) === String(c.valor) ? "selected" : ""}>${esc(t)}</option>`;
    }).join("")}</select>`;
  } else if (c.tipo === "textarea") {
    entrada = `<textarea name="${c.nome}" maxlength="${c.max || 240}">${valor}</textarea>`;
  } else {
    entrada = `<input name="${c.nome}" type="${c.tipo || "text"}" value="${valor}" ${c.max ? `maxlength="${c.max}"` : ""} ${c.so_leitura ? "readonly" : ""} ${c.dica ? `placeholder="${esc(c.dica)}"` : ""} ${c.sugestoes ? `list="sug-${c.nome}" autocomplete="off"` : ""}>`;
    if (c.sugestoes) entrada += `<datalist id="sug-${c.nome}">${c.sugestoes.map((x) => `<option value="${esc(x)}">`).join("")}</datalist>`;
  }
  return `<label>${esc(c.rotulo)}${entrada}</label>`;
}

// ---------- telas ----------
const TELAS = {
  painel() {
    const funcionarios = servico.funcionarios;
    const ativos = funcionarios.filter((f) => f.situacao === "Ativo").length;
    const vagasAbertas = servico.vagas.filter((v) => v.situacao === "Aberta").length;
    let folha = `<div class="valor">Restrito</div><small>Disponível para RH e administrador</small>`;
    if (servico.podeVerSalarios) {
      const total = servico.folhaDoMes(HOJE).reduce((s, f) => s + f.base + f.bonus - f.desconto, 0);
      folha = `<div class="valor">${dinheiro(total)}</div><small>Líquido simulado</small>`;
    }
    // segunda a sexta da semana de referência
    const segunda = somarDias(HOJE, 1 - diaDaSemana(HOJE));
    const semana = [0, 1, 2, 3, 4].map((i) => somarDias(segunda, i));
    const presentes = semana.map((d) => servico.presentes(d));
    const maior = Math.max(1, ...presentes);
    const hoje = servico.agenda.filter((a) => a.quando.startsWith(HOJE));
    return `
      <div class="metricas">
        <div class="cartao metrica"><span class="rotulo">Funcionários ativos</span><div class="valor">${ativos}</div><small>${funcionarios.length} no cadastro</small></div>
        <div class="cartao metrica"><span class="rotulo">Presentes hoje</span><div class="valor">${servico.presentes(HOJE)}</div><small>de ${ativos} funcionários ativos</small></div>
        <div class="cartao metrica"><span class="rotulo">Vagas abertas</span><div class="valor">${vagasAbertas}</div><small>${servico.candidatos.length} candidaturas</small></div>
        <div class="cartao metrica"><span class="rotulo">Folha de ${MESES[Number(HOJE.slice(5, 7)) - 1]}</span>${folha}</div>
      </div>
      <div class="duas">
        <div class="cartao"><h3>Presença na semana</h3>
          <div class="grafico">${semana.map((d, i) => `
            <div class="barra"><b>${presentes[i]}</b><div class="coluna" style="height:${Math.max(4, (presentes[i] / maior) * 170)}px"></div><small>${DIAS_SEMANA[diaDaSemana(d)]}</small></div>`).join("")}
          </div>
        </div>
        <div class="cartao"><h3>Hoje na agenda</h3>
          ${hoje.length ? hoje.map((a) => `<div class="compromisso"><b>${a.quando.slice(11)}</b><p>${esc(a.titulo)}</p><small>${esc(a.categoria)}</small></div>`).join("") : `<p class="vazio">Nenhum compromisso hoje.</p>`}
        </div>
      </div>`;
  },

  funcionarios() {
    // busca ignora maiúsculas e acentos ("operacoes" encontra "Operações")
    const semAcento = (t) => String(t).normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
    const busca = semAcento(estado.busca);
    const lista = servico.funcionarios.filter((f) =>
      (!busca || [f.nome, f.cargo, f.departamento, String(f.id)].some((x) => semAcento(x).includes(busca))) &&
      (!estado.filtroDep || f.departamento === estado.filtroDep) &&
      (!estado.filtroSit || f.situacao === estado.filtroSit));
    const opcao = (v, t, sel) => `<option value="${esc(v)}" ${v === sel ? "selected" : ""}>${esc(t)}</option>`;
    return `
      <div class="barra-acoes">
        <label>Buscar<input id="busca" value="${esc(estado.busca)}" placeholder="Nome, cargo ou departamento"></label>
        <label>Departamento<select id="filtro-dep">${opcao("", "Todos os departamentos", estado.filtroDep)}${Opcoes.departamentos.map((d) => opcao(d, d, estado.filtroDep)).join("")}</select></label>
        <label>Situação<select id="filtro-sit">${opcao("", "Todas as situações", estado.filtroSit)}${Opcoes.situacoesFuncionario.map((d) => opcao(d, d, estado.filtroSit)).join("")}</select></label>
        <span class="espaco"></span>
        ${servico.podeEditar ? `<button class="btn primario" id="novo-funcionario">Novo funcionário</button>` : ""}
      </div>
      <div class="cartao tabela-rolagem"><table>
        <thead><tr><th>Matrícula</th><th>Nome</th><th>Cargo</th><th>Departamento</th><th>Situação</th><th>Admissão</th><th></th></tr></thead>
        <tbody>${lista.map((f) => `<tr>
          <td>${f.id}</td><td>${esc(f.nome)}</td><td>${esc(f.cargo)}</td><td>${esc(f.departamento)}</td><td>${etiqueta(f.situacao)}</td><td>${dataBR(f.admissao)}</td>
          <td><div class="acoes-linha"><button class="mini" data-ficha="${f.id}">Consultar ficha</button>${servico.podeEditar ? `<button class="mini" data-editar="${f.id}">Editar</button>${f.situacao !== "Inativo" ? `<button class="mini" data-inativar="${f.id}">Inativar</button>` : ""}<button class="mini perigo" data-excluir="${f.id}">Excluir</button>` : ""}</div></td>
        </tr>`).join("") || `<tr><td colspan="7" class="vazio">Nenhum funcionário encontrado.</td></tr>`}</tbody>
      </table></div>`;
  },

  ponto() {
    if (estado.modoPonto === "periodo") return TELAS.periodo();
    const registros = servico.pontosEntre(estado.diaPonto, estado.diaPonto);
    const presentes = registros.filter((r) => r.entrada).length;
    return `
      <div class="barra-acoes">
        <label>Dia<input type="date" id="dia-ponto" value="${estado.diaPonto}"></label>
        <p class="muted" style="padding-bottom:10px">${presentes} presente(s) · ${registros.length} registro(s)</p>
        <span class="espaco"></span>
        <button class="btn contorno" id="consultar-periodo">Consultar período</button>
        ${servico.podeEditar ? `<button class="btn contorno" id="lancar-falta">Lançar falta</button><button class="btn contorno" id="lancar-atraso">Lançar atraso</button><button class="btn primario" id="registrar-ponto">Registrar ponto</button>` : ""}
      </div>
      <div class="cartao tabela-rolagem"><table>
        <thead><tr><th>Funcionário</th><th>Entrada</th><th>Saída</th><th>Situação</th><th>Motivo</th>${servico.podeEditar ? "<th></th>" : ""}</tr></thead>
        <tbody>${registros.map((r) => `<tr>
          <td>${esc(servico.nome(r.pessoa))}</td><td>${r.entrada || "—"}</td><td>${r.saida || "—"}</td><td>${etiqueta(r.situacao)}</td><td class="muted">${esc(r.motivo)}</td>
          ${servico.podeEditar ? `<td><div class="acoes-linha"><button class="mini" data-editar-ponto="${r.pessoa}">Editar registro</button></div></td>` : ""}
        </tr>`).join("") || `<tr><td colspan="6" class="vazio">Nenhum registro de ponto nesse dia.</td></tr>`}</tbody>
      </table></div>`;
  },

  // Frequência por período (parte do Clóvis)
  periodo() {
    const f = estado.periodo;
    let r, erro = "";
    try {
      r = servico.resumoDoPeriodo(f.de, f.ate, f.pessoa ? Number(f.pessoa) : null, f.ocorrencia || null);
    } catch (e) {
      if (!(e instanceof ErroRegra)) throw e;
      // período inválido: mostra o aviso e deixa os filtros na tela para corrigir
      erro = e.message;
      r = { registros: [], total: 0, presencas: 0, atrasos: 0, faltas: 0, horas: 0, porFuncionario: [], comparecimento: 0 };
    }
    const funcionarios = servico.funcionarios;
    const opcao = (v, t, sel) => `<option value="${esc(v)}" ${String(v) === String(sel) ? "selected" : ""}>${esc(t)}</option>`;
    const plural = (n, um, varios) => `${n} ${n === 1 ? um : varios}`;
    return `
      <div class="barra-acoes">
        <button class="btn contorno" id="voltar-dia">← Voltar para o dia</button>
        <span class="espaco"></span>
      </div>
      <div class="cartao" style="margin-bottom:16px">
        <h3>Frequência por período</h3>
        <p class="muted" style="margin:-8px 0 14px">Presenças, atrasos e faltas de um intervalo de até ${Frequencia.maximoDeDias} dias.</p>
        <div class="barra-acoes" style="margin-bottom:0">
          <label>De<input type="date" id="per-de" value="${f.de}"></label>
          <label>Até<input type="date" id="per-ate" value="${f.ate}"></label>
          <label>Funcionário<select id="per-pessoa">${opcao("", "Todos os funcionários", f.pessoa)}${funcionarios.map((x) => opcao(x.id, x.nome, f.pessoa)).join("")}</select></label>
          <label>Ocorrência<select id="per-ocorrencia">${opcao("", "Todas as ocorrências", f.ocorrencia)}${Opcoes.situacoesPonto.map((o) => opcao(o, o, f.ocorrencia)).join("")}</select></label>
        </div>
        ${erro ? `<p class="erro" style="margin-top:12px">${esc(erro)}</p>` : ""}
      </div>
      <div class="metricas">
        <div class="cartao metrica"><span class="rotulo">Registros</span><div class="valor">${r.total}</div><small>${dataBR(f.de)} a ${dataBR(f.ate)}</small></div>
        <div class="cartao metrica"><span class="rotulo">Presenças e atrasos</span><div class="valor">${r.presencas + r.atrasos}</div><small>${plural(r.presencas, "presença", "presenças")} · ${plural(r.atrasos, "atraso", "atrasos")}</small></div>
        <div class="cartao metrica"><span class="rotulo">Faltas</span><div class="valor">${r.faltas}</div><small>comparecimento de ${Math.round(r.comparecimento * 100)}%</small></div>
        <div class="cartao metrica"><span class="rotulo">Horas trabalhadas</span><div class="valor" style="font-size:26px">${Frequencia.formatarHoras(r.horas)}</div><small>da entrada até a saída</small></div>
      </div>
      <div class="duas">
        <div class="cartao tabela-rolagem"><h3>Registros do período</h3><table>
          <thead><tr><th>Dia</th><th>Funcionário</th><th>Entrada</th><th>Saída</th><th>Situação</th><th>Motivo</th></tr></thead>
          <tbody>${r.registros.map((x) => `<tr><td>${dataBR(x.dia)}</td><td>${esc(servico.nome(x.pessoa))}</td><td>${x.entrada || "—"}</td><td>${x.saida || "—"}</td><td>${etiqueta(x.situacao)}</td><td class="muted">${esc(x.motivo)}</td></tr>`).join("") || `<tr><td colspan="6" class="vazio">Nenhum registro nesse período.</td></tr>`}</tbody>
        </table></div>
        <div class="cartao tabela-rolagem"><h3>Por funcionário</h3><table>
          <thead><tr><th>Nome</th><th class="num">Pres.</th><th class="num">Atr.</th><th class="num">Faltas</th><th class="num">Horas</th></tr></thead>
          <tbody>${r.porFuncionario.map((x) => `<tr><td>${esc(x.nome)}</td><td class="num">${x.presencas}</td><td class="num">${x.atrasos}</td><td class="num">${x.faltas}</td><td class="num">${Frequencia.formatarHoras(x.horas)}</td></tr>`).join("") || `<tr><td colspan="5" class="vazio">—</td></tr>`}</tbody>
        </table></div>
      </div>`;
  },

  recrutamento() {
    const candidatos = servico.candidatos;
    return `
      <div class="barra-acoes">
        <h3 style="font-size:18px">Vagas</h3><span class="espaco"></span>
        ${servico.podeEditar ? `<button class="btn contorno" id="nova-vaga">Nova vaga</button><button class="btn primario" id="novo-candidato">Novo candidato</button>` : ""}
      </div>
      <div class="vagas">${servico.vagas.map((v) => `
        <div class="cartao vaga">
          <div class="linha"><h3>${esc(v.titulo)}</h3>${etiqueta(v.situacao)}</div>
          <p class="muted">${esc(v.departamento)} · ${candidatos.filter((c) => c.vaga === v.id).length} candidato(s)</p>
          ${servico.podeEditar ? `<div><button class="mini" data-alternar="${v.id}">${v.situacao === "Aberta" ? "Encerrar vaga" : "Reabrir vaga"}</button></div>` : ""}
        </div>`).join("")}
      </div>
      <div class="cartao tabela-rolagem"><h3>Candidatos</h3><table>
        <thead><tr><th>Nome</th><th>Vaga</th><th>Etapa</th>${servico.podeEditar ? "<th></th>" : ""}</tr></thead>
        <tbody>${candidatos.map((c) => `<tr>
          <td>${esc(c.nome)}</td><td>${esc(servico.tituloVaga(c.vaga))}</td><td>${etiqueta(c.etapa)}</td>
          ${servico.podeEditar ? `<td><div class="acoes-linha"><button class="mini" data-editar-candidato="${c.id}">Editar</button></div></td>` : ""}
        </tr>`).join("") || `<tr><td colspan="4" class="vazio">Nenhum candidato ainda.</td></tr>`}</tbody>
      </table></div>`;
  },

  agenda() {
    const itens = servico.agenda;
    return `
      <div class="barra-acoes"><span class="espaco"></span>
        ${servico.podeEditar ? `<button class="btn primario" id="novo-compromisso">Novo compromisso</button>` : ""}
      </div>
      <div class="cartao tabela-rolagem"><table>
        <thead><tr><th>Data</th><th>Hora</th><th>Compromisso</th><th>Categoria</th></tr></thead>
        <tbody>${itens.map((a) => `<tr>
          <td>${dataBR(a.quando.slice(0, 10))}${a.quando.startsWith(HOJE) ? ' <span class="etiqueta e-Aberta">hoje</span>' : ""}</td>
          <td>${a.quando.slice(11)}</td><td>${esc(a.titulo)}</td><td>${esc(a.categoria)}</td>
        </tr>`).join("") || `<tr><td colspan="4" class="vazio">Nenhum compromisso.</td></tr>`}</tbody>
      </table></div>`;
  },

  salarios() {
    if (!servico.podeVerSalarios) {
      return `<div class="cartao"><h3>Acesso restrito</h3><p class="muted">Seu perfil não tem acesso aos salários.</p></div>`;
    }
    const folha = servico.folhaDoMes(estado.mesFolha + "-01");
    const total = folha.reduce((s, f) => s + f.base + f.bonus - f.desconto, 0);
    return `
      <div class="barra-acoes">
        <label>Competência<input type="month" id="mes-folha" value="${estado.mesFolha}"></label>
        <p class="muted" style="padding-bottom:10px">Folha de ${mesPorExtenso(estado.mesFolha + "-01")}</p>
        <span class="espaco"></span>
        <button class="btn primario" id="lancar-salario">Lançar salário</button>
      </div>
      <div class="cartao tabela-rolagem"><table>
        <thead><tr><th>Funcionário</th><th class="num">Salário base</th><th class="num">Bônus</th><th class="num">Desconto</th><th class="num">Líquido</th><th></th></tr></thead>
        <tbody>${folha.map((f) => `<tr>
          <td>${esc(servico.nome(f.pessoa))}</td><td class="num">${dinheiro(f.base)}</td><td class="num">${dinheiro(f.bonus)}</td>
          <td class="num">${dinheiro(f.desconto)}</td><td class="num"><b>${dinheiro(f.base + f.bonus - f.desconto)}</b></td>
          <td><div class="acoes-linha"><button class="mini" data-editar-folha="${f.pessoa}">Editar</button></div></td>
        </tr>`).join("") || `<tr><td colspan="6" class="vazio">Nenhum lançamento nesse mês.</td></tr>`}</tbody>
        ${folha.length ? `<tfoot><tr><td>Total</td><td></td><td></td><td></td><td class="num">${dinheiro(total)}</td><td></td></tr></tfoot>` : ""}
      </table></div>`;
  },
};

// ---------- botões de cada tela ----------
const ao = (seletor, evento, fn) => document.querySelectorAll(seletor).forEach((el) => el.addEventListener(evento, fn));

function formFuncionario(f) {
  const novo = !f;
  f = f || { id: 0, nome: "", cargo: "", departamento: "Pessoas", situacao: "Ativo", admissao: HOJE };
  abrirJanela(novo ? "Novo funcionário" : `Editar funcionário · matrícula ${f.id}`, [
    { nome: "nome", rotulo: "Nome", valor: f.nome, max: 120 },
    { nome: "cargo", rotulo: "Cargo", valor: f.cargo, max: 120, sugestoes: servico.cargosCadastrados() }, // sugestão de cargos (Jefferson)
    { nome: "departamento", rotulo: "Departamento", tipo: "select", opcoes: Opcoes.departamentos, valor: f.departamento },
    { nome: "situacao", rotulo: "Situação", tipo: "select", opcoes: Opcoes.situacoesFuncionario, valor: f.situacao },
    { nome: "admissao", rotulo: "Data de admissão", tipo: "date", valor: f.admissao },
  ], (v) => servico.salvarFuncionario({ id: f.id, ...v }));
}

// Ficha do funcionário (parte do Jefferson)
function abrirFicha(id) {
  const ficha = servico.consultarFicha(id, HOJE);
  const f = ficha.funcionario;
  const p = ficha.ultimoPonto;
  const salario = !ficha.salarioVisivel ? "Sem acesso aos salários neste perfil"
    : ficha.ultimaFolha ? `${dinheiro(ficha.ultimaFolha.base + ficha.ultimaFolha.bonus - ficha.ultimaFolha.desconto)} líquido (${mesPorExtenso(ficha.ultimaFolha.competencia)})`
    : "Nenhum lançamento nos últimos 12 meses";
  const linhas = [
    ["Matrícula", f.id], ["Cargo", f.cargo], ["Departamento", f.departamento], ["Situação", f.situacao],
    ["Admissão", dataBR(f.admissao)], ["Tempo de casa", ficha.tempoDeCasa],
    [`Ponto de ${MESES[Number(HOJE.slice(5, 7)) - 1]}`, `${ficha.presencas} presença(s) · ${ficha.atrasos} atraso(s) · ${ficha.faltas} falta(s)`],
    ["Último ponto", p ? `${dataBR(p.dia)} · ${p.situacao}${p.entrada ? ` · ${p.entrada} às ${p.saida}` : ""}` : "Nenhum registro"],
    ["Último salário", salario],
  ];
  abrirJanela(`Ficha · ${f.nome}`, [{ tipo: "html", html:
    `<table class="ficha">${linhas.map(([k, v]) => `<tr><th>${esc(k)}</th><td>${esc(v)}</td></tr>`).join("")}</table>` }],
    () => {}, "Fechar");
  $("janela-cancelar").hidden = true;
}

function formPonto(registro) {
  const edicao = !!registro;
  const funcionarios = servico.funcionarios.filter((f) => edicao || f.situacao === "Ativo");
  registro = registro || { pessoa: funcionarios[0]?.id, dia: estado.diaPonto, entrada: "08:00", saida: "17:00", situacao: "Presença", motivo: "" };
  const ajustar = (form) => {
    const falta = form.elements.situacao.value === "Falta";
    const comMotivo = form.elements.situacao.value !== "Presença";
    form.elements.entrada.disabled = falta;
    form.elements.saida.disabled = falta;
    form.elements.motivo.disabled = !comMotivo;
    form.elements.motivo.placeholder = comMotivo ? "Opcional" : "Só para atraso ou falta";
  };
  abrirJanela(edicao ? "Editar registro de ponto" : "Registrar ponto", [
    { nome: "pessoa", rotulo: "Funcionário", tipo: "select", opcoes: funcionarios.map((f) => [f.id, f.nome]), valor: registro.pessoa },
    { nome: "dia", rotulo: "Dia", tipo: "date", valor: registro.dia, so_leitura: edicao },
    { nome: "situacao", rotulo: "Situação", tipo: "select", opcoes: Opcoes.situacoesPonto, valor: registro.situacao, aoMudar: ajustar },
    { nome: "entrada", rotulo: "Entrada", tipo: "time", valor: registro.entrada || "08:00" },
    { nome: "saida", rotulo: "Saída", tipo: "time", valor: registro.saida || "17:00" },
    { nome: "motivo", rotulo: "Motivo", tipo: "textarea", valor: registro.motivo, max: 240 },
  ], (v) => servico.salvarPonto({ ...v, pessoa: Number(v.pessoa), entrada: v.entrada ?? "", saida: v.saida ?? "", motivo: v.motivo ?? "" }, edicao));
  $("form-janela").elements.pessoa.disabled = edicao; // na edição o funcionário não muda (faz parte da chave)
}

// Lançar falta / Lançar atraso (parte do Clóvis)
function formOcorrencia(tipo) {
  const equipe = servico.funcionarios.filter((f) => f.situacao === "Ativo");
  if (!equipe.length) { aviso("Cadastre um funcionário ativo primeiro", true); return; }
  const inicial = servico.semRegistro(estado.diaPonto)[0] || equipe[0];
  const falta = tipo === "Falta";
  const campos = [
    { nome: "pessoa", rotulo: "Funcionário", tipo: "select", opcoes: equipe.map((f) => [f.id, f.nome]), valor: inicial.id },
    { nome: "dia", rotulo: "Data", tipo: "date", valor: estado.diaPonto },
  ];
  if (!falta) campos.push({ nome: "entrada", rotulo: "Entrada", tipo: "time", valor: "08:15" }, { nome: "saida", rotulo: "Saída", tipo: "time", valor: "17:00" });
  campos.push({ nome: "motivo", rotulo: "Motivo (opcional)", tipo: "textarea", max: 240 });
  campos.push({ tipo: "aviso", texto: falta ? "A falta fica sem horário de entrada e de saída." : `No atraso, a entrada precisa ser depois das ${Frequencia.horarioPadrao}.` });
  abrirJanela(falta ? "Lançar falta" : "Lançar atraso", campos, (v) => {
    if (falta) servico.lancarFalta(Number(v.pessoa), v.dia, v.motivo, HOJE);
    else servico.lancarAtraso(Number(v.pessoa), v.dia, v.entrada, v.saida, v.motivo, HOJE);
    estado.diaPonto = v.dia;
  });
}

function formCandidato(c) {
  const novo = !c;
  const vagas = servico.vagas.filter((v) => v.situacao === "Aberta" || (c && v.id === c.vaga));
  c = c || { id: 0, nome: "", vaga: vagas[0]?.id, etapa: "Triagem" };
  abrirJanela(novo ? "Novo candidato" : "Editar candidatura", [
    { nome: "nome", rotulo: "Nome do candidato", valor: c.nome, max: 120 },
    { nome: "vaga", rotulo: "Vaga", tipo: "select", opcoes: vagas.map((v) => [v.id, v.titulo]), valor: c.vaga },
    { nome: "etapa", rotulo: "Etapa", tipo: "select", opcoes: Opcoes.etapas, valor: c.etapa },
  ], (v) => servico.salvarCandidato({ id: c.id, nome: v.nome, vaga: Number(v.vaga), etapa: v.etapa }));
}

// Novo lançamento ou edição (a edição é a parte do Cleberson Dias)
function formFolha(f) {
  const funcionarios = servico.funcionarios;
  const edicao = !!f;
  f = f || { pessoa: funcionarios[0]?.id, competencia: estado.mesFolha + "-01", base: "", bonus: 0, desconto: 0 };
  const brl = (v) => (v === "" ? "" : Number(v).toFixed(2).replace(".", ","));
  abrirJanela(edicao ? "Editar lançamento" : "Lançar salário", [
    { nome: "pessoa", rotulo: "Funcionário", tipo: "select", opcoes: funcionarios.map((x) => [x.id, x.nome]), valor: f.pessoa },
    { nome: "competencia", rotulo: "Competência", tipo: "month", valor: f.competencia.slice(0, 7) },
    { nome: "base", rotulo: "Salário base (R$)", valor: brl(f.base), dica: "Ex.: 3.500,00" },
    { nome: "bonus", rotulo: "Bônus (R$)", valor: brl(f.bonus) },
    { nome: "desconto", rotulo: "Desconto (R$)", valor: brl(f.desconto) },
    { tipo: "aviso", texto: edicao
      ? "Funcionário e competência não mudam na edição. Altere os valores e confirme."
      : "Um lançamento por funcionário e competência. Salvar de novo atualiza o valor existente." },
  ], (v) => {
    servico.salvarFolha({ pessoa: Number(v.pessoa), competencia: (v.competencia || "") + "-01", base: lerValor(v.base), bonus: lerValor(v.bonus), desconto: lerValor(v.desconto) });
    estado.mesFolha = v.competencia;
  });
  // na edição, funcionário e competência são a chave do lançamento: ficam travados
  $("form-janela").elements.pessoa.disabled = edicao;
  $("form-janela").elements.competencia.disabled = edicao;
}

const LIGACOES = {
  funcionarios() {
    $("busca").addEventListener("input", (e) => {
      estado.busca = e.target.value;
      const pos = e.target.selectionStart;
      desenhar();
      const campo = $("busca");
      campo.focus();
      campo.setSelectionRange(pos, pos);
    });
    ao("#novo-funcionario", "click", () => formFuncionario(null));
    ao("#filtro-dep", "change", (e) => { estado.filtroDep = e.target.value; desenhar(); });
    ao("#filtro-sit", "change", (e) => { estado.filtroSit = e.target.value; desenhar(); });
    ao("[data-ficha]", "click", (e) => abrirFicha(Number(e.target.dataset.ficha)));
    // duplo clique: quem edita abre a edição; o Gestor abre a ficha
    ao("#area tbody tr", "dblclick", (e) => e.currentTarget.querySelector(servico.podeEditar ? "[data-editar]" : "[data-ficha]")?.click());
    ao("[data-inativar]", "click", (e) => {
      const f = servico.funcionarios.find((x) => x.id === Number(e.target.dataset.inativar));
      abrirJanela("Inativar funcionário?", [{ tipo: "aviso", texto: `${f.nome} passa para a situação Inativo. O histórico de ponto e salário continua guardado.` }],
        () => { servico.mudarSituacao(f.id, "Inativo"); aviso("Funcionário inativado."); }, "Inativar");
    });
    ao("[data-editar]", "click", (e) => formFuncionario(servico.funcionarios.find((f) => f.id === Number(e.target.dataset.editar))));
    ao("[data-excluir]", "click", (e) => {
      const f = servico.funcionarios.find((x) => x.id === Number(e.target.dataset.excluir));
      abrirJanela("Excluir funcionário?", [{ tipo: "aviso", texto: `${f.nome} (matrícula ${f.id}) será excluído do cadastro.` }],
        () => { servico.apagarFuncionario(f.id); aviso("Funcionário excluído."); }, "Excluir");
    });
  },
  ponto() {
    ao("#dia-ponto", "change", (e) => { if (e.target.value) { estado.diaPonto = e.target.value; desenhar(); } });
    ao("#registrar-ponto", "click", () => formPonto(null));
    ao("#lancar-falta", "click", () => formOcorrencia("Falta"));
    ao("#lancar-atraso", "click", () => formOcorrencia("Atraso"));
    ao("#consultar-periodo", "click", () => { estado.modoPonto = "periodo"; desenhar(); });
    ao("#voltar-dia", "click", () => { estado.modoPonto = "dia"; desenhar(); });
    const filtro = (id, campo) => ao(id, "change", (e) => { estado.periodo[campo] = e.target.value; desenhar(); });
    filtro("#per-de", "de"); filtro("#per-ate", "ate"); filtro("#per-pessoa", "pessoa"); filtro("#per-ocorrencia", "ocorrencia");
    ao("[data-editar-ponto]", "click", (e) => {
      const pessoa = Number(e.target.dataset.editarPonto);
      formPonto(servico.pontosEntre(estado.diaPonto, estado.diaPonto).find((r) => r.pessoa === pessoa));
    });
  },
  recrutamento() {
    ao("#nova-vaga", "click", () => abrirJanela("Nova vaga", [
      { nome: "titulo", rotulo: "Título da vaga", max: 120 },
      { nome: "departamento", rotulo: "Departamento", tipo: "select", opcoes: Opcoes.departamentos },
    ], (v) => servico.adicionarVaga(v.titulo, v.departamento)));
    ao("#novo-candidato", "click", () => formCandidato(null));
    ao("[data-alternar]", "click", (e) => {
      try { servico.alternarVaga(Number(e.target.dataset.alternar)); desenhar(); }
      catch (erro) { if (erro instanceof ErroRegra) aviso(erro.message, true); else throw erro; }
    });
    ao("[data-editar-candidato]", "click", (e) => formCandidato(servico.candidatos.find((c) => c.id === Number(e.target.dataset.editarCandidato))));
  },
  agenda() {
    ao("#novo-compromisso", "click", () => abrirJanela("Novo compromisso", [
      { nome: "titulo", rotulo: "Título", max: 120 },
      { nome: "dia", rotulo: "Data", tipo: "date", valor: HOJE },
      { nome: "hora", rotulo: "Horário", tipo: "time", valor: "09:00" },
      { nome: "categoria", rotulo: "Categoria", tipo: "select",
        opcoes: Opcoes.categoriasAgenda.filter((c) => servico.podeVerSalarios || c !== "Salários") },
    ], (v) => servico.adicionarCompromisso(v.titulo, v.dia, v.hora, v.categoria)));
  },
  salarios() {
    ao("#mes-folha", "change", (e) => { if (e.target.value) { estado.mesFolha = e.target.value; desenhar(); } });
    ao("#lancar-salario", "click", () => formFolha(null));
    ao("#area tbody tr", "dblclick", (e) => e.currentTarget.querySelector("[data-editar-folha]")?.click());
    ao("[data-editar-folha]", "click", (e) => {
      const pessoa = Number(e.target.dataset.editarFolha);
      formFolha(servico.folhaDoMes(estado.mesFolha + "-01").find((f) => f.pessoa === pessoa));
    });
  },
};
