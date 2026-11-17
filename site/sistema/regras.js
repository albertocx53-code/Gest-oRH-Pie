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
