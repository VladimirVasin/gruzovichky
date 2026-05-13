# Worker Thought Tree

Дата сверки с кодом: 2026-05-13.

## Назначение

Документ описывает дерево мыслей жителей: какие события рождают мысли, какие временные состояния их усиливают, как мысли превращаются в мнения, знания, социальные сигналы и сигналы ноосферы.

Код остается источником правды. Эта карта нужна, чтобы новые мысли добавлялись не плоским списком, а в понятную цепочку:

```text
причина -> статус/состояние -> мысль -> мнение/знание -> социальный сигнал -> ноосфера/UI
```

## Термины

- Thought: конкретная мысль жителя. В модели это `WorkerThought`: `Key`, `TemplateKey`, `Kind`, `Tone`, `Priority`, `Intensity`, время создания, active/resolved flags и placeholders.
- Active Thought: мысль, которая живет пока условие актуально. Создается через `AddOrKeepActiveWorkerThought`, но сначала проходит pending-формирование.
- Pending Thought: мысль в процессе формирования. В модели это `PendingWorkerThought`; она появляется сразу, а в `WorkerThought` попадает после `ReadyWorldHour`, если условие еще валидно.
- Affect State: временное эмоциональное/ситуационное состояние из `WorkerAffect`: вид, сила, источник, причина, срок жизни.
- Need Status: технический статус потребности `Ok / Warning / Critical` для еды, сна и досуга.
- Weakness: устойчивая слабость жителя: `None`, `Alcoholism`, `Gambling`. Слабость влияет на выбор и интерпретацию опыта, но не является positive/negative perk.
- Knowledge: существующий слой `WorkerMemory` / `PendingWorkerKnowledge`; хранит факт/слух/опыт/мнение о месте или теме.
- Opinion: два текущих слоя: lightweight `WorkerOpinion` от мыслей и `WorkerTopicOpinion` / `WorkerKnowledge` opinion score для тем и мест.
- Noosphere Signal: публичный `SocialSignal` или Noosphere Vision insight, который делает личную мысль/состояние видимым в ноосфере.

## Главное Правило

Новые мысли не добавляются "куда попало".

Каждая новая мысль должна иметь:

- ветку дерева;
- источник/триггер;
- условие жизни;
- связь с affect/need/weakness, если есть;
- влияние на opinion/knowledge/noosphere;
- способ отображения в UI;
- запись в этом документе.

## Системный Поток

1. Триггер в симуляции вызывает `RecordWorkerThought`, `AddOrKeepActiveWorkerThought` или `AddOrKeepPendingWorkerThought`.
2. Active thoughts сначала становятся pending thoughts, затем становятся `WorkerThought`, если условие еще валидно.
3. `RecordWorkerThought` может обновить lightweight `WorkerOpinion`, если передан `opinionDelta`.
4. Любая записанная мысль проходит через `RecordSocialSignalFromWorkerThought`; этот сигнал публичен для ноосферы.
5. WorkerKnowledge формируется отдельно, но может менять шаблон pending thought на `*_known_place`.
6. Affect states создают собственные active thoughts и влияют на building knowledge opinion score/confidence.
7. Workers UI показывает current/pending/recent thoughts, F9/States показывает справочник личности/состояний/служебных пометок, Noosphere Vision собирает массовые состояния и social signals.

Основные файлы:

- `Assets/Scripts/Runtime/Core/GameBootstrap.RuntimeModels.cs`
- `Assets/Scripts/Runtime/Core/GameBootstrap.WorkerPerks.cs`
- `Assets/Scripts/Runtime/Actors/GameBootstrap.WorkerThoughts.cs`
- `Assets/Scripts/Runtime/Actors/GameBootstrap.WorkerThoughts.Active.cs`
- `Assets/Scripts/Runtime/Actors/GameBootstrap.WorkerThoughtFormation.cs`
- `Assets/Scripts/Runtime/Actors/GameBootstrap.WorkerAffects.cs`
- `Assets/Scripts/Runtime/Actors/GameBootstrap.WorkerKnowledgeFormation.cs`
- `Assets/Scripts/Runtime/Actors/GameBootstrap.SocialSignals.cs`
- `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs`
- `Assets/Scripts/Runtime/UI/GameBootstrap.FleetCanvas.StatesScreen.cs`
- `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.NoosphereVision.Insights.cs`

## Главное Дерево

### 1. Needs / Потребности

Потребности создают active critical thoughts, сервисные instant thoughts и fallback thoughts.

Meal:

- `need_meal_warning`
- `need_meal_critical`
- `need_meal_critical_known_place`
- `meal_service_good`
- `used_snack`
- `service_missing`
- `service_unaffordable`
- `need_fallback_bad`

Sleep:

- `need_sleep_warning`
- `need_sleep_critical`
- `need_sleep_critical_known_place`
- `sleep_service_good`
- `home_sleep_good`
- `used_coffee`
- `service_missing`
- `service_unaffordable`
- `need_fallback_bad`

Leisure:

- `need_leisure_warning`
- `need_leisure_critical`
- `need_leisure_critical_known_place`
- `leisure_service_good`
- `service_missing`
- `service_unaffordable`
- `need_fallback_bad`

### 2. Work / Работа

Мысли про отсутствие работы, стартовую вакансию, найденную работу и зарплату.

- `no_job_warning`
- `no_job_warning_known_place`
- `no_job_today`
- `starter_job_suggestion`
- `starter_job_resolved`
- `job_found`
- `salary_paid`

### 3. Money / Деньги

Деньги как самостоятельная тема и как причина давления.

- `low_money`
- `salary_paid`
- `service_unaffordable`
- `affect_financial_pressure`
- `affect_gambling_excitement`
- `affect_gambling_regret`

### 4. Family / Семья

Семейные события, дом, дети и тревоги.

- `house_bought`
- `family_formed`
- `child_born`
- `affect_family_anxiety`

### 5. Social / Социальные Связи

Разговоры, знакомства и перенос тем между жителями.

- `social_talk_good`
- `social_shared_place`
- `social_learned_new_topic`

### 6. City / Город

