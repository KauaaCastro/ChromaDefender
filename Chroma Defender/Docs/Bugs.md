# Bugs — status (atualizado 2026-11-27)

| # | Bug | Causa | Status |
|---|---|---|---|
| 1 | Fase 1: primeira queda (x 110–112) | emenda entre `FlorestGround` (deslocado 56,68) e `FlorestGround_Continuacao`; fundo era só terra pintada (`Subsolo`) | **Corrigido**: buraco fechado com chão de verdade (colunas 109–112 do Continuacao). Testado: anda por cima. |
| 2 | Fase 1: dois slimes empilhados | `Slime_Enemy (2)` e `(6)` na mesma posição | **Corrigido**: cópia `(6)` removida |
| 3 | Fase 1: 3 ratos invisíveis (só a vida) | ordem de desenho 2/3, atrás do tilemap `Castle Entrace` (3) | **Corrigido**: ordem 15 (igual ao prefab) |
| 4 | Fase 2: texto / notificação da chave | — | Sem alteração (decisão do usuário) |
| 5 | Fase 2: sombra branca | — | Não é bug (decisão do usuário) |
| 6 | Buraco não mata | morte só em y < -15: o jogador caía ~1,5 s fora da tela / dentro da terra pintada | **Corrigido**: `DeathZone` 1,8 m abaixo do chão em cada buraco (Fase 1: 1; Castelo: 5; Área 2B: 7). Morre em ~0,7 s. A regra de -15 continua como reserva. |
| 7a | Baú não abre | `Bau_Madeira_ComboLeve` (Floresta Sombria) com o colisor DESLIGADO | **Corrigido**: colisor religado. Testado: detecta e abre. |
| 7b | Barreira invisível antes da última porta | árvore `Tree_4_1 (4)` com colisor sólido de 3 x 12,7 m (x 328–331) | **Corrigido**: colisor desligado (árvore continua visível) |
| extra | Porta sem destino `Atalho_Salao` | par removido do castelo | **Removida** |
| extra | Pontes da Passarela (2B) bloqueavam o pulo sobre os buracos | plataformas sólidas a 3 m bem em cima dos buracos (regressão da subida de plataformas) | **Corrigido**: 4 pontes viraram plataformas vazadas (`Plataformas_Vazadas`, one-way) |

## Observações (não alteradas)
- **Pântano x 411–414**: a plataforma original (topo 2,84) fica sobre o buraco; pular o buraco a partir do chão bate a cabeça nela. A travessia é por cima da plataforma (caixotes). Avaliar se é o desejado.
- `Porta_sala01_meio` (Passarela, área 2B) continua sem destino.
- Fase 1 usa `jumpForce 5` (castelo e 2B usam 7); o Player da Fase 1 e o da 2B ficam dentro da Main Camera (o jogo corrige em execução).
- Aviso no console em todas as cenas: "Sprite Tiling ... not generated with Full Rect" (cosmético, importação de sprites).
- Nenhuma cena gera erro/exceção em execução (Menu, Fase 1, Castelo, Área 2B, Fase 3).
