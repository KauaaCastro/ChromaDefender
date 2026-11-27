# Relatório de Balanceamento — Chroma Defender

Levantado em 2026-11-27 a partir dos prefabs e das cenas (`Fase2_Castelo`, `Sala_03_Grande`).
**Nada aqui foi testado jogando**: são números e contas. Use como mapa para os próximos ajustes.

## 1. Jogador (cena Fase2_Castelo)
| Atributo | Valor | Observação |
|---|---|---|
| Vida | 150 | invulnerável 0,6 s após levar dano |
| Pulo | jumpForce 7 (sobe ~2,43 m) | define a altura de TODAS as plataformas (degraus ≤ 2,2 m) |
| Velocidade | 4 | |
| Combo leve | 20 / 25 / 35 / 50 | começa com 2 golpes; baú Floresta Sombria → 3; baú Torre (+1) → 4 |
| Pesado | 70 / 80 / 115 | baú Pântano → 1; Ruínas → 2; Coliseu → 3 |
| Especial (F/E) | 70 por corte + 85 final | ⚠ muito forte: vários cortes de 70 por uso |

⚠ A cena `Sala_03_Grande` tem um Player **separado**: os valores dele precisam ser conferidos/igualados (só o jumpForce foi ajustado para 7).

## 2. Inimigos (prefabs)
| Inimigo | Vida | Contato | Ataque | Detecção | Recarga |
|---|---|---|---|---|---|
| Rato | 35 | 12 | 20 | 5 | 1,6 |
| Morcego | 40 | 14 | 22 | 6,5 | 1,6 |
| Arqueiro | 55 | 8 | 14 | 8,5 | 2,2 |
| ShadowSpikes | 60 | 10 | 24 | 3,5 | 2,0 |
| Mago Sombrio | 65 | 10 | 16 | 9 | 2,6 |
| Esqueleto Espada | 70 | 12 | 20 | 6 | 1,5 |
| Esqueleto Lança | 75 | 12 | 22 | 6,5 | 1,9 |
| Slime (2,4x) | 80 | 15 | 20 | 5 | 1,6 |
| Mímico | 85 | 18 | 32 | 4 | 1,6 |
| Esqueleto Escudo | 90 | 12 | 18 | 5,5 | 1,8 |

Leitura: o jogador mata quase tudo com 2–4 golpes leves; inimigos tiram 8–22 de 150 (7–15 golpes para morrer).
⚠ O Mímico (32) e o Morcego (22, rápido e voador) são os picos de dano. Rato e Morcego morrem com 2 golpes.
⚠ Os prefabs de Rato/Slime/Morcego/Mímico vêm do gerador `Assets/Editor/EnemyPrefabCreator.cs` — ajuste lá (ou desative o gerador) para a mudança não se perder.

## 3. Chefes
| Chefe | Onde | Vida | Notas |
|---|---|---|---|
| Ceifador Sombrio | Pântano | ver cena (padrão 500) | arena x 498–514 |
| Arquimago do Pântano | Arena de Batalha | 750 | invoca 3 lacaios |
| Mestre de Armas | Coliseu (ondas 3 e 5) | 380 | |
| Arquimago do Coliseu | Coliseu (onda 5) | 550 | junto com o Mestre |
| **Senhor do Abismo** | Sala do Trono | **1600** | escudo fogo/gelo a cada 10 s (7 s enfurecido); defesa 6 leves → 3 pesados → 3 especiais; atordoado 6 s com dano x1,5; foice 26, colunas 22, orbes 16, contato 18 |

Conta do chefe final: com combo leve (~30/golpe, só elemento certo) ≈ 55 acertos + 3 quebras de defesa. Com o especial (70 por corte) cai muito mais rápido → **o especial provavelmente domina a luta**.

## 4. Arenas
| Arena | Ondas (inimigos) | Vida dos inimigos | Cura entre ondas | Chefe |
|---|---|---|---|---|
| Arena de Batalha (caminho 3) | 6 / 8 / 8 / 9 / 10 | x1,5 | 50 HP, intervalo 10 s, 8 HP/s | Arquimago 750 |
| Coliseu (área 2B) | 7 / 9 / 5+Mestre / 10 / 4+Arquimago+Mestre | x1,5 | 50 HP, intervalo 8 s | nas ondas |

Totem de Vida (chefe final): a cada 90 s, ativo 20 s, cura até 60 HP.

## 5. Pontos a revisar (prioridade)
1. **Especial forte demais** (70 por corte): testar 35–45 por corte, ou ele anula a mecânica de defesa do chefe final.
2. **Vida do jogador (150) x picos de dano** (Mímico 32, chefes 22–34): em arenas cheias o jogador pode morrer rápido demais. Considerar vida 200 ou dano de contato menor nas ondas.
3. **Coliseu onda 5** (2 chefes + 4 inimigos): confirmar jogando se é possível sem o golpe pesado 3.
4. **Player da Sala_03_Grande** diferente do da Fase2: igualar vida, dano e combos (ou tirar da cena e usar um prefab único).
5. **Progressão do combo leve**: os 2 upgrades vêm só do caminho 3; quem entra na 2B já tem tudo.
6. Inimigos fracos (Rato/Morcego 35–40) somem rápido demais nas ondas finais mesmo com x1,5.

## 6. Onde mexer
- Jogador: componente `PlayerBehavior` do objeto `Player` (nas DUAS cenas).
- Inimigos: prefabs em `Assets/Prefabs/Enemies/` (e o gerador acima).
- Arenas: componente `ArenaWaves` (`Salas_Novas/Sala_Batalha/Arena_Ondas` e `Fase_Coliseu_Pedra/Gameplay/Coliseu_Ondas`).
- Chefe final: `Trono_Abismo/Gameplay/Boss_SenhorDoAbismo` (`BossAbismo`) e `Totem_de_Vida` (`LifeTotem`).
