using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Tutorial da fase 2, ANTES do cronômetro começar. Funciona em ETAPAS. Em cada etapa:
///   1) aparece o BALÃO só com a frase (padrão 5 s) e some;
///   2) aparece a LISTA DE TAREFAS (na borda esquerda, no meio da tela);
///   3) se 'Reconhecer Mecanicas' estiver LIGADO, a lista marca cada tarefa quando o jogador a cumpre
///      e some quando todas estiverem marcadas; se estiver DESLIGADO, fica um tempo fixo e some;
///   4) vem a próxima etapa.
/// Depois da última etapa aparece o BALÃO FINAL (conclusão do tutorial) e, em seguida,
/// o DIÁLOGO DE INÍCIO DA FASE (o balão da Dra. Ju com o botão de fechar).
/// Quando o jogador fecha esse diálogo, o script ZERA o tempo da fase, libera o cronômetro
/// (LevelManager3) e dispara 'Ao Terminar'.
///
/// Enquanto um balão está na tela, a Dra. Ju aparece junto e o Oxigenius fica PARADO
/// (o componente de movimento é desligado e religado; o arquivo MovementF2.cs não é alterado).
///
/// Não altera nenhum script existente. Só lê input, o Rigidbody2D do jogador e o método público
/// getPulledNode() do NodeClickManager, e escreve em dois campos públicos em tempo de jogo:
/// TimeManager.elapsedTime (para zerar o relógio) e o texto do relógio do LevelManager3.
///
/// Serve tanto na cena "Fase 2" quanto na "Fase 2 Alt" (não depende dos nodeID dos nós).
/// IMPORTANTE: este script vai em um objeto que fique SEMPRE ativo (não nos balões nem nas listas).
/// </summary>
public class TutorialF2 : MonoBehaviour
{
    public enum TipoTarefa
    {
        Andar,       // andar uma distância mínima com A/D ou setas
        Pular,       // pular de verdade (sai do chão)
        AbrirMapa,   // apertar a tecla do mapa
        PegarCorda,  // clicar e segurar um nó ligado (qualquer parte do cérebro)
        ConectarNo   // soltar a corda em cima do nó do tutorial
    }

    [Serializable]
    public class Tarefa
    {
        [Tooltip("Nome só para organizar no Inspector.")]
        public string nome;
        public TipoTarefa tipo;
        [Tooltip("Toggle da lista que será marcado ao concluir.")]
        public Toggle checkbox;
        [HideInInspector] public bool concluida;
        [NonSerialized] public TMP_Text texto;        // texto do toggle (achado sozinho)
        [NonSerialized] public Color corOriginal;     // cor do texto antes de concluir
    }

    [Serializable]
    public class Etapa
    {
        [Tooltip("Nome só para organizar no Inspector.")]
        public string nome;

        [Header("1) Balão com a frase")]
        [Tooltip("Objeto do balão (só com a frase). Opcional.")]
        public GameObject balao;
        [Tooltip("Segundos com o balão na tela.")]
        public float tempoBalao = 5f;

        [Header("2) Lista de tarefas")]
        [Tooltip("Objeto da lista (painel com os toggles), na borda esquerda.")]
        public GameObject checklist;
        [Tooltip("Se ligado, a lista só some depois que todas as tarefas forem cumpridas.")]
        public bool reconhecerMecanicas = false;
        [Tooltip("Se ligado, só a primeira tarefa pendente é verificada (ex.: 'pegar' antes de 'conectar').")]
        public bool exigirOrdem = false;
        [Tooltip("Segundos que a lista fica depois de tudo marcado.")]
        public float tempoAposConcluir = 2f;
        [Tooltip("Segundos que a lista fica na tela quando NÃO reconhece mecânicas (só para visualizar).")]
        public float tempoSemReconhecimento = 4f;
        public List<Tarefa> tarefas = new List<Tarefa>();

        [Header("Eventos da etapa (opcional)")]
        [Tooltip("Dispara quando a lista aparece. Ex.: ligar o nó do tutorial (SetActive true).")]
        public UnityEvent aoMostrarLista;
        [Tooltip("Dispara quando a lista some. Ex.: desligar o nó do tutorial (SetActive false).")]
        public UnityEvent aoConcluirEtapa;
    }