Общее восприятие города, приезд, мусор, стабильность.

- `worker_arrived`
- `stable_life`
- `street_litter_low`
- `street_litter_medium`
- `street_litter_high`
- `affect_litter_irritation`
- `affect_stable_routine`

### 7. Transport / Транспорт

Решения и проблемы перемещения.

- `bus_chosen`
- `bus_unavailable`

### 8. Knowledge Reflection / Осмысление Знаний

Мысли, которые возникают от накопления знания, а не от прямой нужды.

- `knowledge_reflection_building`
- `social_learned_new_topic`

### 9. Affect Thoughts / Мысли От Состояний

Каждый сильный active affect создает active thought с ключом `affect_*`.

- `affect_financial_pressure`
- `affect_family_anxiety`
- `affect_relief_after_rest`
- `affect_hangover`
- `affect_litter_irritation`
- `affect_gambling_excitement`
- `affect_gambling_regret`
- `affect_stable_routine`

## Реестр Thought Keys

### `need_meal_warning`

- Ветка: Needs / Meal.
- Запускает: сейчас активная логика не рождает warning; ключ есть в шаблонах и Workers UI.
- Тип: template/UI-ready, фактически inactive.
- Tone/priority: Negative в UI; active rules только resolve warning при recovery/escalation.
- Placeholders: нет.
- Влияние: если будет создан через общую запись мысли, даст social signal и UI; сейчас служебный будущий слой.
- Создается: нет активного create hook; warning key вычисляется в `GameBootstrap.WorkerThoughts.Active.cs:311`.
- Отображается: dedicated case в `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:514`; справочная пометка в `GameBootstrap.FleetCanvas.StatesScreen.cs:161`.
- Notes/TODO: при добавлении warning-логики нужно не забыть pending/resolve условия.

### `need_meal_critical`

- Ветка: Needs / Meal.
- Запускает: `LastMealNeedStatus == Critical`.
- Тип: active thought через pending formation.
- Tone/priority: Negative, intensity 90, priority Critical.
- Placeholders: `{need}`.
- Влияние: `WorkerOpinion` по subject `Need/Meal` с delta `-6`, social signal, Noosphere, Workers UI.
- Создается: `UpdateWorkerActiveNeedThought` в `GameBootstrap.WorkerThoughts.Active.cs:309`, запись на `:322`.
- Отображается: dedicated case в `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:515`.
- Notes/TODO: при наличии знания о месте может использовать template `need_meal_critical_known_place`.

### `need_meal_critical_known_place`

- Ветка: Needs / Meal.
- Запускает: critical meal thought плюс найденное знание о `Canteen` или `Kiosk`.
- Тип: template variant для pending/active thought `need_meal_critical`.
- Tone/priority: наследует `need_meal_critical`.
- Placeholders: `{knownPlace}`.
- Влияние: добавляет building subject/opinion context, помогает связать нужду с известным местом.
- Создается: template заменяется в `ApplyWorkerThoughtKnowledgeContext`, `GameBootstrap.WorkerThoughtFormation.cs:426`.
- Отображается: рендерится через `need_meal_critical` UI case, `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:515`.
- Notes/TODO: это не отдельный `Thought.Key`, а `TemplateKey`.

### `meal_service_good`

- Ветка: Needs / Meal.
- Запускает: успешная еда в столовой.
- Тип: instant thought.
- Tone/priority: Positive, intensity 54, priority Normal default.
- Placeholders: `{service}`, `{need}`.
- Влияние: `WorkerOpinion` по building type с delta `+5`, social signal, Noosphere, Workers UI.
- Создается: helper `RecordWorkerServiceThought` в `GameBootstrap.WorkerThoughts.cs:241`; вызов для Canteen в `GameBootstrap.Transport.DriverWalk.cs:538`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:525`.
- Notes/TODO: покупка еды также пишет building knowledge отдельно перед/во время service flow.

### `used_snack`

- Ветка: Needs / Meal.
- Запускает: auto-use `Snack`, когда потребность в еде проседает.
- Тип: instant thought.
- Tone/priority: Positive, intensity 54/72 по старому статусу, priority Normal.
- Placeholders: `{need}`.
- Влияние: `WorkerOpinion` по `Need/Meal` с delta `+4`, social signal, Noosphere, Workers UI.
- Создается: `RecordWorkerNeedConsumableThought`, `GameBootstrap.WorkerThoughts.Active.cs:348`; вызов после применения relief в `GameBootstrap.WorkerNeeds.cs:370`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:520`.
- Notes/TODO: item id выбирает `used_snack` vs `used_coffee`.

### `need_sleep_warning`

- Ветка: Needs / Sleep.
- Запускает: сейчас активная логика не рождает warning; ключ есть в шаблонах и Workers UI.
- Тип: template/UI-ready, фактически inactive.
- Tone/priority: Negative в UI.
- Placeholders: нет.
- Влияние: пока только справочная готовность.
- Создается: нет активного create hook; warning key вычисляется в `GameBootstrap.WorkerThoughts.Active.cs:311`.
- Отображается: dedicated case в `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:516`; справочная пометка в `GameBootstrap.FleetCanvas.StatesScreen.cs:161`.
- Notes/TODO: warning сейчас только резолвится.

### `need_sleep_critical`

