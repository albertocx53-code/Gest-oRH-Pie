// Regras do sistema na versão web.
// É a mesma lógica do RhService.cs e do Authentication.cs (parte do Rickelmy),
// escrita em JavaScript para rodar no navegador.

const Opcoes = {
  departamentos: ["Pessoas", "Tecnologia", "Financeiro", "Comercial", "Operações"],
  situacoesFuncionario: ["Ativo", "Férias", "Afastado", "Inativo"],
  situacoesPonto: ["Presença", "Atraso", "Falta"],
  etapas: ["Triagem", "Entrevista", "Teste técnico", "Proposta", "Contratado"],
  categoriasAgenda: ["Funcionários", "Ponto", "Recrutamento", "Salários"],
};

function exigir(condicao, mensagem) {
  if (!condicao) throw new ErroRegra(mensagem);
}

function texto(valor, campo) {
  valor = (valor || "").trim();
  exigir(valor.length > 0 && valor.length <= 120, `Preencha ${campo} com até 120 caracteres.`);
  return valor;
}

function dataValida(dia) {
  exigir(/^\d{4}-\d{2}-\d{2}$/.test(dia || ""), "Informe uma data válida.");
  const ano = Number(dia.slice(0, 4));
  exigir(ano >= 1900 && ano <= 2100, "Informe uma data entre 1900 e 2100.");
  return dia;
}

function horaValida(hora) {
  return /^([01]\d|2[0-3]):[0-5]\d$/.test(hora || "");
}

function valorValido(v) {
  return Number.isFinite(v) && v >= 0 && v <= 1_000_000 && Math.abs(Math.round(v * 100) - v * 100) < 1e-6;
}

const inicioDoMes = (dia) => dia.slice(0, 7) + "-01";

// ---------- login (Authentication.cs) ----------
const deHex = (hex) => new Uint8Array(hex.match(/../g).map((b) => parseInt(b, 16)));

async function pbkdf2(senha, salt, iteracoes, tamanho) {
  const chave = await crypto.subtle.importKey("raw", new TextEncoder().encode(senha), "PBKDF2", false, ["deriveBits"]);
  const bits = await crypto.subtle.deriveBits({ name: "PBKDF2", hash: "SHA-256", salt, iterations: iteracoes }, chave, tamanho * 8);
  return new Uint8Array(bits);
}

function iguaisEmTempoFixo(a, b) {
  if (a.length !== b.length) return false;
  let diferenca = 0;
  for (let i = 0; i < a.length; i++) diferenca |= a[i] ^ b[i];
  return diferenca === 0;
}

async function entrar(usuario, senha) {
  usuario = (usuario || "").trim();
  if (!usuario || usuario.length > 64 || !senha || senha.length > 128) return null;
  const conta = Repositorio.contaPorLogin(usuario);
  // Conta inexistente também passa pelo hash, para a resposta demorar o mesmo tempo.
  const salt = conta ? deHex(conta.salt) : new Uint8Array(16);
  const esperado = conta ? deHex(conta.hash) : new Uint8Array(32);
  const calculado = await pbkdf2(senha, salt, conta ? conta.iteracoes : 600000, esperado.length);
  const confere = iguaisEmTempoFixo(calculado, esperado);
  calculado.fill(0);
  return confere && conta ? { nome: conta.nome, perfil: conta.perfil, ativa: true } : null;
}

// ---------- serviço (RhService.cs) ----------
class Servico {
  constructor(sessao) {
    this.sessao = sessao;
  }

  get podeEditar() {
    return this.sessao.ativa && (this.sessao.perfil === "Administrador" || this.sessao.perfil === "Recursos Humanos");
  }
  get podeVerSalarios() { return this.podeEditar; }

  ler(funcao) {
    exigir(this.sessao.ativa, "Entre novamente para continuar.");
    return funcao();
  }

  gravar(funcao) {
    exigir(this.sessao.ativa, "Entre novamente para continuar.");
    exigir(this.podeEditar, "Seu perfil pode consultar os dados, sem editar registros.");
    funcao();
  }

  get funcionarios() { return this.ler(() => Repositorio.funcionarios()); }
  get vagas() { return this.ler(() => Repositorio.vagas()); }
  get candidatos() { return this.ler(() => Repositorio.candidatos()); }
  get agenda() {
    return this.ler(() => Repositorio.agenda().filter((a) => this.podeVerSalarios || a.categoria !== "Salários"));
  }

