# Worker Thought Influence Matrix

Дата сверки с кодом: 2026-05-13.

## Назначение

Этот документ описывает будущий психологический feedback layer для жителей:

```text
WorkerThought -> WorkerOpinion -> bias будущей WorkerThought
```

Это дизайн-матрица, а не новая реализация. Текущий код уже частично поддерживает общий opinion-bias в `GameBootstrap.WorkerThoughtBias.cs`: pending-мысль может быть мягко окрашена прошлым `WorkerOpinion`. Но в коде пока нет явной матрицы конкретных связей `source thought -> target thought`. Этот документ нужен как спецификация перед следующими реализациями.

Главная идея: прошлая мысль не командует будущей. Она только окрашивает реакцию, когда реальное событие уже хочет создать новую мысль.

## Текущие Опоры В Коде

- `WorkerThought` и `PendingWorkerThought`: `Assets/Scripts/Runtime/Core/GameBootstrap.RuntimeModels.cs`.
- Запись мысли и обновление lightweight `WorkerOpinion`: `GameBootstrap.WorkerThoughts.cs`.
- Active/pending formation: `GameBootstrap.WorkerThoughts.Active.cs`, `GameBootstrap.WorkerThoughtFormation.cs`.
- Generic opinion bias: `GameBootstrap.WorkerThoughtBias.cs`.
- Explicit influence-rule infrastructure: `GameBootstrap.WorkerThoughtInfluence.cs`; rule catalog currently enables the first implementation slice `Money + Work + Paid Services`.
- Affect states и affect-thoughts: `GameBootstrap.WorkerAffects.cs`.
- Knowledge/opinion score по местам и темам: `GameBootstrap.WorkerKnowledgeFormation.cs`.
- Публичные social signals для Noosphere: `GameBootstrap.SocialSignals.cs`.
- Traits/weaknesses: `GameBootstrap.WorkerPerks.cs`.

## Базовые Правила

1. Thought influence is indirect: мысль влияет через `WorkerOpinion`, а не напрямую.
2. Ни одна мысль не создаёт другую мысль сама по себе.
3. Влияние применяется только если реальный trigger уже хочет создать target thought.
4. Влияние может менять `intensity`, `priority`, `formation time`, `opinion delta`, optional wording.
5. У каждого влияния есть time window.
6. У каждого влияния есть caps.
7. Позитивный и негативный опыт оба важны.
8. Weakness может модифицировать влияние, но не должна hard-force поведение.
9. Противоречивые чувства разрешены.
10. Каждая связь должна иметь human-readable reason.

## Стандартный Формат Правила

Для реализации каждую связь стоит хранить в таком формате:

```md
#### `source_thought` -> `target_thought`

- Source branch:
- Target branch:
- Trigger required:
- Window:
- Direction:
- Intensity delta:
- Priority effect:
- Formation time effect:
- Opinion delta modifier:
- Weakness modifier:
- Trait modifier:
- Human logic:
- Safety/cap:
- Example wording:
```

`Direction`:

- `amplify`
- `dampen`
- `relief`
- `contradict`
- `stabilize`

Ниже матрица дана в компактном виде. Каждая строка таблицы соответствует одному такому rule-entry.

## Подробные Кейсы

### `low_money`

Narrative meaning: житель уже жил в состоянии, где любая мелочь превращается в проблему. Будущие платные сервисы, отсутствие работы и азартные потери ощущаются не как отдельные события, а как подтверждение финансовой опасности.

Direct influence:

- `service_unaffordable`
- `affect_financial_pressure`
- `no_job_warning`
- `salary_paid`
- `affect_gambling_regret`

Dampening/relief links:

- `salary_paid` должен смягчать будущий `low_money` и `affect_financial_pressure`.
- `job_found` и `stable_life` должны постепенно снижать confidence негативного money-opinion.

Weakness interactions:

- `Gambling`: сильнее связывает `low_money` с `affect_gambling_regret`.
- `Alcoholism`: сильнее связывает `low_money` с Bar/service cost, но без автогенерации мыслей о баре.

Trait interactions:

- `Frugal`: +3..+5 к money/service-unaffordable influence.
- `Anxious`: быстрее формирует тревожные money-thoughts.
- `Adaptable`: быстрее отпускает негатив после `salary_paid` или `job_found`.

Example player-visible wording:

- "Денег почти нет. Старый опыт делает это заметнее."
- "Зарплата пришла вовремя, тревога чуть отпустила."

### `no_job_warning`

Narrative meaning: отсутствие работы даёт человеку ощущение, что день не собран. Будущая нехватка денег, стартовая вакансия и найденная работа окрашиваются через вопрос занятости.

Direct influence:

- `starter_job_suggestion`
- `job_found`
- `low_money`
- `affect_financial_pressure`
- `salary_paid`

Dampening/relief links:

- `job_found` должен резко ослаблять future no-job pressure.
- `salary_paid` стабилизирует, но не заменяет `job_found`: деньги без работы временно помогают, но не закрывают идентичность/роль.

Weakness interactions:

- Weakness не должна напрямую менять работу.
- `Gambling` может усилить финансовое последствие, если безработица совпала с проигрышем.

Trait interactions:

- `Dutiful`: сильнее переживает отсутствие работы.
- `Anxious`: быстрее формирует `no_job_warning`.
- `Adaptable`: легче принимает starter job как переходный этап.

Example player-visible wording:

- "Работы пока нет. Старый опыт делает это заметнее."
- "Работа найдена, и старая тревога наконец отступает."

### `affect_hangover`

Narrative meaning: бар мог дать облегчение, но тело запомнило неприятные последствия. Следующий отдых в баре или усталость воспринимаются противоречиво.

Direct influence:

- `need_sleep_critical`
- `leisure_service_good`
- `affect_relief_after_rest`
- `affect_financial_pressure`
- `service_unaffordable`

Dampening/relief links:

- `home_sleep_good` и `sleep_service_good` должны ослаблять след похмелья.
- `stable_life` может снижать confidence негативного Bar-related opinion.

Weakness interactions:

- `Alcoholism`: усиливает противоречие, а не превращает бар в 100% плохой выбор.
- `Gambling`: не должна влиять на hangover.

Trait interactions:

- `Anxious`: сильнее future sleep/need anxiety.
- `Impulsive`: ярче эмоциональный след после бара.
- `Adaptable`: быстрее отпускает негативный след.

Example player-visible wording:

- "Бар помог выдохнуть, но утром стало тяжелее."
- "Старый опыт делает отдых в баре заметно противоречивым."

### `affect_gambling_regret`

Narrative meaning: проигрыш не отменяет интерес к риску, но делает следующий опыт вокруг денег и Gambling Hall более болезненным и противоречивым.

Direct influence:

- `low_money`
- `affect_financial_pressure`
- `service_unaffordable`
- `affect_gambling_excitement`
- `leisure_service_good`

Dampening/relief links:

- `salary_paid` снимает часть money pressure.
- `affect_gambling_excitement` может конфликтовать с regret, но не стирать его.

Weakness interactions:

- `Gambling`: regret становится не просто минусом, а "тянет и пугает одновременно".
- `Alcoholism`: не влияет.

Trait interactions:

- `Impulsive`: сильнее качели win/loss.
- `Cautious`: сильнее негатив к Gambling Hall.
- `Frugal`: сильнее money consequence.

Example player-visible wording:

- "Ставка ушла в минус, и теперь деньги чувствуются острее."
- "Выигрыш манит, но прошлый проигрыш мешает радоваться спокойно."

### `child_born`

Narrative meaning: рождение ребёнка даёт радость и одновременно повышает цену стабильности. Деньги, работа, дом, childcare и семейная тревога становятся более значимыми.

Direct influence:

- `affect_family_anxiety`
- `low_money`
- `no_job_warning`
- `stable_life`
- `service_unaffordable`

Dampening/relief links:

- `stable_life`, `salary_paid`, `home_sleep_good` и доступный childcare должны смягчать тревогу.

Weakness interactions:

- Weakness не должна делать человека "хуже родителем".
- `Gambling` и `Alcoholism` могут усилить money/family contrast только через реальные события.

Trait interactions:

- `Dutiful`: сильнее family thoughts.
- `Anxious`: выше intensity anxiety.
- `Trusting`: легче принимает городские решения о childcare/school как поддержку.

Example player-visible wording:

- "Дом стал другим. Теперь стабильность важнее."
- "С ребёнком любая нехватка денег звучит громче."

### `street_litter_high`

Narrative meaning: высокий мусор меняет ощущение города как среды. Это не просто визуальная грязь, а сигнал, что город не справляется с базовым порядком.

Direct influence:

- `affect_litter_irritation`
- `stable_life`
- `worker_arrived`
- `social_talk_good`
- `bus_unavailable`

Dampening/relief links:

- Последующие чистые маршруты и `stable_life` должны снижать confidence негативного litter/city opinion.

Weakness interactions:

- Weakness не влияет напрямую.

Trait interactions:

- `Meticulous`: резко усиливает litter influence.
- `Anxious`: мусор легче связывается с общим ощущением нестабильности.
- `Adaptable`: быстрее отпускает после улучшения.

Example player-visible wording:

- "Грязь на улицах уже раздражает. Старый опыт делает это заметнее."
- "После чистых улиц город снова кажется собраннее."

### `stable_life`

Narrative meaning: стабильность не делает человека слепым, но даёт ему запас доверия. Негативные события не отменяются, однако воспринимаются менее катастрофично.

Direct influence:

- `low_money`
- `no_job_warning`
- `affect_financial_pressure`
- `affect_family_anxiety`
- `affect_stable_routine`

Dampening/relief links:

- Работает как recovery layer для money/work/family/need pressure.
- Не должен гасить `street_litter_high` или `bus_unavailable` полностью, если проблема повторяется.

Weakness interactions:

- Weakness может снижать устойчивость стабильности только через реальные события: проигрыш, похмелье, траты.

Trait interactions:

- `Adaptable`: быстрее принимает новую стабильность.
- `Stubborn`: дольше держит старую тревогу.
- `Trusting`: быстрее переносит стабильность на общее доверие к городу.

Example player-visible wording:

- "Последние дни были ровными, поэтому новая проблема не сбивает сразу."
- "Стабильный ритм помогает не разгонять тревогу."

## Матрица По Веткам

Условные обозначения:

- `I`: intensity delta.
- `P`: priority effect.
- `F`: formation time multiplier.
- `O`: opinion delta modifier.
- `W/T`: weakness / trait modifiers.
- `Cap`: safety/cap.

### 1. Money