    [Header("Etapas (na ordem em que aparecem)")]
    [SerializeField] private List<Etapa> etapas = new List<Etapa>();

    [Header("Balão final (depois da última etapa)")]
    [Tooltip("Balão que avisa que a fase vai começar. Opcional.")]
    [SerializeField] private GameObject balaoFinal;
    [SerializeField] private float tempoBalaoFinal = 5f;

    [Header("Diálogo de início da fase (depois do tutorial)")]
    [Tooltip("Objeto do diálogo que apresenta a missão (ex.: 'Dialogo Inicial'). Fica escondido durante o tutorial, aparece depois do balão final, e o cronômetro só começa quando o jogador o fecha. Opcional.")]
    [SerializeField] private GameObject dialogoInicioFase;

    [Tooltip("Menu de pausa da cena (objeto PauseSystem). Se ficar vazio, é achado sozinho. A cena começa marcada como 'pausada' por causa do diálogo inicial, e nesse estado o jogo ignora os cliques nos pontos; o tutorial desmarca isso enquanto roda.")]
    [SerializeField] private PauseMenu menuPausa;

    [Header("Dra. Ju e trava de movimento")]
    [Tooltip("Imagem da doutora que aparece junto com TODOS os balões (objeto do Canvas, atrás dos balões). Opcional.")]
    [SerializeField] private GameObject doutora;
    [Tooltip("Arraste o Player (pega o componente Player Movement F2). Ele é desligado enquanto o jogador deve ficar parado.")]
    [SerializeField] private PlayerMovementF2 movimentoJogador;
    [Tooltip("Se ligado, o Oxigenius não anda nem pula enquanto um balão (com a doutora) está na tela.")]
    [SerializeField] private bool travarMovimentoComBalao = true;
    [Tooltip("Se ligado, a trava só vale no balão da PRIMEIRA etapa (a de movimento); nos outros balões o jogador continua livre.")]
    [SerializeField] private bool travarSoNaPrimeiraEtapa = true;
    [Tooltip("Se ligado, o Oxigenius também fica parado durante o diálogo inicial (até o tutorial começar).")]
    [SerializeField] private bool travarNoDialogoInicial = true;
    [Tooltip("Cor do texto de uma tarefa depois de concluída.")]
    [SerializeField] private Color corTarefaConcluida = new Color(0.55f, 1f, 0.6f, 1f);

    [Header("Início")]
    [Tooltip("LIGADO: o tutorial começa sozinho quando a cena abre. DESLIGADO: começa quando algo chamar o método Iniciar (ex.: um botão).")]
    [SerializeField] private bool iniciarAutomaticamente = true;
    [SerializeField] private float atrasoInicial = 0.5f;
    [Tooltip("Se ligado e o jogador desligou os tutoriais no menu do jogo (preferência 'tutorialOn'), o tutorial é pulado.")]
    [SerializeField] private bool respeitarOpcaoDoMenu = true;

    [Header("Cronômetro da fase")]
    [Tooltip("Arraste o objeto LevelManager. O cronômetro só começa depois do tutorial.")]
    [SerializeField] private LevelManager3 levelManager;
    [SerializeField] private bool segurarCronometroAteOFim = true;
    [Tooltip("Zera o tempo contado pelo TimeManager quando o tutorial termina. Sem isto, o relógio começaria já descontado do tempo do diálogo e do tutorial.")]
    [SerializeField] private bool zerarTempoAoTerminar = true;