  pontosEntre(de, ate) {
    return this.ler(() => {
      exigir(de <= ate, "A data final deve ser igual ou posterior à inicial.");
      return Repositorio.pontos().filter((p) => p.dia >= de && p.dia <= ate);
    });
  }

  folhaDoMes(dia) {
    return this.ler(() => {
      exigir(this.podeVerSalarios, "Seu perfil não tem acesso aos salários.");
      return Repositorio.folha().filter((f) => f.competencia === inicioDoMes(dia));
    });
  }

  nome(id) {
    const f = this.funcionarios.find((x) => x.id === id);
    return f ? f.nome : `Matrícula ${id}`;
  }

  tituloVaga(id) {
    const v = this.vagas.find((x) => x.id === id);
    return v ? v.titulo : "Vaga indisponível";
  }

  presentes(dia) {
    return this.pontosEntre(dia, dia).filter((p) => p.entrada).length;
  }

  salvarFuncionario(f) {
    this.gravar(() => {
      if (f.id) exigir(Repositorio.funcionarios().some((x) => x.id === f.id), "Funcionário não encontrado.");
      exigir(Opcoes.departamentos.includes(f.departamento), "Selecione um departamento válido.");
      exigir(Opcoes.situacoesFuncionario.includes(f.situacao), "Selecione uma situação válida.");
      dataValida(f.admissao);
      Repositorio.salvarFuncionario({ ...f, nome: texto(f.nome, "nome"), cargo: texto(f.cargo, "cargo") });
    });
  }

  apagarFuncionario(id) {
    this.gravar(() => {
      exigir(Repositorio.funcionarios().some((x) => x.id === id), "Funcionário não encontrado.");
      // Ponto e folha usam a matrícula. Inativar mantém o histórico.
      exigir(!Repositorio.pontos().some((p) => p.pessoa === id) && !Repositorio.folha().some((f) => f.pessoa === id),
        "Este cadastro tem registros de ponto ou salário. Você pode mudar a situação para Inativo.");
      Repositorio.apagarFuncionario(id);
    });
  }

  salvarPonto(r, edicao) {
    this.gravar(() => {
      dataValida(r.dia);
      exigir(Repositorio.funcionarios().some((f) => f.id === r.pessoa && (edicao || f.situacao === "Ativo")),
        "Selecione um funcionário ativo para registrar o ponto.");
      exigir(Opcoes.situacoesPonto.includes(r.situacao), "Selecione uma ocorrência válida.");
      const existe = Repositorio.pontos().some((p) => p.pessoa === r.pessoa && p.dia === r.dia);
      exigir(existe === edicao, edicao ? "O registro de ponto não foi encontrado." : "Já existe ponto nesse dia. Use Editar registro para alterá-lo.");
      let entrada = (r.entrada || "").trim();
      let saida = (r.saida || "").trim();
      if (r.situacao === "Falta") { entrada = ""; saida = ""; }
      else exigir(horaValida(entrada) && horaValida(saida) && saida > entrada, "Informe horários válidos. A saída deve ser posterior à entrada.");
      const motivo = r.situacao === "Atraso" || r.situacao === "Falta" ? (r.motivo || "").trim() : "";
      exigir(motivo.length <= 240, "O motivo pode ter até 240 caracteres.");
      Repositorio.salvarPonto({ ...r, entrada, saida, motivo }, edicao);
    });
  }

  adicionarVaga(titulo, departamento) {
    this.gravar(() => {
      exigir(Opcoes.departamentos.includes(departamento), "Selecione um departamento válido.");
      Repositorio.salvarVaga({ id: 0, titulo: texto(titulo, "título da vaga"), departamento, situacao: "Aberta" });
    });
  }

  alternarVaga(id) {
    this.gravar(() => {
      const vaga = Repositorio.vagas().find((v) => v.id === id);
      exigir(vaga, "Vaga não encontrada.");
      Repositorio.salvarVaga({ ...vaga, situacao: vaga.situacao === "Aberta" ? "Encerrada" : "Aberta" });
    });
  }

  salvarCandidato(c) {
    this.gravar(() => {
      const original = Repositorio.candidatos().find((x) => x.id === c.id);
      exigir(!c.id || original, "Candidatura não encontrada.");
      // Vaga encerrada mantém quem já estava nela, mas não aceita inscrição nova.
      exigir(Repositorio.vagas().some((v) => v.id === c.vaga && (v.situacao === "Aberta" || (original && original.vaga === v.id))),
        "Selecione uma vaga aberta.");
      exigir(Opcoes.etapas.includes(c.etapa), "Selecione uma etapa válida.");
      Repositorio.salvarCandidato({ ...c, nome: texto(c.nome, "nome do candidato") });
    });
  }