| Rule | Trigger required | Window | Direction | I | P | F | O | W/T | Human logic | Example wording |
|---|---|---:|---|---:|---|---:|---:|---|---|---|
| `low_money` -> `service_unaffordable` | платный сервис недоступен | 48h | `amplify` | +10 | +1 max | 0.85 | -2 | Frugal +4, Anxious +3 | Денежная тревога подтвердилась ценой сервиса. | "Опять всё упирается в деньги." |
| `low_money` -> `affect_financial_pressure` | money affect уже создаётся | 48h | `amplify` | +12 | +1 max | 0.82 | -2 | Frugal +3, Anxious +5 | Старая нехватка денег делает новое давление ожидаемым. | "Это уже не случайность, деньги давят." |
| `low_money` -> `no_job_warning` | нет работы | 36h | `amplify` | +8 | +1 max | 0.9 | -1 | Dutiful +3 | Без денег отсутствие работы звучит опаснее. | "Без работы деньги быстро растают." |
| `low_money` -> `salary_paid` | получена зарплата | 72h | `relief` | +6 | 0 | 0.9 | +2 | Adaptable +2 relief | Зарплата воспринимается как выдох после давления. | "Деньги пришли вовремя." |
| `low_money` -> `affect_gambling_regret` | проигрыш или broke | 48h | `amplify` | +13 | +1 max | 0.8 | -3 | Gambling +4, Frugal +4 | Проигрыш подтверждает страх остаться без денег. | "Игра ударила туда, где уже болело." |
| `service_unaffordable` -> `low_money` | low-money condition активен | 48h | `amplify` | +8 | 0 | 0.9 | -1 | Frugal +3 | Отказ сервиса делает бедность конкретной. | "Даже нужное стало недоступным." |
| `service_unaffordable` -> `affect_financial_pressure` | affect создаётся | 48h | `amplify` | +9 | +1 max | 0.86 | -2 | Frugal +4 | Цена превратилась в pressure. | "Платные места теперь ощущаются больнее." |
| `salary_paid` -> `low_money` | low-money condition снова активен | 48h | `dampen` | -8 | 0 | 1.05 | +1 | Adaptable -2 extra | Недавняя зарплата даёт запас надежды. | "Недавно платили, значит можно выровнять." |
| `salary_paid` -> `affect_financial_pressure` | money affect создаётся | 48h | `relief` | -10 | -1 max | 1.08 | +2 | Frugal keeps -2 less relief | Поступление денег снижает давление. | "После зарплаты тревога не такая острая." |
| `salary_paid` -> `service_unaffordable` | сервис снова недоступен | 24h | `contradict` | +4 | 0 | 0.95 | -1 | Frugal +3 | Зарплата была, но цены всё равно давят. | "Даже после зарплаты не хватает." |
| `salary_paid` -> `stable_life` | routine стабильна | 72h | `stabilize` | +7 | 0 | 0.9 | +2 | Adaptable +2 | Регулярная оплата поддерживает чувство порядка. | "Есть ритм: работа платит." |
| `affect_financial_pressure` -> `service_unaffordable` | платный сервис недоступен | 48h | `amplify` | +11 | +1 max | 0.84 | -2 | Frugal +4 | Pressure подтверждается новым отказом. | "Платные места снова не для меня." |
| `affect_financial_pressure` -> `no_job_warning` | нет работы | 48h | `amplify` | +9 | +1 max | 0.88 | -1 | Dutiful +3 | Деньги давят, значит работа нужна быстрее. | "Нужна работа, иначе не вытянуть." |
| `affect_financial_pressure` -> `salary_paid` | получена зарплата | 72h | `relief` | +8 | 0 | 0.9 | +2 | Adaptable +2 | Деньги снимают часть накопленного давления. | "Можно выдохнуть хотя бы сейчас." |
| `affect_gambling_regret` -> `low_money` | low-money condition активен | 48h | `amplify` | +12 | +1 max | 0.82 | -3 | Gambling +3, Frugal +4 | Проигрыш объясняет, почему деньги кончились. | "Проигрыш теперь слышен в каждом счёте." |
| `affect_gambling_regret` -> `service_unaffordable` | платный сервис недоступен | 48h | `amplify` | +9 | 0 | 0.88 | -2 | Gambling +2, Frugal +3 | После проигрыша недоступность сервиса личнее. | "Ставка отняла запас." |
| `affect_gambling_regret` -> `affect_gambling_excitement` | выигрыш после regret | 24h | `contradict` | +5 | 0 | 0.95 | -1 | Gambling turns to mixed, Impulsive +4 | Радость от выигрыша конфликтует с прошлым проигрышем. | "Радует, но прошлый минус помнится." |
| `affect_gambling_regret` -> `affect_financial_pressure` | money pressure создаётся | 48h | `amplify` | +12 | +1 max | 0.82 | -3 | Frugal +4 | Regret напрямую связан с кошельком. | "Игра снова бьёт по деньгам." |
| `affect_gambling_excitement` -> `leisure_service_good` | отдых в GamblingHall успешен | 24h | `amplify` | +7 | 0 | 0.9 | +2 | Gambling +4, Impulsive +4 | Недавний выигрыш делает азарт ярче. | "Тут бывает удача." |
| `affect_gambling_excitement` -> `affect_gambling_regret` | следующий проигрыш | 24h | `contradict` | +8 | +1 max | 0.86 | -2 | Impulsive +5 | Резкий разворот от подъёма к сожаленью. | "После подъёма падение больнее." |
| `affect_gambling_excitement` -> `service_unaffordable` | денег не хватило после ставок | 24h | `contradict` | +6 | 0 | 0.92 | -1 | Gambling +2, Frugal +3 | Позитивный азарт сталкивается с ценой. | "Выигрыш был, но деньги опять кончились." |

### 2. Work

| Rule | Trigger required | Window | Direction | I | P | F | O | W/T | Human logic | Example wording |
|---|---|---:|---|---:|---|---:|---:|---|---|---|
| `no_job_warning` -> `starter_job_suggestion` | доступна стартовая работа | 48h | `amplify` | +8 | 0 | 0.86 | +1 | Dutiful +3, Adaptable +2 | Безработица делает стартовую работу приемлемой. | "Лучше простая работа, чем пустой день." |
| `no_job_warning` -> `job_found` | работа найдена | 72h | `relief` | +10 | 0 | 0.82 | +2 | Dutiful +2 | Найденная работа закрывает активную тревогу. | "Появился понятный следующий шаг." |
| `no_job_warning` -> `low_money` | money low активен | 48h | `amplify` | +8 | +1 max | 0.88 | -1 | Frugal +3 | Нет работы, значит деньги быстро растают. | "Без работы каждый расход страшнее." |
| `no_job_warning` -> `affect_financial_pressure` | pressure создаётся | 48h | `amplify` | +9 | +1 max | 0.86 | -2 | Anxious +4 | Безработица объясняет финансовое давление. | "Денег нет, потому что нет работы." |
| `no_job_warning` -> `salary_paid` | получена зарплата | 72h | `relief` | +6 | 0 | 0.9 | +2 | Dutiful +2 | Оплата подтверждает, что роль найдена. | "Работа начала возвращать контроль." |
| `starter_job_suggestion` -> `job_found` | взята стартовая/простая работа | 48h | `relief` | +7 | 0 | 0.88 | +2 | Adaptable +3 | Предложенный путь сработал. | "Простая работа оказалась выходом." |
| `starter_job_suggestion` -> `no_job_warning` | работа всё ещё не найдена | 24h | `amplify` | +5 | 0 | 0.94 | -1 | Stubborn +2 | Подсказка не реализована, тревога держится. | "Я знаю, что надо, но пока не вышло." |
| `job_found` -> `no_job_warning` | no-job снова активен | 72h | `dampen` | -10 | -1 max | 1.1 | +2 | Adaptable +3 | Недавний опыт найденной работы даёт надежду. | "Работу уже удавалось найти." |
| `job_found` -> `salary_paid` | зарплата по новой работе | 96h | `stabilize` | +8 | 0 | 0.88 | +2 | Dutiful +2 | Работа стала источником денег. | "Работа теперь платит." |
| `job_found` -> `stable_life` | needs закрыты, деньги ок | 96h | `stabilize` | +7 | 0 | 0.9 | +2 | Adaptable +2 | Работа делает ритм понятным. | "День наконец собран." |