    [Header("Referências do reconhecimento (preencha ao ligar 'Reconhecer Mecanicas')")]
    [Tooltip("Arraste o Player (pega o Rigidbody2D). Usado por Andar e Pular.")]
    [SerializeField] private Rigidbody2D jogador;
    [Tooltip("Arraste o objeto LevelManager. Usado por PegarCorda e ConectarNo.")]
    [SerializeField] private NodeClickManager nodeClickManager;
    [Tooltip("Nó do tutorial (o ponto cinza). Se tiver NodeBehavior e a tag 'node', a conexão segue as regras normais da fase (corda recusada e som de erro se vier do ponto errado) e a tarefa é marcada quando ele fica ligado. Usado por ConectarNo.")]
    [SerializeField] private GameObject noAlvo;
    [Tooltip("Opcional: sprite que o nó do tutorial passa a mostrar depois de conectado (ex.: o ponto amarelo).")]
    [SerializeField] private Sprite spriteNoAlvoLigado;
    [Tooltip("Se ligado, o nó do tutorial pisca (cor e tamanho) até o jogador conectá-lo.")]
    [SerializeField] private bool piscarNoAlvo = true;
    [Tooltip("Cor para a qual o nó do tutorial pisca.")]
    [SerializeField] private Color corPiscarNoAlvo = new Color(0.55f, 1f, 0.6f, 1f);
    [Tooltip("Piscadas por segundo.")]
    [SerializeField] private float velocidadePiscar = 1.5f;
    [Tooltip("Quanto o nó cresce no auge da piscada (1 = não cresce).")]
    [SerializeField] private float escalaPiscar = 1.2f;
    [Tooltip("Opcional: dispara quando o jogador conecta o nó do tutorial (ex.: tocar um som, ligar um cabo).")]
    [SerializeField] private UnityEvent aoConectarNoAlvo;

    [Header("Parâmetros do reconhecimento")]
    [SerializeField] private float distanciaParaAndar = 1.5f;
    [SerializeField] private KeyCode teclaMapa = KeyCode.M;
    [Tooltip("Velocidade vertical mínima para considerar que o pulo aconteceu.")]
    [SerializeField] private float velocidadeMinimaPulo = 2f;

    [Header("Extras")]
    [Tooltip("Tecla para pular o tutorial inteiro (útil para testar). None = desligado.")]
    [SerializeField] private KeyCode teclaPular = KeyCode.Tab;
    [SerializeField] private UnityEvent aoTerminar;

    // estado interno
    private bool iniciado = false;
    private bool terminou = false;
    private bool encerrando = false;   // já está no diálogo de início da fase (não dá mais para pular)
    private Coroutine rotina;

    // reconhecimento
    private Etapa atual;
    private bool verificando = false;
    private float distanciaAndada = 0f;
    private float ultimoX;
    private float tempoUltimoPulo = -10f;
    private bool puxavaNoLigado = false;      // no frame anterior, o jogador segurava um nó ligado?
    private bool soltouSobreNoAlvo = false;   // já soltou a corda em cima do nó do tutorial?
    private Sprite spriteNoAlvoOriginal;      // para devolver o nó ao estado "desligado" a cada tentativa
    private bool movimentoTravado = false;
    private SpriteRenderer rendererNoAlvo;    // para piscar o nó do tutorial
    private NodeBehavior comportamentoNoAlvo; // se existir, a conexão é validada pelo próprio jogo
    private Color corNoAlvoOriginal = Color.white;
    private Vector3 escalaNoAlvoOriginal = Vector3.one;

    void Awake()
    {
        if (segurarCronometroAteOFim && levelManager)
        {
            // desliga o LevelManager3 antes do Start dele rodar (o cronômetro só começa depois do tutorial)
            levelManager.enabled = false;

            // com ele desligado o relógio ficaria parado em "00:00"; mostramos o tempo cheio (ex.: 05:00)
            if (levelManager.timerText)
            {
                float total = levelManager.timerLength > 0f ? levelManager.timerLength : 300f;
                levelManager.timerText.text = string.Format("{0:00}:{1:00}",
                    Mathf.FloorToInt(total / 60f), Mathf.FloorToInt(total % 60f));
            }
        }

        if (noAlvo)
        {
            escalaNoAlvoOriginal = noAlvo.transform.localScale;
            comportamentoNoAlvo = noAlvo.GetComponent<NodeBehavior>();
            rendererNoAlvo = noAlvo.GetComponent<SpriteRenderer>();
            if (rendererNoAlvo)
            {
                spriteNoAlvoOriginal = rendererNoAlvo.sprite;
                corNoAlvoOriginal = rendererNoAlvo.color;
            }
        }
    }

    void Start()
    {
        EsconderTudo();
        ValidarReferencias();

        // o diálogo de início da fase só aparece depois do tutorial
        if (dialogoInicioFase) dialogoInicioFase.SetActive(false);

        // a doutora ainda está falando no diálogo inicial: o jogador espera parado
        if (travarNoDialogoInicial && !iniciarAutomaticamente) StartCoroutine(TravarDepoisDoPrimeiroFrame());

        if (iniciarAutomaticamente) Iniciar();
    }