  adicionarCompromisso(titulo, dia, hora, categoria) {
    this.gravar(() => {
      dataValida(dia);
      exigir(Opcoes.categoriasAgenda.includes(categoria), "Selecione uma categoria válida.");
      exigir(horaValida((hora || "").trim()), "Informe um horário no formato HH:mm.");
      Repositorio.adicionarCompromisso({ titulo: texto(titulo, "título"), quando: `${dia} ${hora.trim()}`, categoria });
    });
  }

  salvarFolha(f) {
    this.gravar(() => {
      exigir(this.podeVerSalarios, "Seu perfil não tem acesso aos salários.");
      exigir(Repositorio.funcionarios().some((x) => x.id === f.pessoa), "Funcionário não encontrado.");
      dataValida(f.competencia);
      exigir(valorValido(f.base) && valorValido(f.bonus) && valorValido(f.desconto),
        "Informe valores entre R$ 0,00 e R$ 1.000.000,00, com até duas casas decimais.");
      exigir(f.desconto <= f.base + f.bonus, "O desconto não pode superar salário e bônus.");
      Repositorio.salvarFolha({ ...f, competencia: inicioDoMes(f.competencia) });
    });
  }
}

// ---------- ponto e frequência (FrequenciaDePonto.cs, parte do Clóvis) ----------
const Frequencia = {
  horarioPadrao: "08:00", // entrar depois disso é atraso
  maximoDeDias: 366,

  minutos(hora) {
    const [h, m] = hora.split(":").map(Number);
    return h * 60 + m;
  },

  // horas entre entrada e saída, em minutos (falta ou horário inválido = 0)
  horasTrabalhadas(r) {
    if (!horaValida(r.entrada) || !horaValida(r.saida) || r.saida <= r.entrada) return 0;
    return this.minutos(r.saida) - this.minutos(r.entrada);
  },

  formatarHoras(minutos) {
    return `${Math.floor(minutos / 60)} h ${String(minutos % 60).padStart(2, "0")} min`;
  },

  diasEntre(de, ate) {
    const d = (iso) => Date.UTC(+iso.slice(0, 4), +iso.slice(5, 7) - 1, +iso.slice(8, 10));
    return Math.round((d(ate) - d(de)) / 86400000);
  },
};

Servico.prototype.consultarPeriodo = function (de, ate, matricula = null, ocorrencia = null) {
  dataValida(de); dataValida(ate);
  exigir(de <= ate, "A data final deve ser igual ou posterior à inicial.");
  exigir(Frequencia.diasEntre(de, ate) <= Frequencia.maximoDeDias - 1, `O período pode ter no máximo ${Frequencia.maximoDeDias} dias.`);
  const funcionarios = this.funcionarios;
  if (matricula !== null) exigir(funcionarios.some((f) => f.id === matricula), "Funcionário não encontrado.");
  if (ocorrencia !== null) exigir(Opcoes.situacoesPonto.includes(ocorrencia), "Selecione uma ocorrência válida.");
  const nomes = Object.fromEntries(funcionarios.map((f) => [f.id, f.nome]));
  return this.pontosEntre(de, ate)
    .filter((r) => matricula === null || r.pessoa === matricula)
    .filter((r) => ocorrencia === null || r.situacao === ocorrencia)
    .sort((a, b) => a.dia.localeCompare(b.dia) || (nomes[a.pessoa] || "").localeCompare(nomes[b.pessoa] || "", "pt-BR") || a.pessoa - b.pessoa);
};

Servico.prototype.resumoDoPeriodo = function (de, ate, matricula = null, ocorrencia = null) {
  const registros = this.consultarPeriodo(de, ate, matricula, ocorrencia);
  const conta = (lista, s) => lista.filter((r) => r.situacao === s).length;
  const somar = (lista) => lista.reduce((t, r) => t + Frequencia.horasTrabalhadas(r), 0);
  const grupos = {};
  registros.forEach((r) => (grupos[r.pessoa] = grupos[r.pessoa] || []).push(r));
  const porFuncionario = Object.entries(grupos)
    .map(([id, lista]) => ({ matricula: +id, nome: this.nome(+id), presencas: conta(lista, "Presença"), atrasos: conta(lista, "Atraso"), faltas: conta(lista, "Falta"), horas: somar(lista) }))
    .sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR"));
  const presencas = conta(registros, "Presença"), atrasos = conta(registros, "Atraso");
  return {
    registros, total: registros.length, presencas, atrasos, faltas: conta(registros, "Falta"), horas: somar(registros), porFuncionario,
    // quem chegou atrasado também compareceu
    comparecimento: registros.length ? (presencas + atrasos) / registros.length : 0,
  };
};