### 3. Needs / Meal

| Rule | Trigger required | Window | Direction | I | P | F | O | W/T | Human logic | Example wording |
|---|---|---:|---|---:|---|---:|---:|---|---|---|
| `need_meal_critical` -> `meal_service_good` | еда найдена | 24h | `relief` | +9 | 0 | 0.84 | +2 | Adaptable +2 | Сильный голод делает нормальную еду ценнее. | "Наконец нормально поел." |
| `need_meal_critical` -> `used_snack` | snack использован | 12h | `relief` | +5 | 0 | 0.9 | +1 | Impulsive +2 | Быстрый перекус спасает, но не решает всё. | "Перекус помог дотянуть." |
| `need_meal_critical` -> `service_unaffordable` | еда недоступна по деньгам | 24h | `amplify` | +10 | +1 max | 0.84 | -2 | Frugal +3, Anxious +3 | Голод плюс цена ощущаются как провал системы. | "Еда нужна сейчас, а денег нет." |
| `meal_service_good` -> `need_meal_critical` | голод снова критический | 48h | `dampen` | -7 | 0 | 1.05 | +1 | Adaptable +2 | Память о месте снижает панику. | "Я знаю, где можно поесть." |
| `meal_service_good` -> `stable_life` | needs закрыты | 48h | `stabilize` | +5 | 0 | 0.94 | +1 | Meticulous +1 | Регулярная еда собирает день. | "С едой стало проще держать ритм." |
| `used_snack` -> `need_meal_critical` | голод снова критический | 12h | `dampen` | -4 | 0 | 1.02 | 0 | Impulsive +1 | Snack даёт краткую память о временном решении. | "Хотя бы перекус иногда спасает." |
| `used_snack` -> `service_unaffordable` | нормальная еда недоступна | 24h | `contradict` | +4 | 0 | 0.98 | -1 | Frugal +2 | Перекус был обходным путём, но сервис всё ещё дорогой. | "Перекус не заменяет нормальную еду." |

### 4. Needs / Sleep

| Rule | Trigger required | Window | Direction | I | P | F | O | W/T | Human logic | Example wording |
|---|---|---:|---|---:|---|---:|---:|---|---|---|
| `need_sleep_critical` -> `sleep_service_good` | мотель/сон помог | 24h | `relief` | +9 | 0 | 0.84 | +2 | Adaptable +2 | Сильная усталость делает восстановление заметным. | "Сон реально спас." |
| `need_sleep_critical` -> `home_sleep_good` | сон дома | 48h | `relief` | +10 | 0 | 0.82 | +3 | Dutiful +2 | Дом превращает сон в стабильность. | "Дома восстанавливаться спокойнее." |
| `need_sleep_critical` -> `used_coffee` | кофе использован | 12h | `relief` | +5 | 0 | 0.9 | +1 | Impulsive +2 | Кофе помогает коротко, но не заменяет сон. | "Кофе держит, но ненадолго." |
| `sleep_service_good` -> `need_sleep_critical` | усталость снова критическая | 48h | `dampen` | -7 | 0 | 1.05 | +1 | Adaptable +2 | Житель помнит, где восстановиться. | "Я уже знаю, где можно выспаться." |
| `sleep_service_good` -> `stable_life` | routine закрыта | 48h | `stabilize` | +6 | 0 | 0.94 | +1 | Dutiful +1 | Нормальный сон делает день управляемым. | "После сна день ровнее." |
| `home_sleep_good` -> `need_sleep_critical` | усталость снова критическая | 72h | `dampen` | -9 | 0 | 1.08 | +2 | Adaptable +2 | Свой дом снижает страх усталости. | "Дома можно прийти в себя." |
| `home_sleep_good` -> `affect_family_anxiety` | family pressure создаётся | 48h | `dampen` | -5 | 0 | 1.04 | +1 | Dutiful +2 | Дом как опора смягчает семейный стресс. | "Дома хотя бы есть опора." |
| `used_coffee` -> `need_sleep_critical` | усталость возвращается | 12h | `contradict` | +5 | 0 | 0.96 | -1 | Anxious +2 | Кофе был временным костылём. | "Кофе не заменил сон." |
| `used_coffee` -> `salary_paid` | смена/работа после кофе оплачена | 24h | `stabilize` | +3 | 0 | 0.98 | +1 | Dutiful +1 | Короткий рывок оказался полезным. | "Дотянул до оплаты." |

### 5. Needs / Leisure

| Rule | Trigger required | Window | Direction | I | P | F | O | W/T | Human logic | Example wording |
|---|---|---:|---|---:|---|---:|---:|---|---|---|
| `need_leisure_critical` -> `leisure_service_good` | отдых сработал | 24h | `relief` | +8 | 0 | 0.86 | +2 | Adaptable +2 | Напряжение делает отдых заметнее. | "Наконец выдохнул." |
| `need_leisure_critical` -> `affect_relief_after_rest` | relief affect создаётся | 24h | `amplify` | +7 | 0 | 0.9 | +1 | Impulsive +2 | После предела облегчение ярче. | "Отдых правда отпустил." |
| `need_leisure_critical` -> `affect_hangover` | отдых дал плохой хвост | 24h | `contradict` | +8 | +1 max | 0.88 | -2 | Alcoholism +4, Anxious +2 | Попытка отдохнуть обернулась платой. | "Хотел выдохнуть, а стало тяжелее." |
| `leisure_service_good` -> `need_leisure_critical` | досуг снова критичен | 48h | `dampen` | -6 | 0 | 1.04 | +1 | Adaptable +2 | Есть память о месте для отдыха. | "Я знаю, где можно отвлечься." |
| `leisure_service_good` -> `affect_relief_after_rest` | relief создаётся | 24h | `amplify` | +6 | 0 | 0.92 | +1 | Alcoholism Bar +2, Gambling Hall +2 | Удачный отдых усиливает чувство облегчения. | "Это место помогает выдохнуть." |
| `leisure_service_good` -> `affect_hangover` | bar hangover создаётся | 24h | `contradict` | +6 | 0 | 0.94 | -1 | Alcoholism +4 | Хорошее место может оставить плохой след. | "Помогло, но цена неприятная." |