    // Espera um frame antes de travar: o script de movimento precisa rodar o Start dele primeiro
    // (é lá que ele guarda o sprite padrão e esconde o braço).
    private IEnumerator TravarDepoisDoPrimeiroFrame()
    {
        yield return null;
        if (!terminou && !verificando) TravarMovimento(true);
    }

    void Update()
    {
        // enquanto travado, garante que o personagem não escorregue para os lados
        if (movimentoTravado && jogador && Mathf.Abs(jogador.linearVelocity.x) > 0.01f)
            jogador.linearVelocity = new Vector2(0f, jogador.linearVelocity.y);

        if (!iniciado || terminou) return;

        // com o jogo pausado (menu do Esc) nada é verificado
        if (Time.timeScale == 0f) return;

        if (!encerrando && teclaPular != KeyCode.None && Input.GetKeyDown(teclaPular))
        {
            Pular();
            return;
        }

        if (verificando && atual != null)
        {
            AtualizarContadores();
            ChecarTarefas(atual);
        }

        PiscarNoAlvo();
    }

    // Enquanto a lista está na tela e o nó do tutorial ainda não foi conectado, ele pisca
    // (vai e volta entre a cor normal e a cor de destaque, crescendo um pouco).
    private void PiscarNoAlvo()
    {
        if (!piscarNoAlvo || !noAlvo || !noAlvo.activeInHierarchy) return;
        if (!verificando || soltouSobreNoAlvo) return;

        // 0 -> 1 -> 0, suave
        float t = 0.5f - 0.5f * Mathf.Cos(Time.time * velocidadePiscar * 2f * Mathf.PI);
        if (rendererNoAlvo) rendererNoAlvo.color = Color.Lerp(corNoAlvoOriginal, corPiscarNoAlvo, t);
        noAlvo.transform.localScale = escalaNoAlvoOriginal * Mathf.Lerp(1f, escalaPiscar, t);
    }

    // devolve cor e tamanho normais ao nó do tutorial
    private void PararDePiscar()
    {
        if (!noAlvo) return;
        if (rendererNoAlvo) rendererNoAlvo.color = corNoAlvoOriginal;
        noAlvo.transform.localScale = escalaNoAlvoOriginal;
    }

    /// <summary>
    /// Começa o tutorial. É chamado sozinho no Start quando 'Iniciar Automaticamente' está ligado.
    /// Chamar de novo não faz nada (por isso não há problema se um botão também o chamar).
    /// </summary>
    public void Iniciar()
    {
        if (iniciado) return;
        iniciado = true;

        // A cena vem com o menu de pausa marcado como "pausado" (isPaused) porque originalmente o
        // diálogo inicial abria primeiro. Com isso o NodeClickManager ignora os pontos e a corda não
        // funciona. Como o diálogo agora vem depois do tutorial, liberamos aqui (sem mexer no tempo
        // do jogo e sem tocar o som do menu).
        DefinirPausaDoDialogo(false);

        // o jogador desligou os tutoriais no menu do jogo? então só libera o cronômetro
        if (respeitarOpcaoDoMenu && PlayerPrefs.GetInt("tutorialOn", 1) == 0)
        {
            Debug.Log("[TUTORIAL] Tutoriais desligados no menu, pulando.");
            EsconderTudo();
            rotina = StartCoroutine(Encerramento());
            return;
        }

        rotina = StartCoroutine(Sequencia());
    }

    private IEnumerator Sequencia()
    {
        // WaitForSeconds (e não Realtime) de propósito: se o jogador pausar, os tempos também pausam
        yield return new WaitForSeconds(atrasoInicial);

        for (int i = 0; i < etapas.Count; i++)
        {
            Etapa e = etapas[i];
            PrepararTarefas(e);

            // 1) balão com a frase (doutora na tela; jogador parado só se a trava valer para esta etapa)
            bool travar = travarMovimentoComBalao && (i == 0 || !travarSoNaPrimeiraEtapa);
            if (e.balao)
                yield return MostrarBalao(e.balao, e.tempoBalao, travar);

            // 2) lista de tarefas (a validação só começa aqui; o jogador volta a se mover)
            TravarMovimento(false);
            if (e.checklist) e.checklist.SetActive(true);
            e.aoMostrarLista.Invoke();
            Debug.Log("[TUTORIAL] Lista da etapa '" + e.nome + "' na tela.");

            atual = e;
            verificando = e.reconhecerMecanicas && e.tarefas.Count > 0;
            ResetarContadores();

            if (verificando)
            {
                while (!TodasConcluidas(e))
                    yield return null;
                yield return new WaitForSeconds(e.tempoAposConcluir);
            }
            else
            {
                yield return new WaitForSeconds(e.tempoSemReconhecimento);
            }

            // 3) some a lista e passa para a próxima etapa
            verificando = false;
            atual = null;
            if (e.checklist) e.checklist.SetActive(false);
            e.aoConcluirEtapa.Invoke();
        }

        // balão final: avisa que a fase vai começar
        if (balaoFinal)
            yield return MostrarBalao(balaoFinal, tempoBalaoFinal, travarMovimentoComBalao && !travarSoNaPrimeiraEtapa);

        yield return Encerramento();
    }