- Ветка: Needs / Sleep.
- Запускает: `LastSleepNeedStatus == Critical`.
- Тип: active thought через pending formation.
- Tone/priority: Negative, intensity 90, priority Critical.
- Placeholders: `{need}`.
- Влияние: `WorkerOpinion` по subject `Need/Sleep` с delta `-6`, social signal, Noosphere, Workers UI.
- Создается: `UpdateWorkerActiveNeedThought`, `GameBootstrap.WorkerThoughts.Active.cs:309`, запись на `:322`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:517`.
- Notes/TODO: может перейти на known-place template.

### `need_sleep_critical_known_place`

- Ветка: Needs / Sleep.
- Запускает: critical sleep thought плюс знание о `PersonalHouse` или `Motel`.
- Тип: template variant для `need_sleep_critical`.
- Tone/priority: наследует `need_sleep_critical`.
- Placeholders: `{knownPlace}`.
- Влияние: добавляет building subject/opinion context.
- Создается: `ApplyWorkerThoughtKnowledgeContext`, `GameBootstrap.WorkerThoughtFormation.cs:433`.
- Отображается: через `need_sleep_critical` UI case, `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:517`.
- Notes/TODO: это `TemplateKey`, не отдельный `Thought.Key`.

### `sleep_service_good`

- Ветка: Needs / Sleep.
- Запускает: успешная ночевка в Motel.
- Тип: instant thought.
- Tone/priority: Positive, intensity 46.
- Placeholders: `{service}`, `{need}`.
- Влияние: `WorkerOpinion` по `Motel` с delta `+3`, social signal, Noosphere, Workers UI.
- Создается: helper `RecordWorkerServiceThought`, `GameBootstrap.WorkerThoughts.cs:241`; вызов в `GameBootstrap.Transport.DriverWalk.cs:353`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:526`.
- Notes/TODO: рядом формируется building knowledge о Motel.

### `home_sleep_good`

- Ветка: Needs / Sleep / Housing.
- Запускает: сон в Personal House.
- Тип: instant thought.
- Tone/priority: Positive, intensity 48.
- Placeholders: `{home}`.
- Влияние: `WorkerOpinion` по `PersonalHouse` с delta `+3`, social signal, Noosphere, Workers UI.
- Создается: `GameBootstrap.Transport.DriverWalk.cs:285`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:526`.
- Notes/TODO: home sleep также связан с household/family loop.

### `used_coffee`

- Ветка: Needs / Sleep.
- Запускает: auto-use `Coffee`.
- Тип: instant thought.
- Tone/priority: Positive, intensity 54/72 по старому статусу.
- Placeholders: `{need}`.
- Влияние: `WorkerOpinion` по `Need/Sleep` с delta `+4`, social signal, Noosphere, Workers UI.
- Создается: `RecordWorkerNeedConsumableThought`, `GameBootstrap.WorkerThoughts.Active.cs:348`; вызов в `GameBootstrap.WorkerNeeds.cs:370`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:521`.
- Notes/TODO: coffee сейчас остается item-level emergency relief, не отдельный affect.

### `need_leisure_warning`

- Ветка: Needs / Leisure.
- Запускает: сейчас активная логика не рождает warning; ключ есть в шаблонах и Workers UI.
- Тип: template/UI-ready, фактически inactive.
- Tone/priority: Negative в UI.
- Placeholders: нет.
- Влияние: пока только справочная готовность.
- Создается: нет активного create hook; warning key вычисляется в `GameBootstrap.WorkerThoughts.Active.cs:311`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:518`; справочная пометка в `GameBootstrap.FleetCanvas.StatesScreen.cs:161`.
- Notes/TODO: warning сейчас только резолвится.

### `need_leisure_critical`

- Ветка: Needs / Leisure.
- Запускает: `LastLeisureNeedStatus == Critical`.
- Тип: active thought через pending formation.
- Tone/priority: Negative, intensity 90, priority Critical.
- Placeholders: `{need}`.
- Влияние: `WorkerOpinion` по subject `Need/Leisure` с delta `-6`, social signal, Noosphere, Workers UI.
- Создается: `UpdateWorkerActiveNeedThought`, `GameBootstrap.WorkerThoughts.Active.cs:309`, запись на `:322`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:519`.
- Notes/TODO: может перейти на known-place template.

### `need_leisure_critical_known_place`

- Ветка: Needs / Leisure.
- Запускает: critical leisure thought плюс знание о `Bar`, `CityPark` или `GamblingHall`.
- Тип: template variant для `need_leisure_critical`.
- Tone/priority: наследует `need_leisure_critical`.
- Placeholders: `{knownPlace}`.
- Влияние: добавляет building subject/opinion context.
- Создается: `ApplyWorkerThoughtKnowledgeContext`, `GameBootstrap.WorkerThoughtFormation.cs:440`.
- Отображается: через `need_leisure_critical` UI case, `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:519`.
- Notes/TODO: это `TemplateKey`, не отдельный `Thought.Key`.

### `leisure_service_good`

- Ветка: Needs / Leisure.
- Запускает: успешный досуг в Bar или CityPark.
- Тип: instant thought.
- Tone/priority: Positive, intensity 38-42.
- Placeholders: `{service}`, `{need}`.
- Влияние: `WorkerOpinion` по building type с delta `+3`, social signal, Noosphere, Workers UI; Bar/CityPark могут дополнительно создавать affects.
- Создается: helper `RecordWorkerServiceThought`, `GameBootstrap.WorkerThoughts.cs:241`; Bar вызов `GameBootstrap.Transport.DriverWalk.cs:491`, CityPark `:618` и `:628`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:527`.
- Notes/TODO: `RecordWorkerLeisureAffect` сейчас создает Bar affects; CityPark hook пока пустой.

### `service_missing`

- Ветка: Needs / Service Availability.
- Запускает: нужное сервисное здание не построено.
- Тип: instant thought.
- Tone/priority: Negative, intensity 62.
- Placeholders: `{service}`, `{need}`, `{reason}`.
- Влияние: `WorkerOpinion` по building type с delta `-5`, social signal, Noosphere, Workers UI.
- Создается: `RecordWorkerServiceMissingThought`, `GameBootstrap.WorkerThoughts.cs:263`; вызовы в `GameBootstrap.Drivers.LifeCycle.cs:666` и `GameBootstrap.Drivers.LifeCycle.Services.cs:403`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:522`.
- Notes/TODO: общий ключ для еды/сна/досуга, ветка определяется placeholder need/service.

### `service_unaffordable`