### 6. Family

| Rule | Trigger required | Window | Direction | I | P | F | O | W/T | Human logic | Example wording |
|---|---|---:|---|---:|---|---:|---:|---|---|---|
| `house_bought` -> `family_formed` | family formed | 120h | `stabilize` | +7 | 0 | 0.9 | +2 | Dutiful +2 | Дом делает семью правдоподобной. | "Есть куда обживаться." |
| `house_bought` -> `child_born` | ребёнок родился | 240h | `stabilize` | +6 | 0 | 0.92 | +2 | Dutiful +2 | Свой дом смягчает нагрузку рождения. | "Ребёнок родился уже в своём доме." |
| `house_bought` -> `affect_family_anxiety` | family anxiety создаётся | 120h | `dampen` | -6 | 0 | 1.04 | +1 | Anxious reduces relief | Дом даёт опору, но не решает всё. | "Дом есть, значит не всё разваливается." |
| `family_formed` -> `child_born` | ребёнок родился | 240h | `stabilize` | +7 | 0 | 0.9 | +2 | Dutiful +3 | Новая семья делает рождение частью пути. | "Семья стала больше." |
| `family_formed` -> `affect_family_anxiety` | family pressure создаётся | 120h | `amplify` | +6 | 0 | 0.92 | -1 | Anxious +4, Dutiful +3 | Семья увеличивает ответственность. | "Теперь решения касаются не только меня." |
| `family_formed` -> `stable_life` | routine стабильна | 120h | `stabilize` | +6 | 0 | 0.92 | +2 | Adaptable +2 | Семья может стать частью стабильности. | "Мы обживаемся." |
| `child_born` -> `affect_family_anxiety` | childcare/upkeep pressure | 240h | `amplify` | +12 | +1 max | 0.82 | -3 | Dutiful +4, Anxious +5 | Ребёнок повышает цену нестабильности. | "Теперь за домом стоят дети." |
| `child_born` -> `low_money` | money low активен | 168h | `amplify` | +9 | +1 max | 0.88 | -2 | Frugal +4, Dutiful +3 | Расходы семьи делают нехватку денег острее. | "С ребёнком денег нужно больше." |
| `child_born` -> `no_job_warning` | нет работы | 168h | `amplify` | +8 | +1 max | 0.9 | -1 | Dutiful +4 | Ребёнок усиливает потребность в роли/доходе. | "Нужно держаться за работу." |
| `child_born` -> `stable_life` | routine стабильна | 240h | `stabilize` | +7 | 0 | 0.92 | +2 | Adaptable +2 | Если всё держится, ребёнок усиливает смысл стабильности. | "Дом стал живым, и ритм держится." |
| `child_born` -> `service_unaffordable` | childcare/paid service недоступен | 168h | `amplify` | +9 | +1 max | 0.88 | -2 | Frugal +3, Dutiful +4 | Недоступный сервис задевает семью, не только кошелёк. | "Это уже вопрос ребёнка." |
| `affect_family_anxiety` -> `low_money` | money low активен | 72h | `amplify` | +8 | +1 max | 0.9 | -1 | Frugal +3 | Семейная тревога делает деньги темой безопасности. | "Семья держится на этих деньгах." |
| `affect_family_anxiety` -> `stable_life` | routine стабильна | 96h | `relief` | +6 | 0 | 0.92 | +2 | Adaptable +2 | Стабильность снижает семейное давление. | "Сегодня семья будто спокойнее." |
| `affect_family_anxiety` -> `no_job_warning` | нет работы | 72h | `amplify` | +9 | +1 max | 0.88 | -2 | Dutiful +4 | Безработица угрожает семейной роли. | "Без работы семью не удержать." |

### 7. Social

| Rule | Trigger required | Window | Direction | I | P | F | O | W/T | Human logic | Example wording |
|---|---|---:|---|---:|---|---:|---:|---|---|---|
| `social_talk_good` -> `social_shared_place` | встреча в месте | 72h | `amplify` | +5 | 0 | 0.94 | +1 | Sociable +4, Reserved -3 | Хороший разговор делает повторную встречу значимой. | "Мы уже нормально говорили." |
| `social_talk_good` -> `social_learned_new_topic` | новая тема в разговоре | 72h | `amplify` | +6 | 0 | 0.92 | +1 | Trusting +3, Skeptical -3 | Приятный собеседник повышает вес его темы. | "От него тема звучит убедительнее." |
| `social_talk_good` -> `affect_family_anxiety` | family anxiety активна | 24h | `dampen` | -4 | 0 | 1.02 | +1 | Sociable +2 | Разговор временно разгружает тревогу. | "После разговора не так одиноко." |
| `social_shared_place` -> `social_talk_good` | idle talk случился | 72h | `amplify` | +5 | 0 | 0.94 | +1 | Sociable +3 | Общее место создаёт знакомство. | "Мы часто пересекаемся." |
| `social_shared_place` -> `leisure_service_good` | отдых в том же месте | 48h | `amplify` | +4 | 0 | 0.96 | +1 | Sociable +2 | Место с людьми кажется теплее. | "Тут есть знакомые лица." |
| `social_learned_new_topic` -> `knowledge_reflection_building` | topic knowledge сформировалось | 72h | `amplify` | +6 | 0 | 0.9 | +1 | Curious +4, Skeptical -2 | Услышанная тема требует осмысления. | "Эта тема не выходит из головы." |
| `social_learned_new_topic` -> `social_talk_good` | новый разговор с тем же worker | 72h | `amplify` | +4 | 0 | 0.96 | +1 | Trusting +3 | Тема закрепляет связь с источником. | "С ним есть о чём говорить." |

### 8. City / Litter