Servico.prototype.semRegistro = function (dia) {
  const comRegistro = new Set(this.pontosEntre(dia, dia).map((r) => r.pessoa));
  return this.funcionarios.filter((f) => f.situacao === "Ativo" && !comRegistro.has(f.id));
};

Servico.prototype.conferirDiaDoLancamento = function (dia, hoje) {
  if (this.podeEditar && dia > hoje) throw new ErroRegra("Não é possível lançar falta ou atraso em uma data futura.");
};

Servico.prototype.lancarFalta = function (matricula, dia, motivo, hoje) {
  this.conferirDiaDoLancamento(dia, hoje);
  this.salvarPonto({ pessoa: matricula, dia, entrada: "", saida: "", situacao: "Falta", motivo }, false);
};

Servico.prototype.lancarAtraso = function (matricula, dia, entrada, saida, motivo, hoje) {
  this.conferirDiaDoLancamento(dia, hoje);
  if (this.podeEditar && horaValida((entrada || "").trim()) && entrada.trim() <= Frequencia.horarioPadrao)
    throw new ErroRegra(`Para lançar atraso, a entrada deve ser depois das ${Frequencia.horarioPadrao}.`);
  this.salvarPonto({ pessoa: matricula, dia, entrada, saida, situacao: "Atraso", motivo }, false);
};

// ---------- cadastro de funcionários (CadastroDeFuncionarios.cs, parte do Jefferson) ----------
function tempoDeCasa(admissao, referencia) {
  if (admissao > referencia) return "Admissão futura";
  const [aa, am, ad] = admissao.split("-").map(Number);
  const [ra, rm, rd] = referencia.split("-").map(Number);
  let meses = (ra - aa) * 12 + rm - am;
  if (rd < ad) meses--; // o mês só conta quando o dia da admissão já chegou
  const anos = Math.floor(meses / 12);
  meses %= 12;
  if (anos === 0 && meses === 0) return "Menos de 1 mês";
  const partes = [];
  if (anos > 0) partes.push(anos === 1 ? "1 ano" : `${anos} anos`);
  if (meses > 0) partes.push(meses === 1 ? "1 mês" : `${meses} meses`);
  return partes.join(" e ");
}