- Ветка: Needs / Money.
- Запускает: сервис есть, но у жителя не хватает денег на fee.
- Тип: instant thought.
- Tone/priority: Negative, intensity 74.
- Placeholders: `{service}`, `{balance}`.
- Влияние: `WorkerOpinion` по building type с delta `-7`, social signal, Noosphere, Workers UI; может поддерживать FinancialPressure affect.
- Создается: `GameBootstrap.Drivers.LifeCycle.Services.cs:410`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:523`.
- Notes/TODO: относится сразу к Needs и Money.

### `need_fallback_bad`

- Ветка: Needs / Fallback.
- Запускает: потребность закрыта бесплатным/неидеальным fallback-способом.
- Тип: instant thought.
- Tone/priority: Negative, intensity 72.
- Placeholders: `{need}`, `{reason}`.
- Влияние: `WorkerOpinion` по need с delta `-8`, social signal, Noosphere, Workers UI.
- Создается: `RecordWorkerNeedFallbackThought`, `GameBootstrap.Drivers.LifeCycle.cs:631`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:524`.
- Notes/TODO: важно отличать от `service_missing`: fallback уже произошел, missing только объясняет недоступность.

### `no_job_warning`

- Ветка: Work.
- Запускает: житель unemployed по правилам `IsWorkerUnemployedForThoughts`.
- Тип: active thought через pending formation.
- Tone/priority: Negative, intensity 52/70, priority Normal/High по деньгам.
- Placeholders: `{reason}`.
- Влияние: `WorkerOpinion` по `city_work` с delta `-3`, social signal, Noosphere, Workers UI; участвует в current-thought importance.
- Создается: `UpdateWorkerActiveNoJobThought`, `GameBootstrap.WorkerThoughts.Active.cs:209`, запись на `:221`; также daily migration может вызвать `RecordWorkerNoJobThought`, `GameBootstrap.WorkerMigration.cs:89`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:508`.
- Notes/TODO: основной активный ключ вместо legacy `no_job_today`.

### `no_job_warning_known_place`

- Ветка: Work.
- Запускает: `no_job_warning` плюс знание о потенциальном рабочем месте.
- Тип: template variant для thought key `no_job_warning`.
- Tone/priority: наследует `no_job_warning`.
- Placeholders: `{knownPlace}`.
- Влияние: добавляет building subject/opinion context к рабочей тревоге.
- Создается: `ApplyWorkerThoughtKnowledgeContext`, `GameBootstrap.WorkerThoughtFormation.cs:419`.
- Отображается: через `no_job_warning` UI case, `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:508`.
- Notes/TODO: это `TemplateKey`, не отдельный `Thought.Key`.

### `no_job_today`

- Ветка: Work.
- Запускает: сейчас активная логика не использует.
- Тип: legacy alias/template.
- Tone/priority: исторически Negative.
- Placeholders: нет.
- Влияние: если старые мысли с этим ключом есть в памяти, UI покажет их как no-job state.
- Создается: текущих create hooks нет.
- Отображается: alias в `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:508`; legacy note в `GameBootstrap.FleetCanvas.StatesScreen.cs:162`.
- Notes/TODO: оставить только для совместимости, новые места должны использовать `no_job_warning`.

### `starter_job_suggestion`

- Ветка: Work.
- Запускает: unemployed worker с money `< 100`, пока нет `starter_job_resolved`.
- Тип: active thought через pending formation.
- Tone/priority: Neutral, intensity 42, priority Normal.
- Placeholders: нет.
- Влияние: `WorkerOpinion` по `city_work` с delta `-1`, social signal, Noosphere, Workers UI.
- Создается: `UpdateWorkerActiveNoJobThought`, `GameBootstrap.WorkerThoughts.Active.cs:239`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:510`.
- Notes/TODO: закрывается при job found или достаточных деньгах.

### `starter_job_resolved`

- Ветка: Work.
- Запускает: первая работа найдена.
- Тип: instant technical closing thought.
- Tone/priority: Neutral, intensity 8, priority Low.
- Placeholders: нет.
- Влияние: блокирует повторное появление `starter_job_suggestion`; не должен шуметь в UI.
- Создается: `RecordWorkerJobFoundThought`, `GameBootstrap.WorkerThoughts.Active.cs:406`.
- Отображается: скрывается из HUD в `ShouldShowWorkerThoughtInHud`, `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:474`.
- Notes/TODO: техническая закрывающая мысль.

### `job_found`

- Ветка: Work.
- Запускает: житель подписывает первый контракт после отсутствия контракта.
- Тип: instant thought.
- Tone/priority: Positive, intensity 66.
- Placeholders: `{job}`.
- Влияние: `WorkerOpinion` по `city_work` с delta `+8`, social signal, Noosphere, Workers UI; резолвит no-job thoughts.
- Создается: `RecordWorkerJobFoundThought`, `GameBootstrap.WorkerThoughts.Active.cs:378`; вызов после контракта в `GameBootstrap.WorkerContracts.cs:222`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:512`.
- Notes/TODO: вместе пишет `starter_job_resolved`.

### `salary_paid`

- Ветка: Work / Money.
- Запускает: выплата зарплаты за смену.
- Тип: instant thought.
- Tone/priority: Positive, intensity 52/68 по текущему балансу.
- Placeholders: `{amount}`, `{balance}`.
- Влияние: `WorkerOpinion` по `salary` с delta `+4`, social signal, Noosphere, Workers UI.
- Создается: `GameBootstrap.Drivers.HiringAndShifts.cs:662`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:513`.
- Notes/TODO: это и work feedback, и money relief.

### `low_money`

- Ветка: Money.
- Запускает: `worker.Money < 15`.
- Тип: active thought через pending formation.
- Tone/priority: Negative, intensity 66/82, priority High/Critical.
- Placeholders: `{balance}`.
- Влияние: `WorkerOpinion` по `money` с delta `-5`, social signal, Noosphere, Workers UI; усиливает current-thought importance.
- Создается: `UpdateWorkerActiveLowMoneyThought`, `GameBootstrap.WorkerThoughts.Active.cs:280`, запись на `:290`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:511`.
- Notes/TODO: может сосуществовать с `affect_financial_pressure`, но это разные слои.

### `house_bought`

- Ветка: Family / Housing.
- Запускает: покупка Personal House.
- Тип: instant thought.
- Tone/priority: Positive, intensity 80.
- Placeholders: `{home}`.
- Влияние: `WorkerOpinion` по `PersonalHouse` с delta `+10`, social signal, Noosphere, Workers UI; рядом пишется building knowledge.
- Создается: `GameBootstrap.Transport.DriverWalk.cs:200`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:537`.
- Notes/TODO: сейчас относится к `WorkerThoughtKind.Family`, не к Housing kind.