    // Mostra o diálogo de início da fase, espera o jogador fechá-lo e só então libera o cronômetro.
    private IEnumerator Encerramento()
    {
        encerrando = true;
        TravarMovimento(false);

        if (dialogoInicioFase)
        {
            // volta ao comportamento original: enquanto o diálogo está aberto, os pontos não respondem.
            // O botão de fechar do diálogo chama PauseMenu.Resume(), que libera de novo.
            DefinirPausaDoDialogo(true);
            dialogoInicioFase.SetActive(true);
            Debug.Log("[TUTORIAL] Diálogo de início da fase na tela.");
            // o botão de fechar do próprio diálogo o desativa; é isso que esperamos
            while (dialogoInicioFase.activeSelf)
                yield return null;
        }

        Finalizar();
    }

    // mostra um balão por alguns segundos, com a doutora ao lado e o jogador parado
    private IEnumerator MostrarBalao(GameObject balao, float segundos, bool travar)
    {
        TravarMovimento(travar);
        if (doutora) doutora.SetActive(true);
        balao.SetActive(true);

        yield return new WaitForSeconds(segundos);

        balao.SetActive(false);
        if (doutora) doutora.SetActive(false);
    }

    // marca/desmarca a flag pública isPaused do menu de pausa (não altera Time.timeScale)
    private void DefinirPausaDoDialogo(bool pausado)
    {
        if (!menuPausa) menuPausa = FindFirstObjectByType<PauseMenu>();
        if (menuPausa) menuPausa.isPaused = pausado;
        else Debug.LogWarning("[TUTORIAL] PauseMenu não encontrado na cena.");
    }

    // liga/desliga o componente de movimento do jogador (não mexe no script dele)
    private void TravarMovimento(bool travar)
    {
        movimentoTravado = travar;
        if (!movimentoJogador) return;

        movimentoJogador.enabled = !travar;

        if (travar)
        {
            // para o personagem onde está e tira a animação de corrida
            if (jogador) jogador.linearVelocity = new Vector2(0f, jogador.linearVelocity.y);
            Animator anim = movimentoJogador.GetComponent<Animator>();
            if (anim && anim.enabled) anim.SetBool("isRunning", false);
        }
    }

    // =================================================================
    // RECONHECIMENTO DAS MECÂNICAS
    // =================================================================
    private void PrepararTarefas(Etapa e)
    {
        foreach (Tarefa t in e.tarefas)
        {
            t.concluida = false;
            if (!t.checkbox) continue;

            t.checkbox.isOn = false;
            if (!t.texto)
            {
                t.texto = t.checkbox.GetComponentInChildren<TMP_Text>(true);
                if (t.texto) t.corOriginal = t.texto.color;
            }
            if (t.texto) t.texto.color = t.corOriginal;
        }
    }

    private void ResetarContadores()
    {
        distanciaAndada = 0f;
        tempoUltimoPulo = -10f;
        puxavaNoLigado = false;
        soltouSobreNoAlvo = false;
        if (jogador) ultimoX = jogador.position.x;
    }

    // contadores que precisam ser atualizados todo frame
    private void AtualizarContadores()
    {
        if (jogador)
        {
            float x = jogador.position.x;
            // só conta distância se o jogador está realmente apertando para andar
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f)
                distanciaAndada += Mathf.Abs(x - ultimoX);
            ultimoX = x;
        }

        if (Input.GetButtonDown("Jump"))
            tempoUltimoPulo = Time.time;