Servico.prototype.consultarFicha = function (matricula, referencia) {
  const funcionario = this.funcionarios.find((f) => f.id === matricula);
  exigir(funcionario, "Funcionário não encontrado.");
  const inicio = referencia.slice(0, 8) + "01";
  const [a, m] = [Number(referencia.slice(0, 4)), Number(referencia.slice(5, 7))];
  const fim = `${referencia.slice(0, 8)}${String(new Date(a, m, 0).getDate()).padStart(2, "0")}`;
  const doMes = this.pontosEntre(inicio, fim).filter((p) => p.pessoa === matricula);
  const ultimoPonto = this.pontosEntre("1900-01-01", referencia).filter((p) => p.pessoa === matricula).pop() || null;
  // o salário só é procurado quando o perfil pode ver a folha; volta até 12 meses
  let ultimaFolha = null;
  if (this.podeVerSalarios) {
    for (let i = 0; i < 12 && !ultimaFolha; i++) {
      const d = new Date(a, m - 1 - i, 1);
      const mes = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-01`;
      ultimaFolha = this.folhaDoMes(mes).find((f) => f.pessoa === matricula) || null;
    }
  }
  const conta = (s) => doMes.filter((p) => p.situacao === s).length;
  return { funcionario, tempoDeCasa: tempoDeCasa(funcionario.admissao, referencia), presencas: conta("Presença"),
    atrasos: conta("Atraso"), faltas: conta("Falta"), ultimoPonto, salarioVisivel: this.podeVerSalarios, ultimaFolha };
};

Servico.prototype.cargosCadastrados = function () {
  const vistos = new Map();
  this.funcionarios.forEach((f) => { const c = f.cargo.trim(); if (c && !vistos.has(c.toLowerCase())) vistos.set(c.toLowerCase(), c); });
  return [...vistos.values()].sort((x, y) => x.localeCompare(y, "pt-BR"));
};

Servico.prototype.mudarSituacao = function (matricula, novaSituacao) {
  const atual = this.funcionarios.find((f) => f.id === matricula);
  exigir(atual, "Funcionário não encontrado.");
  exigir(atual.situacao !== novaSituacao, `O cadastro já está com a situação ${novaSituacao}.`);
  this.salvarFuncionario({ ...atual, situacao: novaSituacao });
};

// ---------- recrutamento (Recrutamento.cs, parte do Kerlon) ----------
const SituacoesDeVaga = ["Aberta", "Encerrada"];
const indiceDaEtapa = (etapa) => { const i = Opcoes.etapas.indexOf(etapa); return i < 0 ? Infinity : i; };

Servico.prototype.consultarVagas = function (situacao = null, departamento = null) {
  exigir(situacao === null || SituacoesDeVaga.includes(situacao), "Selecione uma situação de vaga válida.");
  exigir(departamento === null || Opcoes.departamentos.includes(departamento), "Selecione um departamento válido.");
  return this.vagas
    .filter((v) => (situacao === null || v.situacao === situacao) && (departamento === null || v.departamento === departamento))
    .sort((a, b) => (a.situacao === "Aberta" ? 0 : 1) - (b.situacao === "Aberta" ? 0 : 1)
      || a.titulo.localeCompare(b.titulo, "pt-BR") || a.id - b.id);
};

Servico.prototype.encontrarVaga = function (id) {
  const vaga = this.vagas.find((v) => v.id === id);
  exigir(vaga, "Vaga não encontrada.");
  return vaga;
};

Servico.prototype.encontrarCandidato = function (id) {
  const c = this.candidatos.find((x) => x.id === id);
  exigir(c, "Candidatura não encontrada.");
  return c;
};

Servico.prototype.candidatosDaVaga = function (vagaId, etapa = null) {
  this.encontrarVaga(vagaId);
  exigir(etapa === null || Opcoes.etapas.includes(etapa), "Selecione uma etapa válida.");
  return this.candidatos
    .filter((c) => c.vaga === vagaId && (etapa === null || c.etapa === etapa))
    .sort((a, b) => indiceDaEtapa(a.etapa) - indiceDaEtapa(b.etapa) || a.nome.localeCompare(b.nome, "pt-BR") || a.id - b.id);
};

// Quantos candidatos há em cada etapa, na ordem do processo (etapas vazias aparecem com zero)
Servico.prototype.funilDeEtapas = function (vagaId = null) {
  if (vagaId !== null) this.encontrarVaga(vagaId);
  const lista = this.candidatos.filter((c) => vagaId === null || c.vaga === vagaId);
  return Opcoes.etapas.map((etapa) => ({ etapa, candidatos: lista.filter((c) => c.etapa === etapa).length }));
};

// O mesmo nome (sem diferenciar maiúsculas) não entra duas vezes na mesma vaga
Servico.prototype.conferirDuplicidade = function (nome, vagaId, ignorar) {
  const limpo = (nome || "").trim().toLocaleLowerCase("pt-BR");
  exigir(!this.candidatos.some((c) => c.id !== ignorar && c.vaga === vagaId && c.nome.toLocaleLowerCase("pt-BR") === limpo),
    "Este candidato já está inscrito nesta vaga.");
};

Servico.prototype.salvarCandidatura = function (c) {
  if (this.podeEditar) this.conferirDuplicidade(c.nome, c.vaga, c.id);
  this.salvarCandidato(c);
};

Servico.prototype.vincularAVaga = function (candidatoId, novaVagaId) {
  const atual = this.encontrarCandidato(candidatoId);
  exigir(atual.vaga !== novaVagaId, "O candidato já está nesta vaga.");
  if (this.podeEditar) this.conferirDuplicidade(atual.nome, novaVagaId, candidatoId);
  this.salvarCandidato({ ...atual, vaga: novaVagaId });
};

Servico.prototype.avancarEtapa = function (candidatoId) {
  const atual = this.encontrarCandidato(candidatoId);
  const i = indiceDaEtapa(atual.etapa);
  exigir(i < Opcoes.etapas.length - 1, "O candidato já está na última etapa do processo.");
  this.salvarCandidato({ ...atual, etapa: Opcoes.etapas[i + 1] });
};