| Rule | Trigger required | Window | Direction | I | P | F | O | W/T | Human logic | Example wording |
|---|---|---:|---|---:|---|---:|---:|---|---|---|
| `worker_arrived` -> `stable_life` | первые дни прошли ровно | 72h | `stabilize` | +5 | 0 | 0.94 | +1 | Trusting +2 | Удачный старт помогает принять город. | "Похоже, здесь можно устроиться." |
| `worker_arrived` -> `street_litter_low` | мусор замечен в первые дни | 24h | `amplify` | +4 | 0 | 0.96 | -1 | Meticulous +3 | Первое впечатление легко окрашивается деталями. | "Сразу видно, где город недособран." |
| `stable_life` -> `low_money` | money low активен | 72h | `dampen` | -7 | 0 | 1.06 | +1 | Adaptable +3, Stubborn -3 | Стабильная память снижает катастрофизацию. | "Бывало ровно, значит можно выправить." |
| `stable_life` -> `no_job_warning` | нет работы | 72h | `dampen` | -7 | 0 | 1.06 | +1 | Adaptable +3 | Ранее город давал опору. | "Система уже помогала собраться." |
| `stable_life` -> `affect_financial_pressure` | pressure создаётся | 72h | `relief` | -8 | -1 max | 1.08 | +2 | Stubborn weakens relief | Хорошая рутина даёт запас спокойствия. | "Не всё сразу рушится." |
| `stable_life` -> `affect_family_anxiety` | family anxiety создаётся | 72h | `relief` | -7 | 0 | 1.06 | +2 | Dutiful keeps concern | Стабильный быт поддерживает семью. | "Дома в целом держится." |
| `stable_life` -> `affect_stable_routine` | stable routine создаётся | 120h | `stabilize` | +8 | 0 | 0.9 | +2 | Adaptable +2 | Стабильная мысль подтверждает stable affect. | "Ритм становится привычным." |
| `street_litter_low` -> `street_litter_medium` | мусор повторяется/растёт | 48h | `amplify` | +5 | 0 | 0.94 | -1 | Meticulous +4 | Небольшой мусор становится паттерном. | "Это уже не случайная бумажка." |
| `street_litter_low` -> `affect_litter_irritation` | litter affect создаётся | 48h | `amplify` | +4 | 0 | 0.96 | -1 | Meticulous +4 | Первые раздражители копятся. | "Мусор начинает цеплять взгляд." |
| `street_litter_medium` -> `street_litter_high` | мусор ухудшился | 48h | `amplify` | +7 | 0 | 0.9 | -2 | Meticulous +4 | Средний уровень делает высокий ожидаемым. | "Стало заметно хуже." |
| `street_litter_medium` -> `affect_litter_irritation` | affect создаётся | 48h | `amplify` | +7 | 0 | 0.9 | -2 | Meticulous +5 | Непорядок уже мешает, а не просто виден. | "Улицы начинают раздражать." |
| `street_litter_high` -> `affect_litter_irritation` | affect создаётся | 72h | `amplify` | +13 | +1 max | 0.8 | -3 | Meticulous +6, Anxious +3 | Высокий мусор подтверждает раздражение городом. | "Грязь стала частью маршрута." |
| `street_litter_high` -> `stable_life` | stable thought создаётся | 72h | `contradict` | -8 | 0 | 1.08 | -2 | Meticulous -4 stronger | Трудно чувствовать стабильность в грязной среде. | "Ритм есть, но улицы портят ощущение." |
| `street_litter_high` -> `worker_arrived` | новый arrival/первое впечатление | 24h | `amplify` | +6 | 0 | 0.92 | -1 | Trusting -2, Skeptical +2 | Первое впечатление города становится грязным. | "Город встретил не лучшим видом." |
| `street_litter_high` -> `social_talk_good` | разговор случился рядом с грязным маршрутом | 24h | `dampen` | -4 | 0 | 1.02 | 0 | Sociable softens | Неприятная среда чуть портит настроение разговора. | "Даже разговор не полностью отвлёк." |
| `street_litter_high` -> `bus_unavailable` | транспорт не помог на грязном маршруте | 24h | `amplify` | +5 | 0 | 0.94 | -1 | Meticulous +3 | Грязный маршрут и плохой транспорт складываются в city frustration. | "И улицы плохие, и добраться сложно." |
| `affect_litter_irritation` -> `street_litter_high` | high litter thought создаётся | 48h | `amplify` | +10 | +1 max | 0.84 | -2 | Meticulous +5 | Состояние раздражения делает мусор громче. | "Грязь уже заранее раздражает." |
| `affect_litter_irritation` -> `stable_life` | stable thought создаётся | 48h | `dampen` | -6 | 0 | 1.05 | -1 | Adaptable reduces | Раздражение мешает поверить в ровный город. | "День ровный, но улицы цепляют." |
| `affect_stable_routine` -> `stable_life` | stable thought создаётся | 96h | `stabilize` | +8 | 0 | 0.9 | +2 | Adaptable +2 | Состояние подтверждает мысль о стабильности. | "Ритм держится не первый день." |
| `affect_stable_routine` -> `low_money` | money low активен | 72h | `dampen` | -5 | 0 | 1.04 | +1 | Stubborn weakens | Ровный период даёт запас спокойствия. | "Проблема есть, но не вся жизнь рушится." |
| `affect_stable_routine` -> `affect_family_anxiety` | family anxiety создаётся | 72h | `dampen` | -5 | 0 | 1.04 | +1 | Dutiful keeps concern | Стабильный быт снижает семейную тревогу. | "Дома пока держится." |

### 9. Transport

| Rule | Trigger required | Window | Direction | I | P | F | O | W/T | Human logic | Example wording |
|---|---|---:|---|---:|---|---:|---:|---|---|---|
| `bus_chosen` -> `bus_unavailable` | автобус не помог позже | 24h | `contradict` | +6 | 0 | 0.92 | -1 | Cautious +2 | Ожидание удобства столкнулось с отказом. | "Я рассчитывал на автобус." |
| `bus_chosen` -> `stable_life` | поездки стабильно помогают | 72h | `stabilize` | +5 | 0 | 0.94 | +1 | Trusting +2 | Предсказуемый транспорт поддерживает ритм. | "Маршрут стал понятным." |
| `bus_unavailable` -> `bus_chosen` | автобус снова предлагается | 48h | `dampen` | -5 | 0 | 1.04 | -1 | Cautious +3 | Недавний отказ снижает доверие к автобусу. | "В прошлый раз автобус не выручил." |
| `bus_unavailable` -> `no_job_warning` | опоздание/нет работы из-за пути | 24h | `amplify` | +5 | 0 | 0.94 | -1 | Dutiful +3 | Транспортная проблема угрожает работе. | "Даже добраться до работы сложно." |
| `bus_unavailable` -> `need_sleep_critical` | долгий пеший путь усилил усталость | 24h | `amplify` | +5 | 0 | 0.94 | -1 | Anxious +2 | Плохой транспорт превращается в усталость. | "Пешком слишком долго." |