### `family_formed`

- Ветка: Family.
- Запускает: образование семьи.
- Тип: instant thought.
- Tone/priority: Positive, intensity 82.
- Placeholders: `{otherWorker}`, `{home}`, `{family}`.
- Влияние: `WorkerOpinion` по partner worker с delta `+12`, social signal, Noosphere, Workers UI.
- Создается: `RecordWorkerFamilyFormedThought`, `GameBootstrap.WorkerFamilies.cs:253`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:535`.
- Notes/TODO: family readiness/household logic живет рядом, но мысль только фиксирует событие.

### `child_born`

- Ветка: Family.
- Запускает: рождение ребенка в семье.
- Тип: instant thought.
- Tone/priority: Positive, intensity 95.
- Placeholders: `{child}`, `{family}`.
- Влияние: `WorkerOpinion` по child subject с delta `+18`, social signal, Noosphere, Workers UI.
- Создается: `GameBootstrap.WorkerChildren.cs:160`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:536`.
- Notes/TODO: downstream family pressure может позже создать `affect_family_anxiety`.

### `social_talk_good`

- Ветка: Social.
- Запускает: положительный разговор без shared-place контекста.
- Тип: instant thought.
- Tone/priority: Positive, intensity 48.
- Placeholders: `{otherWorker}`.
- Влияние: `WorkerOpinion` по other worker с переданным social delta, social signal, Noosphere, Workers UI.
- Создается: `RecordWorkerSocialThought`, `GameBootstrap.WorkerSocial.cs:339`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:531`.
- Notes/TODO: частота и сила связаны с traits/social memory.

### `social_shared_place`

- Ветка: Social.
- Запускает: social interaction с общим местом.
- Тип: instant thought.
- Tone/priority: Positive, intensity 36.
- Placeholders: `{otherWorker}`, `{place}`.
- Влияние: `WorkerOpinion` по other worker, social signal, Noosphere, Workers UI.
- Создается: `RecordWorkerSocialThought`, `GameBootstrap.WorkerSocial.cs:339`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:532`.
- Notes/TODO: место приходит как building placeholder.

### `social_learned_new_topic`

- Ветка: Social / Knowledge Reflection.
- Запускает: осмысление conversation-topic knowledge после разговора.
- Тип: pending thought.
- Tone/priority: Positive/Neutral/Negative по tone knowledge, priority Normal.
- Placeholders: `{otherWorker}`, `{topic}`.
- Влияние: `WorkerOpinion` по source worker с delta от `OpinionScore / 16`, social signal, Noosphere, Workers UI.
- Создается: `QueueWorkerKnowledgeReflectionThought`, `GameBootstrap.WorkerThoughtFormation.cs:559`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:533`.
- Notes/TODO: это мост Knowledge -> Thought.

### `worker_arrived`

- Ветка: City.
- Запускает: регистрация нового жителя.
- Тип: instant thought.
- Tone/priority: Neutral, intensity 38.
- Placeholders: `{source}`.
- Влияние: `WorkerOpinion` по `town_arrival` с delta `+1`, social signal, Noosphere, Workers UI.
- Создается: `RecordWorkerArrivalThought`, `GameBootstrap.WorkerThoughts.cs:286`; вызов в `GameBootstrap.Actors.cs:389`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:509`.
- Notes/TODO: стартовая мысль для первых/прибывших жителей.

### `stable_life`

- Ветка: City.
- Запускает: дневной satisfaction delta `>= 8`, если деньги не в low-money зоне.
- Тип: instant thought.
- Tone/priority: Positive, intensity 34.
- Placeholders: нет.
- Влияние: `WorkerOpinion` по `city_life` с delta `+2`, social signal, Noosphere, Workers UI.
- Создается: `GameBootstrap.WorkerMigration.cs:98`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:548`.
- Notes/TODO: не путать с affect `StableRoutine`; это дневная позитивная мысль.

### `street_litter_low`

- Ветка: City / Litter.
- Запускает: nearby street litter perception `>= 0.75`.
- Тип: active thought через pending formation.
- Tone/priority: Negative, intensity 32, priority Low.
- Placeholders: нет.
- Влияние: `WorkerOpinion` по `street_litter` с delta `-1`, social signal, Noosphere, Workers UI.
- Создается: `UpdateWorkerStreetLitterActiveThought`, `GameBootstrap.WorkerLitterExperience.cs:155`, запись на `:166`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:528`.
- Notes/TODO: active severity mutually resolves medium/high.

### `street_litter_medium`

- Ветка: City / Litter.
- Запускает: nearby street litter perception `>= 3.8`.
- Тип: active thought через pending formation.
- Tone/priority: Negative, intensity 54, priority Normal.
- Placeholders: нет.
- Влияние: `WorkerOpinion` по `street_litter` с delta `-2`, social signal, Noosphere, Workers UI; может поддержать `IrritatedByLitter`.
- Создается: `UpdateWorkerStreetLitterActiveThought`, `GameBootstrap.WorkerLitterExperience.cs:155`, запись на `:166`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:529`.
- Notes/TODO: active severity mutually resolves low/high.

### `street_litter_high`

- Ветка: City / Litter.
- Запускает: nearby street litter perception `>= 7.0`.
- Тип: active thought через pending formation.
- Tone/priority: Negative, intensity 76, priority High.
- Placeholders: нет.
- Влияние: `WorkerOpinion` по `street_litter` с delta `-4`, social signal, Noosphere, Workers UI; сильнее всего поддерживает `IrritatedByLitter`.
- Создается: `UpdateWorkerStreetLitterActiveThought`, `GameBootstrap.WorkerLitterExperience.cs:155`, запись на `:166`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:530`.
- Notes/TODO: красивое отображение в Workers UI уже есть.

### `bus_chosen`

