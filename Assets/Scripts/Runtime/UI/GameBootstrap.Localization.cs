using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private enum GameLanguage
    {
        English,
        Russian
    }

    private static GameLanguage selectedLanguage = GameLanguage.Russian;
    private float localizationRefreshTimer;

    private static readonly Dictionary<string, string> RussianUi = new()
    {
        { "Continue", "Продолжить" },
        { "New Game Debug", "Новая игра: Debug" },
        { "New Game User", "Новая игра" },
        { "Exit", "Выход" },
        { "Language:", "Язык:" },
        { "Loading...", "Загрузка..." },
        { "Camera & lighting...", "Камера и свет..." },
        { "Populating water...", "Заполняем воду..." },
        { "Setting up locations...", "Размещаем локации..." },
        { "Building road network...", "Строим дорожную сеть..." },
        { "Generating terrain...", "Генерируем землю..." },
        { "Smoothing terrain...", "Сглаживаем землю..." },
        { "Applying terrain...", "Применяем рельеф..." },
        { "Setting up ground...", "Настраиваем почву..." },
        { "Building grid...", "Строим сетку..." },
        { "Edge highways...", "Магистраль..." },
        { "Atmosphere...", "Атмосфера..." },
        { "Road lanterns...", "Фонари..." },
        { "Planting trees...", "Сажаем деревья..." },
        { "Placing benches...", "Ставим лавочки..." },
        { "Wildlife...", "Живность..." },
        { "Water effects...", "Эффекты воды..." },
        { "Visual tools...", "Визуальные инструменты..." },
        { "Vehicles...", "Транспорт..." },
        { "Audio...", "Звук..." },
        { "Fleet UI...", "Интерфейс автопарка..." },
        { "Economy UI...", "Интерфейс экономики..." },
        { "Build UI...", "Интерфейс строительства..." },
        { "HUD...", "HUD..." },
        { "Finishing up...", "Завершаем..." },
        { "Done!", "Готово!" },

        { "Welcome to Lo-Fi Delivery Co.", "Добро пожаловать в Lo-Fi Delivery Co." },
        { "Alright, buddy, you've been handed a problem the size of a small town. For some reason, it even comes with a steering wheel.\n\nThe map is empty - right where the road is supposed to be. You'll have to... convince it to appear.\nAnd buildings, unfortunately, won't build themselves.\n\nStart gently: take a look around, fill in what's missing, and try not to go bankrupt before noon.", "Итак, дружище, тебе досталась проблема размером с городок. К ней зачем-то прикручен руль.\n\nНа карте — пусто. Ровно там, где должна быть дорога. Придётся её… убедить появиться.\nДа и здания, к несчастью, сами себя не построят.\n\nНачни мягко: осмотрись, поставь недостающее и постарайся не обанкротиться до обеда." },
        { "You are starting a small logistics town. In this mode the world begins lean: some important buildings must be placed manually from the Build menu.\n\nStart by looking around, planning roads, and deciding where your first service and production buildings should go.", "Ты начинаешь маленький логистический городок. В этом режиме мир стартует проще: часть важных зданий нужно построить вручную через меню строительства.\n\nОсмотрись, спланируй дороги и реши, где появятся первые сервисные и производственные здания." },
        { "Skip tutorial", "Пропустить обучение" },
        { "OK", "OK" },
        { "Build a Motel", "Построй Mотель" },
        { "A motel is a soft landing for people who do not yet understand what they have agreed to.\n\nIn Lo-Fi Delivery Co. it is where drivers are hired, wait between shifts, and pretend the road has not already taken something from them.\n\nOpen the Building menu at the top - or press B - choose Motel, and place it wherever your optimism still fits.", "Мотель — это мягкая посадка для людей, которые ещё не поняли, на что подписались.\n\nВ Lo-Fi Delivery Co. здесь нанимают водителей, они ждут между сменами и делают вид, что дорога ещё ничего у них не забрала.\n\nОткрой меню Building сверху — или нажми B — выбери Motel и поставь его там, где ещё помещается твой оптимизм." },
        { "Open the Workers Panel", "Открой панель Рабочих" },
        { "The motel stands. Structurally sound, morally ambiguous - the usual.\n\nNow it needs a person inside it. Someone to drive the truck, carry the crates, and ask no questions about where the money went.\n\nOpen the Workers panel at the top of the screen.", "Мотель стоит. С точки зрения конструкции — надёжно. Морально — вопросы есть.\n\nТеперь туда нужен человек. Кто-то, кто будет вести грузовик, таскать ящики и не задавать лишних вопросов о том, куда делись деньги.\n\nОткрой панель Workers в верхней части экрана." },
        { "Hire a Worker", "Найми рабочего" },
        { "The Workers panel is open. Good. You are further along than most people get.\n\nThere is a button at the bottom. Hire New Worker. It costs money — some of which you still have.\n\nPress it.", "Панель Workers открыта. Хорошо. Ты уже дальше, чем большинство.\n\nВнизу есть кнопка. Hire New Worker. Она стоит денег — часть которых у тебя ещё есть.\n\nНажми её." },
        { "The Worker is on Their Way!", "Рабочий уже едет!" },
        { "A human being will arrive by bus, already wearing the expression of someone who has made several poor decisions to get here.\n\nThat is your employee now. Treat them accordingly.", "Приедет автобус. Из него выйдет человек с видом того, кто принял несколько плохих решений, чтобы оказаться здесь.\n\nТеперь это твой сотрудник. Обращайся соответственно." },
        { "Timber, Unfortunately", "Древесина, к сожалению" },
        { "See that Forest? It is not scenery. It is inventory with leaves.\n\nA Forest produces logs. Later, a Sawmill turns those logs into boards. That is the first honest-looking lie your economy will tell.\n\nNext, assign your worker to the Forest in the Productions tab.", "Видишь этот Лес? Это не пейзаж. Это склад с листьями.\n\nЛес производит брёвна. Потом Лесопилка превратит их в доски. Так экономика впервые сделает вид, что всё честно.\n\nТеперь назначь рабочего в Лес во вкладке Производство." },
        { "Choose the Person", "Выбери человека" },
        { "Before a building can pretend to be productive, it needs a person to blame.\n\nSelect your first worker in the list on the left. Or press OK and I will perform this tiny ceremony for you.", "Прежде чем здание сможет изображать продуктивность, ему нужен человек, на которого всё можно списать.\n\nВыдели первого рабочего в списке слева. Или нажми OK, и я проведу этот крошечный ритуал за тебя." },
        { "Send Them to the Trees", "Отправь его к деревьям" },
        { "There. A worker, selected and vulnerable to management.\n\nPress Assign on the Forest row. Or press OK, and the bureaucracy will complete itself.", "Вот он. Рабочий, выделенный и беззащитный перед управлением.\n\nНажми Assign в строке Леса. Или нажми OK, и бюрократия завершит себя сама." },
        { "Look at Them Go", "Посмотри, трудится" },
        { "Observe our small hero at work. A person, a forest, and the ancient agreement between wage labor and questionable planning.\n\nIt is almost adorable. Do not say that out loud. Management has a reputation to maintain.", "Посмотри на нашего маленького героя за работой. Человек, лес и древний договор между наёмным трудом и сомнительным планированием.\n\nПочти умилительно. Только не говори это вслух. У управления есть репутация." },
        { "Trucks Don't Drive Themselves", "Грузовики сами не ездят" },
        { "The Forest worker is settled in. But logs in a forest are just expensive scenery.\n\nOpen Fleet, assign a driver to a truck, and set a route. That is how cargo starts moving.", "Рабочий обустроился в Лесу. Но брёвна в лесу — это просто дорогой пейзаж.\n\nОткрой Автопарк, назначь водителя в грузовик и задай маршрут. Вот так грузы начинают двигаться." },
        { "Logs Are Not Boards", "Брёвна — не доски" },
        { "A log is only furniture in its larval form.\n\nTo turn Logs into Boards, you need a Sawmill. Open Building, choose Sawmill, and place it where the roads can eventually make peace with it.", "Бревно — это мебель в личиночной стадии.\n\nЧтобы превратить Logs в Boards, нужна Лесопилка. Открой Стройку, выбери Sawmill и поставь её там, где дороги однажды смогут с ней договориться." },
        { "Sawmill Placed", "Лесопилка поставлена" },
        { "The Sawmill stands. A box with teeth, waiting for logs to become boards and for everyone involved to call that progress.\n\nOne delicate problem remains: resources do not teleport. Tragically. Trucks will have to carry them between buildings, because civilization is mostly moving piles from one rectangle to another.", "Лесопилка стоит. Коробка с зубами, ожидающая, когда брёвна станут досками, а все участники назовут это прогрессом.\n\nОстаётся одна деликатная проблема: ресурсы не телепортируются. Трагедия. Грузовикам придётся возить их между зданиями, потому что цивилизация — это в основном перекладывание куч из одного прямоугольника в другой." },
        { "Two new workers have appeared.\n\nTime to put someone in a truck — open Fleet and assign a driver to a route.", "Появились два новых рабочих.\n\nПора посадить кого-нибудь в грузовик — открой Автопарк и назначь водителя на маршрут." },
        { "Select the Truck", "Выбери грузовик" },
        { "The Fleet panel is open. Somewhere inside it sits Truck 1, a loyal rectangle with wheels and no opinions worth recording.\n\nSelect Truck 1 in the list. Or press OK, and I will gently point your attention at the machine that will soon inherit all our logistical sins.", "Автопарк открыт. Где-то внутри сидит Truck 1 — верный прямоугольник на колёсах, чьё мнение лучше не заносить в протокол.\n\nВыбери Truck 1 в списке. Или нажми OK, и я мягко укажу на машину, которая скоро унаследует все наши логистические грехи." },
        { "Give It a Driver", "Дай ему водителя" },
        { "A truck without a driver is just furniture with fuel anxiety.\n\nNow assign a free worker to Truck 1. Press Assign in the first driver slot, or press OK and I will open the little personnel drawer for you.", "Грузовик без водителя — это мебель с топливной тревожностью.\n\nТеперь назначь свободного рабочего на Truck 1. Нажми Назначить в первом слоте водителя или нажми OK, и я открою этот маленький кадровый ящик за тебя." },
        { "Choose a Driver", "Выбери водителя" },
        { "There they are: the available souls. Not the one currently serving the trees. We do not rip people out of production just because a dropdown got lonely.\n\nSelect a free worker from the list. Or press OK, and the first available volunteer will become Truck 1's problem.", "Вот они: доступные души. Не тот, кто сейчас служит деревьям. Мы не выдёргиваем людей из производства только потому, что выпадающему списку стало одиноко.\n\nВыбери свободного рабочего из списка. Или нажми OK, и первый доступный доброволец станет проблемой Truck 1." },

        { "Fleet", "Автопарк" },
        { "Workers", "Рабочие" },
        { "Drivers", "Водители" },
        { "Shifts", "Смены" },
        { "Resources", "Ресурсы" },
        { "Economy", "Экономика" },
        { "Trade", "Торговля" },
        { "Build", "Стройка" },
        { "Building", "Стройка" },
        { "R - rotate", "R - повернуть" },
        { "Map", "Карта" },
        { "Speed", "Скорость" },
        { "Time", "Время" },
        { "Treasury", "Казна" },
        { "Morning", "Утро" },
        { "Day", "День" },
        { "Evening", "Вечер" },
        { "Night", "Ночь" },
        { "Paused", "Пауза" },
        { "PAUSE", "ПАУЗА" },
        { "Shift Management", "Управление сменами" },
        { "Idle", "Свободен" },
        { "Assigned", "Назначен" },
        { "No drivers assigned", "Водители не назначены" },
        { "Assign", "Назначить" },
        { "Remove", "Убрать" },
        { "Intercity", "Межгород" },
        { "No driver assigned", "Водитель не назначен" },
        { "Assign one dedicated driver to inter city duty", "Назначьте одного водителя на междугородние рейсы" },
        { "Logistics", "Логистика" },
        { "Productions", "Производство" },
        { "Hours:", "Часы:" },
        { "No worker assigned", "Рабочий не назначен" },
        { "Select a worker", "Выберите рабочего" },
        { "Select a worker to assign", "Выберите рабочего для назначения" },
        { "Slot occupied", "Слот занят" },
        { "Worker not available", "Рабочий недоступен" },

        { "Truck Details", "Грузовик" },
        { "Truck Overview", "Обзор грузовика" },
        { "Current State", "Текущее состояние" },
        { "Assigned Driver", "Назначенный водитель" },
        { "Select Driver", "Выбрать водителя" },
        { "No available drivers.", "Нет доступных водителей." },
        { "No truck selected. Select a truck from the fleet list.", "Грузовик не выбран. Выбери грузовик из списка." },
        { "Buy New Truck", "Купить грузовик" },
        { "Fuel", "Топливо" },
        { "Energy", "Энергия" },
        { "Cargo", "Груз" },
        { "Navigation", "Навигация" },
        { "Route", "Маршрут" },
        { "Payout", "Оплата" },
        { "Follow Camera", "Следить камерой" },
        { "Exit Follow", "Выйти из камеры" },

        { "Hire Driver", "Нанять водителя" },
        { "Hire Worker", "Нанять рабочего" },
        { "No workers yet.", "Рабочих пока нет." },
        { "No drivers yet.", "Водителей пока нет." },
        { "Salary", "Зарплата" },
        { "Money", "Деньги" },
        { "Status", "Статус" },
        { "Duty", "Роль" },
        { "Local", "Локальный" },

        { "Build Menu", "Строительство" },
        { "Road", "Дорога" },
        { "Sawmill", "Лесопилка" },
        { "Motel", "Мотель" },
        { "Bar", "Бар" },
        { "Canteen", "Столовая" },
        { "Furniture Factory", "Мебельная фабрика" },
        { "Open Resources", "Открыть ресурсы" },

        { "Warehouse", "Склад" },
        { "Parking", "Парковка" },
        { "Gas Station", "Заправка" },
        { "Fuel Stop", "Заправка" },
        { "Forest", "Лес" },
        { "Bus Stop", "Автобусная остановка" },
        { "Service Fee", "Стоимость услуги" },
        { "Workers inside", "Рабочих внутри" },
        { "Finished goods storage", "Склад готовой продукции" },
        { "Production paused at night", "Производство остановлено ночью" },

        { "Logs", "Брёвна" },
        { "Boards", "Доски" },
        { "Cotton", "Хлопок" },
        { "Textile", "Ткань" },
        { "Furniture", "Мебель" },

        { "Economy & Trade", "Экономика и торговля" },
        { "Create Order", "Создать заказ" },
        { "Resource:", "Ресурс:" },
        { "Action:", "Действие:" },
        { "Buy", "Купить" },
        { "Sell", "Продать" },
        { "PLACE ORDER", "РАЗМЕСТИТЬ ЗАКАЗ" },
        { "Active Orders", "Активные заказы" },
        { "No active trade orders.", "Активных торговых заказов нет." },
        { "Trade Dispatch", "Торговый рейс" },
        { "Resource", "Ресурс" },
        { "Offer", "Предложение" },
        { "Mode", "Режим" },
        { "Buy / Imports", "Покупка / импорт" },
        { "Sell / Exports", "Продажа / экспорт" },
        { "Send on Trade Run", "Отправить в торговый рейс" },
        { "No money movements yet.", "Движений денег пока нет." },
        { "Assign an Intercity driver to unlock trade dispatch.", "Назначьте межгородского водителя, чтобы открыть торговлю." },
        { "Ready to dispatch via edge highway", "Готово к рейсу через магистраль" },
        { "Intercity driver is busy", "Межгородский водитель занят" },

        { "Regional Map", "Карта регионов" },
        { "Open/Close: M", "Открыть/закрыть: M" },
        { "Region Grid", "Сетка регионов" },
        { "Region Details", "Детали региона" },
        { "Current region", "Текущий регион" },
        { "External region", "Внешний регион" },
        { "Unsurveyed region", "Неисследованный регион" },
        { "Produced Resources", "Производимые ресурсы" },
        { "Regional Notes", "Заметки региона" },
        { "Current Trade Context", "Текущий торговый контекст" },
        { "Your Town", "Твой город" },
        { "Textile District", "Текстильный район" },
        { "Cotton Plains", "Хлопковые равнины" },
        { "River Port", "Речной порт" },
        { "North Ridge", "Северный кряж" },
        { "Forest Belt", "Лесной пояс" },
        { "Dry South", "Сухой юг" },
        { "Freight Steppe", "Грузовая степь" },
        { "Coastal Gate", "Прибрежные ворота" },
        { "UNKNOWN", "НЕИЗВЕСТНО" },
        { "LOCAL", "ЛОКАЛЬНО" },
        { "ROUTE", "МАРШРУТ" },
        { "TEXTILE", "ТКАНЬ" },
        { "COTTON", "ХЛОПОК" },

        { "Cell", "Клетка" },
        { "Ground", "Земля" },
        { "Water", "Вода" },
        { "Grass", "Трава" },
        { "Sand", "Песок" },
        { "None", "Нет" },
        { "Unknown", "Неизвестно" },
        { "Location", "Локация" },
        { "Forest -> Sawmill", "Лес -> Лесопилка" },
        { "Sawmill -> Warehouse", "Лесопилка -> Склад" },
        { "Warehouse -> Furniture Factory (Boards)", "Склад -> Мебельная фабрика (Доски)" },
        { "Warehouse -> Furniture Factory (Textile)", "Склад -> Мебельная фабрика (Ткань)" },
        { "Furniture Factory -> Warehouse", "Мебельная фабрика -> Склад" },
        { "Selected building", "Выбранное здание" },
        { "No workers assigned", "Рабочие не назначены" },
        { "No trucks inside", "Грузовиков внутри нет" },
        { "The fleet is currently out on routes.", "Автопарк сейчас на маршрутах." },
        { "Available Routes", "Доступные маршруты" },
        { "No routes available right now.", "Сейчас маршрутов нет." },
        { "Routes appear when cargo and roads exist.", "Маршруты появятся, когда есть груз и дороги." },
        { "Assign route", "Назначить маршрут" },
        { "No trips available.", "Рейсов нет." },
    };

    private static bool IsRussianLanguage() => selectedLanguage == GameLanguage.Russian;

    private static string L(string value)
    {
        if (!IsRussianLanguage() || string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (RussianUi.TryGetValue(value, out string translated))
        {
            return translated;
        }

        return LocalizeCommonFragments(value);
    }

    private static string LocalizeCommonFragments(string value)
    {
        string translated = value;
        foreach (KeyValuePair<string, string> pair in RussianUi)
        {
            if (pair.Key.Length < 4)
            {
                continue;
            }

            translated = translated.Replace(pair.Key, pair.Value);
        }

        translated = translated
            .Replace("Import x1", "Импорт x1")
            .Replace("Export x1", "Экспорт x1")
            .Replace("Treasury", "Казна")
            .Replace("Worker", "Рабочий")
            .Replace("Driver", "Водитель")
            .Replace("Truck", "Грузовик")
            .Replace("Open", "Открыть")
            .Replace("Close", "Закрыть")
            .Replace("Ready", "Готово")
            .Replace("Busy", "Занят")
            .Replace("No ", "Нет ")
            .Replace("Buy", "Купить")
            .Replace("Sell", "Продать");

        return translated;
    }

    private static string ToEnglishIfKnown(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        foreach (KeyValuePair<string, string> pair in RussianUi)
        {
            if (pair.Value == value)
            {
                return pair.Key;
            }
        }

        return value;
    }

    private void SetLanguage(GameLanguage language)
    {
        if (selectedLanguage == language)
        {
            return;
        }

        selectedLanguage = language;
        SessionDebugLogger.Log("UI", $"Language switched to {selectedLanguage}.");
        RefreshLocalizedUi();
        PlayUiSound(uiSelectClip, 0.82f);
    }

    private void RefreshLocalizedUi()
    {
        UpdateMainMenuTexts();
        RefreshAllTextComponentsForLanguage();
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
        isEconomyScreenDirty = true;
        isBuildScreenDirty = true;
        isWorldMapScreenDirty = true;
    }

    private void RefreshAllTextComponentsForLanguage()
    {
        Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
        foreach (Text text in texts)
        {
            string english = ToEnglishIfKnown(text.text);
            text.text = L(english);
        }
    }

    private void LocalizeCanvas(GameObject canvasRoot)
    {
        if (!IsRussianLanguage() || canvasRoot == null) return;
        foreach (Text text in canvasRoot.GetComponentsInChildren<Text>(true))
        {
            if (text != null)
                text.text = L(text.text);
        }
    }

    private void UpdateRuntimeLocalizationTick()
    {
        // Localization is now applied inline via LocalizeCanvas() at the end of each
        // UI update function, so the periodic full-scene scan is no longer needed.
    }
}