### 10. Affect Thoughts

| Rule | Trigger required | Window | Direction | I | P | F | O | W/T | Human logic | Example wording |
|---|---|---:|---|---:|---|---:|---:|---|---|---|
| `affect_relief_after_rest` -> `need_leisure_critical` | leisure снова критичен | 48h | `dampen` | -6 | 0 | 1.04 | +1 | Adaptable +2 | Житель помнит, что отдых может помочь. | "Я уже выдыхал, значит снова смогу." |
| `affect_relief_after_rest` -> `affect_hangover` | бар дал тяжесть | 24h | `contradict` | +6 | 0 | 0.94 | -1 | Alcoholism +3 | Облегчение конфликтует с плохим хвостом. | "Помогло, но потом накрыло." |
| `affect_relief_after_rest` -> `stable_life` | routine стабильна | 48h | `stabilize` | +5 | 0 | 0.94 | +1 | Adaptable +2 | Отдых восстановил запас устойчивости. | "После отдыха день собрался." |
| `affect_hangover` -> `need_sleep_critical` | усталость критическая | 24h | `amplify` | +10 | +1 max | 0.84 | -2 | Alcoholism +4, Anxious +3 | Похмелье делает усталость телеснее. | "После бара усталость тяжелее." |
| `affect_hangover` -> `leisure_service_good` | отдых в Bar снова сработал | 48h | `contradict` | +5 | 0 | 0.96 | -1 | Alcoholism makes mixed | Бар помогает, но прошлый хвост мешает радоваться. | "Хорошо, но я помню утро после бара." |
| `affect_hangover` -> `affect_relief_after_rest` | relief после бара | 48h | `contradict` | +5 | 0 | 0.96 | -1 | Alcoholism +3 | Облегчение окрашено риском последствий. | "Отпустило, но не бесплатно." |
| `affect_hangover` -> `affect_financial_pressure` | деньги низкие после бара | 24h | `amplify` | +7 | 0 | 0.9 | -2 | Frugal +3 | Плохой отдых плюс траты становятся финансовым уколом. | "Бар ещё и стоил денег." |
| `affect_hangover` -> `service_unaffordable` | платный сервис недоступен | 24h | `amplify` | +6 | 0 | 0.92 | -1 | Frugal +3 | После трат недоступность звучит справедливо. | "Деньги ушли не туда." |
| `affect_gambling_regret` -> `leisure_service_good` | GamblingHall дал отдых/выигрыш | 48h | `contradict` | +5 | 0 | 0.96 | -1 | Gambling mixed | Место манит, но прошлый проигрыш остаётся. | "Тянет сыграть, но память неприятная." |
| `affect_gambling_excitement` -> `affect_financial_pressure` | pressure после ставок | 24h | `contradict` | +7 | +1 max | 0.9 | -2 | Impulsive +3 | Радость от риска сталкивается с кошельком. | "Азарт был яркий, но деньги реальны." |
| `affect_financial_pressure` -> `affect_stable_routine` | routine пытается стать стабильной | 48h | `dampen` | -8 | 0 | 1.08 | -2 | Frugal +3 | Финансовое давление мешает считать жизнь ровной. | "Пока деньги давят, стабильность хрупкая." |
| `affect_family_anxiety` -> `affect_stable_routine` | routine пытается стать стабильной | 72h | `dampen` | -7 | 0 | 1.06 | -1 | Dutiful +3 | Семейная тревога мешает полному спокойствию. | "Ритм есть, но семья всё ещё давит." |
| `affect_litter_irritation` -> `worker_arrived` | новый arrival/первое впечатление | 24h | `amplify` | +5 | 0 | 0.94 | -1 | Meticulous +3 | Город встречает через раздражающий фон. | "Сразу видно, где порядок не держат." |
| `affect_stable_routine` -> `affect_financial_pressure` | pressure создаётся | 72h | `dampen` | -5 | 0 | 1.04 | +1 | Adaptable +2 | Стабильный ритм даёт запас до паники. | "Проблема есть, но день не развалился." |

### 11. Stability / Recovery

| Rule | Trigger required | Window | Direction | I | P | F | O | W/T | Human logic | Example wording |
|---|---|---:|---|---:|---|---:|---:|---|---|---|
| `job_found` -> `affect_financial_pressure` | money pressure создаётся | 72h | `relief` | -7 | 0 | 1.05 | +1 | Dutiful +2 | Работа даёт ожидаемый путь к деньгам. | "Работа есть, значит давление можно пережить." |
| `meal_service_good` -> `affect_stable_routine` | stable routine создаётся | 48h | `stabilize` | +4 | 0 | 0.96 | +1 | Meticulous +1 | Закрытая еда помогает быту. | "Бытовые вещи начали работать." |
| `home_sleep_good` -> `affect_stable_routine` | stable routine создаётся | 72h | `stabilize` | +6 | 0 | 0.92 | +2 | Dutiful +2 | Сон дома сильнее всего поддерживает ритм. | "Дом держит режим." |
| `salary_paid` -> `affect_stable_routine` | stable routine создаётся | 72h | `stabilize` | +7 | 0 | 0.9 | +2 | Adaptable +2 | Деньги подтверждают устойчивость работы. | "Оплата пришла, значит система работает." |
| `stable_life` -> `service_unaffordable` | сервис недоступен | 48h | `dampen` | -4 | 0 | 1.02 | +1 | Frugal blocks some relief | Стабильная память снижает общий драматизм, но цену не отменяет. | "Неприятно, но это не весь город." |

## Не Связывать

Эти связи выглядят соблазнительно, но не проходят human-logic test:

- `bus_chosen` не должен сильно влиять на `child_born`.
- `used_coffee` не должен влиять на `affect_gambling_excitement`.
- `worker_arrived` не должен влиять на все будущие city thoughts навсегда.
- `social_talk_good` не должен автоматически лечить `low_money`.
- `stable_life` не должен полностью гасить реальные critical needs.
- `starter_job_resolved` не должен влиять на психологию напрямую: это технический closure thought.