- Ветка: Transport.
- Запускает: worker выбирает local bus trip.
- Тип: instant thought.
- Tone/priority: Positive, intensity 34.
- Placeholders: `{reason}`.
- Влияние: `WorkerOpinion` по `local_bus` с delta `+2`, social signal, Noosphere, Workers UI.
- Создается: `GameBootstrap.Drivers.cs:293`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:538`.
- Notes/TODO: display description пока фиксированная, не выводит reason.

### `bus_unavailable`

- Ветка: Transport.
- Запускает: нет bus driver/available bus или нет safe path to stop.
- Тип: instant thought.
- Tone/priority: Negative, intensity 36/42.
- Placeholders: `{reason}`.
- Влияние: `WorkerOpinion` по `local_bus` с delta `-2/-3`, social signal, Noosphere, Workers UI.
- Создается: `GameBootstrap.Drivers.cs:236` и `GameBootstrap.Drivers.cs:271`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:539`.
- Notes/TODO: display description пока не рендерит конкретный reason.

### `knowledge_reflection_building`

- Ветка: Knowledge Reflection.
- Запускает: formed building knowledge, которое еще актуально/displayable.
- Тип: pending thought.
- Tone/priority: Neutral, intensity 30-58, priority Low.
- Placeholders: `{knownPlace}`.
- Влияние: `WorkerOpinion` по building type с delta `+1`, social signal, Noosphere, Workers UI.
- Создается: `QueueWorkerKnowledgeReflectionThought`, `GameBootstrap.WorkerThoughtFormation.cs:594`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:534`.
- Notes/TODO: это второй мост Knowledge -> Thought.

### `affect_financial_pressure`

- Ветка: Affect Thoughts / Money.
- Запускает: active `FinancialPressure` с intensity `>= 42`.
- Тип: active thought через pending formation.
- Tone/priority: Negative; priority Normal/High/Critical по intensity.
- Placeholders: `{reason}`.
- Влияние: `WorkerOpinion` по `money` с delta `-3`, social signal, Noosphere, Workers UI; affect также снижает building knowledge score для Motel/Bar/GamblingHall.
- Создается: affect в `GameBootstrap.WorkerAffects.cs:33`, thought в `ApplyWorkerAffectThoughts`, `GameBootstrap.WorkerAffects.cs:506` и `:524`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:540`; Noosphere Vision chain в `GameBootstrap.NoosphereVision.Insights.cs:374`.
- Notes/TODO: не дублировать с `low_money`: low_money это прямой active thought, financial pressure это state.

### `affect_family_anxiety`

- Ветка: Affect Thoughts / Family.
- Запускает: active `FamilyAnxiety` с intensity `>= 42`.
- Тип: active thought через pending formation.
- Tone/priority: Negative; priority по intensity.
- Placeholders: `{reason}`.
- Влияние: `WorkerOpinion` по `family` с delta `-3`, social signal, Noosphere, Workers UI; affect повышает knowledge score/confidence для family-related buildings.
- Создается: affect в `GameBootstrap.WorkerAffects.cs:65`, thought в `GameBootstrap.WorkerAffects.cs:506` и `:524`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:541`; Noosphere Vision chain в `GameBootstrap.NoosphereVision.Insights.cs:374`.
- Notes/TODO: reason пока берется из family pressure, не из отдельной child/school thought.

### `affect_relief_after_rest`

- Ветка: Affect Thoughts / Needs.
- Запускает: Bar leisure without hangover.
- Тип: active thought через pending formation.
- Tone/priority: Positive; priority по intensity.
- Placeholders: `{reason}`.
- Влияние: `WorkerOpinion` по `city` с delta `+3`, social signal, Noosphere, Workers UI; affect повышает Bar knowledge score.
- Создается: `RecordWorkerLeisureAffect`, `GameBootstrap.WorkerAffects.cs:164`, set на `:186`; thought в `:524`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:542`; Noosphere Vision chain в `GameBootstrap.NoosphereVision.Insights.cs:374`.
- Notes/TODO: сейчас relief after rest в основном Bar-driven.

### `affect_hangover`

- Ветка: Affect Thoughts / Needs.
- Запускает: Bar leisure при усталости/critical sleep или Alcoholism weakness random chance.
- Тип: active thought через pending formation.
- Tone/priority: Negative; priority по intensity.
- Placeholders: `{reason}`.
- Влияние: `WorkerOpinion` по `city` с delta `-3`, social signal, Noosphere, Workers UI; affect снижает Bar knowledge score.
- Создается: `RecordWorkerLeisureAffect`, `GameBootstrap.WorkerAffects.cs:164`, set на `:182`; thought в `:524`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:543`; Noosphere Vision chain в `GameBootstrap.NoosphereVision.Insights.cs:374`.
- Notes/TODO: weakness Alcoholism повышает шанс и интенсивность.

### `affect_litter_irritation`

- Ветка: Affect Thoughts / City / Litter.
- Запускает: `IrritatedByLitter` от накопленного/пикового litter exposure.
- Тип: active thought через pending formation.
- Tone/priority: Negative; priority по intensity.
- Placeholders: `{reason}`.
- Влияние: `WorkerOpinion` по `litter` с delta `-3`, social signal, Noosphere, Workers UI.
- Создается: affect в `GameBootstrap.WorkerAffects.cs:112`, set на `:129`; thought в `:524`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:544`; Noosphere Vision chain в `GameBootstrap.NoosphereVision.Insights.cs:374`.
- Notes/TODO: `Meticulous` усиливает intensity.

### `affect_gambling_excitement`

