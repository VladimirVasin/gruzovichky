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
        { "Tutorial", "Обучение" },
        { "New Game", "Новая игра" },
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
        { "Welcome to Tutorial mode.\n\nYou start with an almost empty map, a highway connection, a bus stop, and a few workers.\n\nBefore building, learn the camera controls: zoom in, zoom out, move the map, and rotate the view.", "Добро пожаловать в режим обучения.\n\nТы начинаешь почти с пустой картой, подключением к магистрали, автобусной остановкой и несколькими рабочими.\n\nПеред строительством освой управление камерой: приблизь, отдали, сдвинь карту и поверни обзор." },
        { "Build the first road", "Построй первую дорогу" },
        { "Now build your first road.\n\nOpen the Build menu at the bottom of the screen or press B. Choose a road tool, then left-click the start cell.\n\nMove the cursor to the end cell and left-click again to build the road.\n\nYour first road must connect to the Highway. Otherwise the town is cut off from outside traffic.", "Теперь построй первую дорогу.\n\nОткрой меню Стройка снизу экрана или нажми B. Выбери инструмент дороги и нажми левой кнопкой по начальной клетке.\n\nПереведи курсор на конечную клетку и нажми левой кнопкой ещё раз, чтобы построить дорогу.\n\nПервая дорога обязательно должна соединяться с Магистралью. Иначе город будет отрезан от внешнего движения." },
        { "Build the town core", "Построй основу города" },
        { "The road is only useful when it connects important places.\n\nThree core buildings are now unlocked: Warehouse, Motel, and Parking.\n\nBuild all three from the Build menu. You can open Build at the bottom of the screen or press B.\n\nEvery building needs road access. If a building is not connected by road, workers and vehicles will not be able to use it properly.", "Дорога полезна только тогда, когда соединяет важные места.\n\nТеперь открыты три основных здания: Склад, Мотель и Парковка.\n\nПострой все три через меню Стройка. Его можно открыть снизу экрана или клавишей B.\n\nК каждому зданию обязательно должна быть подведена дорога. Если здание не подключено к дороге, рабочие и транспорт не смогут нормально им пользоваться." },
        { "Warehouse is your central storage.\n\nFinished resources are collected here, and future routes will use it as the main place for loading and unloading goods.", "Склад - это центральное хранилище.\n\nЗдесь собираются готовые ресурсы, а будущие маршруты будут использовать его как основное место погрузки и разгрузки." },
        { "Motel gives workers a place to check in, rest, and return to when they have no active task.\n\nAfter this building exists, workers and cats can move from the starting stop into their normal idle area.", "Мотель даёт рабочим место для регистрации, отдыха и возвращения, когда у них нет активной задачи.\n\nПосле постройки рабочие и коты смогут уйти от стартовой остановки в свою обычную idle-зону." },
        { "Parking is the base for vehicles.\n\nTrucks and local buses start from here, return here, and use it as the town transport yard.", "Парковка - это база транспорта.\n\nГрузовики и городские автобусы стартуют отсюда, возвращаются сюда и используют её как транспортный двор города." },
        { "Build a Lumberjack Camp", "Построй Лагерь лесорубов" },
        { "The town needs its first production building.\n\nLumberjack Camp gathers Logs from nearby trees. Build it close to a dense patch of forest so workers do not spend the whole day walking.", "Городу нужно первое производственное здание.\n\nЛагерь лесорубов добывает Брёвна из ближайших деревьев. Построй его рядом с густым лесом, чтобы рабочие не тратили весь день на дорогу." },
        { "Assign a Lumberjack", "Назначь лесоруба" },
        { "The Lumberjack Camp is built, but buildings do not work by themselves.\n\nOpen Staffing, select the Lumberjack Camp vacancy, choose a worker, and assign them.", "Лагерь лесорубов построен, но здания сами не работают.\n\nОткрой Кадры, выбери вакансию Лагеря лесорубов, затем выбери рабочего и назначь его." },
        { "Lumberjack Work", "Работа лесоруба" },
        { "The assigned worker will go to the camp during production hours, walk to nearby trees, chop them into Logs, and carry those Logs back one by one.\n\nLater you will move those Logs through the logistics chain.", "Назначенный рабочий будет приходить в лагерь в рабочие часы, идти к ближайшим деревьям, рубить их на Брёвна и носить Брёвна обратно по одному.\n\nПозже эти Брёвна нужно будет отправлять дальше по логистической цепочке." },
        { "Skip tutorial", "Пропустить обучение" },
        { "OK", "OK" },
        { "Bees", "Пчёлки" },
        { "Дурачок, не мешай пчёлкам", "Дурачок, не мешай пчёлкам" },
        { "This is Tutorial mode. The town starts with missing buildings, roads, and workers.\n\nYour goal is simple: build the missing pieces, connect them with roads, attract workers, assign jobs, and move resources with trucks.\n\nStart by building a Motel.", "Это режим обучения. Город стартует без части зданий, дорог и рабочих.\n\nТвоя цель простая: построить недостающее, соединить здания дорогами, привлечь рабочих, назначить им работу и перевозить ресурсы грузовиками.\n\nНачни со строительства Мотеля." },
        { "The Motel lets new arrivals check in and gives workers a place to rest.\n\nOpen Building at the bottom, or press B. Choose Motel and place it near your road plan.\n\nIn Build mode, press R to rotate the building before placing it.", "Мотель принимает новых рабочих и даёт им место для отдыха.\n\nОткрой Стройку снизу или нажми B. Выбери Motel и поставь его рядом с будущими дорогами.\n\nВ режиме строительства нажимай R, чтобы повернуть здание перед установкой." },
        { "The Motel is ready, so workers have somewhere to check in.\n\nOpen the Workers panel at the top of the screen. This is where arrivals and current workers are tracked.", "Мотель готов: новым рабочим есть где заселиться.\n\nОткрой панель Рабочие в верхней части экрана. Здесь отслеживаются приезды и текущие рабочие." },
        { "Workers now arrive automatically by bus. Open Staffing and keep the city livable to attract them.", "Рабочие теперь сами приезжают на автобусе. Открывай Кадры и поддерживай город удобным, чтобы привлечь их." },
        { "Your new worker is arriving by bus.\n\nWait for the bus to stop and for the worker to walk to the Motel. After that, the worker can be assigned to jobs.", "Новый рабочий едет на автобусе.\n\nДождись, пока автобус остановится, а рабочий дойдёт до Мотеля. После этого его можно назначать на работу." },
        { "Forest Production", "Производство в Лесу" },
        { "Forest produces Logs.\n\nTo start production, assign a worker to Forest in Shifts > Productions. Production workers operate from 08:00 to 18:00.", "Лес производит Брёвна.\n\nЧтобы запустить производство, назначь рабочего в Лес: Смены > Производство. Производственные рабочие работают с 08:00 до 18:00." },
        { "Select a Worker", "Выбери рабочего" },
        { "Select a free worker from the list on the left.\n\nYou can also press OK and the first available worker will be selected automatically.", "Выбери свободного рабочего в списке слева.\n\nМожно нажать OK, и первый доступный рабочий будет выбран автоматически." },
        { "Assign to Forest", "Назначь в Лес" },
        { "Press Assign on the Forest row to send the selected worker there.\n\nYou can also press OK and the tutorial will assign the worker for you.", "Нажми Назначить в строке Леса, чтобы отправить туда выбранного рабочего.\n\nМожно нажать OK, и обучение назначит рабочего автоматически." },
        { "Forest Is Working", "Лес работает" },
        { "The worker is now producing Logs at Forest.\n\nLogs are raw material. They must be moved and processed before they become useful for the town.", "Рабочий теперь производит Брёвна в Лесу.\n\nБрёвна - это сырьё. Их нужно перевезти и обработать, прежде чем они станут полезны городу." },
        { "Use Trucks", "Используй грузовики" },
        { "Resources do not move automatically.\n\nOpen Fleet, assign a driver to a truck, then choose a route to move cargo between buildings.", "Ресурсы не перемещаются сами.\n\nОткрой Автопарк, назначь водителя в грузовик, затем выбери маршрут для перевозки груза между зданиями." },
        { "Build a Sawmill", "Построй Лесопилку" },
        { "Logs must be processed into Boards.\n\nOpen Building, choose Sawmill, and place it with its entrance connected to a road.", "Брёвна нужно переработать в Доски.\n\nОткрой Стройку, выбери Лесопилку и поставь её так, чтобы вход был соединён с дорогой." },
        { "Sawmill converts Logs into Boards.\n\nResources still need transport: use trucks to deliver Logs from Forest to Sawmill and move finished Boards onward.", "Лесопилка превращает Брёвна в Доски.\n\nРесурсы всё равно нужно перевозить: доставь Брёвна из Леса на Лесопилку грузовиком, а готовые Доски вези дальше." },
        { "Two new workers have arrived at the Bus Stop and will walk to the Motel.\n\nNext, open Fleet and assign a free worker to Truck #1.", "Два новых рабочих приехали на автобусную остановку и идут к Мотелю.\n\nДальше открой Автопарк и назначь свободного рабочего в Грузовик #1." },
        { "Select Truck #1 in the Fleet list.\n\nYou can also press OK and the tutorial will select the truck automatically.", "Выбери Грузовик #1 в списке Автопарка.\n\nМожно нажать OK, и обучение выберет грузовик автоматически." },
        { "Assign a Driver", "Назначь водителя" },
        { "Truck #1 needs a driver before it can run routes.\n\nPress Assign in Driver Slot 1. Only free workers can be assigned to trucks.", "Грузовику #1 нужен водитель, иначе он не сможет выполнять маршруты.\n\nНажми Назначить в слоте Водитель 1. В грузовики можно назначать только свободных рабочих." },
        { "Choose any free worker from the driver list.\n\nWorkers already assigned to production are not shown here. The tutorial will continue after you assign a driver.", "Выбери любого свободного рабочего из списка водителей.\n\nРабочие, уже назначенные на производство, здесь не показываются. Обучение продолжится после назначения водителя." },
        { "Staff the Sawmill", "Назначь рабочего на Лесопилку" },
        { "You have assigned a worker to Forest. Now assign a worker to Sawmill.\n\nOpen Shifts, go to Productions, and assign a free worker to the Sawmill row.", "Ты уже назначил рабочего в Лес. Теперь назначь рабочего на Лесопилку.\n\nОткрой Смены, перейди в Производство и назначь свободного рабочего в строку Лесопилки." },
        { "Sawmill Ready", "Лесопилка готова" },
        { "The Sawmill now has a worker.\n\nNext, use Fleet routes to deliver Logs from Forest to Sawmill, then move Boards onward to Warehouse.", "У Лесопилки теперь есть рабочий.\n\nДальше используй маршруты Автопарка: доставь Брёвна из Леса на Лесопилку, затем перевези Доски на Склад." },
        { "Build a Motel", "Построй Мотель" },
        { "Open the Workers Panel", "Открой панель Рабочих" },
        { "Hire a Worker", "Найми рабочего" },
        { "The Worker is on Their Way!", "Рабочий уже едет!" },
        { "Sawmill Placed", "Лесопилка поставлена" },
        { "Select the Truck", "Выбери грузовик" },
        { "Choose a Driver", "Выбери водителя" },

        { "Fleet", "Автопарк" },
        { "Worker", "Рабочий" },
        { "Workers", "Рабочие" },
        { "Resident", "Житель" },
        { "Residents", "Жители" },
        { "Social", "Связи" },
        { "Staffing", "Кадры" },
        { "Drivers", "Водители" },
        { "Assignments", "Вакансии" },
        { "Roles", "Вакансии" },
        { "Vacancies", "Вакансии" },
        { "Shifts", "Смены" },
        { "Resources", "Ресурсы" },
        { "Economy", "Экономика" },
        { "Trade", "Торговля" },
        { "Stats", "Справка" },
        { "Taxes", "Налоги" },
        { "Tax rate", "Налоговая ставка" },
        { "Tax policies", "Налоговые правила" },
        { "Current taxable bank", "Налогооблагаемые кассы" },
        { "Last collected", "Собрано в последний раз" },
        { "Taxed buildings", "Обложено зданий" },
        { "Taxes today", "Налоги сегодня" },
        { "Previous day", "Прошлый день" },
        { "Last daily reserve tax", "Последний дневной налог с касс" },
        { "Enabled policies", "Активные правила" },
        { "Primary rate controls service sales tax", "Главная ставка управляет налогом с услуг" },
        { "Service Sales Tax", "Налог с продаж услуг" },
        { "Daily Cash Reserve Tax", "Дневной налог с касс" },
        { "Salary Withholding", "Удержание с зарплаты" },
        { "Transport Fare Tax", "Налог с проезда" },
        { "Property Transfer Tax", "Налог на покупку дома" },
        { "Vehicle Registration Tax", "Налог на покупку машины" },
        { "Gambling Revenue Tax", "Налог с дохода игровых залов" },
        { "Import Tariff", "Импортная пошлина" },
        { "Export Duty", "Экспортная пошлина" },
        { "Construction Permit Fee", "Строительный сбор" },
        { "Building cash reserve", "Касса здания" },
        { "Service sales", "Продажи услуг" },
        { "Salary income", "Зарплата" },
        { "Transport fares", "Проезд" },
        { "Property purchases", "Покупка жилья" },
        { "Vehicle purchases", "Покупка машин" },
        { "Gambling revenue", "Доход игровых залов" },
        { "Trade imports", "Импорт товаров" },
        { "Trade exports", "Экспорт товаров" },
        { "Construction permits", "Строительные разрешения" },
        { "Daily", "Ежедневно" },
        { "Per transaction", "За транзакцию" },
        { "receiver pays", "платит получатель" },
        { "payer pays", "платит плательщик" },
        { "ON", "ВКЛ" },
        { "OFF", "ВЫКЛ" },
        { "Next collection", "Следующий сбор" },
        { "Building Bank", "Касса здания" },
        { "Build", "Стройка" },
        { "Building", "Стройка" },
        { "Lumberjack Camp", "Лагерь лесорубов" },
        { "R - rotate", "R - повернуть" },
        { "Map", "Карта" },
        { "Speed", "Скорость" },
        { "Time", "Время" },
        { "Treasury", "Казна" },
        { "Population", "Население" },
        { "Morning", "Утро" },
        { "Day", "День" },
        { "Evening", "Вечер" },
        { "Night", "Ночь" },
        { "Paused", "Пауза" },
        { "PAUSE", "ПАУЗА" },
        { "Shift Management", "Управление сменами" },
        { "Idle", "Свободен" },
        { "Assigned", "Назначен" },
        { "Production", "Производство" },
        { "Buildings", "Здания" },
        { "Services", "Сервисы" },
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
        { "Select a resident from the list to view details.", "Выбери жителя из списка, чтобы увидеть детали." },
        { "No truck selected. Select a truck from the fleet list.", "Грузовик не выбран. Выбери грузовик из списка." },
        { "Buy New Truck", "Слоты Parking" },
        { "Fleet capacity reached.", "Лимит автопарка достигнут." },
        { "Need", "Нужно" },
        { "to hire a new truck.", "для слота Parking." },
        { "to hire a new worker.", "для приезда нового рабочего." },
        { "Adds a new truck directly to parking.", "Добавляет новый грузовик прямо на парковку." },
        { "Fuel", "Топливо" },
        { "Cargo", "Груз" },
        { "Empty", "Пусто" },
        { "Navigation", "Навигация" },
        { "Route", "Маршрут" },
        { "Payout", "Оплата" },
        { "Follow Camera", "Следить камерой" },
        { "Exit Follow", "Выйти из камеры" },
        { "Open Fleet", "Открыть Автопарк" },

        { "Hire Driver", "Нанять водителя" },
        { "Hire Worker", "Приезд рабочих" },
        { "Hire New Worker", "Приезд рабочих" },
        { "Another worker is currently arriving by bus.", "Другой рабочий сейчас едет на автобусе." },
        { "Build a Motel first so new workers have somewhere to check in.", "Сначала построй Мотель, чтобы новым рабочим было где зарегистрироваться." },
        { "New hires arrive at the bus stop before checking in at the motel.", "Новые рабочие приезжают на автобусную остановку перед регистрацией в мотеле." },
        { "No workers yet.", "Рабочих пока нет." },
        { "No drivers yet.", "Водителей пока нет." },
        { "Salary", "Зарплата" },
        { "Money", "Деньги" },
        { "Status", "Статус" },
        { "Duty", "Роль" },
        { "thinks:", "думает:" },
        { "Hardworking", "Трудолюбивый" },
        { "Patient", "Терпеливый" },
        { "Persistent", "Упорный" },
        { "Diligent", "Старательный" },
        { "Reliable", "Надёжный" },
        { "Attentive", "Внимательный" },
        { "Careful", "Аккуратный" },
        { "Brave", "Храбрый" },
        { "Practical", "Практичный" },
        { "Humble", "Скромный" },
        { "Energetic", "Энергичный" },
        { "Polite", "Вежливый" },
        { "Seasoned", "Бывалый" },
        { "Thoughtful", "Задумчивый" },
        { "Honest", "Честный" },
        { "Steady", "Стойкий" },
        { "Observant", "Наблюдательный" },
        { "Decent", "Порядочный" },
        { "Quiet", "Тихий" },
        { "Hopeful", "Надеющийся" },
        { "lumberjack", "лесоруб" },
        { "sawyer", "пильщик" },
        { "cabinetmaker", "столяр" },
        { "warehouse loader", "складской грузчик" },
        { "motel attendant", "дежурный мотеля" },
        { "bartender", "бармен" },
        { "canteen worker", "работник столовой" },
        { "yard driver", "парковочный водитель" },
        { "fuel attendant", "заправщик" },
        { "station hand", "дежурный остановки" },
        { "intercity driver", "межгородский водитель" },
        { "driver", "водитель" },
        { "worker", "рабочий" },
        { "unemployed", "безработный" },
        { "(for now) unemployed", "(пока ещё) безработный" },
        { "Role", "Роль" },
        { "Truck", "Грузовик" },
        { "Shift", "Смена" },
        { "Balance", "Баланс" },
        { "Local", "Локальный" },
        { "Service", "Сервис" },
        { "Intercity Driver", "Межгородский водитель" },
        { "Lumberjack", "Лесоруб" },
        { "Sawmill Worker", "Рабочий лесопилки" },
        { "Carpenter", "Столяр" },
        { "Warehouse Loader", "Грузчик склада" },
        { "Employment Clerk", "Клерк биржи труда" },
        { "Cleaner", "Уборщик" },
        { "Production Worker", "Рабочий производства" },
        { "Service Worker", "Работник сервиса" },
        { "Bus Driver", "Водитель автобуса" },
        { "Truck Driver", "Водитель грузовика" },
        { "Job Seeker", "Ищет работу" },
        { "Unemployed", "Безработный" },
        { "On Trade Run", "В торговом рейсе" },
        { "On Bus Route", "На автобусном маршруте" },
        { "Arriving by Bus", "Едет на автобусе" },
        { "Sleeping", "Спит" },
        { "Walking", "Идёт" },
        { "On Shift", "На смене" },
        { "At Parking", "На парковке" },
        { "Bus Driver: no shift", "Водитель автобуса: смена не назначена" },
        { "Walking from Bus Stop", "Идёт от остановки" },
        { "Wandering", "Гуляет" },
        { "At Canteen", "В столовой" },
        { "At Gambling Hall", "В автоматах" },
        { "Shift at {0}:00", "Смена в {0}:00" },

        { "Build Menu", "Строительство" },
        { "Road", "Дорога" },
        { "Sawmill", "Лесопилка" },
        { "Motel", "Мотель" },
        { "Bar", "Бар" },
        { "Canteen", "Столовая" },
        { "Gambling Hall", "Игровые автоматы" },
        { "Labor Exchange", "Биржа труда" },
        { "Cleaning Depot", "Служба уборки" },
        { "Furniture Factory", "Мебельная фабрика" },
        { "Open Resources", "Открыть ресурсы" },

        { "Warehouse", "Склад" },
        { "Parking", "Парковка" },
        { "Gas Station", "Заправка" },
        { "Fuel Stop", "Заправка" },
        { "Forest", "Лесозаготовка" },
        { "Bus Stop", "Автобусная остановка" },
        { "Service Fee", "Стоимость услуги" },
        { "Workers inside", "Рабочих внутри" },
        { "Worker on shift", "Рабочий на смене" },
        { "Finished goods storage", "Склад готовой продукции" },
        { "Production paused at night", "Производство остановлено ночью" },

        { "Logs", "Брёвна" },
        { "Boards", "Доски" },
        { "Cotton", "Хлопок" },
        { "Textile", "Ткань" },
        { "Furniture", "Мебель" },
        { "Food", "Еда" },

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
        { "Assign a Truck Driver shift to unlock trade dispatch.", "Назначьте водителя грузовика на смену, чтобы открыть торговлю." },
        { "Ready to dispatch via edge highway", "Готово к рейсу через магистраль" },
        { "No available Truck Driver on shift", "Нет доступного водителя грузовика на смене" },
        { "Trade driver is still arriving", "Торговый водитель ещё прибывает" },
        { "Trade driver is busy", "Торговый водитель занят" },
        { "Trade needs an available parked truck", "Для торговли нужен свободный грузовик на парковке" },

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
        { "Cotton & Textile Belt", "Хлопково-текстильный пояс" },
        { "Barren Flats", "Пустые угодья" },
        { "River Port", "Речной порт" },
        { "North Ridge", "Северный кряж" },
        { "Forest Belt", "Лесной пояс" },
        { "Dry South", "Сухой юг" },
        { "Grain", "Зерно" },
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
        { "Forest -> Warehouse", "Лес -> Склад" },
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

    private static LocalizedStringTable russianUiTable;
    private static LocalizedStringTable RussianUiTable => russianUiTable ??= LoadRussianUiTable();

    private static LocalizedStringTable LoadRussianUiTable()
    {
        Dictionary<string, string> merged = new(RussianUi);
        TextAsset externalTable = Resources.Load<TextAsset>("Localization/ui.ru");
        if (externalTable != null)
        {
            Dictionary<string, string> externalValues = LocalizationJsonLoader.ParseFlatJsonObject(externalTable.text);
            foreach (KeyValuePair<string, string> pair in externalValues)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        return new LocalizedStringTable(merged);
    }

    private static bool IsRussianLanguage() => selectedLanguage == GameLanguage.Russian;

    private static string L(string value)
    {
        if (!IsRussianLanguage() || string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (RussianUiTable.TryTranslate(value, out string translated))
        {
            return translated;
        }

        return LocalizeCommonFragments(value);
    }

    private static string LocalizeCommonFragments(string value)
    {
        string translated = value;
        translated = RussianUiTable.TranslateCommonFragments(translated);

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

        return RussianUiTable.ToSourceKeyIfKnown(value);
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
        isStatesScreenDirty = true;
        isSocialGraphScreenDirty = true;
        UpdateTutorialGoalsLocalization();
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