## Safety Rules Для Реализации

- Максимальный intensity delta от influence: `+15 / -10`.
- Максимальный priority increase: один уровень.
- Minimum formation time: `0.12h`.
- Обычное influence window: `24-72h`.
- Family/house/child links могут жить дольше: `120-240h`, но с малым delta.
- Stable/recovery thoughts могут снижать negative opinion confidence, но не удалять мнение мгновенно.
- Active thought refresh не должен повторно стакать opinion.
- Технические мысли вроде `starter_job_resolved` не влияют на будущую психологию напрямую.
- `*_known_place` template variants наследуют influence от base key: `need_meal_critical`, `need_sleep_critical`, `need_leisure_critical`, `no_job_warning`.
- Weakness усиливает интерпретацию только если реальное событие уже произошло.
- Contradict-direction не должен переворачивать мысль в противоположную. Он добавляет mixed wording и слабый delta.
- SocialSignal и Noosphere получают уже окрашенную мысль, но не должны сами становиться source thought для этой матрицы без отдельного правила.

## Suggested Data Shape

Не реализовывать в этом проходе.

```csharp
private sealed class WorkerThoughtInfluenceRule
{
    public string SourceThoughtKey;
    public string TargetThoughtKey;
    public WorkerThoughtInfluenceDirection Direction;
    public int IntensityDelta;
    public int OpinionDeltaModifier;
    public float FormationTimeMultiplier;
    public float WindowHours;
    public WorkerWeaknessKind? WeaknessModifier;
    public WorkerTraitKind? TraitModifier;
    public string HumanReasonRu;
    public string HumanReasonEn;
}
```

```csharp
private enum WorkerThoughtInfluenceDirection
{
    Amplify,
    Dampen,
    Relief,
    Contradict,
    Stabilize
}
```

Markdown/YAML-like example:

```md
source: low_money
target: service_unaffordable
direction: amplify
windowHours: 48
intensityDelta: +10
priorityEffect: +1 max
formationTimeMultiplier: 0.85
opinionDeltaModifier: -2
weaknessModifier: none
traitModifier: Frugal +4, Anxious +3
humanReason: "денежная проблема подтвердилась"
```

## Итоговая Готовность

Implemented in code:

- Slice `Money + Work + Paid Services`: 23 enabled rules in `GameBootstrap.WorkerThoughtInfluence.cs`.
- Covered source thoughts: `low_money`, `service_unaffordable`, `salary_paid`, `affect_financial_pressure`, `no_job_warning`, `starter_job_suggestion`, `job_found`.
- Covered target thoughts include: `service_unaffordable`, `affect_financial_pressure`, `no_job_warning`, `salary_paid`, `low_money`, `starter_job_suggestion`, `job_found`, `affect_stable_routine`.
- Slice `Needs`: 30 enabled rules for Meal, Sleep, Leisure, `affect_relief_after_rest`, and `affect_hangover`.
- Slice `Family`: 14 enabled rules for `house_bought`, `family_formed`, `child_born`, and `affect_family_anxiety`.
- Slice `City/Litter`: 21 enabled rules for `worker_arrived`, `stable_life`, `street_litter_low`, `street_litter_medium`, `street_litter_high`, `affect_litter_irritation`, and `affect_stable_routine`.
- Slice `Social`: 7 enabled rules for `social_talk_good`, `social_shared_place`, and `social_learned_new_topic`.
- Slice `Transport`: 5 enabled rules for `bus_chosen` and `bus_unavailable`.
- Slice `Gambling contradiction`: 10 enabled rules for `low_money`, `affect_gambling_regret`, and `affect_gambling_excitement`.
- Total enabled rules: 110.
- Additional covered source thoughts: `need_meal_critical`, `meal_service_good`, `used_snack`, `need_sleep_critical`, `sleep_service_good`, `home_sleep_good`, `used_coffee`, `need_leisure_critical`, `leisure_service_good`, `affect_relief_after_rest`, `affect_hangover`.
- Additional covered Family source thoughts: `house_bought`, `family_formed`, `child_born`, `affect_family_anxiety`.
- Additional covered City/Litter source thoughts: `worker_arrived`, `stable_life`, `street_litter_low`, `street_litter_medium`, `street_litter_high`, `affect_litter_irritation`, `affect_stable_routine`.
- Additional covered Social source thoughts: `social_talk_good`, `social_shared_place`, `social_learned_new_topic`.
- Additional covered Transport source thoughts: `bus_chosen`, `bus_unavailable`.
- Additional covered Gambling source thoughts: `affect_gambling_regret`, `affect_gambling_excitement`.
- Not yet implemented: remaining Stability/Recovery rules outside these slices.

Ready for implementation:

- Stability / Recovery links.

Risky connections:

- Remaining family-child-school pressure links, because they can become too sticky if confidence decay is weak.
- Stable/recovery links, because they can accidentally erase real problems.

Document-only for now:

- Social-topic influence on arbitrary `WorkerTopicOpinion`.
- Full family-child-school pressure matrix.
- Transport influence beyond direct bus reliability and fatigue/work access.

Recommended first implementation slice:

```text
Money + Work + Paid Services
```

Small testable set:

- `low_money`
- `service_unaffordable`
- `salary_paid`
- `no_job_warning`
- `affect_financial_pressure`

Why this slice first:

- easy to trigger in play;
- opinion subjects already exist: `money`, `city_work`, paid building types;
- player-visible effect is understandable;
- runaway risk is low with existing caps and cooldowns.

## Summary

- Source thoughts covered: 36.
- Influence rules described: 119.
- High-detail source thoughts: `low_money`, `no_job_warning`, `affect_hangover`, `affect_gambling_regret`, `child_born`, `street_litter_high`, `stable_life`.
- Top 5 implementation candidates:
  1. `low_money` -> `service_unaffordable`
  2. `service_unaffordable` -> `affect_financial_pressure`
  3. `salary_paid` -> `affect_financial_pressure`
  4. `no_job_warning` -> `job_found`
  5. `affect_financial_pressure` -> `no_job_warning`