- Ветка: Affect Thoughts / Money.
- Запускает: gambling net win.
- Тип: active thought через pending formation.
- Tone/priority: Positive; priority по intensity.
- Placeholders: `{reason}`.
- Влияние: `WorkerOpinion` по `gambling` с delta `+3`, social signal, Noosphere, Workers UI; affect повышает GamblingHall knowledge score.
- Создается: `RecordWorkerGamblingAffect`, `GameBootstrap.WorkerAffects.cs:196`, set на `:242`; вызов gambling runtime `GameBootstrap.Transport.Runtime.cs:832`; thought в `:524`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:545`; Noosphere Vision chain в `GameBootstrap.NoosphereVision.Insights.cs:374`.
- Notes/TODO: `Impulsive` усиливает выигрышную реакцию.

### `affect_gambling_regret`

- Ветка: Affect Thoughts / Money.
- Запускает: gambling loss или broke.
- Тип: active thought через pending formation.
- Tone/priority: Negative; priority по intensity.
- Placeholders: `{reason}`.
- Влияние: `WorkerOpinion` по `gambling` с delta `-3`, social signal, Noosphere, Workers UI; affect снижает GamblingHall knowledge score, но при Gambling weakness делает отношение более противоречивым.
- Создается: `RecordWorkerGamblingAffect`, `GameBootstrap.WorkerAffects.cs:196`, set на `:213`; broke вызов `GameBootstrap.Transport.Runtime.cs:790`, result вызов `:832`; thought в `:524`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:546`; Noosphere Vision chain в `GameBootstrap.NoosphereVision.Insights.cs:374`.
- Notes/TODO: может дополнительно создать `FinancialPressure`.

### `affect_stable_routine`

- Ветка: Affect Thoughts / City.
- Запускает: деньги >= 45, нет critical needs, житель трудоустроен, нет FinancialPressure/FamilyAnxiety.
- Тип: active thought через pending formation.
- Tone/priority: Positive; priority по intensity.
- Placeholders: `{reason}`.
- Влияние: `WorkerOpinion` по `city` с delta `+3`, social signal, Noosphere, Workers UI.
- Создается: affect в `GameBootstrap.WorkerAffects.cs:138`, set на `:154`; thought в `:524`.
- Отображается: `GameBootstrap.FleetCanvas.WorkersScreen.Thoughts.cs:547`; Noosphere Vision chain в `GameBootstrap.NoosphereVision.Insights.cs:374`.
- Notes/TODO: отдельный state, не то же самое, что daily `stable_life`.

## Affect States

### `FinancialPressure`

- Источник: мало денег (`Money <= 15`) или нужный сервис стал слишком дорогим.
- Причина: `money` / unaffordable service.
- Мысль: `affect_financial_pressure`.
- Knowledge: снижает оценку Motel, Bar, GamblingHall; GamblingHall получает более сильный минус.
- Traits: `Anxious` и `Cautious` усиливают thought intensity, `Adaptable` смягчает negative intensity.
- Noosphere/UI: Workers personality states, Workers Thoughts, Noosphere Vision state chain.

### `FamilyAnxiety`

- Источник: семья с низким happiness, missing childcare, missing school, unpaid upkeep.
- Причина: family pressure / last happiness reason.
- Мысль: `affect_family_anxiety`.
- Knowledge: повышает важность Kindergarten, schools, PersonalHouse, CityHall.
- Traits: `Anxious` и `Dutiful` усиливают.
- Noosphere/UI: Workers personality states, Workers Thoughts, Noosphere Vision state chain.

### `ReliefAfterRest`

- Источник: Bar visit without hangover.
- Причина: Bar gave relief.
- Мысль: `affect_relief_after_rest`.
- Knowledge: дает плюс Bar.
- Traits: `Impulsive` усиливает.
- Noosphere/UI: Workers personality states, Workers Thoughts, Noosphere Vision state chain.

### `Hangover`

- Источник: Bar visit when tired/critical sleep, or Alcoholism weakness chance.
- Причина: rest helped but fatigue caught up.
- Мысль: `affect_hangover`.
- Knowledge: дает минус Bar.
- Traits: `Anxious` усиливает.
- Noosphere/UI: Workers personality states, Workers Thoughts, Noosphere Vision state chain.

### `GamblingExcitement`

- Источник: positive net gambling result.
- Причина: bet paid off.
- Мысль: `affect_gambling_excitement`.
- Knowledge: дает плюс GamblingHall.
- Traits: `Impulsive` усиливает.
- Noosphere/UI: Workers personality states, Workers Thoughts, Noosphere Vision state chain.

### `GamblingRegret`

- Источник: negative net gambling result or broke.
- Причина: loss or no money for bet.
- Мысль: `affect_gambling_regret`.
- Knowledge: дает минус GamblingHall; при Gambling weakness минус мягче и причина становится противоречивой.
- Traits: `Impulsive` и `Cautious` усиливают.
- Noosphere/UI: Workers personality states, Workers Thoughts, Noosphere Vision state chain.

### `IrritatedByLitter`

- Источник: `StreetLitterPeakToday >= medium threshold` или накопленная `StreetLitterExposureMemory`.
- Причина: litter on daily routes.
- Мысль: `affect_litter_irritation`.
- Knowledge: напрямую building knowledge не меняет, но дает city/litter thought and social signal.
- Traits: `Meticulous` усиливает.
- Noosphere/UI: Workers personality states, Workers Thoughts, Noosphere Vision state chain.

### `StableRoutine`

- Источник: стабильная работа, деньги, быт и отсутствие тяжелых состояний.
- Причина: work, money, and daily needs feel steady.
- Мысль: `affect_stable_routine`.
- Knowledge: напрямую building knowledge не меняет.
- Traits: negative mitigation by `Adaptable` не применяется, потому что tone positive.
- Noosphere/UI: Workers personality states, Workers Thoughts, Noosphere Vision state chain.

## Weakness

### `None`

- Генерация: слабость может отсутствовать; шанс слабости сейчас 33%.
- Поведение: leisure weights default: CityPark 5, GamblingHall 3, Bar 3.
- Мысли/эффекты: не добавляет специальных effects.
- UI: показывается как "нет устойчивой слабости".

### `Alcoholism`

- Генерация: одна из двух возможных слабостей, взаимоисключающая с `Gambling`.
- Выбор: leisure weights Bar 8, CityPark 3, GamblingHall 1.
- Мысли/эффекты: после Bar чаще/сильнее возможны `ReliefAfterRest` или `Hangover`; `Hangover` получает higher intensity.
- Knowledge: Bar получает stronger leisure critical bonus; Bar affects сильнее окрашивают opinion.
- Noosphere/UI: отображается в Personality/Weaknesses и в Noosphere Vision chain как weakness source.
- Код: weights `GameBootstrap.Drivers.LifeCycle.Services.cs:59`; effects `GameBootstrap.WorkerAffects.cs:176`; knowledge `GameBootstrap.WorkerKnowledgeFormation.cs:397`.