        // Nó do tutorial é um nó de verdade: o NodeClickManager decide se a conexão vale
        // (aceita só a corda vinda do ponto amarelo do tutorial; do cérebro, recusa com som de erro).
        // A tarefa é marcada quando o nó fica ligado.
        if (comportamentoNoAlvo)
        {
            if (!soltouSobreNoAlvo && comportamentoNoAlvo.isPositive)
            {
                soltouSobreNoAlvo = true;
                AcenderNoAlvo();
                Debug.Log("[TUTORIAL] Nó do tutorial conectado.");
            }
        }
        // Sem NodeBehavior: detecta "soltou um nó ligado em cima do nó do tutorial" (aceita qualquer origem).
        else if (nodeClickManager)
        {
            bool puxandoLigado = PuxandoNoLigado();

            // No frame em que o mouse solta, o NodeClickManager pode já ter limpado o nó puxado
            // (depende da ordem em que os scripts rodam), por isso usamos o valor do frame anterior.
            if (Input.GetMouseButtonUp(0) && (puxavaNoLigado || puxandoLigado)
                && !soltouSobreNoAlvo && MouseSobreNoAlvo())
            {
                soltouSobreNoAlvo = true;
                AcenderNoAlvo();
                Debug.Log("[TUTORIAL] Corda solta sobre o nó do tutorial.");
            }

            puxavaNoLigado = puxandoLigado;
        }
    }

    private void ChecarTarefas(Etapa e)
    {
        foreach (Tarefa t in e.tarefas)
        {
            if (t.concluida) continue;

            if (Verificar(t.tipo))
            {
                t.concluida = true;
                if (t.checkbox) t.checkbox.isOn = true;
                if (t.texto) t.texto.color = corTarefaConcluida;
                Debug.Log("[TUTORIAL] Tarefa concluída: " + t.nome);
            }

            // se exige ordem, para na primeira tarefa pendente
            if (e.exigirOrdem && !t.concluida) break;
        }
    }

    private bool Verificar(TipoTarefa tipo)
    {
        switch (tipo)
        {
            case TipoTarefa.Andar:
                return jogador && distanciaAndada >= distanciaParaAndar;

            case TipoTarefa.Pular:
                // apertou pulo há pouco E está subindo (garante que estava no chão)
                return jogador
                    && (Time.time - tempoUltimoPulo) < 0.3f
                    && jogador.linearVelocity.y > velocidadeMinimaPulo;

            case TipoTarefa.AbrirMapa:
                return Input.GetKeyDown(teclaMapa);

            case TipoTarefa.PegarCorda:
                return PuxandoNoLigado();

            case TipoTarefa.ConectarNo:
                return soltouSobreNoAlvo;
        }
        return false;
    }

    // o jogador está segurando um nó que já está ligado (amarelo)
    private bool PuxandoNoLigado()
    {
        if (!nodeClickManager) return false;

        GameObject puxado = nodeClickManager.getPulledNode();
        if (!puxado) return false;

        NodeBehavior comportamento = puxado.GetComponent<NodeBehavior>();
        return comportamento && comportamento.isPositive;
    }

    // o cursor do mouse está em cima do collider do nó do tutorial?
    private bool MouseSobreNoAlvo()
    {
        if (!noAlvo || !noAlvo.activeInHierarchy || !Camera.main) return false;

        Vector2 mundo = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        foreach (Collider2D c in Physics2D.OverlapPointAll(mundo))
        {
            if (c.gameObject == noAlvo || c.transform.IsChildOf(noAlvo.transform))
                return true;
        }
        return false;
    }

    // resposta visual da conexão (opcional): troca o sprite e dispara o evento
    private void AcenderNoAlvo()
    {
        PararDePiscar();
        if (noAlvo && spriteNoAlvoLigado)
        {
            SpriteRenderer sr = noAlvo.GetComponent<SpriteRenderer>();
            if (sr) sr.sprite = spriteNoAlvoLigado;
        }
        aoConectarNoAlvo.Invoke();
    }

    private bool TodasConcluidas(Etapa e)
    {
        foreach (Tarefa t in e.tarefas)
            if (!t.concluida) return false;
        return true;
    }

    // =================================================================
    // FIM / PULAR
    // =================================================================
    private void Pular()
    {
        if (rotina != null) StopCoroutine(rotina);
        verificando = false;
        if (atual != null) atual.aoConcluirEtapa.Invoke();
        atual = null;
        EsconderTudo();
        PararDePiscar();
        rotina = StartCoroutine(Encerramento());
    }

    private void Finalizar()
    {
        if (terminou) return;
        terminou = true;

        // o jogador sempre termina liberado
        TravarMovimento(false);

        PararDePiscar();

        // devolve o nó do tutorial ao sprite original (ele normalmente já foi desligado pelo evento da etapa)
        if (noAlvo && spriteNoAlvoOriginal)
        {
            SpriteRenderer sr = noAlvo.GetComponent<SpriteRenderer>();
            if (sr) sr.sprite = spriteNoAlvoOriginal;
        }

        // O LevelManager3 calcula "tempo restante = timerLength - TimeManager.elapsedTime",
        // e o TimeManager conta desde que a cena abriu. Zeramos para o relógio começar cheio.
        if (zerarTempoAoTerminar && TimeManager.instance != null)
            TimeManager.instance.elapsedTime = 0f;

        // libera o cronômetro da fase
        if (levelManager) levelManager.enabled = true;

        Debug.Log("[TUTORIAL] Tutorial terminou, cronômetro liberado.");
        aoTerminar.Invoke();
    }

    private void EsconderTudo()
    {
        foreach (Etapa e in etapas)
        {
            if (e.balao) e.balao.SetActive(false);
            if (e.checklist) e.checklist.SetActive(false);
        }
        if (balaoFinal) balaoFinal.SetActive(false);
        if (doutora) doutora.SetActive(false);
    }

    private void ValidarReferencias()
    {
        if ((travarMovimentoComBalao || travarNoDialogoInicial) && !movimentoJogador)
            Debug.LogWarning("[TUTORIAL] Campo 'Movimento Jogador' faltando: o Oxigenius não vai ficar parado durante os balões.");

        if (segurarCronometroAteOFim && !levelManager)
            Debug.LogWarning("[TUTORIAL] Campo 'Level Manager' faltando: o cronômetro vai correr durante o tutorial.");

        if (noAlvo)
        {
            if (!noAlvo.GetComponentInChildren<Collider2D>(true))
                Debug.LogWarning("[TUTORIAL] O 'No Alvo' não tem Collider2D: a tarefa ConectarNo nunca será marcada.");
            // (comparação por texto de propósito: CompareTag dá erro se a tag não existir no projeto)
            if (noAlvo.tag == "node" && !noAlvo.GetComponent<NodeBehavior>())
                Debug.LogWarning("[TUTORIAL] O 'No Alvo' tem a tag 'node' mas não tem NodeBehavior: o NodeClickManager dá erro ao soltar a corda nele.");
            if (noAlvo.GetComponent<NodeBehavior>() && noAlvo.tag != "node")
                Debug.LogWarning("[TUTORIAL] O 'No Alvo' tem NodeBehavior mas não tem a tag 'node': o jogo não vai deixar conectar nele.");
        }

        foreach (Etapa e in etapas)
        {
            if (!e.balao && !e.checklist)
                Debug.LogWarning("[TUTORIAL] A etapa '" + e.nome + "' não tem balão nem lista.");

            if (!e.reconhecerMecanicas) continue;

            foreach (Tarefa t in e.tarefas)
            {
                if (!t.checkbox)
                    Debug.LogWarning("[TUTORIAL] Toggle faltando na tarefa '" + t.nome + "' da etapa '" + e.nome + "'.");
                if ((t.tipo == TipoTarefa.Andar || t.tipo == TipoTarefa.Pular) && !jogador)
                    Debug.LogWarning("[TUTORIAL] Campo 'Jogador' faltando (tarefa " + t.tipo + ").");
                if ((t.tipo == TipoTarefa.PegarCorda || t.tipo == TipoTarefa.ConectarNo) && !nodeClickManager)
                    Debug.LogWarning("[TUTORIAL] Campo 'Node Click Manager' faltando (tarefa " + t.tipo + ").");
                if (t.tipo == TipoTarefa.ConectarNo && !noAlvo)
                    Debug.LogWarning("[TUTORIAL] Campo 'No Alvo' faltando (tarefa ConectarNo).");
            }
        }
    }
}