### `Gambling`

- Генерация: одна из двух возможных слабостей, взаимоисключающая с `Alcoholism`.
- Выбор: leisure weights GamblingHall 8, Bar 2, CityPark 2.
- Мысли/эффекты: gambling win/loss создает `GamblingExcitement` / `GamblingRegret`; broke может создать `FinancialPressure`.
- Knowledge: GamblingHall получает initial positive bias, но regret делает отношение противоречивым.
- Noosphere/UI: отображается в Personality/Weaknesses и в Noosphere Vision chain.
- Код: weights `GameBootstrap.Drivers.LifeCycle.Services.cs:59`; risk logic `GameBootstrap.Transport.Runtime.cs:774`; knowledge `GameBootstrap.WorkerKnowledgeFormation.cs:406`.

## Knowledge / Opinion / Social Signal / Noosphere

- `RecordWorkerThought` пишет `WorkerOpinion`, если передан `opinionDelta` и subject не `None`.
- `RecordWorkerThought` всегда вызывает `RecordSocialSignalFromWorkerThought`; эти social signals публичны для Noosphere.
- `WorkerKnowledge` не заменяется мыслями. Оно формируется через `PendingWorkerKnowledge` и `WorkerMemory`, затем может породить reflection thoughts.
- `WorkerTopicOpinion` остается отдельным слоем opinions для conversation topics.
- Affect states влияют на `PendingWorkerKnowledge` в `ApplyWorkerAffectsToBuildingKnowledge`.
- Noosphere Vision отдельно собирает strongest affect state и показывает цепочку `weakness -> cause -> state -> thought -> topic`.

## Feedback Loops

Целевой контролируемый контур:

```text
событие -> WorkerThought -> WorkerOpinion -> bias будущей WorkerThought
```

`WorkerOpinion` не является триггером мысли. Он не создаёт новые `WorkerThought` сам по себе и не должен запускать hidden gameplay-бонусы.

Текущая реализация:
- точка входа: `AddOrKeepPendingWorkerThought` в `GameBootstrap.WorkerThoughtFormation.cs`;
- helper layer: `GameBootstrap.WorkerThoughtBias.cs`;
- bias применяется только к мысли, которая уже появилась из реального события/условия;
- используется exact opinion по subject, затем мягкий fallback по веткам `money`, `city_work`, `street_litter`, `Need/Meal`, `Need/Sleep`, `Need/Leisure`, `family`, `gambling`, `local_bus`, `city`;
- минимальная устойчивость мнения: `Confidence >= 12`;
- старые мнения дают меньше веса через effective confidence decay;
- same-tick guard не даёт только что записанному opinion немедленно разгонять мысль в тот же tick/hour;
- `starter_job_resolved` исключён из bias как техническая закрывающая мысль.

На что влияет bias:
- `Intensity`: усиливает совпадающие по тону мысли и мягко снижает противоречащие прошлому опыту;
- `FormationHours`: совпадающие мысли формируются быстрее, противоречащие чуть медленнее;
- `Priority`: сильный совпадающий bias может поднять priority на один шаг;
- `Tone`: только neutral-мысли могут окраситься в positive/negative при сильном и уверенном opinion;
- `Wording`: при сильном bias добавляется короткий optional suffix `{opinionBias}`.

Ограничения:
- opinion не создаёт thought;
- cap усиления intensity: `+15`;
- minimum formation time: `0.12h`;
- active thought refresh не пишет новый opinion и не должен самораскачиваться;
- debug log: `THOUGHT_BIAS`.

## UI Поверхности

- Workers / Thoughts: current important thought, pending thought progress, recent thoughts, life opinions.
- Workers / Personality: `Характер`, `Слабости`, `Состояния`.
- F9 / States: справочник character/weakness/affect, needs/statuses и служебные пометки thought-system.
- Noosphere HUD: social signals, knowledge events, city experience.
- Noosphere Vision: массовые состояния и цепочка cause/state/thought/topic.

## Known Gaps

- `need_*_warning` есть в шаблонах и Workers UI, но активная логика сейчас рождает только `Critical`; warning сейчас только резолвится и помечен как future/reference layer.
- `no_job_today` выглядит как legacy/alias рядом с `no_job_warning`; активная логика использует `no_job_warning`.
- `starter_job_resolved` является технической закрывающей мыслью и скрывается из Workers UI.
- `street_litter_low/medium/high` активны и теперь имеют отдельное читаемое отображение в Workers UI.
- `need_*_critical_known_place` и `no_job_warning_known_place` являются `TemplateKey`-вариантами, а не отдельными `Thought.Key`.
- `CityPark` вызывает `RecordWorkerLeisureAffect`, но сейчас не создает отдельный nature/relief affect.
- Transport UI cases для bus thoughts пока не рендерят конкретный `{reason}` в описании.

## Правила Добавления Новых Мыслей

1. Выбрать ветку дерева до написания кода.
2. Решить тип: instant, active или pending reflection.
3. Задать `Kind`, `Tone`, `Priority`, `Intensity` и cooldown/resolve условия.
4. Описать placeholders и fallback labels.
5. Указать, нужен ли `opinionDelta`; если да, выбрать subject type/key.
6. Проверить, не должен ли trigger сначала создавать `Affect State`, а уже affect создавать мысль.
7. Если мысль опирается на место/тему, проверить связь с `WorkerKnowledge` и `WorkerTopicOpinion`.
8. Проверить, какой `WorkerOpinion` может окрашивать эту мысль через feedback loop, и не создаёт ли это самораскачку.
9. Добавить dedicated Workers UI case, если fallback будет плохо читаться.
10. Проверить, нужен ли F9/States reference или Noosphere Vision chain.
11. Обновить этот документ: ветка, карточка ключа, source/display paths, known gaps при необходимости.
